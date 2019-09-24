
-- requires `generic-layer.lua`

function Override.AsyncLoad()
	local bgLandscape, bgPortrait = LoadGenericBackgrounds();
	print(bgLandscape, bgPortrait);

	Layouts.Landscape.BackgroundTexture = bgLandscape;
	Layouts.WideLandscape.BackgroundTexture = bgLandscape;
	
	Layouts.Portrait.BackgroundTexture = bgPortrait;
	Layouts.TallPortrait.BackgroundTexture = bgPortrait;

	return true;
end

local function ControllerButtonPressed(button)
	if (button == ControllerInput.Start) then
		layer.Continue();
	end
end

local function ControllerButtonReleased(button)
end

local function ControllerAxisChanged(axis, delta)
end

function Override.Init()
	nsc.controller.pressed.Connect(ControllerButtonPressed);
	nsc.controller.released.Connect(ControllerButtonReleased);
	nsc.controller.axisChanged.Connect(ControllerAxisChanged);
end

-- Landscape Layout

function Layouts.Landscape.DrawScorePanel(self, x, y, w, h)
	g2d.SetColor(255, 255, 255, 225);
	g2d.FillRect(x, y, w, h);

	g2d.SetColor(255, 255, 255, 255);
	DrawNumber(layer.result.Score, 4, 4, true, x - 10, y - 10, w - 20, h - 20);
end

function Layouts.Landscape.Update(self, delta, total)
end

function Layouts.Landscape.Draw(self)
	DrawBackgroundFilled(self.BackgroundTexture);

	self:DrawScorePanel(10, 10, 400, 100);
end

-- Wide Landscape Layout

function Layouts.WideLandscape.Update(self, delta, total)
end

function Layouts.WideLandscape.Draw(self)
	DrawBackgroundFilled(self.BackgroundTexture);
end

-- Portrait Layout

function Layouts.Portrait.Update(self, delta, total)
end

function Layouts.Portrait.Draw(self)
	DrawBackgroundFilled(self.BackgroundTexture);
end


-- Tall Portrait Layout

function Layouts.TallPortrait.Update(self, delta, total)
end

function Layouts.TallPortrait.Draw(self)
	DrawBackgroundFilled(self.BackgroundTexture);
end

