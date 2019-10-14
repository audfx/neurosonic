
include "layerLayout";

local rawLoadedChartSets, preprocessedChartSets, chartSets;
local selectedSetIndex, selectedChartIndex, visualSelectedChartIndex;

local chartGridColumnCount = 3;

local startBtnTexture;
local cellFrame;

local targetGridOffsetY, animGridOffsetY = 0, 0;

function nsc.layer.construct()
end

function nsc.layer.doAsyncLoad()
    rawLoadedChartSets = nsc.charts.getChartSets();
    preprocessedChartSets = { };

    for k, v in next, rawLoadedChartSets do
        local newValue = { hidden = {
            allCharts = { },
            charts = { { }, { }, { }, { }, { } },
            categoryIndices = { 0, 0, 0, 0, 0 },
        } };

        --print(#v.charts, v.filePath);
        for _, chart in next, v.charts do
            if (chart ~= nil) then
                table.insert(newValue.hidden.allCharts, chart);
            end
        end

        -- maybe???
        if (#newValue.hidden.allCharts > 0) then
            for k, v in next, newValue.hidden.allCharts do
                table.insert(newValue.hidden.charts[v.difficultyIndex], v);
            end
        
            local minValue, maxValue = 1, 5;
            for i = 1, 5 do
                if (#newValue.hidden.charts[i] ~= 0) then
                    minValue = i;
                    break;
                end
            end
            for i = 5, 1, -1 do
                if (#newValue.hidden.charts[i] ~= 0) then
                    maxValue = i;
                    break;
                end
            end

            newValue.hidden.minChartCategory = minValue;
            newValue.hidden.maxChartCategory = maxValue;
    
            setmetatable(newValue, { __index = function(self, key)
                if (rawget(self, "hidden")[key]) then
                    return rawget(self, "hidden")[key]
                end
                return v[key];
            end; });
            table.insert(preprocessedChartSets, newValue);
        end
    end

    chartSets = table.shallowCopy(preprocessedChartSets);

    selectedSetIndex = 1;
    for i = 1, 5 do
        if (#chartSets[selectedSetIndex].charts[i] ~= 0) then
            selectedChartIndex = i;
            break;
        end
    end
    visualSelectedChartIndex = selectedChartIndex;

    startBtnTexture = nsc.graphics.queueTextureLoad("legend/start");

    cellFrame = nsc.graphics.queueTextureLoad("chartSelect/setFrame");

    Layouts.Landscape.Background = nsc.graphics.queueTextureLoad("genericBackground_LS");
    Layouts.Portrait.Background = nsc.graphics.queueTextureLoad("generigBackground_PR");

    return true;
end

function nsc.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function nsc.layer.init()
    nsc.openCurtain();

    nsc.input.controller.axisTicked:connect(function(axis, dir)
        if (axis == ControllerInput.Laser0Axis) then
            local set = chartSets[selectedSetIndex];
            
            selectedChartIndex = math.max(set.minChartCategory, math.min(set.maxChartCategory, visualSelectedChartIndex + dir));

            -- NOTE(local): there should NEVER be a case where there's no charts at all, as that SHOULD be filtered out in the preprocess
            while (#set.charts[selectedChartIndex] == 0) do
                if (selectedChartIndex == #set.charts) then
                    selectedChartIndex = 0;
                end
                selectedChartIndex = selectedChartIndex + dir;
            end

            visualSelectedChartIndex = selectedChartIndex;
        elseif (axis == ControllerInput.Laser1Axis) then
            local nextSetIndex = selectedSetIndex + dir;
            if (nextSetIndex <= 0) then
                nextSetIndex = #chartSets;
            elseif (nextSetIndex > #chartSets) then
                nextSetIndex = 1;
            end

            selectedSetIndex = nextSetIndex;
            
            local set = chartSets[selectedSetIndex];
            -- reset the visual chart selection index
            visualSelectedChartIndex = math.max(set.minChartCategory, math.min(set.maxChartCategory, selectedChartIndex));

            -- NOTE(local): there should NEVER be a case where there's no charts at all, as that SHOULD be filtered out in the preprocess
            while (#set.charts[visualSelectedChartIndex] == 0) do
                if (visualSelectedChartIndex == #set.charts) then
                    visualSelectedChartIndex = 0; -- 0 because we add 1 afterwards
                end
                visualSelectedChartIndex = visualSelectedChartIndex + 1;
            end
        end
    end);

    nsc.input.controller.pressed:connect(function(button)
        if (button == ControllerInput.Back) then
            nsc.closeCurtain(0.2, nsc.layer.pop);
        elseif (button == ControllerInput.Start) then
            local set = chartSets[selectedSetIndex];
            local chart = set.charts[visualSelectedChartIndex][1];
            nsc.closeCurtain(0.2, function() nsc.game.pushGameplay(chart); end);
        end
    end);
end

function nsc.layer.update(delta, total)
    if (math.abs(animGridOffsetY - targetGridOffsetY) < 1) then
        animGridOffsetY = targetGridOffsetY;
    else
        local speed = 20;
        animGridOffsetY = animGridOffsetY + (targetGridOffsetY - animGridOffsetY) * speed * delta;
    end

    Layout.Update(delta, total);
end

function nsc.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();
end

-- Shared State Management

-- Shared Rendering

local function renderChartGridPanel(x, y, w, h)
    local margin, padding = 15, 50;
    local ncols = chartGridColumnCount;

    local colWidth = (w - 2 * margin - (ncols - 1) * padding) / ncols;
    local cellSize = colWidth;

    local stepAmount = 1;
    local stepY = (padding + cellSize) * stepAmount / ncols;

    local offsetY = -LayoutHeight / 2 + margin + cellSize / 2 + math.floor((selectedSetIndex - 1) / ncols) * (padding + cellSize) + ((selectedSetIndex - 1) % ncols) * stepY;
    --offsetY = math.max(offsetY, 0);

    targetGridOffsetY = offsetY;
    if (math.abs(targetGridOffsetY - animGridOffsetY) > LayoutHeight) then
        animGridOffsetY = targetGridOffsetY;
    end
    offsetY = animGridOffsetY;

    local minIndex = math.max(1, selectedSetIndex - ncols * 3);
    local maxIndex = math.min(#chartSets, selectedSetIndex + ncols * 3);

    for i = minIndex, maxIndex do
        local chartSet = chartSets[i];
        local isSelected = i == selectedSetIndex;

        local icol = (i - 1) % ncols;
        local irow = math.floor((i - 1) / ncols);

        local cellX = x + margin + (cellSize + padding) * icol;
        local cellY = y + margin + (cellSize + padding) * irow - offsetY + stepY * icol;

        nsc.graphics.draw(chartSet.allCharts[1].getJacketTexture(), cellX + cellSize * 0.02, cellY + cellSize * 0.02, cellSize * 0.7, cellSize * 0.7);
        if (isSelected) then
            nsc.graphics.setImageColor(255, 240, 250, 255);
        else
            nsc.graphics.setImageColor(100, 100, 100, 255);
        end
        nsc.graphics.draw(cellFrame, cellX, cellY, cellSize, cellSize);
        nsc.graphics.setImageColor(255, 255, 255, 255);
    end
end

local function renderChartInfoPanelLandscape(x, y, w, h)
    local chartSet = chartSets[selectedSetIndex];

    local chart = chartSet.charts[visualSelectedChartIndex][1];
    nsc.graphics.draw(chart.getJacketTexture(), x + w * 0.25, y + w * 0.1, w * 0.5, w * 0.5);

    -- 5 bubbles
    local bubbleMargin, bubblePadding = 10, 4;
    local bubbleSize = (w * 0.5 - 2 * bubbleMargin - 4 * bubblePadding) * 0.2;

    for i = 1, 5 do
        local category = chartSet.charts[i];

        if (#category == 0) then
            nsc.graphics.setColor(60, 60, 60, 100);
            nsc.graphics.fillRect(x + w * 0.25 + bubbleMargin + (i - 1) * (bubblePadding + bubbleSize), y + w * 0.6 + bubbleMargin, bubbleSize, bubbleSize);
        else
            local r, g, b = category[1].difficultyColor;
            nsc.graphics.setColor(r, g, b, 255);
            nsc.graphics.fillRect(x + w * 0.25 + bubbleMargin + (i - 1) * (bubblePadding + bubbleSize), y + w * 0.6 + bubbleMargin, bubbleSize, bubbleSize);

            if (i == visualSelectedChartIndex) then
                nsc.graphics.setImageColor(255, 255, 255, 255);
            else
                nsc.graphics.setImageColor(128, 128, 128, 255);
            end
            nsc.graphics.draw(chartSet.charts[i][1].getJacketTexture(), x + 2 + w * 0.25 + bubbleMargin + (i - 1) * (bubblePadding + bubbleSize), y + 2 + w * 0.6 + bubbleMargin, bubbleSize - 4, bubbleSize - 4);
        end
    end
    nsc.graphics.setImageColor(255, 255, 255, 255);

    nsc.graphics.setColor(255, 255, 255, 255);
    nsc.graphics.setTextAlign(Anchor.TopCenter);
    nsc.graphics.drawString(chart.songTitle, x + w * 0.5, y + w * 0.6 + 2 * bubbleMargin + bubbleSize);

    nsc.graphics.setColor(0, 0, 0, 200);
    nsc.graphics.fillRect(x + w * 0.25, y + w * 0.55, w * 0.5, w * 0.04);
end

-- Landscape

function Layouts.Landscape.Render(self)
    Layout.DrawBackgroundFilled(self.Background);

    renderChartGridPanel(LayoutWidth - LayoutHeight, 0, LayoutHeight, LayoutHeight);
    renderChartInfoPanelLandscape(0, 0, LayoutWidth - LayoutHeight, LayoutHeight);
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end

