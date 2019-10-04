
include "layerLayout";

local rawLoadedChartSets;

local chartSets;
local selectedSetIndex, selectedChartIndex = 1, 1;

local chartGridColumnCount = 3;

local startBtnTexture;

local cellFrame;

local targetGridOffsetY, animGridOffsetY = 0, 0;

function nsc.layer.construct()
end

function nsc.layer.doAsyncLoad()
    rawLoadedChartSets = nsc.charts.getChartSets();
    preprocessedChartSets = { };

    chartSets = table.shallowCopy(rawLoadedChartSets);

    startBtnTexture = nsc.graphics.loadTextureAsync("legend/start");

    cellFrame = nsc.graphics.loadTextureAsync("chartSelect/setFrame");

    Layouts.Landscape.Background = nsc.graphics.loadTextureAsync("genericBackground_LS");
    Layouts.Portrait.Background = nsc.graphics.loadTextureAsync("generigBackground_PR");

    return true;
end

function nsc.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function nsc.layer.init()
    nsc.openCurtain();

    nsc.input.controller.axisTicked:connect(function(axis, dir)
        if (axis == ControllerInput.Laser0Axis) then
        elseif (axis == ControllerInput.Laser1Axis) then
            local nextSetIndex = selectedSetIndex + dir;
            if (nextSetIndex <= 0) then
                nextSetIndex = #chartSets;
            elseif (nextSetIndex > #chartSets) then
                nextSetIndex = 1;
            end
            selectedSetIndex = nextSetIndex;
        end
    end);

    nsc.input.controller.pressed:connect(function(button)
        if (button == ControllerInput.Back) then
            nsc.closeCurtain(0.2, nsc.layer.pop);
        elseif (button == ControllerInput.Start) then
            local set = chartSets[selectedSetIndex];

            local chart = set.charts[#set.charts];
            if (chart.difficultyLevel > 18 and #set.charts > 1) then chart = set.charts[#set.charts - 1]; end

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

    local stepAmount = 0.25;
    local stepY = (padding + cellSize) * stepAmount / ncols;

    local offsetY = -LayoutHeight / 2 + margin + cellSize / 2 + math.floor((selectedSetIndex - 1) / ncols) * (padding + cellSize) + ((selectedSetIndex - 1) % ncols) * stepY;
    offsetY = math.max(offsetY, 0);

    targetGridOffsetY = offsetY;
    offsetY = animGridOffsetY;

    for i = 1, #chartSets do
        local chartSet = chartSets[i];
        local isSelected = i == selectedSetIndex;

        local icol = (i - 1) % ncols;
        local irow = math.floor((i - 1) / ncols);

        local cellX = x + margin + (cellSize + padding) * icol;
        local cellY = y + margin + (cellSize + padding) * irow - offsetY + stepY * icol;

        nsc.graphics.draw(chartSet.charts[1].getJacketTexture(), cellX + cellSize * 0.02, cellY + cellSize * 0.02, cellSize * 0.7, cellSize * 0.7);
        if (isSelected) then
            nsc.graphics.setImageColor(240, 240, 240, 255);
        else
            nsc.graphics.setImageColor(160, 160, 160, 255);
        end
        nsc.graphics.draw(cellFrame, cellX, cellY, cellSize, cellSize);
        nsc.graphics.setImageColor(255, 255, 255, 255);
    end
end

-- Landscape

function Layouts.Landscape.Render(self)
    Layout.DrawBackgroundFilled(self.Background);

    renderChartGridPanel(LayoutWidth - LayoutHeight, 0, LayoutHeight, LayoutHeight);
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end

