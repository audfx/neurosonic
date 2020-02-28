
local WAIT_TIME = 0.25;
local TRANSITION_TIME = 0.5;
local HOLD_TIME = 2.0;
local TOTAL_TIME = WAIT_TIME + TRANSITION_TIME + HOLD_TIME;

local alpha = 0;
local timer = 0;

local transitionFunction = function()
    if (theori.isFirstLaunch()) then
        theori.layer.push("initialSetup");
    else
        theori.layer.push("titleScreen");
    end
end;

function theori.layer.init()
    theori.input.controller.pressed:connect(function(controller, button)
        if (button == "start") then
            timer = TOTAL_TIME;
        end
    end);
end

function theori.layer.update(delta, total)
    timer = timer + delta;
    if (timer > TOTAL_TIME) then
        theori.layer.setInvalidForResume();
        theori.graphics.closeCurtain(0.25, transitionFunction);
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

function theori.layer.render()
    local text = theori.graphics.getStaticTexture("audfx-text-large");
    
    local w, h = theori.graphics.getViewportSize();

    local width = w * 0.7;
    local height = width * text.Height / text.Width;
    
    theori.graphics.setFillToTexture(text, 0, 169, 255, 255 * alpha);
    theori.graphics.fillRect((w - width) / 2, (h - height) / 2, width, height);
end