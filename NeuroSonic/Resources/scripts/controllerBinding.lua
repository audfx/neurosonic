
include "layerLayout";

local bgName = "bgHighContrast";

function theori.layer.doAsyncLoad()
    Layouts.Landscape.Background = theori.graphics.queueTextureLoad(bgName .. "_LS");
    Layouts.Portrait.Background = theori.graphics.queueTextureLoad(bgName .. "_PR");
    
    return true;
end

function theori.layer.construct()
end

function theori.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function theori.layer.init()
    theori.graphics.openCurtain();

    theori.input.controller.pressed:connect(function(controller, button)
        if (button == "back") then
            theori.graphics.closeCurtain(0.2, theori.layer.pop);
        elseif (button == "start") then
        end
    end);
end

function theori.layer.update(delta, total)
    Layout.Update(delta, total);
end

function theori.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();
end

-- Landscape

function Layouts.Landscape.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end

