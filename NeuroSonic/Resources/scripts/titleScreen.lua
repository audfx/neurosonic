
include "layerLayout";
include "linearToggle";

local bgName = "bgHighContrast";

local titleTexture;

local titleLoop;
local titleLoopBeatDuration = 60.0 / 132;
local titleLoopBeatTimer, titleLoopMeasureTimer = 0, 0;

local fontSlant;

local startBtnTexture;

local function createButton(text, img, desc, callback)
    local b = linearToggle();
    b.text = text;
    b.img = img;
    b.desc = desc;
    b.callback = callback;

    return b;
end

local selectedButtonIndex = 1;
local buttons = {
    primary = { -- NOTE: for the intro animation to work correctly, there MUST be 4 buttons in the primary slot!
        title = "NEUROSONIC",
        buttons = {
            createButton("PLAY NEUROSONIC", "play", "Play NeuroSonic!", function()
                titleLoop.stop();
                theori.graphics.closeCurtain(0.2, function() theori.layer.push("chartSelect"); end);
            end),

            createButton("CHART EDITOR", "editor", "Create and edit custom charts for your favorite songs.", nil),

            createButton("GET SONGS", "getSongs", "Download official charts for your favorite songs.", nil),

            createButton("SETTINGS", "settings", "Configure your NeuroSonic experience.", function()
                theori.graphics.closeCurtain(0.1, function() theori.layer.push("userConfig"); end);
            end),

            createButton("EXIT", "exit", "Leaving so soon? :(", function()
                theori.graphics.closeCurtain(0.1, theori.game.exit);
            end),
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

    fontSlant = theori.graphics.getStaticFont("slant");

    return true;
end

function theori.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function theori.layer.resumed()
    if (not titleLoop.isPlaying) then
        titleLoop.position = 4 * titleLoopBeatDuration;
        titleLoop.play();
    end
    
    theori.graphics.openCurtain();
end

function theori.layer.init()
    titleTexture = theori.graphics.getStaticTexture("title");

    titleLoop = theori.audio.getStaticAudio("launchtower-title-loop");
    titleLoop.position = 4 * titleLoopBeatDuration;
    titleLoop.play();

    currentButtons.buttons[1].to = 1;
    currentButtons.buttons[1].alpha = 0;

    theori.graphics.openCurtain();

    theori.input.controller.pressed:connect(function(controller, button)
        if (button == "back") then
            theori.graphics.closeCurtain(0.1, theori.game.exit);
        elseif (button == "start") then
            local callback = currentButtons.buttons[selectedButtonIndex].callback;
            if (callback) then callback(); end;
        end
    end);

    theori.input.controller.axisTicked:connect(function(controller, axis, dir)
        currentButtons.buttons[selectedButtonIndex]:toggle();
        selectedButtonIndex = 1 + (selectedButtonIndex + dir - 1) % #currentButtons.buttons;
        currentButtons.buttons[selectedButtonIndex]:toggle();
    end);
end

function theori.layer.update(delta, total)
    local audioPosition = titleLoop.position;
    
    titleLoopBeatTimer = (audioPosition % titleLoopBeatDuration) / titleLoopBeatDuration;
    titleLoopMeasureTimer = (audioPosition % (4 * titleLoopBeatDuration)) / (4 * titleLoopBeatDuration);

    for i, button in next, currentButtons.buttons do
        button:update(delta * 5);
    end

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

    local btnHeight = h * 0.1;
    local btnSpaceX, btnSpaceY = w * 0.05, h * 0.25;

    local pulsePoint, pulseScaleAmt = 0.04, 0.04;
    local pulse = (titleLoopBeatTimer < pulsePoint) and
        titleLoopBeatTimer / pulsePoint or
        1 - (titleLoopBeatTimer - pulsePoint) / (1 - pulsePoint);
    
    Layout.DrawBackgroundFilled(self.Background);

    for i, button in next, currentButtons.buttons do
        local linear = button:sampleLinear();
        local xOff = button:sample();

        local x, y = btnSpaceX + (i - 1) * w * 0.025, btnSpaceY + (i - 1) * btnHeight;

        theori.graphics.setFillToColor(255 - 85 * linear, 255 - 45 * linear, 255, 255 - 45 * pulse);

        theori.graphics.setFont(fontSlant);
        theori.graphics.setFontSize(btnHeight * 0.65);
        theori.graphics.setTextAlign(Anchor.BottomLeft);
        theori.graphics.fillString(button.text, x + xOff * w * 0.02, y + btnHeight * 0.5);

        theori.graphics.setFont(nil);
        theori.graphics.setFontSize(btnHeight * 0.3);
        theori.graphics.setTextAlign(Anchor.TopLeft);
        theori.graphics.fillString(button.desc, x + w * 0.0075 * (1 + xOff * 0.7), y + btnHeight * 0.5);
    end

    theori.graphics.setFillToTexture(startBtnTexture, 255, 255, 255, 255);
    theori.graphics.fillRect((w - 100) / 2, (h - 50) * 3 / 4, 100, 100);
end

-- Portrait

function Layouts.Portrait.Render(self)
    Layout.DrawBackgroundFilled(self.Background);
end
