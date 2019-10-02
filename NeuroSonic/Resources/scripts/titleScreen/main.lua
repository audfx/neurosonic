
include "layerLayout";

local titleTexture;

local startBtnTexture;

function nsc.layer.construct(...)
    local args = { ... };
    if (#args > 0 and args[1]) then
        exitOnLeave = false;
    end
end

function nsc.layer.doAsyncLoad()
    startBtnTexture = nsc.graphics.loadTextureAsync("legend/start");

    Layouts.Landscape.Background = nsc.graphics.loadTextureAsync("genericBackground_LS");
    Layouts.Portrait.Background = nsc.graphics.loadTextureAsync("generigBackground_PR");

    return true;
end

function nsc.layer.init()
    titleTexture = nsc.graphics.getStaticTexture("title");

    Layout.CalculateLayout();

    nsc.openCurtain();

    nsc.input.controller.pressed:connect(function(button)
        if (button == ControllerInput.Back) then
            nsc.closeCurtain(0.1, nsc.game.exit);
        elseif (button == ControllerInput.Start) then
            -- go to login scren stuff and then chart selection
        end
    end);
end

function nsc.layer.update(delta, total)
    Layout.Update(delta, total);
end

function nsc.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();

    local w, h = LayoutWidth, LayoutHeight;
    nsc.graphics.draw(startBtnTexture, (w - 100) / 2, (h - 50) * 3 / 4, 100, 100);

    nsc.graphics.setTextAlign(Anchor.TopCenter);
    nsc.graphics.drawString("PRESS START", w / 2, h * 3 / 4 + 60);
end

-- Landscape

function Layouts.Landscape.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end
