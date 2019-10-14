
include "layerLayout";

function nsc.layer.doAsyncLoad()
    Layouts.Landscape.Background = nsc.graphics.queueTextureLoad("genericBackground_LS");
    Layouts.Portrait.Background = nsc.graphics.queueTextureLoad("generigBackground_PR");
    
    return true;
end

function nsc.layer.construct()
end

function nsc.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function nsc.layer.init()
    nsc.openCurtain();

    nsc.input.controller.pressed:connect(function(button)
        if (button == ControllerInput.Back) then
            nsc.closeCurtain(0.2, nsc.layer.pop);
        elseif (button == ControllerInput.Start) then
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
end

-- Landscape

function Layouts.Landscape.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end

