
include "layerLayout";
include "linearToggle";

local bgName = "bgHighContrast";

local fontSlant;

local controller = nil;

local isListeningForInput, inputPrompt = false;
local listenDebounce = false;
local onInputListened = nil;
local notifyMessage, notifyTimer = nil, 0;

local scrollCamera = 0;
local itemHeight = 100;

local function notifyUser(message)
    notifyMessage = message;
    notifyTimer = 8;
end

local function setToListenForInput(prompt, callback)
    isListeningForInput = true;
    inputPrompt = prompt;
    onInputListened = function(device, input, axis)
        listenDebounce = true;
        isListeningForInput = false;
        callback(device, input, axis);
    end;
end

-- device = "keyboard", "mouse", "<gamepad object>"
-- input = KeyCode, MouseButton / "motion" / "wheel", integer
-- axis = nil, nil / Axis / integer direction, nil / integer axis
local function setButtonBinding(bindingLabel)
    return function(device, input, axis)
        if (axis) then
            notifyUser("Cannot currently assign an axis to a button input.");
            return;
        end

        local bindings = controller.getButtonBindings(bindingLabel);
        bindings[1] = { device = device, input = input, axis = axis };

        controller.setButtonBindings(bindingLabel, bindings);
    end;
end
local function setAxisBinding(bindingLabel, posnegMessage)
    return function(device, input, axis)
        if (posnegMessage) then -- two inputs for buttons, otherwise regular axis
            local isKeyboardButton = device == "keyboard";
            local isMouseButton = device == "mouse" and not axis;

            local isGamepadButton = (device ~= "keyboard" and device ~= "mouse") and not axis;
            if (isGamepadButton) then
                notifyUser("Gamepad buttons are currently not supported for axis bindings.");
                return;
            end

            if (isKeyboardButton or isMouseButton) then
                -- when in pos/neg mode we need two buttons
                setToListenForInput(posnegMessage, function(device2, input2, axis2)
                    if (device ~= device2) then
                        notifyUser("When setting a binding with positive/negative directionality, you set inputs with different input devices. This is not currently supported.");
                        return;
                    end

                    if (axis) then
                        notifyUser("When setting a binding with positive/negative directionality, you set a single input for a positive and an axis for the negative. This is currently not supported.");
                        return;
                    end

                    if (device ~= "keyboard" and device ~= "mouse") then
                        notifyUser("Gamepad buttons are currently not supported for axis bindings.");
                        return;
                    end

                    local bindings = controller.getButtonBindings(bindingLabel);
                    bindings[1] = { device = device, input = input, input2 = input2, axis = axis, axisStyle = ControllerAxisStyle.Linear };

                    controller.setAxisBindings(bindingLabel, bindings);
                end);
            else
                if (device == "mouse" and device ~= "motion") then
                    notifyUser("Mouse wheel is currently not supported as an input method.");
                    return;
                end

                local bindings = controller.getButtonBindings(bindingLabel);
                bindings[1] = { device = device, input = input, axis = axis, axisStyle = ControllerAxisStyle.Radial };

                controller.setAxisBindings(bindingLabel, bindings);
            end
        else -- one input for buttons, otherwise regular axis
            if (device == "mouse" and (input == "motion" or input == "wheel")) then
                notifyUser("Mouse " .. input .. " is currently not supported as an input method.");
                return;
            end

            if ((device ~= "keyboar" and device ~= "mouse") and not axis) then
                notifyUser("Gamepad buttons are currently not supported for axis bindings.");
                return;
            end

            local bindings = controller.getButtonBindings(bindingLabel);
            bindings[1] = { device = device, input = input, axis = axis };

            controller.setAxisBindings(bindingLabel, bindings);
        end
    end;
end

local function createRangeEntry(title, desc, configKey, min, max, step)
    local result = linearToggle();
    result.kind = "range";
    result.title = title;
    result.desc = desc;
    result.key = configKey;
    result.min = min;
    result.max = max;
    result.step = step;
    result.getValue = function(self) return theori.config.get(self.key); end;
    result.change = function(self, dir) theori.config.set(self.key, math.max(self.min, math.min(self.max, self:getValue() + self.step * dir))); self:updateValue(); end;
    result.value = 0;
    result.updateValue = function(self) self.value = (self:getValue() - self.min) / (self.max - self.min); end;
    result:updateValue();
    return result;
