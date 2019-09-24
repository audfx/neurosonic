
-- requires `generic-layer.lua`

function Override.Init()
	game.Begin();
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
	local w, h = 400, 120;
	local x, y = LayoutWidth - 10 - w, 10;
	
	g2d.SetColor(60, 60, 60, 225);
	g2d.FillRect(x, y, w, h);

	g2d.SetColor(255, 255, 255);
	g2d.SetFont(nil, 24);
	g2d.SetTextAlign(Anchor.TopLeft);
	g2d.Write("SCORE", x + 10, y + 10);

	DrawNumber(game.scoring.Score, 4, 4, true, x + 10, y + h / 3, w - 20, h / 3);
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

function Layouts.TallPortrait.Update(self, delta, total)
end

function Layouts.TallPortrait.Draw(self)
end
