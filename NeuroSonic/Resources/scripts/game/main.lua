
include "layerLayout";

local fontSlant;


--------------------------------------------------
-- Textures --------------------------------------
--------------------------------------------------
local textures =
{
	noise = { },
	numbers = { },
	gauge =
	{
		effective = { },
		excessive = { },
	},
};
--------------------------------------------------

function theori.layer.doAsyncLoad()
	for i = 0, 9 do
		textures.numbers[i] = theori.graphics.queueTextureLoad("combo/" .. i);
	end
	for i = 0, 9 do
		textures.noise[i] = theori.graphics.queueTextureLoad("noise/" .. i);
	end

	textures.gauge.ratebg = theori.graphics.queueTextureLoad("game/gauge/rate_bg");
	textures.gauge.effective.background = theori.graphics.queueTextureLoad("game/gauge/gauge_back_eff");
	textures.gauge.effective.fillNormal = theori.graphics.queueTextureLoad("game/gauge/fill_normal_eff");
	textures.gauge.effective.fillPass = theori.graphics.queueTextureLoad("game/gauge/fill_pass_eff");
	textures.gauge.effective.outline = theori.graphics.queueTextureLoad("game/gauge/anim_outline_gauge_eff");

    return true;
end

function theori.layer.doAsyncFinalize()
    return true;
end

function theori.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function theori.layer.init()
    fontSlant = theori.graphics.getStaticFont("slant");

    theori.graphics.openCurtain();
end

function theori.layer.update(delta, total)
    local w, h = LayoutWidth, LayoutHeight;

    Layout.Update(delta, total);
end

function theori.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();
end

local function renderScoreSpriteNum(x, y, w, r, g, b, a)
	local num = scoring.score;
	local dig = 8;

	local digTextures = { };
	for i = dig, 1, -1 do
		local tento, tentom1 = math.pow(10, i), math.pow(10, i - 1);
		local tex = textures.numbers[math.floor((num % tento) / tentom1)];

		table.insert(digTextures, tex);
	end

	local scaleFactor = 0.8;
	local scaleRatio = 1 / (2 * scaleFactor);

	local xPos = x;
	for i, texture in next, digTextures do
		local s;
		if (i <= 4) then
			s = w * scaleRatio / 4;
		else
			s = (w - (w * scaleRatio)) / 4;
		end

		theori.graphics.setFillToTexture(texture, r, g, b, a);
		theori.graphics.fillRect(xPos, y - s, s, s);
		xPos = xPos + s;
	end
end

-- Landscape Layout

local function renderJacketPanel(x, y, w)
	local h = w * 0.5;

	-- values
	local margin = h * 0.05;
	local rounding = margin;

	local jacketSize = h * 0.8 - 2 * margin;
	local diffHeight = h - 3 * margin - jacketSize;
	
	local trackNumTotalHeight = h * 0.425;
	local gameInfoTotalHeight = h * 0.425;

	local progressWidth = w - 2 * margin - jacketSize;
	local progressHeight = h - trackNumTotalHeight - gameInfoTotalHeight;

	-- jacket/diff panel background
	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.fillRoundedRectVarying(x, y, jacketSize + 2 * margin, h, 0, 0, rounding, rounding);

	-- track label background
	theori.graphics.fillRoundedRectVarying(x + jacketSize + 2 * margin, y, progressWidth, trackNumTotalHeight + progressHeight, 0, rounding, rounding, 0);

	-- jacket
	local jacketTexture = game.chart.getJacketTexture();
	if (jacketTexture) then
		theori.graphics.setFillToTexture(jacketTexture, 255, 255, 255, 255);
	else
		theori.graphics.setFillToColor(0, 0, 0, 200);
	end
	theori.graphics.fillRoundedRectVarying(x + margin, x + margin, jacketSize, jacketSize,
										   0, rounding, 0, 0);

	-- diff frame
	theori.graphics.setFillToColor(0, 10, 40, 255);
	theori.graphics.fillRoundedRectVarying(x + margin, y + 2 * margin + jacketSize, jacketSize, diffHeight, 0, 0, rounding, rounding);

	-- progress bar frame
	theori.graphics.setFillToColor(255, 0, 200, 255);
	theori.graphics.fillRoundedRect(x + 3 * margin + jacketSize, y + trackNumTotalHeight + progressHeight / 4, progressWidth - 2 * margin, progressHeight / 2, progressHeight / 4);

	-- progress bar fill
	local progressBubbleSize = progressHeight / 4;
	local progressCenter = progressBubbleSize / 2 + x + 3 * margin + jacketSize + (progressWidth - 2 * margin - progressBubbleSize) * game.progress;

	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.fillRect(x + 3 * margin + jacketSize + progressBubbleSize / 2, y + trackNumTotalHeight + progressHeight / 2 - 1, progressWidth - progressBubbleSize - 2 * margin, 2);
	theori.graphics.fillRoundedRect(progressCenter - progressBubbleSize * 0.5, y + trackNumTotalHeight + progressHeight / 2 - progressBubbleSize * 0.5, progressBubbleSize, progressBubbleSize, progressBubbleSize / 2);

	-- "Track N"
	theori.graphics.setFillToColor(0, 0, 0, 255);
	theori.graphics.setFont(fontSlant);
	theori.graphics.setFontSize(trackNumTotalHeight * 0.75);
	theori.graphics.setTextAlign(Anchor.BottomLeft);
	theori.graphics.fillString("Track", x + 2 * margin + jacketSize, y + trackNumTotalHeight);

	local trackTextWidth, _ = theori.graphics.measureString("Track ");
	theori.graphics.setFontSize(trackNumTotalHeight * 0.85);
	theori.graphics.fillString("Ex", x + 2 * margin + jacketSize + trackTextWidth, y + trackNumTotalHeight);

	-- game info text
	local textSpace = w - 2 * margin - jacketSize;

	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.setFont(fontSlant);
	theori.graphics.setFontSize((gameInfoTotalHeight / 2) * 0.75);
	theori.graphics.setTextAlign(Anchor.BottomRight);
	theori.graphics.fillString("BPM", x + 2 * margin + jacketSize + textSpace * 0.6, y + trackNumTotalHeight + progressHeight + gameInfoTotalHeight / 2);
	theori.graphics.fillString("HI-SPEED", x + 2 * margin + jacketSize + textSpace * 0.6, y + trackNumTotalHeight + progressHeight + gameInfoTotalHeight * 0.95);
	
	theori.graphics.setTextAlign(Anchor.BottomLeft);
	theori.graphics.fillString(tostring(math.floor(game.bpm)), x + 2 * margin + jacketSize + textSpace * 0.7, y + trackNumTotalHeight + progressHeight + gameInfoTotalHeight / 2);
	theori.graphics.fillString(string.format("%.1f", game.hispeed), x + 2 * margin + jacketSize + textSpace * 0.7, y + trackNumTotalHeight + progressHeight + gameInfoTotalHeight * 0.95);
