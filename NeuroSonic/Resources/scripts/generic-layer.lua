
Override = { };

Layout = "Landscape";

LayoutWidth = 0;
LayoutHeight = 0;
LayoutScale = 1;

ViewportWidth = 0;
ViewportHeight = 0;

GlobalTimer = 0;

local ScoreDigitTextures = { };

-- Layouting

Layouts = {
	Landscape = { },
	WideLandscape = { },
	Portrait = { },
	TallPortrait = { },
};

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

function LoadGenericDigitTextures()
	for i = 0, 9 do
		ScoreDigitTextures[i] = res.QueueTextureLoad("textures/combo/" .. i);
	end
end

function LoadGenericBackgrounds()
	local bgLandscape = res.QueueTextureLoad("textures/bgHighContrast_LS");
	local bgPortrait = res.QueueTextureLoad("textures/bgHighContrast_PR");

	return bgLandscape, bgPortrait;
end

-- Default Background stuffs

function DrawBackgroundFilled(bgTex)
	local bgTexW, bgTexH = bgTex.Width, bgTex.Height;
	local scale = math.max(LayoutWidth / bgTexW, LayoutHeight / bgTexH);
	g2d.Image(bgTex, (LayoutWidth - bgTexW * scale) / 2, (LayoutHeight - bgTexH * scale) / 2, bgTexW * scale, bgTexH * scale);
end

function DrawNumber(score, nbig, nsmall, centered, x, y, w, h)
	local ndigw = ScoreDigitTextures[0].Width * (h / ScoreDigitTextures[0].Height);
	local ndigdw = ndigw * nbig + ndigw * 0.75 * nsmall;

	local dw, dh = ndigdw, h;
	if (ndigdw > w) then
		dh = dh * w / ndigdw;
		dw = w;
	end

	local xoff = centered and (w - dw) / 2 or 0;
	for i = nbig + nsmall - 1, 0, -1 do
		local digit = math.floor(score / (10 ^ i)) % 10;
		local digs = i >= nsmall and 1 or 0.75;

		local texture = ScoreDigitTextures[digit];
		local textureScale = digs * dh / texture.Height;
		
		g2d.Image(texture, x + xoff, y + h / 2 + dh / 2 - texture.Height * textureScale,
				  texture.Width * textureScale, texture.Height * textureScale);
		xoff = xoff + texture.Width * textureScale;
	end
end

-- Impls

function AsyncLoad()
	if (Override.AsyncLoad) then
		if (not Override.AsyncLoad()) then
			return false;
		end
	end
	
	LoadGenericDigitTextures();

	return true;
end

function AsyncFinalize()
	if (Override.AsyncFinalize) then
		if (not Override.AsyncFinalize()) then
			return false;
		end
	end

	return true;
end

function Init()
	CalculateLayout();

	if (Override.Init) then
		Override.Init();
	end
end

function Update(delta, total)
    GlobalTimer = GlobalTimer + delta;

	do -- check if layout needs to be refreshed
		local cvw, cvh = g2d.GetViewportSize();
		if (ViewportWidth != cvw or ViewportHeight != cvh) then
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

function Layouts.Landscape.Draw(self)
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

function Layouts.TallPortrait.Update(self, delta, total)
end

function Layouts.TallPortrait.Draw(self)
end

