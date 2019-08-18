
local Layout;
local LayoutWidth, LayoutHeight;
local LayoutScale;

local ViewportWidth, ViewportHeight;

local IntroAnimTimer = 0.0;

local Layouts = {
	Landscape = { },
	WideLandscape = { },
	Portrait = { },
};

local function CalculateLayout()
	ViewportWidth, ViewportHeight = g2d.GetViewportSize();
	Layout = ViewportWidth > ViewportHeight and "Landscape" or "Portrait";
	if (Layout == "Landscape") then
		if (ViewportWidth / ViewportHeight > 2) then
			Layout = "Wide-" .. Layout;
			LayoutWidth = 1680;
		else
			LayoutWidth = 1280;
		end
	else
		LayoutWidth = 720;
	end
	LayoutScale = ViewportWidth / LayoutWidth;
	LayoutHeight = ViewportHeight / LayoutScale;
end

local function DoLayoutTransform()
	g2d.Scale(LayoutScale, LayoutScale);
end

function AsyncLoad()
	return true;
end

function AsyncFinalize()
	return true;
end

function Init()
	CalculateLayout();
	game.Begin();
end

function Update(delta, total)
	do -- check if layout needs to be refreshed
		local cvw, cvh = g2d.GetViewportSize();
		if (ViewportWidth != cvw and ViewportHeight != cvh) then
			CalculateLayout();
		end
	end

	Layouts[Layout]:Update(delta, total);
end

function Draw()
	g2d.SaveTransform();
	DoLayoutTransform();

	Layouts[Layout]:Draw();

	g2d.RestoreTransform();
end

-- Landscape Layout

function Layouts.Landscape.Update(self, delta, total)
end

function Layouts.Landscape.DrawHeader(self)
	local w, h = LayoutWidth / 2, 20;
	local x, y = (LayoutWidth - w) / 2, 0;

	g2d.SaveTransform();
	g2d.Translate(x, y);

	g2d.SetColor(255, 255, 255);
	g2d.FillRect(0, 0, w, h);

	g2d.SetColor(0, 0, 0);
	g2d.SetFont(nil, 16);
	g2d.SetTextAlign(Anchor.MiddleCenter);
	g2d.Write(game.meta.SongTitle .. " / " .. game.meta.SongArtist, w / 2, h / 2);

	g2d.RestoreTransform();
end

function Layouts.Landscape.DrawChartInfo(self)
	local x, y = 10, 10;

	g2d.SaveTransform();
	g2d.Translate(x, y);

	g2d.SetColor(60, 60, 60, 225);
	g2d.FillRect(0, 0, 300, 120);

	local diffPlateHeight = 20;
	local jacketPadding = 5;
	local jacketSize = 120 - 20 - 3 * jacketPadding - diffPlateHeight;

	g2d.SetColor(255, 255, 255);
	g2d.FillRect(10, 10, jacketSize + 2 * jacketPadding, 120 - 20);

	g2d.SetColor(0, 0, 0); -- TEMP, DRAW JACKET IMAGE
	g2d.FillRect(10 + jacketPadding, 10 + jacketPadding, jacketSize, jacketSize);

	local diffColor = game.meta.DifficultyColor;
	g2d.SetColor(diffColor.x, diffColor.y, diffColor.z);
	g2d.FillRect(10 + jacketPadding, 10 + 2 * jacketPadding + jacketSize, jacketSize, diffPlateHeight);

	g2d.SetColor(0, 0, 0);
	g2d.SetFont(nil, 12);
	g2d.SetTextAlign(Anchor.MiddleLeft);
	g2d.Write(game.meta.DifficultyName .. " " .. game.meta.DifficultyLevel, 10 + 2 * jacketPadding, 10 + 2 * jacketPadding + jacketSize + diffPlateHeight / 2);

	g2d.RestoreTransform();
end

function Layouts.Landscape.Draw(self)
	self:DrawChartInfo();
	self:DrawHeader();
	
	-- Score
	g2d.SetColor(60, 60, 60, 225);
	g2d.FillRect(LayoutWidth - 10 - 300, 10, 300, 120);
	
	-- Gauge
	g2d.SetColor(60, 60, 60, 225);
	g2d.FillRect(LayoutWidth * 3 / 4 - 35, LayoutHeight / 2 - 200, 70, 400);
		
	-- Chart Info
	g2d.SetColor(60, 60, 60, 225);
	g2d.FillRect(10, LayoutHeight - 10 - 160, 300, 160);
end

-- Wide Landscape Layout

function Layouts.WideLandscape.Update(self, delta, total)
end

function Layouts.WideLandscape.Draw(self)
end

-- Portrait Layout

function Layouts.Portrait.Update(self, delta, total)
end

function Layouts.Portrait.Draw(self)
end
