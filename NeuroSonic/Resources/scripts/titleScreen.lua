
include "layerLayout";

local titleTexture;

local titleLoop;
local titleLoopState = "intro";
local titleLoopBeatDuration = 60.0 / 132;
local titleLoopIntroBeatsCounter = 0;
local titleLoopBeatTimer, titleLoopMeasureTimer = 0, 0;
local isFirstTitleLoop = true;

local startBtnTexture;

function nsc.layer.construct(...)
    local args = { ... };
    if (#args > 0 and args[1]) then
        exitOnLeave = false;
    end
end

function nsc.layer.doAsyncLoad()
    startBtnTexture = nsc.graphics.queueTextureLoad("legend/start");

    Layouts.Landscape.Background = nsc.graphics.queueTextureLoad("genericBackground_LS");
    Layouts.Portrait.Background = nsc.graphics.queueTextureLoad("generigBackground_PR");

    return true;
end

function nsc.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function nsc.layer.resumed()
    if (not titleLoop.isPlaying) then
        titleLoopState = "intro";
        isFirstTitleLoop = true;

        titleLoop.playFromStart();
    end
    
    nsc.openCurtain();
end

function nsc.layer.init()
    titleTexture = nsc.graphics.getStaticTexture("title");

    titleLoop = nsc.audio.getStaticAudio("launchtower-title-loop");
    titleLoop.play();

    nsc.openCurtain();

    nsc.input.controller.pressed:connect(function(button)
        if (titleLoopState == "intro") then
            return;
        end
        
        if (button == ControllerInput.Back) then
            nsc.closeCurtain(0.1, nsc.game.exit);
        elseif (button == ControllerInput.Start) then
            if (nsc.input.controller.isDown(ControllerInput.BT0)) then
                nsc.closeCurtain(0.2, function() nsc.layer.push("settingsMenu"); end);
            elseif (nsc.input.controller.isDown(ControllerInput.BT1)) then
                nsc.closeCurtain(0.2, function() nsc.layer.push("bindingMenu"); end);
            elseif (nsc.input.controller.isDown(ControllerInput.BT3)) then
                nsc.closeCurtain(0.2, function() nsc.layer.push("configMenu"); end);
            else
                -- go to login scren stuff and then chart selection
                titleLoop.stop();
                nsc.closeCurtain(0.2, function() nsc.layer.push("chartSelect"); end);
            end
        end
    end);
end

function nsc.layer.update(delta, total)
    local audioPosition = titleLoop.position;
    if (audioPosition > 8 * titleLoopBeatDuration) then
        isFirstTitleLoop = false;
    end

    if (titleLoopState == "intro") then
        if (audioPosition >= 4 * titleLoopBeatDuration) then
            titleLoopIntroBeatsCounter = 0;
            titleLoopState = "coreLoop";
        else
            titleLoopIntroBeatsCounter = audioPosition / titleLoopBeatDuration;
        end
    elseif (titleLoopState == "coreLoop") then
        -- nothing yet?
    end
    
    titleLoopBeatTimer = (audioPosition % titleLoopBeatDuration) / titleLoopBeatDuration;
    titleLoopMeasureTimer = (audioPosition % (4 * titleLoopBeatDuration)) / (4 * titleLoopBeatDuration);

    Layout.Update(delta, total);
end

function nsc.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();

    local w, h = LayoutWidth, LayoutHeight;

    if (titleLoopState == "intro") then
        nsc.graphics.setColor(0, 0, 0, 255);
        nsc.graphics.fillRect(0, 0, w, h);
    else
        do
            local pulsePoint, pulseScaleAmt = 0.04, 0.02;
            local pulseScale = pulseScaleAmt * ((titleLoopBeatTimer < pulsePoint) and
                titleLoopBeatTimer / pulsePoint or
                1 - (titleLoopBeatTimer - pulsePoint) / (1 - pulsePoint));

            local width = w * 0.8 * (1 - pulseScale);
            local height = width * titleTexture.Height / titleTexture.Width;

            nsc.graphics.draw(titleTexture, (w - width) / 2, h / 2 - height - 10, width, height);

            width = w * 0.8 * (1 + pulseScale * 0.5);
            height = width * titleTexture.Height / titleTexture.Width;

            nsc.graphics.setImageColor(255, 255, 255, 70);
            nsc.graphics.draw(titleTexture, (w - width) / 2, h / 2 - height - 10, width, height);
        end
        
        nsc.graphics.setImageColor(255, 255, 255, 255);
        nsc.graphics.draw(startBtnTexture, (w - 100) / 2, (h - 50) * 3 / 4, 100, 100);

        nsc.graphics.setTextAlign(Anchor.TopCenter);
        nsc.graphics.drawString("PRESS START", w / 2, h * 3 / 4 + 60);
    end
end

-- Landscape

function Layouts.Landscape.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end
