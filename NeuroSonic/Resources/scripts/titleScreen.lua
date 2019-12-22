
include "layerLayout";

local bgName = "bgHighContrast";

local titleTexture;

local titleLoop;
local titleLoopState = "intro";
local titleLoopBeatDuration = 60.0 / 132;
local titleLoopIntroBeatsCounter = 0;
local titleLoopBeatTimer, titleLoopMeasureTimer = 0, 0;
local isFirstTitleLoop = true;

local startBtnTexture;

function theori.layer.construct(...)
    local args = { ... };
    if (#args > 0 and args[1]) then
        exitOnLeave = false;
    end
end

function theori.layer.doAsyncLoad()
    startBtnTexture = theori.graphics.queueTextureLoad("legend/start");

    Layouts.Landscape.Background = theori.graphics.queueTextureLoad(bgName .. "_LS");
    Layouts.Portrait.Background = theori.graphics.queueTextureLoad(bgName .. "_PR");

    return true;
end

function theori.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function theori.layer.resumed()
    if (not titleLoop.isPlaying) then
        titleLoopState = "intro";
        isFirstTitleLoop = true;

        titleLoop.playFromStart();
    end
    
    theori.graphics.openCurtain();
end

function theori.layer.init()
    titleTexture = theori.graphics.getStaticTexture("title");

    titleLoop = theori.audio.getStaticAudio("launchtower-title-loop");
    titleLoop.play();

    theori.graphics.openCurtain();

    theori.input.controller.pressed:connect(function(controller, button)
        if (titleLoopState == "intro") then
            return;
        end
        
        if (button == "back") then
            theori.graphics.closeCurtain(0.1, theori.game.exit);
        elseif (button == "start") then
            if (controller:isDown(0)) then
                theori.graphics.closeCurtain(0.2, function() theori.layer.push("userConfig"); end);
            elseif (controller:isDown(1)) then
                theori.graphics.closeCurtain(0.2, function() theori.layer.push("controllerBinding"); end);
            elseif (controller:isDown(2)) then
            elseif (controller:isDown(3)) then
            elseif (controller:isDown(4)) then
            elseif (controller:isDown(5)) then
            else
                titleLoop.stop();
                theori.graphics.closeCurtain(0.2, function() theori.layer.push("chartSelect"); end);
            end
        end
    end);
end

function theori.layer.update(delta, total)
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

function theori.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();

    local w, h = LayoutWidth, LayoutHeight;

    if (titleLoopState == "intro") then
        theori.graphics.setColor(0, 0, 0, 255);
        theori.graphics.fillRect(0, 0, w, h);
    else
        do
            local pulsePoint, pulseScaleAmt = 0.04, 0.02;
            local pulseScale = pulseScaleAmt * ((titleLoopBeatTimer < pulsePoint) and
                titleLoopBeatTimer / pulsePoint or
                1 - (titleLoopBeatTimer - pulsePoint) / (1 - pulsePoint));

            local width = w * 0.8 * (1 - pulseScale);
            local height = width * titleTexture.Height / titleTexture.Width;

            theori.graphics.draw(titleTexture, (w - width) / 2, h / 2 - height - 10, width, height);

            width = w * 0.8 * (1 + pulseScale * 0.5);
            height = width * titleTexture.Height / titleTexture.Width;

            theori.graphics.setImageColor(255, 255, 255, 70);
            theori.graphics.draw(titleTexture, (w - width) / 2, h / 2 - height - 10, width, height);
        end
        
        theori.graphics.setImageColor(255, 255, 255, 255);
        theori.graphics.draw(startBtnTexture, (w - 100) / 2, (h - 50) * 3 / 4, 100, 100);

        theori.graphics.setTextAlign(Anchor.TopCenter);
        theori.graphics.drawString("PRESS START", w / 2, h * 3 / 4 + 60);
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
