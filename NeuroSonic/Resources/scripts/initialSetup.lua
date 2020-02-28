
local fontSlant;

local function createChoice(name, callback)
    return {
        name = name,
        callback = callback,
	};
end

local choices = {
    createChoice("CONFIG", function()
        theori.graphics.closeCurtain(0.2, function() theori.layer.push("userConfig"); end);
    end),

    createChoice("CONTINUE", function()
        theori.graphics.closeCurtain(0.25, function() theori.layer.push("titleScreen"); end);
    end),
};

local choiceIndex = 1;

local welcomeText = {
    "Welcome to NeuroSonic!",
    "",
    "The game hasn't been configured yet, so you'll need to set it up.",
    "Use your keyboard to navigate the required menus to configure the game.",
    "The arrow keys will navigate the menus or, in the case of the",
    "config menu, change the value assigned to the selected setting.",
    "",
    "Use enter/return to select menu items, including to initiate a",
    "controller input re-binding on the config screen.",
    "(also use ctrl+enter/return to remove a selected binding instead)",
    "",
    "NeuroSonic allows (almost!) any input to be bound to a controller command,",
    "when prompted simply press the key or button you'd like to bind or, in case",
    "of axes, simply move the mouse or gamepad axis (or the positive first, then",
    "negative key if binding an axis to a pair of keys.)",
    "",
    "Use escape to leave a sub-menu or cancel a binding process.",
    "",
    "This version of the game is in a very early development state! Please be",
    "aware there will be issues and missing features in many places given how",
    "new and untested it is. If you have issues or suggestions, please head to",
    "github.com/audfx/neurosonic and leave a GitHub issue with a brief",
    "description and the version of the game at minimum.",
};

function theori.layer.resumed()
    theori.graphics.openCurtain();
end

function theori.layer.init()
    fontSlant = theori.graphics.getStaticFont("slant");

    theori.input.keyboard.pressed.connect(function(key)
        if (key == KeyCode.LEFT) then
            choiceIndex = 1 + ((choiceIndex - 2 + #choices) % #choices);
        elseif (key == KeyCode.RIGHT) then
            choiceIndex = 1 + (choiceIndex % #choices);
        elseif (key == KeyCode.RETURN) then
            choices[choiceIndex].callback();
        end
    end);
    
    theori.graphics.openCurtain();
end

function theori.layer.update(delta, total)
end

function theori.layer.render()
    local w, h = theori.graphics.getViewportSize();

    theori.graphics.setFillToColor(255, 255, 255, 255);

    theori.graphics.setFont(nil);
    theori.graphics.setTextAlign(Anchor.TopCenter);

    local maxTextHeight, maxTextWidth = h * 0.85, w * 0.975;
    local lineHeight, lineScale = maxTextHeight / #welcomeText, 1.2;
    
    theori.graphics.setFontSize(lineHeight * lineScale);

    local maxLineWidth = 0;
    for _, line in next, welcomeText do
        if (line) then
            local lineWidth, _ = theori.graphics.measureString(line);
            maxLineWidth = math.max(maxLineWidth, lineWidth);
        end
    end
    
    if (maxLineWidth > maxTextWidth) then
        local scale = maxTextWidth / maxLineWidth;
        lineHeight = lineHeight * scale;
    end
    
    if (#welcomeText * lineHeight > maxTextHeight) then
        local scale = maxTextHeight / (#welcomeText * lineHeight);
        lineHeight = lineHeight * scale;
    end

    theori.graphics.setFontSize(lineHeight * lineScale);

    for i, line in next, welcomeText do
        if (line) then
            local lineY = -lineHeight * 0.25 + (i - 1) * lineHeight;
            theori.graphics.fillString(line, w / 2, lineY);
        end
    end

    theori.graphics.setFont(fontSlant);
    theori.graphics.setFontSize(h * 0.05);
    theori.graphics.setTextAlign(Anchor.BottomCenter);

    local bounds = w * #choices / (#choices + 2);

    for i, c in next, choices do
        if (i == choiceIndex) then
            theori.graphics.setFillToColor(255, 255, 0, 255);
        else
            theori.graphics.setFillToColor(255, 255, 255, 255);
        end
        theori.graphics.fillString(c.name, (w - bounds) / 2 + (i - 1) * (bounds / (#choices - 1)), h * 0.975);
    end
end