end

local function createBindingEntry(title, desc, callback)
    local result = linearToggle();
    result.kind = "binding";
    result.title = title;
    result.desc = desc;
    result.callback = callback;
    return result;
end

local configIndex = 1;
local configOptions = {
    createRangeEntry("Input Offset", "The offset applied to input judgements.", "NeuroSonic.InputOffset", -100, 100, 1),
    createRangeEntry("Video Offset", "The offset applied to rendering systems.", "NeuroSonic.VideoOffset", -100, 100, 1),

    createBindingEntry("Start Button", "The start button.", function() setToListenForInput("Bind `Start` Button.", setButtonBinding("start")); end),
    createBindingEntry("Back Button", "The back button.", function() setToListenForInput("Bind `Back` Button.", setButtonBinding("back")); end),
    
    createBindingEntry("BT-A", "The A button.", function() setToListenForInput("Bind `BT-A` Button.", setButtonBinding(0)); end),
    createBindingEntry("BT-B", "The B button.", function() setToListenForInput("Bind `BT-B` Button.", setButtonBinding(1)); end),
    createBindingEntry("BT-C", "The C button.", function() setToListenForInput("Bind `BT-C` Button.", setButtonBinding(2)); end),
    createBindingEntry("BT-D", "The D button.", function() setToListenForInput("Bind `BT-D` Button.", setButtonBinding(3)); end),
    
    createBindingEntry("FX-L", "The Left FX button.", function() setToListenForInput("Bind `FX-L` Button.", setButtonBinding(4)); end),
    createBindingEntry("FX-R", "The Right FX button.", function() setToListenForInput("Bind `FX-R` Button.", setButtonBinding(5)); end),
    
    createBindingEntry("Left Laser", "The +/- directions for the left laser.", function() setToListenForInput("Bind `Left Laser` Positive (or analog) Axis.", setAxisBinding(0, "Bind `Left Laser` Negative Axis.")); end),
    createBindingEntry("Right Laser", "The +/- directions for the right laser.", function() setToListenForInput("Bind `Right Laser` Positive (or analog) Axis.", setAxisBinding(1, "Bind `Right Laser` Negative Axis.")); end),
};

function theori.layer.doAsyncLoad()
    Layouts.Landscape.Background = theori.graphics.queueTextureLoad(bgName .. "_LS");
    Layouts.Portrait.Background = theori.graphics.queueTextureLoad(bgName .. "_PR");
    
    fontSlant = theori.graphics.getStaticFont("slant");

    return true;
end

function theori.layer.construct()
end

function theori.layer.onClientSizeChanged(w, h)
    Layout.CalculateLayout();
end

function theori.layer.init()
    controller = nsc.input.getController();

    configOptions[1].to = 1;
    configOptions[1].alpha = 0;

    theori.graphics.openCurtain();

    theori.input.controller.axisTicked.connect(function(controller, axis, dir)
        if (isListeningForInput) then return; end

        if (axis == 0) then
            local option = configOptions[configIndex];
            if (option.kind == "range") then
                option:change(dir);
            end
        elseif (axis == 1) then
            configOptions[configIndex]:toggle();
            configIndex = 1 + (configIndex - 1 + dir) % #configOptions;
            configOptions[configIndex]:toggle();
        end
    end);

    theori.input.controller.pressed.connect(function(controller, button)
        if (isListeningForInput or listenDebounce) then return; end

        if (button == "back") then
            theori.graphics.closeCurtain(0.2, theori.layer.pop);
        elseif (button == "start") then
            configOptions[configIndex].callback();
        end
    end);

    theori.input.keyboard.pressedRaw.connect(function(key)
        if (isListeningForInput) then
            onInputListened("keyboard", key, nil);
        end
    end);

    theori.input.mouse.pressedRaw.connect(function(button)
        if (isListeningForInput) then
            onInputListened("mouse", button, nil);
        end
    end);

    theori.input.mouse.movedRaw.connect(function(x, y, dx, dy)
        if (isListeningForInput) then
            if (dx ~= 0 and dy ~= 0) then
                return;
            end
            onInputListened("mouse", "motion", dx ~= 0 and Axis.X or Axis.Y);
        end
    end);

    theori.input.gamepad.pressedRaw.connect(function(gamepad, button)
        if (isListeningForInput) then
            onInputListened(gamepad, button, nil);
        end
    end);

    theori.input.gamepad.axisChangedRaw.connect(function(gamepad, axis, value)
        if (isListeningForInput) then
            onInputListened(gamepad, axis, axis);
        end
    end);
