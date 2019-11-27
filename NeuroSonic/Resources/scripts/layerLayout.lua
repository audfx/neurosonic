
LayoutKind = "Landscape";

LayoutWidth = 0;
LayoutHeight = 0;
LayoutScale = 1;

ViewportWidth = 0;
ViewportHeight = 0;

-- Layouting

Layout = { };

Layouts = {
	Landscape = { },
	WideLandscape = { },
	Portrait = { },
	TallPortrait = { },
};

function Layout.CalculateLayout()
	ViewportWidth, ViewportHeight = theori.graphics.getViewportSize();
	LayoutKind = ViewportWidth > ViewportHeight and "Landscape" or "Portrait";

	if (LayoutKind == "Landscape") then
		if (ViewportWidth / ViewportHeight > 2) then
			LayoutKind = "WideLandscape";
		end

		LayoutHeight = 720;

		LayoutScale = ViewportHeight / LayoutHeight;
		LayoutWidth = ViewportWidth / LayoutScale;
	else
		if (ViewportHeight / ViewportWidth > 2) then
			LayoutKind = "TallPortrait";
		end

		LayoutWidth = 720;

		LayoutScale = ViewportWidth / LayoutWidth;
		LayoutHeight = ViewportHeight / LayoutScale;
	end
end

function Layout.DoTransform()
	theori.graphics.scale(LayoutScale, LayoutScale);
end

function Layout.DrawBackgroundFilled(bgTex)
	local bgTexW, bgTexH = bgTex.Width, bgTex.Height;
	local scale = math.max(LayoutWidth / bgTexW, LayoutHeight / bgTexH);
	theori.graphics.draw(bgTex, (LayoutWidth - bgTexW * scale) / 2, (LayoutHeight - bgTexH * scale) / 2, bgTexW * scale, bgTexH * scale);
end

function Layout.CheckLayout()
	do -- check if layout needs to be refreshed
		local cvw, cvh = theori.graphics.getViewportSize();
		if (ViewportWidth != cvw or ViewportHeight != cvh) then
			Layout.CalculateLayout();
		end
	end
end

-- Layout update might not be FANTASTIC, as it might be better to encourage
--  that all layout perspectives behave on the same state?
function Layout.Update(delta, total)
	Layouts[LayoutKind]:Update(delta, total);
end

function Layout.Render()
	Layouts[LayoutKind]:Render();
end

-- Landscape Layout

function Layouts.Landscape.Update(self, delta, total)
end

function Layouts.Landscape.Render(self)
end

-- Wide Landscape Layout

function Layouts.WideLandscape.Update(self, delta, total)
	Layouts.Landscape:Update(delta, total);
end

function Layouts.WideLandscape.Render(self)
	Layouts.Landscape:Render();
end

-- Portrait Layout

function Layouts.Portrait.Update(self, delta, total)
end

function Layouts.Portrait.Render(self)
end

-- Tall Portrait Layout

function Layouts.TallPortrait.Update(self, delta, total)
	Layouts.Portrait:Update(delta, total);
end

function Layouts.TallPortrait.Render(self)
	Layouts.Portrait:Render();
end