end

local function renderScorePanel(x, y, w)
	local h = w * 0.5;

	local margin = h * 0.05;
	local rounding = margin;

	local scoreHeight = h * 2 / 3;
	local chainHeight = h - scoreHeight;

	-- score background
	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.fillRoundedRectVarying(x, y, w, scoreHeight, 0, 0, rounding, rounding);

	-- score label
	theori.graphics.setFillToColor(0, 0, 0, 255);
	theori.graphics.setFont(fontSlant);
	theori.graphics.setFontSize(scoreHeight * 0.4);
	theori.graphics.setTextAlign(Anchor.BottomLeft);
	theori.graphics.fillString("score", x + margin, y + scoreHeight * 0.4);
	
	-- score display
	renderScoreSpriteNum(x + margin, y + scoreHeight - margin, w - 2 * margin, 0, 0, 0, 255);

	-- max combo display
	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.setFont(fontSlant);
	theori.graphics.setTextAlign(Anchor.TopLeft);
	theori.graphics.setFontSize(chainHeight * 0.55);
	theori.graphics.fillString("MAXIMUM CHAIN", x + w * 0.3, y + scoreHeight * 0.95);
	
	theori.graphics.setTextAlign(Anchor.TopRight);
	theori.graphics.fillString("9999", x + w * 0.25, y + scoreHeight * 0.95);
end

local function renderGauge(x, y, h)
	local gaugeAspect = textures.gauge.effective.background.aspectRatio;
	local gaugeTransitionPoint = 0.7;

	local w = h * gaugeAspect;
	x, y = x - w / 2, y - h / 2;
	
	local gy = y + h * (18 / 362);
	local gh = h * (328 / 362);

	-- bg
	theori.graphics.setFillToTexture(textures.gauge.effective.background, 255, 255, 255, 255);
	theori.graphics.fillRect(x, y, w, h);

	-- fill
	theori.graphics.saveScissor();
	theori.graphics.scissor(x * LayoutScale, (gy + gh * (1 - scoring.gauge)) * LayoutScale, w * LayoutScale, (gh * scoring.gauge) * LayoutScale);

	if (scoring.gauge < gaugeTransitionPoint) then
		theori.graphics.setFillToTexture(textures.gauge.effective.fillNormal, 255, 255, 255, 255);
	else
		theori.graphics.setFillToTexture(textures.gauge.effective.fillPass, 255, 255, 255, 255);
	end
	theori.graphics.fillRect(x, y, w, h);

	theori.graphics.restoreScissor();

	-- outline
	theori.graphics.setFillToTexture(textures.gauge.effective.outline, 255, 255, 255, 255);
	theori.graphics.fillRect(x, y, w, h);

	-- rate
	local rateAspect = textures.gauge.ratebg.aspectRatio;
	local rateW = w * 0.9;
	local rateH = rateW / rateAspect;
	local rateX, rateY = x - rateW * 0.8, gy + gh * (1 - scoring.gauge) - rateH / 2;

	theori.graphics.setFillToTexture(textures.gauge.ratebg, 255, 255, 255, 255);
	theori.graphics.fillRect(rateX, rateY, rateW, rateH);

	-- rate text
	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.setFont(nil);
	theori.graphics.setFontSize(rateH * 0.6);
	theori.graphics.setTextAlign(Anchor.MiddleRight);
	theori.graphics.fillString(tostring(math.floor(scoring.gauge * 100 + 0.5)), rateX + rateW * 0.65, rateY + rateH * 0.425);
end

function Layouts.Landscape.Render(self)
    local w, h = LayoutWidth, LayoutHeight;
	
	renderJacketPanel(0, 0, w * 0.2);
	renderScorePanel(w * 0.8, 0, w * 0.2);
	renderGauge(w * 0.75, h / 2, h * 0.55);
end

-- Wide Landscape Layout

function Layouts.WideLandscape.Render(self)
    Layouts.Landscape:Render();
end

-- Portrait Layout

function Layouts.Portrait.Render(self)
end


-- Tall Portrait Layout

function Layouts.TallPortrait.Render(self)
    Layouts.Portrait:Render();
end
