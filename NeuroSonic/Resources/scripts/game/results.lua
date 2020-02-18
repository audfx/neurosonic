
include "layerLayout";
include "linearToggle";

local bgName = "bgHighContrast";

local chart, result;

function theori.layer.doAsyncLoad()
    Layouts.Landscape.Background = theori.graphics.queueTextureLoad(bgName .. "_LS");
    Layouts.Portrait.Background = theori.graphics.queueTextureLoad(bgName .. "_PR");

	return true;
end

function theori.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function theori.layer.construct(c, r)
    chart = c;
    result = r;
end

function theori.layer.init()
    theori.input.keyboard.pressed.connect(function(key)
        if (key == Key.ESCAPE) then
            theori.graphics.closeCurtain(0.25, function() theori.layer.pop(); end);
        end
    end);
    
    theori.input.controller.pressed.connect(function(controller, button)
        if (button == "back") then
            theori.graphics.closeCurtain(0.25, function() theori.layer.pop(); end);
        end
    end);
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

local function renderResultsPanel(x, y, w, h)
    theori.graphics.setFillToColor(0, 0, 0, 180);
    theori.graphics.fillRect(x, y, w, h);

    theori.graphics.setFont(nil);
    theori.graphics.setFontSize(h * 0.1);
    theori.graphics.setTextAlign(Anchor.TopLeft);
    theori.graphics.setFillToColor(255, 255, 255, 255);
    
    theori.graphics.fillString(string.format("%s - %08d", tostring(result.rank), result.score), x, y);
    theori.graphics.fillString(string.format("%.1f%% (%s)", result.gauge * 100, tostring(result.gaugeType)), x, y + h * 0.1);
    theori.graphics.fillString(string.format("Passive: %d", result.passiveBtCount + result.passiveFxCount + result.passiveVolCount), x, y + h * 0.2);
    theori.graphics.fillString(string.format("Perfect: %d", result.perfectBtCount + result.perfectFxCount), x, y + h * 0.3);
    theori.graphics.fillString(string.format("Critical: %d", result.criticalBtCount + result.criticalFxCount), x, y + h * 0.4);
    theori.graphics.fillString(string.format("Early: %d", result.earlyBtCount + result.earlyFxCount), x, y + h * 0.5);
    theori.graphics.fillString(string.format("Late: %d", result.lateBtCount + result.lateFxCount), x, y + h * 0.6);
    theori.graphics.fillString(string.format("Miss: %d", result.missCount + result.badBtCount + result.badFxCount), x, y + h * 0.7);
end

-- Landscape Layout

function Layouts.Landscape.Render(self)
    Layout.DrawBackgroundFilled(self.Background);

    renderResultsPanel(LayoutHeight * 0.1, LayoutHeight * 0.1, LayoutWidth * 0.4, LayoutHeight * 0.8);
end

-- Wide Landscape Layout

function Layouts.WideLandscape.Render(self)
    Layouts.Landscape:Render();
end

-- Portrait Layout

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end


-- Tall Portrait Layout

function Layouts.TallPortrait.Render(self)
    Layouts.Portrait:Render();
end