end

function theori.layer.update(delta, total)
    if (listenDebounce) then listenDebounce = false; end
    
    local w, h = LayoutWidth, LayoutHeight;

    for i, opt in next, configOptions do
        opt:update(delta * 5);
    end

    notifyTimer = math.max(0, notifyTimer - delta);

    itemHeight = math.max(50, h * 0.05);
    local targetCamera = -itemHeight / 2 - (configIndex - 1) * itemHeight;
    scrollCamera = scrollCamera + (targetCamera - scrollCamera) * delta * 10;

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

    if (isListeningForInput) then
        theori.graphics.setFont(fontSlant);
        theori.graphics.setFillToColor(255, 255, 255, 255);
        theori.graphics.setFontSize(h * 0.07);
        theori.graphics.setTextAlign(Anchor.BottomCenter);
        theori.graphics.fillString(inputPrompt, w / 2, h / 2);
    else
        Layout.DrawBackgroundFilled(self.Background);

        local yOff = math.min(-h / 2 + itemHeight, scrollCamera);
        for i, opt in next, configOptions do
            local linear = opt:sampleLinear();
            local xOff = opt:sample();
            local yPos = h / 2 + (i - 1) * itemHeight + yOff;

            theori.graphics.setFont(fontSlant);
            theori.graphics.setFillToColor(255 - 85 * linear, 255 - 45 * linear, 255, 255);

            theori.graphics.setFontSize(itemHeight * 0.7);
            theori.graphics.setTextAlign(Anchor.BottomLeft);
            theori.graphics.fillString(opt.title, w * 0.025 + xOff * w * 0.02, yPos);

            theori.graphics.setFontSize(itemHeight * 0.25);
            theori.graphics.setTextAlign(Anchor.TopLeft);
            theori.graphics.fillString(opt.title, w * 0.025 + w * 0.0075 * (1 + xOff * 0.7), yPos);

            if (opt.kind == "range") then
                local rangeWidth = w * 0.5;
                local rangeXPos = w * 0.975 - rangeWidth;

                theori.graphics.setFillToColor(255, 255, 255, 255);

                local xRel = opt.value;
                local pointXPos = rangeXPos + 3 + xRel * (rangeWidth - 6);

                theori.graphics.fillRect(rangeXPos, yPos - 3, rangeWidth, 6);
                theori.graphics.fillRect(pointXPos - 3, yPos - 13, 6, 10);
                
                theori.graphics.setFont(nil);
                theori.graphics.setFontSize(16);
                theori.graphics.setTextAlign(Anchor.TopCenter);
                theori.graphics.fillString(tostring(opt:getValue()), pointXPos, yPos + 3);
            end
        end
    end

    if (notifyTimer > 0) then
        theori.graphics.setFont(nil);
        theori.graphics.setTextAlign(Anchor.TopCenter);
        theori.graphics.setFontSize(h * 0.08);
        
        theori.graphics.setFillToColor(0, 0, 0, 255);
        theori.graphics.fillString(notifyMessage, w / 2 - 3, h * 0.05 + 3);

        theori.graphics.setFillToColor(255, 255, 255, 255);
        theori.graphics.fillString(notifyMessage, w / 2, h * 0.05);
    end
end

-- Portrait

function Layouts.Portrait.Render(self)
    local w, h = LayoutWidth, LayoutHeight;

    if (isListeningForInput) then
        theori.graphics.setFont(fontSlant);
        theori.graphics.setFillToColor(255, 255, 255, 255);
        theori.graphics.setFontSize(h * 0.035);
        theori.graphics.setTextAlign(Anchor.MiddleCenter);
        theori.graphics.fillString(inputPrompt, w / 2, h / 2);
    else
        Layout.DrawBackgroundFilled(self.Background);
    end
end

