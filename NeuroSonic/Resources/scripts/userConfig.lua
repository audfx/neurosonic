
include "layerLayout";
include "linearToggle";

local bgName = "bgHighContrast";

local fontSlant;

local controller = nil;

local isListeningForInput, inputPrompt = false;
local bindingIndex = 1;

local listenDebounce = false;
local onInputListened = nil;
local notifyMessage, notifyTimer = nil, 0;

local scrollCamera = 0;
local itemHeight = 100;

-- https://love2d.org/wiki/HSV_color bc why not
function hue2rgb(h)
    h = h * 255;
    local s, v = 255, 255;
    if s <= 0 then return v,v,v end
    h, s, v = h/256*6, s/255, v/255
    local c = v*s
    local x = (1-math.abs((h%2)-1))*c
    local m,r,g,b = (v-c), 0,0,0
    if h < 1     then r,g,b = c,x,0
    elseif h < 2 then r,g,b = x,c,0
    elseif h < 3 then r,g,b = 0,c,x
    elseif h < 4 then r,g,b = 0,x,c
    elseif h < 5 then r,g,b = x,0,c
    else              r,g,b = c,0,x
    end return (r+m)*255,(g+m)*255,(b+m)*255
end

local function notifyUser(message)
    notifyMessage = message;
    notifyTimer = 8;
end

local function disableSpecialHotkeys()
    theori.config.set("NeuroSonic.__processSpecialHotkeys", false);
end

local function enableSpecialHotkeys()
    theori.config.set("NeuroSonic.__processSpecialHotkeys", true);
end

local function setToListenForInput(prompt, callback)
    disableSpecialHotkeys();

    isListeningForInput = true;
    inputPrompt = prompt;
    onInputListened = function(device, input, axis)
        enableSpecialHotkeys();

        listenDebounce = true;
        isListeningForInput = false;
        callback(device, input, axis);
    end;
end

