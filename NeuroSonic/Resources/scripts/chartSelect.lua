
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
local startBtnTexture;
local cellFrame, cellTitlePlate;
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
		return cachedFilteredCharts[cacheKey];
	end

	local charts = theori.charts.getChartSetsFiltered(chartListFilters[filter], chartListGroupings[grouping], chartListSortings[sorting]);

	cachedFilteredCharts[cacheKey] = charts;
	return charts;
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

local function getGridCameraPosition()
	local result = 0;
	
	local colIndex = (setIndex - 1) % gridColumnCount;
	local rowIndex = math.floor((setIndex - 1) / gridColumnCount);

	result = result + rowIndex + 0.5 + gridGroupHeaderSize +
		(colIndex * gridRowStepping / (gridColumnCount - 1));
	for i = 1, groupIndex - 1 do
		result = result + getSizeOfGroup(i);
	end
	return result;
end

local function lerpAnimTo(from, to, delta, speed)
	local absDif = math.abs(from - to);
    if (absDif > 10 or absDif < 0.01) then
        return to;
    else
        speed = speed or 10;
        return from + (to - from) * speed * delta;
    end
end
--------------------------------------------------


local function nearestSlot(set, slotIndex)
	local slot = slotIndex;
	while (#set[slot] == 0) do
		if (slot == 1) then break; end
		slot = slot - 1;
	end
	while (#set[slot] == 0) do
		if (slot == 5) then break; end -- OH NO
		slot = slot + 1;
	end
	return set[slot];
end

local function nearestSlotChild(slot, slotChildIndex)
	return slot[math.min(#slot, slotChildIndex)];
end


--------------------------------------------------
-- Theori Layer Functions ------------------------
--------------------------------------------------
function theori.layer.construct()
end

function theori.layer.doAsyncLoad()
	charts = getFilteredChartsForConfiguration("all", "level", "title");

	-- TODO(local): save the selected chart/slot and gather the indices afterward
	-- TODO(local): make sure there's slots in the selected set

    startBtnTexture = theori.graphics.queueTextureLoad("legend/start");

    cellFrame = theori.graphics.queueTextureLoad("chartSelect/setFrame");
    cellTitlePlate = theori.graphics.queueTextureLoad("chartSelect/setTitlePlate");

    Layouts.Landscape.Background = theori.graphics.queueTextureLoad(bgName .. "_LS");
    Layouts.Portrait.Background = theori.graphics.queueTextureLoad(bgName .. "_PR");

    return true;
end

function theori.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function theori.layer.init()
    theori.graphics.openCurtain();
	
	gridCameraPosTarget = getGridCameraPosition();

    theori.input.controller.axisTicked:connect(function(controller, axis, dir)
        if (axis == 0) then
			-- categories
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

local function renderSetCell(set, x, y, w, h)
	local slot = nearestSlot(set, slotIndex);
	local chart = nearestSlotChild(slot, slotChildIndex);

	theori.graphics.setColor(255, 255, 255, 255);
	theori.graphics.draw(chart.getJacketTexture(), x, y, w, h);
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

					if (gi == groupIndex and si == setIndex) then
						theori.graphics.setColor(255, 60, 20, 255);

						theori.graphics.saveTransform();
						--theori.graphics.resetTransform();
						theori.graphics.rotate(timer * 60);
						theori.graphics.rotate(math.sin(timer * 3) * 0.1 + 1);
						theori.graphics.translate((cx + cs / 2) * LayoutScale, (cy + cs / 2) * LayoutScale);
						theori.graphics.fillRect(-cs / 2, -cs / 2, cs, margin * 0.5);
						theori.graphics.fillRect(-cs / 2, -cs / 2 + cs - margin * 0.5, cs, margin * 0.5);
						theori.graphics.fillRect(-cs / 2, -cs / 2 + margin * 0.5, margin * 0.5, cs - margin);
						theori.graphics.fillRect(-cs / 2 + cs - margin * 0.5, -cs / 2 + margin * 0.5, margin * 0.5, cs - margin);
						theori.graphics.restoreTransform();
					end
				end
			end
		end

		yPosRel = yPosRel + groupHeight;
	end
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
