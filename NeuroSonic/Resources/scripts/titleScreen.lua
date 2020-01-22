
include "layerLayout";

local bgName = "bgHighContrast";

local titleTexture;

local titleLoop;
local titleLoopState = "intro";
local titleLoopBeatDuration = 60.0 / 132;
local titleLoopIntroBeatsCounter = 0;
local titleLoopBeatTimer, titleLoopMeasureTimer = 0, 0;
local isFirstTitleLoop = true;

local fontSlant;

local startBtnTexture;

local function createButton(text, img, desc, callback)
    return {
        text = text,
        desc = desc;
    };
end

local buttons = {
    primary = { -- NOTE: for the intro animation to work correctly, there MUST be 4 buttons in the primary slot!
        title = "NEUROSONIC",
        buttons = {
            createButton("PLAY!", "play", "Play NeuroSonic.", function() end),
            createButton("GET SONGS", "getSongs", "Download official charts for your favorite songs.", nil),
            createButton("SETTINGS", "settings", "Configure your NeuroSonic experience.", nil),
            createButton("EXIT", "exit", "Leaving so soon? :(", nil),
        },
    },
};

local currentButtons, nextButtons = buttons.primary, nil;
local buttonTransitionTimer, buttonFlashTimer = 0, 0;

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

    fontSlant = theori.graphics.createFont("slant");

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
    titleLoop.position = 4 * titleLoopBeatDuration;
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
        buttonFlashTimer = buttonFlashTimer + delta;
    end
    
    titleLoopBeatTimer = (audioPosition % titleLoopBeatDuration) / titleLoopBeatDuration;
    titleLoopMeasureTimer = (audioPosition % (4 * titleLoopBeatDuration)) / (4 * titleLoopBeatDuration);

    Layout.Update(delta, total);
end

function theori.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();
end

-- Landscape

function Layouts.Landscape.Render(self)
    local w, h = LayoutWidth, LayoutHeight;

    local btnSpace = h * 0.6;
    local btnSize = btnSpace * 0.5;

    local btnSpaceX, btnSpaceY = w * 0.05, (h - btnSpace) * 0.5;

    local pulsePoint, pulseScaleAmt = 0.04, 0.04;
    local pulse = (titleLoopBeatTimer < pulsePoint) and
        titleLoopBeatTimer / pulsePoint or
        1 - (titleLoopBeatTimer - pulsePoint) / (1 - pulsePoint);
        
    if (titleLoopState == "intro" and false) then
        local fade = 1 - math.max(0, titleLoopIntroBeatsCounter) / 4;

        for i = 1, 4 do
            local t = math.max(0, math.min(1, titleLoopIntroBeatsCounter - (i - 1)));

            local scale = (t + 2) / 3;
            local x1, y1 = btnSpaceX + (btnSize * 1.2) * (i - 1), btnSpaceY;
            local x0, y0 = -btnSize, y1;

            local function outBack(t, b, c, d, s)
                if not s then s = 1.70158 end
                t = t / d - 1
                return c * (t * t * ((s + 1) * t + s) + 1) + b
            end

            local a = outBack(t, 0, 1, 1);
            local x, y = x0 + (x1 - x0) * a, y0 + (y1 - y0) * a;

            theori.graphics.setFillToColor(70, 70, 70, 255 * fade);
            theori.graphics.fillRect(x, y, btnSize * scale, btnSize * scale);

            if (titleLoopIntroBeatsCounter > 3) then
                local flashTimer = (titleLoopIntroBeatsCounter - 3);
                theori.graphics.setFillToColor(255, 255, 255, 255 * flashTimer);
                theori.graphics.fillRect(0, 0, w, h);
            end
        end
    elseif (false) then
        Layout.DrawBackgroundFilled(self.Background);

        local flashTimer = buttonFlashTimer * 2;
        
        for i, button in next, currentButtons.buttons do
            local x, y = btnSpaceX + (btnSize * 1.2) * (i - 1), btnSpaceY;

            theori.graphics.setFillToColor(70, 70, 70, 255);
            theori.graphics.fillRect(x, y, btnSize, btnSize);

            theori.graphics.setFont(fontSlant);
            theori.graphics.setFontSize(btnSize * 0.15);
            theori.graphics.setTextAlign(Anchor.TopLeft);
            theori.graphics.setFillToColor(255, 255, 255, 255 - 50 * pulse);
            theori.graphics.fillString(button.text, x, y + btnSize);

            if (flashTimer < 1) then
                local o = flashTimer * (2 * btnSize);
                theori.graphics.setFillToColor(255, 255, 255, 170 * (1 - flashTimer));
                theori.graphics.fillRect(x - o / 2, y - o / 2, btnSize + o, btnSize + o);
            end
        end

        theori.graphics.setFillToTexture(startBtnTexture, 255, 255, 255, 255);
        theori.graphics.fillRect((w - 100) / 2, (h - 50) * 3 / 4, 100, 100);

        if (flashTimer < 1) then
            theori.graphics.setFillToColor(255, 255, 255, 255 * math.max(0, (1 - flashTimer * 1.5)));
            theori.graphics.fillRect(0, 0, w, h);
        end
    else
        Layout.DrawBackgroundFilled(self.Background);

        for i, button in next, currentButtons.buttons do
            local x, y = btnSpaceX + (btnSize * 1.2) * (i - 1), btnSpaceY;

            theori.graphics.setFillToColor(70, 70, 70, 255);
            theori.graphics.fillRect(x, y, btnSize, btnSize);

            theori.graphics.setFont(fontSlant);
            theori.graphics.setFontSize(btnSize * 0.15);
            theori.graphics.setTextAlign(Anchor.TopLeft);
            theori.graphics.setFillToColor(255, 255, 255, 255 - 50 * pulse);
            theori.graphics.fillString(button.text, x, y + btnSize);
        end

        theori.graphics.setFillToTexture(startBtnTexture, 255, 255, 255, 255);
        theori.graphics.fillRect((w - 100) / 2, (h - 50) * 3 / 4, 100, 100);
    end
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end