-- device = "keyboard", "mouse", "<gamepad object>"
-- input = KeyCode, MouseButton / "motion" / "wheel", integer
-- axis = nil, nil / Axis / integer direction, nil / integer axis
local function setButtonBinding(bindingLabel)
    local index = bindingIndex;
    return function(device, input, axis)
        if (axis) then
            notifyUser("Cannot currently assign an axis to a button input.");
            return;
        end

        local bindings = controller.getButtonBindings(bindingLabel);
        local bi = math.max(1, math.min(#bindings + 1, index));
        bindings[bi] = { device = device, input = input, axis = axis };

        controller.setButtonBindings(bindingLabel, bindings);
    end;
end
local function setAxisBinding(bindingLabel, posnegMessage)
    local index = bindingIndex;
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
                    local bi = math.max(1, math.min(#bindings + 1, index));
                    bindings[bi] = { device = device, input = input, input2 = input2, axis = axis, axisStyle = ControllerAxisStyle.Linear };

                    controller.setAxisBindings(bindingLabel, bindings);
                end);
            else
                if (device == "mouse" and device ~= "motion") then
                    notifyUser("Mouse wheel is currently not supported as an input method.");
                    return;
                end

                local bindings = controller.getButtonBindings(bindingLabel);
                local bi = math.max(1, math.min(#bindings + 1, index));
                bindings[bi] = { device = device, input = input, axis = axis, axisStyle = ControllerAxisStyle.Radial };

                controller.setAxisBindings(bindingLabel, bindings);
            end
        else -- one input for buttons, otherwise regular axis
            if (device == "mouse" and (input == "motion" or input == "wheel")) then
                notifyUser("Mouse " .. input .. " is currently not supported as an input method.");
                return;
            end

            if ((device ~= "keyboard" and device ~= "mouse") and not axis) then
                notifyUser("Gamepad buttons are currently not supported for axis bindings.");
                return;
            end

            local bindings = controller.getButtonBindings(bindingLabel);
            local bi = math.max(1, math.min(#bindings + 1, index));
            bindings[bi] = { device = device, input = input, axis = axis };

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

local function createComboEntry(title, desc, configKey, ...)
    local combos = { ... };

    local result = linearToggle();
    result.kind = "combo";
    result.title = title;
    result.desc = desc;
    result.key = configKey;
    result.combos = combos;
    result.change = function(self, dir) theori.config.set(self.key, self.combos[math.max(1, math.min(#self.combos, self.index + dir))][1]); self:updateIndex(); end;
    result.index = 1;
    result.indexPartial = 1;
    result.updateIndex = function(self) self.index = 1; for i, c in next, self.combos do if c[1] == theori.config.get(self.key) then self.index = i; break; end end end;
    result:updateIndex();
    result.updateItem = function(self, delta)
        delta = delta * 5;
        if (self.indexPartial < self.index) then
            self.indexPartial = math.min(self.indexPartial + delta, self.index);
        elseif (self.indexPartial > self.index) then
            self.indexPartial = math.max(self.indexPartial - delta, self.index);
        end
    end;
    return result;
end

local function createBindingEntry(title, desc, getBindingCallback, setBindingCallback, callback)
    local result = linearToggle();
    result.kind = "binding";
    result.title = title;
    result.desc = desc;
    result.getBindings = getBindingCallback;
    result.setBindings = setBindingCallback;
    result.callback = callback;
    return result;
end

local function createTextEntry(title, desc, configKey, onChangedCallback)
    local result = linearToggle();
    result.kind = "text";
    result.title = title;
    result.desc = desc;
    result.key = configKey;
    result.onChangedCallback = onChangedCallback;
    result.callback = function()
    end;
    return result;
end

local function bGetBinds(label) return function(self) return controller.getButtonBindings(label); end; end
local function aGetBinds(label) return function(self) return controller.getAxisBindings(label); end; end

local function bSetBinds(label) return function(self, bindings) return controller.setButtonBindings(label, bindings); end; end
local function aSetBinds(label) return function(self, bindings) return controller.setAxisBindings(label, bindings); end; end

local configIndex = 1;
local configOptions = {
    createRangeEntry("Master Volume", "The master volume.", "theori.MasterVolume", 0.0, 1.0, 0.01),
    createRangeEntry("Input Offset (ms)", "The offset applied to input judgements.", "NeuroSonic.InputOffset", -100, 100, 1),
    createRangeEntry("Video Offset (ms)", "The offset applied to rendering systems.", "NeuroSonic.VideoOffset", -100, 100, 1),
    
    createComboEntry("Hi Speed Kind", "Which hi speed mode to use.", "NeuroSonic.HiSpeedModKind",
        { HiSpeedMod.Default, "Multiplier" }, { HiSpeedMod.MMod, "Mode BPM" }, { HiSpeedMod.CMod, "Constant BPM" }),

    createRangeEntry("Hi Speed (multiplier)", "The multiplier for relative scroll speed modes.", "NeuroSonic.HiSpeed", 0.1, 10, 0.05),
    createRangeEntry("Mod Speed (bpm)", "The base BPM to base absolute scroll speed modes on.", "NeuroSonic.ModSpeed", 25, 2000, 5),

    createTextEntry("Charts Directory", "Where NeuroSonic can should search for charts.", "theori.ChartsDirectory", function()
        theori.charts.setDatabaseToClean(function()
            theori.charts.setDatabaseToPopulate(function() print("Populate (from user config) finished."); end);
        end);
    end),

    --createRangeEntry("Laser Sensitivity", "The laser sensitivity while playing the game.", "NeuroSonic.LaserSensitivityGame", 0, 10, 0.05),
    
    createRangeEntry("Left Laser Color (Hue)", "The hue of the left laser graphics.", "NeuroSonic.Laser0Color", 0, 360, 5),
    createRangeEntry("Right Laser Color (Hue)", "The hue of the right laser graphics.", "NeuroSonic.Laser1Color", 0, 360, 5),

    createBindingEntry("Controller Toggle Key", "A keyboard key which toggles the controller input functionality.", function() return { { device = "keyboard", input = theori.config.get("NeuroSonic.ControllerToggle") } }; end, nil, function()
        setToListenForInput("Bind Controller Toggle Key.", function(device, input, axis)
            if (device ~= "keyboard") then
                notifyUser("Only a keyboard key may be used as a controller toggle binding.");
            end

            theori.config.set("NeuroSonic.ControllerToggle", input);
        end);
    end),
    
    createBindingEntry("Start Button", "The start button.", bGetBinds("start"), bSetBinds("start"), function() setToListenForInput("Bind `Start` Button.", setButtonBinding("start")); end),
    createBindingEntry("Back Button", "The back button.", bGetBinds("back"), bSetBinds("back"), function() setToListenForInput("Bind `Back` Button.", setButtonBinding("back")); end),
    
    createBindingEntry("BT-A", "The A button.", bGetBinds(0), bSetBinds(0), function() setToListenForInput("Bind `BT-A` Button.", setButtonBinding(0)); end),
    createBindingEntry("BT-B", "The B button.", bGetBinds(1), bSetBinds(1), function() setToListenForInput("Bind `BT-B` Button.", setButtonBinding(1)); end),
    createBindingEntry("BT-C", "The C button.", bGetBinds(2), bSetBinds(2), function() setToListenForInput("Bind `BT-C` Button.", setButtonBinding(2)); end),
    createBindingEntry("BT-D", "The D button.", bGetBinds(3), bSetBinds(3), function() setToListenForInput("Bind `BT-D` Button.", setButtonBinding(3)); end),
    
    createBindingEntry("FX-L", "The Left FX button.", bGetBinds(4), bSetBinds(4), function() setToListenForInput("Bind `FX-L` Button.", setButtonBinding(4)); end),
    createBindingEntry("FX-R", "The Right FX button.", bGetBinds(5), bSetBinds(5), function() setToListenForInput("Bind `FX-R` Button.", setButtonBinding(5)); end),
    
    createBindingEntry("Left Laser", "The +/- directions for the left laser.", aGetBinds(0), aSetBinds(0), function() setToListenForInput("Bind `Left Laser` Positive (or analog) Axis.", setAxisBinding(0, "Bind `Left Laser` Negative Axis.")); end),
    createBindingEntry("Right Laser", "The +/- directions for the right laser.", aGetBinds(1), aSetBinds(1), function() setToListenForInput("Bind `Right Laser` Positive (or analog) Axis.", setAxisBinding(1, "Bind `Right Laser` Negative Axis.")); end),
};

function theori.layer.doAsyncLoad()
    Layouts.Landscape.Background = theori.graphics.queueTextureLoad(bgName .. "_LS");
    Layouts.Portrait.Background = theori.graphics.queueTextureLoad(bgName .. "_PR");
    
    fontSlant = theori.graphics.getStaticFont("slant");

    return true;
end

function theori.layer.construct()
end

function theori.layer.destroy()
    print("Leaving user config screen.");
    enableSpecialHotkeys();
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
            if (option.kind == "range" or option.kind == "combo") then
                option:change(dir);
            elseif (option.kind == "binding") then
                bindingIndex = math.max(1, math.min(2, bindingIndex + dir));
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
            theori.config.save();
            controller.save();

            theori.graphics.closeCurtain(0.2, theori.layer.pop);
        elseif (button == "start") then
            if (controller.isDown(1)) then -- holding BT-B removes bindings instead of setting them
                local bindings = configOptions[configIndex]:getBindings();
                if (#bindings > 0) then
                    local bi = math.max(1, math.min(#bindings + 1, bindingIndex));
                    table.remove(bindings, bi);

                    configOptions[configIndex]:setBindings(bindings);
                end
            elseif (configOptions[configIndex].callback) then
                configOptions[configIndex].callback();
            end
        end
    end);

    theori.input.keyboard.pressed.connect(function(key)
        if (isListeningForInput or listenDebounce) then return; end

        if (key == KeyCode.ESCAPE) then
            theori.config.save();
            controller.save();

            theori.graphics.closeCurtain(0.2, theori.layer.pop);
        elseif (key == KeyCode.RETURN) then
            if (theori.input.keyboard.isDown(KeyCode.LCTRL) or theori.input.keyboard.isDown(KeyCode.RCTRL)) then
                local bindings = configOptions[configIndex]:getBindings();
                if (#bindings > 0) then
                    local bi = math.max(1, math.min(#bindings, bindingIndex));
                    table.remove(bindings, bi);

                    configOptions[configIndex]:setBindings(bindings);
                end
            elseif (configOptions[configIndex].callback) then
                configOptions[configIndex].callback();
            end
        elseif (key == KeyCode.LEFT) then
            local option = configOptions[configIndex];
            if (option.kind == "range" or option.kind == "combo") then
                option:change(-1);
            elseif (option.kind == "binding") then
                bindingIndex = math.max(1, math.min(2, bindingIndex - 1));
            end
        elseif (key == KeyCode.RIGHT) then
            local option = configOptions[configIndex];
            if (option.kind == "range" or option.kind == "combo") then
                option:change(1);
            elseif (option.kind == "binding") then
                bindingIndex = math.max(1, math.min(2, bindingIndex + 1));
            end
        elseif (key == KeyCode.UP) then
            configOptions[configIndex]:toggle();
            configIndex = 1 + (configIndex - 2) % #configOptions;
            configOptions[configIndex]:toggle();
        elseif (key == KeyCode.DOWN) then
            configOptions[configIndex]:toggle();
            configIndex = 1 + configIndex % #configOptions;
            configOptions[configIndex]:toggle();
        end
    end);

    theori.input.keyboard.pressedRaw.connect(function(key)
        if (isListeningForInput) then
            if (key == KeyCode.ESCAPE) then
                enableSpecialHotkeys();

                isListeningForInput = false;
                listenDebounce = true;
                onInputListened = nil;
                return;
            end

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
        if (opt.updateItem) then
            opt:updateItem(delta);
        end
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

            theori.graphics.setFillToColor(255 - 85 * linear, 255 - 45 * linear, 255, 255);

            theori.graphics.setFont(fontSlant);
            theori.graphics.setFontSize(itemHeight * 0.7);
            theori.graphics.setTextAlign(Anchor.BottomLeft);
            theori.graphics.fillString(opt.title, w * 0.025 + xOff * w * 0.02, yPos);
            local titleWidth, _ = theori.graphics.measureString(opt.title);
            
            theori.graphics.setFont(nil);
            theori.graphics.setFontSize(itemHeight * 0.25);
            theori.graphics.setTextAlign(Anchor.TopLeft);
            theori.graphics.fillString(opt.desc, w * 0.025 + w * 0.0075 * (1 + xOff * 0.7), yPos);
            local descWidth, _ = theori.graphics.measureString(opt.desc);

            local lineXPos = math.max(titleWidth + w * 0.025 + xOff * w * 0.02, descWidth + w * 0.025 + w * 0.0075 * (1 + xOff * 0.7));
            local lineWidth = w / 2 - lineXPos - 100;
            theori.graphics.fillRect(lineXPos + 20, yPos - 1, lineWidth, 2);

            if (opt.kind == "range") then
                local rangeWidth = w * 0.5;
                local rangeXPos = w * 0.975 - rangeWidth;
                
                local xRel, xRelZero = opt.value, -opt.min / (opt.max - opt.min);
                local pointXPos = rangeXPos + 3 + xRel * (rangeWidth - 6);
                local zeroXPos = rangeXPos + 3 + xRelZero * (rangeWidth - 6);

                if (string.find(opt.title, "Color")) then
                    local r, g, b = hue2rgb(opt:getValue() / 360);
                    theori.graphics.setFillToColor(r, g, b, 255);
                end

                theori.graphics.fillRect(rangeXPos, yPos - 3, rangeWidth, 6);
                theori.graphics.fillRect(pointXPos - 3, yPos - 13, 6, 10);
                if (xRelZero >= 0 and xRelZero <= 1) then
                    theori.graphics.fillRect(zeroXPos - 3, yPos + 3, 6, 3);
                end
                
                theori.graphics.setFont(nil);
                theori.graphics.setFontSize(16);
                theori.graphics.setTextAlign(Anchor.TopCenter);
                theori.graphics.fillString(math.floor(opt.step) == opt.step and tostring(opt:getValue()) or string.format("%.2f", opt:getValue()), pointXPos, yPos + 3);
            elseif (opt.kind == "combo") then
                local rangeWidth = w * 0.5;
                local xCenter = w * 0.975 - rangeWidth / 2;

                theori.graphics.setFont(fontSlant);
                theori.graphics.setFontSize(itemHeight * 0.8);
                theori.graphics.setTextAlign(Anchor.MiddleCenter);

                local optionWidth, _ = theori.graphics.measureString(opt.combos[opt.index][2]);
                theori.graphics.fillString(opt.combos[opt.index][2], xCenter, yPos);

                if (opt.index > 1) then
                    theori.graphics.fillRect(xCenter - optionWidth / 2 - 35, yPos - 5, 10, 10);
                end
                if (opt.index < #opt.combos) then
                    theori.graphics.fillRect(xCenter + optionWidth / 2 + 25, yPos - 5, 10, 10);
                end
            elseif (opt.kind == "text") then
                local boxWidth = w * 0.5;
                local boxXPos = w * 0.975 - boxWidth;
                local boxYPosBottom = yPos + itemHeight * 0.3;

                theori.graphics.fillRect(boxXPos, boxYPosBottom + 1, boxWidth, 1);

                theori.graphics.setFont(nil);
                theori.graphics.setFontSize(itemHeight * 0.8);
                theori.graphics.setTextAlign(Anchor.BottomLeft);
                theori.graphics.fillString(opt.title, boxXPos, boxYPosBottom - 1);
            elseif (opt.kind == "binding") then
                local rangeWidth = w * 0.5;
                local xCenter = w * 0.975 - rangeWidth / 2;
                
                local bindings = opt:getBindings();
                for j, binding in next, bindings do
                    if (j > 2) then
                        break;
                    end
                    
                    local xBindingWidth = rangeWidth / 4;
                    local xBindingCenter = xCenter - rangeWidth / 4 + (j - 1) * rangeWidth / 2;
                    
                    theori.graphics.setFont(nil);

                    local input, device;
                    if (binding.device == "keyboard") then
                        device = "Keyboard";
                        input = tostring(binding.input);
                    elseif (binding.device == "mouse") then
                        if (binding.input == "motion") then
                            device = "Mouse Motion";
                            input = tostring(binding.axis);
                        elseif (binding.input == "wheel") then
                        else
                            device = "Mouse Button";
                            input = tostring(binding.input);
                        end
                    else
                        device = binding.device.name .. " - " .. (binding.axis and "Axis" or "Button");
                        input = tostring(binding.axis and binding.axis or binding.input);
                    end
                    
                        theori.graphics.setFillToColor(255, 255, 255, 255);
                    if (i == configIndex and j == bindingIndex) then
                        theori.graphics.setFillToColor(255, 255, 127, 255);
                    end
                    
                    theori.graphics.setFontSize(itemHeight * 0.65);
                    theori.graphics.setTextAlign(Anchor.BottomCenter);
                    theori.graphics.fillString(input, xBindingCenter, yPos);
                
                    theori.graphics.setFontSize(itemHeight * 0.3);
                    theori.graphics.setTextAlign(Anchor.TopCenter);
                    theori.graphics.fillString(device, xBindingCenter, yPos);
                end

                if (opt.setBindings) then
                    for j = #bindings + 1, 2 do
                        local xBindingWidth = rangeWidth / 4;
                        local xBindingCenter = xCenter - rangeWidth / 4 + (j - 1) * rangeWidth / 2;
                    
                        if (i == configIndex and j == bindingIndex) then
                            theori.graphics.setFillToColor(127, 127, 50, 255);
                        else
                            theori.graphics.setFillToColor(127, 127, 127, 255);
                        end
                        theori.graphics.setFontSize(itemHeight * 0.65);
                        theori.graphics.setTextAlign(Anchor.Center);
                        theori.graphics.fillString("Not Bound", xBindingCenter, yPos);
                    end
                end
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

