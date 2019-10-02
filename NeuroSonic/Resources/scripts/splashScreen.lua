
local WAIT_TIME = 0.25;
local TRANSITION_TIME = 0.5;
local HOLD_TIME = 2.0;
local TOTAL_TIME = WAIT_TIME + TRANSITION_TIME + HOLD_TIME;

local alpha = 0;
local timer = 0;

local transitionFunction = function()
    nsc.layer.push("titleScreen");
end;

function nsc.layer.init()
    nsc.input.controller.pressed:connect(function(button)
        if (button == ControllerInput.Start) then
            timer = TOTAL_TIME;
            if (nsc.input.controller.isDown(ControllerInput.BT0)) then
                transitionFunction = nsc.game.pushDebugMenu;
            end
        end
    end);
end

function nsc.layer.update(delta, total)
    timer = timer + delta;
    if (timer > TOTAL_TIME) then
        nsc.layer.setInvalidForResume();
        nsc.closeCurtain(0.25, transitionFunction);
    else
        local temp = timer;
        if (temp >= WAIT_TIME) then
            temp = temp - WAIT_TIME;
            if (temp >= TRANSITION_TIME) then
                temp = temp - TRANSITION_TIME;
                if (temp < HOLD_TIME) then
                    alpha = 1;
                end
            else
                alpha = temp / TRANSITION_TIME;
            end
        else
            alpha = 0;
        end
    end
end

function nsc.layer.render()
    local text = nsc.graphics.getStaticTexture("audfx-text-large");
    
    local w, h = nsc.graphics.getViewportSize();

    local width = w * 0.7;
    local height = width * text.Height / text.Width;
    
    nsc.graphics.setImageColor(0, 169, 255, 255 * alpha);
    nsc.graphics.draw(text, (w - width) / 2, (h - height) / 2, width, height);
end