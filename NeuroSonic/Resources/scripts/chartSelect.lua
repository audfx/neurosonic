
include "layerLayout";

local bgName = "bgHighContrast";

local timer;

--------------------------------------------------
-- Chart Data ------------------------------------
--------------------------------------------------
-- the collective of charts the player can select from
-- Whenever the player selects new filters etc., this is what's updated;
--  a full refresh of the render data (and a resync of the camera position)
--  is needed when this is updated.
local charts;
local chartsCache;

-- which grouping the cursor is currently within
local groupIndex = 1;
-- which set the cursor is on within the group
local setIndex = 1;
-- which category slot (each of the 5 difficulties) is currently selected
local slotIndex = 1;
-- which chart in a slot, if there are multiple, is selected
-- This value is shared for all slots, so will be clamped (or reset)
--  between slot index changes.
local slotChildIndex = 1;


--------------------------------------------------
-- Display Data ----------------------------------
--------------------------------------------------
-- the number of columns to display in the grid
local gridColumnCount = 3;
-- the amount of stepping to apply to each grid row
-- Stepping, here, refers to the percentage of the height to step over
--  the entire row, so 1 row will have each cell vertically offset an
--  increasing amount up to the given percentage.
-- This is not this much per column, but a total for every column.
local gridRowStepping = 0.5;

-- the percentage of the size of a cell that is taken up by the group header
local gridGroupHeaderSize = 0.25;

-- where the camera is along the y axis
local gridCameraPos = 0;
local gridCameraPosTarget = 0;
--------------------------------------------------


--------------------------------------------------
-- Textures --------------------------------------
--------------------------------------------------
local textures =
{
	noise = { },
	numbers = { },
	legend = { },
	chartFrame = { },
};

local currentNoiseTexture;
--------------------------------------------------


--------------------------------------------------
-- Database Filters ------------------------------
--------------------------------------------------
local chartListFilters =
{
	all = function(chart) return true; end,
};

local chartListGroupings =
{
	level = function(chart) return chart.difficultyLevel; end,
};

local chartListSortings =
{
	title = function(chart) return chart.songTitle; end,
};

local cachedFilteredCharts = { };
--------------------------------------------------


--------------------------------------------------
-- Chart Filter Utility --------------------------
--------------------------------------------------
local function getFilteredChartsForConfiguration(filter, grouping, sorting)
	local cacheKey = filter .. '|' .. grouping .. '|' .. sorting;
	if (cachedFilteredCharts[cacheKey]) then
		return cachedFilteredCharts[cacheKey].charts;
	end

	local charts = theori.charts.getChartSetsFiltered(chartListFilters[filter], chartListGroupings[grouping], chartListSortings[sorting]);
	local chartsCache = { };

	cachedFilteredCharts[cacheKey] =
	{
		charts = charts,
		chartsCache = chartsCache,
	};
	return charts, chartsCache;
end
--------------------------------------------------


