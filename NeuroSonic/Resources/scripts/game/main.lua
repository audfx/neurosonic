
local Layout;
local LayoutWidth, LayoutHeight;
local LayoutScale;

local ViewportWidth, ViewportHeight;

local IntroAnimTimer = 0.0;

local Layouts = {
	Landscape = { },
	WideLandscape = { },
	Portrait = { },
	TallPortrait = { },
};

--

local ScoreDigitTextures = { };

--

local function CalculateLayout()
	ViewportWidth, ViewportHeight = g2d.GetViewportSize();
	Layout = ViewportWidth > ViewportHeight and "Landscape" or "Portrait";

	if (Layout == "Landscape") then
		if (ViewportWidth / ViewportHeight > 2) then
			Layout = "WideLandscape";
		end

		LayoutHeight = 720;

		LayoutScale = ViewportHeight / LayoutHeight;
		LayoutWidth = ViewportWidth / LayoutScale;
	else
		if (ViewportHeight / ViewportWidth > 2) then
			Layout = "TallPortrait";
		end

		LayoutWidth = 720;

		LayoutScale = ViewportWidth / LayoutWidth;
		LayoutHeight = ViewportHeight / LayoutScale;
	end
end

local function DoLayoutTransform()
	g2d.Scale(LayoutScale, LayoutScale);
end

function AsyncLoad()
	for i = 0, 9 do
		ScoreDigitTextures[i] = res.QueueTextureLoad("textures/combo/" .. i);
	end

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

	g2d.SetColor(255, 255, 255);
	g2d.FillRect(x, y, w * game.Progress, h);

	g2d.SetColor(180, 180, 180);
	g2d.FillRect(x + w * game.Progress, y, w * (1 - game.Progress), h);
	
	g2d.SetColor(0, 0, 0);
	g2d.SetFont(nil, 16);
	g2d.SetTextAlign(Anchor.MiddleCenter);
	g2d.Write(game.meta.SongTitle .. " / " .. game.meta.SongArtist, x + w / 2, y + h / 2);
end

function Layouts.Landscape.DrawScore(self)
	local h = 120;

	local ndigw = ScoreDigitTextures[0].Width * ((h / 3) / ScoreDigitTextures[0].Height);
	local ndigdw = ndigw * 4 + ndigw * 0.75 * 4;

	local w = ndigdw + 20;
	local x, y = LayoutWidth - 10 - w, 10;
	
	g2d.SetColor(60, 60, 60, 225);
	g2d.FillRect(x, y, w, h);

	g2d.SetColor(255, 255, 255);
	g2d.SetFont(nil, 24);
	g2d.SetTextAlign(Anchor.TopLeft);
	g2d.Write("SCORE", x + 10, y + 10);

	--g2d.Write(string.format("%08i", game.scoring.Score), x + w / 2, y + h / 2);
	local score, xoff = game.scoring.Score, 10;
	for i = 7, 0, -1 do
		local digit = math.floor(score / (10 ^ i)) % 10;
		local digs = i >= 4 and 1 or 0.75;

		local texture = ScoreDigitTextures[digit];
		local textureScale = digs * (h / 3) / texture.Height;

		g2d.Image(texture, x + xoff, y + h / 2 + h / 6 - texture.Height * textureScale,
				  texture.Width * textureScale, texture.Height * textureScale);
		xoff = xoff + texture.Width * textureScale;
	end
end

function Layouts.Landscape.DrawGauge(self)
	local w, h = 70, 400;
	local x, y = LayoutWidth * 3 / 4 - 35, LayoutHeight / 2 - 200;
	
	local gauge = game.scoring.Gauge;

	g2d.SetColor(60, 60, 60, 225);
	g2d.FillRect(x, y, w, h * (1 - gauge));
	
	if (gauge >= 0.7) then
		g2d.SetColor(150, 80, 200, 225);
	else
		g2d.SetColor(50, 80, 180, 225);
	end
	g2d.FillRect(x, y + h * (1 - gauge), w, h * gauge);
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
	self:DrawScore();
	self:DrawGauge();
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


-- Tall Portrait Layout

function Layouts.Portrait.Update(self, delta, total)
end

function Layouts.Portrait.Draw(self)
end
