
local livebg = { };

local scrollAmount = 0;
local pathAspect = 12;

--------------------------------------------------
-- Border Information ----------------------------
--------------------------------------------------

local NUMPEAKS = 42;
local PEAKSTEP = 1.0 / NUMPEAKS;

local borderPeaks = { };

math.randomseed(24601);
for i = 0, NUMPEAKS - 2 do
	local x, y = 0.25 + math.random() * 0.75, math.min(1, math.max(0, (i + (math.random() - 0.5) * 0.25) * PEAKSTEP));
	table.insert(borderPeaks, { x = x, y = y });
end

--------------------------------------------------
-- Vector Path Generation ------------------------
--------------------------------------------------

local function createEdgeGraphicsPath()
	local cmds = theori.graphics.createPathCommands();

	cmds:moveTo(0, 0);
	for _, v in next, borderPeaks do
		cmds:lineTo(v.x, v.y);
	end
	cmds:lineTo(borderPeaks[1].x, 1);
	cmds:lineTo(0, 1);
	cmds:close();

	return cmds;
end

local peakPath = createEdgeGraphicsPath();

--

local function renderEdge(w, h, r, g, b)
	theori.graphics.fillPathAt(peakPath, 0, 0, w, h);
end

--------------------------------------------------
-- Public API Functions --------------------------
--------------------------------------------------

function livebg.setScroll(amt)
	scrollAmount = amt % 1;
end

function livebg.render()
	local w, h = theori.graphics.getViewportSize();

	local scalex = 0.2;
	local pathWidth, pathHeight = w * scalex, (w * scalex) * pathAspect;
	
	theori.graphics.setFillToColor(0, 100, 200, 200);
	theori.graphics.saveTransform();
	theori.graphics.translate(0, (scrollAmount * pathHeight) * LayoutScale);
	renderEdge(pathWidth, pathHeight);
	theori.graphics.restoreTransform();
	
	theori.graphics.saveTransform();
	theori.graphics.translate(0, (-pathHeight + scrollAmount * pathHeight) * LayoutScale);
	renderEdge(pathWidth, pathHeight);
	theori.graphics.restoreTransform();
	
	theori.graphics.setFillToColor(200, 0, 100, 200);
	theori.graphics.saveTransform();
	theori.graphics.rotate(180);
	theori.graphics.translate(w, (pathHeight - scrollAmount * pathHeight) * LayoutScale);
	renderEdge(pathWidth, pathHeight);
	theori.graphics.restoreTransform();

	theori.graphics.saveTransform();
	theori.graphics.rotate(180);
	theori.graphics.translate(w, (2 * pathHeight - scrollAmount * pathHeight) * LayoutScale);
	renderEdge(pathWidth, pathHeight);
	theori.graphics.restoreTransform();
end

return livebg;