--------------------------------------------------
-- Chart Grid Functions --------------------------
--------------------------------------------------
local function getSizeOfGroup(chartGroupIndex)
	local chartGroup = charts[chartGroupIndex];

	local rowCount = math.floor((#chartGroup - 1) / gridColumnCount) + 1;
	local steppedColsAtEnd = (#chartGroup - 1) % gridColumnCount;

	return rowCount + gridGroupHeaderSize +
		(steppedColsAtEnd * gridRowStepping / (gridColumnCount - 1));
end

local function getGridCellPosition(gi, si)
	local result = 0;
	
	local colIndex = (si - 1) % gridColumnCount;
	local rowIndex = math.floor((si - 1) / gridColumnCount);

	result = result + rowIndex + 0.5 + gridGroupHeaderSize +
		(colIndex * gridRowStepping / (gridColumnCount - 1));
	for i = 1, gi - 1 do
		result = result + getSizeOfGroup(i);
	end
	return 0.5 + colIndex, result;
end

local function getGridCameraPosition()
	local x, y = getGridCellPosition(groupIndex, setIndex);
	return y;
end

local function lerpAnimTo(from, to, delta, speed)
	local absDif = math.abs(from - to);
    if (absDif > 10 or absDif < 0.01) then
        return to;
    else
        speed = speed or 20;
        return from + (to - from) * speed * delta;
    end
end
--------------------------------------------------


local function nearestSlotIndex(set, slotIndex)
	local slot = slotIndex;
	while (#set[slot] == 0) do
		if (slot == 1) then break; end
		slot = slot - 1;
	end
	while (#set[slot] == 0) do
		if (slot == 5) then break; end -- OH NO
		slot = slot + 1;
	end
	return slot;
end

local function nearestSlot(set, slotIndex)
	return set[nearestSlotIndex(set, slotIndex)];
end

local function nearestSlotChildIndex(slot, slotChildIndex)
	return math.min(#slot, slotChildIndex);
end

local function nearestSlotChild(slot, slotChildIndex)
	return slot[nearestSlotChildIndex(slot, slotChildIndex)];
end

local function getAllRelatedSets(primarySetId)
	local cached = chartsCache[primarySetId];
	if (not cached) then
		chartsCache[primarySetId] = { };
		cached = chartsCache[primarySetId];
	end

	if (cached.relatedSets) then
		return cached.relatedSets;
	end

	local related = { primary };
	for gi, group in next, charts do
		for si, set in next, group do
			if (set.set.ID == primarySetId) then
				table.insert(related, { groupIndex = gi, setIndex = si, set = set });
			end
		end
	end

	cached.relatedSets = related;
	return related;
end


--------------------------------------------------
-- Theori Layer Functions ------------------------
--------------------------------------------------
function theori.layer.construct()
end

function theori.layer.doAsyncLoad()
	charts, chartsCache = getFilteredChartsForConfiguration("all", "level", "title");

	-- TODO(local): save the selected chart/slot and gather the indices afterward
	-- TODO(local): make sure there's slots in the selected set
	
	for i = 0, 9 do
		textures.numbers[i] = theori.graphics.queueTextureLoad("spritenums/slant/" .. i);
	end
	for i = 0, 9 do
		textures.noise[i] = theori.graphics.queueTextureLoad("noise/" .. i);
	end

    textures.legend.start = theori.graphics.queueTextureLoad("legend/start");

    textures.cursor = theori.graphics.queueTextureLoad("chartSelect/cursor");
    textures.cursorOuter = theori.graphics.queueTextureLoad("chartSelect/cursorOuter");
    textures.levelBadge = theori.graphics.queueTextureLoad("chartSelect/levelBadge");
    textures.levelBadgeBorder = theori.graphics.queueTextureLoad("chartSelect/levelBadgeBorder");

	local frameTextures = { "background", "border", "cornerPanel", "cornerPanelBorder", "fill", "infoBorders", "jacketBottomRight", "jacketTopLeft", "storageDevice", "trackDataLabel" };
	for _, texName in next, frameTextures do
		textures.chartFrame[texName] = theori.graphics.queueTextureLoad("chartSelect/chartFrame/" .. texName);
	end

    textures.noJacket = theori.graphics.queueTextureLoad("chartSelect/noJacket");
    textures.noJacketOverlay = theori.graphics.queueTextureLoad("chartSelect/noJacketOverlay");

    Layouts.Landscape.Background = theori.graphics.queueTextureLoad(bgName .. "_LS");
    Layouts.Portrait.Background = theori.graphics.queueTextureLoad(bgName .. "_PR");

    return true;
end

function theori.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function theori.layer.resumed()
	theori.graphics.openCurtain();
end

function theori.layer.init()
    theori.graphics.openCurtain();
	
	gridCameraPosTarget = getGridCameraPosition();

    theori.input.controller.axisTicked:connect(function(controller, axis, dir)
        if (axis == 0) then
			local set = charts[groupIndex][setIndex];
			local cSlotIndex = nearestSlotIndex(set, slotIndex);
			local cSlotChildIndex = nearestSlotChildIndex(set[cSlotIndex], slotChildIndex);

			local completeSet = set.set;
			local relatedSets = getAllRelatedSets(completeSet.ID);

			local newSlotIndex = cSlotIndex;
			local matchingSets = { };
			while (true) do
				newSlotIndex = newSlotIndex + dir;
				if (newSlotIndex < 1 or newSlotIndex > 5) then break; end

				for rsi, rSet in next, relatedSets do
					if (#rSet.set[newSlotIndex] > 0) then
						table.insert(matchingSets, rSet);
					end
				end

				if (#matchingSets > 0) then break; end;
			end

			if (newSlotIndex < 1 or newSlotIndex > 5 or #matchingSets == 0) then return; end

			if (#matchingSets == 1) then
				print("got one");
				local rSet = matchingSets[1];
				groupIndex = rSet.groupIndex;
				setIndex = rSet.setIndex;
				slotIndex = newSlotIndex;
				slotChildIndex = 1;
			elseif (#matchingSets > 1) then
				print("skipping this one chief:", #matchingSets .. "/" .. #relatedSets);
				-- check for the best option
			else
				print("skipping this one chief:", #matchingSets .. "/" .. #relatedSets);
				return;
			end
				
			gridCameraPosTarget = getGridCameraPosition();
        elseif (axis == 1) then
			setIndex = setIndex + dir;
			if (setIndex < 1) then
				groupIndex = groupIndex - 1;
				if (groupIndex < 1) then
					groupIndex = #charts;
				end
				setIndex = #charts[groupIndex];
			elseif (setIndex > #charts[groupIndex]) then
				groupIndex = groupIndex + 1;
				if (groupIndex > #charts) then
					groupIndex = 1;
				end
				setIndex = 1;
			end
			gridCameraPosTarget = getGridCameraPosition();
        end
    end);

    theori.input.controller.pressed:connect(function(controller, button)
        if (button == "back") then
            theori.graphics.closeCurtain(0.2, theori.layer.pop);
        elseif (button == "start") then
			local chart = nearestSlotChild(nearestSlot(charts[groupIndex][setIndex], slotIndex), slotChildIndex);
            theori.graphics.closeCurtain(0.2, function() nsc.pushGameplay(chart); end);
        end
    end);
end

function theori.layer.update(delta, total)
	timer = total;

	currentNoiseTexture = textures.noise[math.floor((timer * 24) % 10)];

    Layout.Update(delta, total);

	-- layout agnostic functions
	gridCameraPos = lerpAnimTo(gridCameraPos, gridCameraPosTarget, delta);
end

function theori.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();
end
--------------------------------------------------


-- Shared State Management

-- Shared Rendering

local function renderSpriteNumCenteredNumDigits(num, dig, x, y, h)
	local digInfos = { };
	local w = 0;

	for i = dig, 1, -1 do
		local tento, tentom1 = math.pow(10, i), math.pow(10, i - 1);
		local tex = textures.numbers[math.floor((num % tento) / tentom1)];
		local texWidth = h * tex.aspectRatio;

		table.insert(digInfos, { texture = tex, width = texWidth });
		w = w + texWidth;
	end

	local xPos, yPos = x - w / 2, y - h / 2;
	for _, info in next, digInfos do
		theori.graphics.draw(info.texture, xPos, yPos, info.width, h);
		xPos = xPos + info.width;
	end
end

local function renderSetCell(set, x, y, w, h)
	local isSelected = charts[groupIndex][setIndex] == set;

	local slot = nearestSlot(set, slotIndex);
	local chart = nearestSlotChild(slot, slotChildIndex);
	
	local r, g, b = chart.difficultyColor;
	local diffLvl = chart.difficultyLevel;
	
	theori.graphics.setImageColor(50, 50, 50, 255);
	theori.graphics.draw(textures.chartFrame.background, x, y, w, h);
	theori.graphics.setImageColor(r, g, b, 255);
	theori.graphics.draw(textures.chartFrame.border, x, y, w, h);
	theori.graphics.setImageColor(50, 50, 50, 255);
	theori.graphics.draw(textures.chartFrame.cornerPanel, x, y, w, h);
	theori.graphics.setImageColor(255, 255, 255, 255);
	theori.graphics.draw(textures.chartFrame.trackDataLabel, x, y, w, h);
	
	theori.graphics.setImageColor(80, 80, 80, 210);
	theori.graphics.draw(textures.chartFrame.fill, x, y, w, h);
	
	theori.graphics.setImageColor(r, g, b, 255);
	theori.graphics.draw(textures.chartFrame.infoBorders, x, y, w, h);
	local jx, jy, jw, jh = x + 0.1 * w, y + 0.155 * h, w * 0.6, h * 0.6;
	if (not chart.hasJacketTexture) then
		theori.graphics.setImageColor(255, 255, 255, 255);
		theori.graphics.draw(textures.noJacket, jx, jy, jw, jh);
		theori.graphics.setImageColor(255, 255, 255, 70);
		theori.graphics.draw(currentNoiseTexture, jx, jy, jw, jh);
		if (isSelected) then
			theori.graphics.setImageColor(255, 255, 255, 175 + 60 * math.abs(math.sin(timer * 3)));
		else
			theori.graphics.setImageColor(255, 255, 255, 205);
		end
		theori.graphics.draw(textures.noJacketOverlay, jx, jy, jw, jh);
	else
		theori.graphics.setImageColor(255, 255, 255, 255);
		theori.graphics.draw(chart.getJacketTexture(), jx, jy, jw, jh);
	end
	
	theori.graphics.setImageColor(r, g, b, 255);
	theori.graphics.draw(textures.chartFrame.jacketTopLeft, x, y, w, h);
	theori.graphics.draw(textures.chartFrame.jacketBottomRight, x, y, w, h);
	
	theori.graphics.setImageColor(r, g, b, 255);
	theori.graphics.draw(textures.levelBadgeBorder, x + w * 0.73, y + h * 0.55, w * 0.22, h * 0.22);
	theori.graphics.setImageColor(50, 50, 50, 170);
	theori.graphics.draw(textures.levelBadge, x + w * 0.73, y + h * 0.55, w * 0.22, h * 0.22);

	theori.graphics.setImageColor(0, 0, 0, 255);
	renderSpriteNumCenteredNumDigits(diffLvl, 2, x + w * 0.835, y + h * 0.665, h * 0.08);
	theori.graphics.setImageColor(255, 255, 255, 255);
	renderSpriteNumCenteredNumDigits(diffLvl, 2, x + w * 0.84, y + h * 0.66, h * 0.08);
	
	theori.graphics.saveTransform();
	if (isSelected) then
		theori.graphics.rotate(timer * 180);
	end
	theori.graphics.translate(LayoutScale * (x + w * (79 / 1028)), LayoutScale * (y + h * (75 / 1028)));
	theori.graphics.setImageColor(255, 255, 255, 255);
	local sdw, sdh = w * (100 / 1028), h * (100 / 1028);
	theori.graphics.draw(textures.chartFrame.storageDevice, -sdw / 2, -sdh / 2, sdw, sdh);
	theori.graphics.restoreTransform();
end

local function renderChartGridPanel(x, y, w, h)
	local cellSize = w / gridColumnCount;
	local hUnits = h / cellSize;
	local totalGroupsHeight = 0;
	for i = 1, #charts do
		totalGroupsHeight = totalGroupsHeight + getSizeOfGroup(i);
	end

	local camPosUnits = math.max(hUnits / 2, math.min(totalGroupsHeight - hUnits / 2, gridCameraPos));

	local minCamera = camPosUnits - hUnits / 2;
	local maxCamera = camPosUnits + hUnits / 2;

	local yOffset = -minCamera * cellSize;
	local margin = cellSize * 0.05;

	local yPosRel = 0;
	for gi = 1, #charts do
		if (yPosRel > maxCamera) then break; end
		local group = charts[gi];

		local groupHeight = getSizeOfGroup(gi);
		-- if the bottom of this group is in view, we start checking cell rendering
		if (yPosRel + groupHeight > minCamera) then
			if (yPosRel + gridGroupHeaderSize > minCamera) then
				theori.graphics.setColor(50, 160, 255, 200);
				theori.graphics.fillRect(x, yOffset + yPosRel * cellSize, w, gridGroupHeaderSize * cellSize);
			end

			for si = 1, #group do
				local set = group[si];

				local selected = gi == groupIndex and si == setIndex;

				local rowIndex = math.floor((si - 1) / gridColumnCount);
				local colIndex = (si - 1) % gridColumnCount;

				local yPosRelSet = yPosRel + gridGroupHeaderSize + rowIndex +
					(colIndex * gridRowStepping / (gridColumnCount - 1));

				if (yPosRelSet < maxCamera and yPosRelSet + 1 > minCamera) then;
					local cx, cy = margin + x + colIndex * cellSize, margin + yOffset + yPosRelSet * cellSize;
					local cs = cellSize - 2 * margin;

					renderSetCell(set, cx, cy, cs, cs);
				end
			end
		end

		yPosRel = yPosRel + groupHeight;
	end

	local cursorCenterX, cursorCenterY = getGridCellPosition(groupIndex, setIndex);
	local s0 = cellSize * (1 + 0.05 * (math.abs(math.sin(timer * 6))));
	local s1 = cellSize * (1 + 0.12 * (math.abs(math.sin(timer * 6))));

	theori.graphics.setImageColor(0, 255, 255, 170 + 60 * (math.abs(math.sin(timer * 3))));
	--theori.graphics.setImageColor(255, 60, 20, 200);
	theori.graphics.draw(textures.cursor, x + cellSize * cursorCenterX - s0 / 2, y + yOffset + cellSize * cursorCenterY - s0 / 2, s0, s0);
	theori.graphics.draw(textures.cursorOuter, x + cellSize * cursorCenterX - s1 / 2, y + yOffset + cellSize * cursorCenterY - s1 / 2, s1, s1);
end

-- Landscape

function Layouts.Landscape.Update(self, delta, total)
end

function Layouts.Landscape.Render(self)
    Layout.DrawBackgroundFilled(self.Background);

    renderChartGridPanel(LayoutWidth - LayoutHeight, 0, LayoutHeight, LayoutHeight);
    --renderChartInfoPanelLandscape(0, 0, LayoutWidth - LayoutHeight, LayoutHeight);
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end
