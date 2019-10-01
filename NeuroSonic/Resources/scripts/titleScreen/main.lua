
local menuIndex = 0;
local menuItems = { count = 0, items = { } };

local exitOnLeave = true;

local function AddMenuItem(text, callback)
    menuItems.items[menuItems.count] = {
        text = text,
        callback = callback
    };
    menuItems.count = menuItems.count + 1;
end

function nsc.layer.construct(...)
    local args = { ... };
    if (#args > 0 and args[1]) then
        exitOnLeave = false;
    end
end

function nsc.layer.doAsyncLoad()
    return true;
end

function nsc.layer.doAsyncFinalize()
    return true;
end

function nsc.layer.init()
    AddMenuItem("This is a Test!", function()
        nsc.closeCurtain(0.2, function()
            if (not exitOnLeave) then
                nsc.layer.setInvalidForResume();
            end
            nsc.layer.push("titleScreen.main", true);
        end);
    end);
    AddMenuItem("What about Chart Select??", function() print("Chart Select pressed!"); end);
    AddMenuItem("These are useless!!", function() print("Useless pressed!"); end);

    nsc.openCurtain();

    nsc.input.controller.axisTicked:connect(function(axis, dir)
        menuIndex = (menuIndex + dir + menuItems.count) % menuItems.count;
        print(menuIndex);
    end);

    nsc.input.controller.pressed:connect(function(button)
        if (button == ControllerInput.Back) then
            nsc.closeCurtain(0.2, exitOnLeave and nsc.exit or nsc.layer.pop);
        elseif (button == ControllerInput.Start) then
            menuItems.items[menuIndex].callback();
        end
    end);
end

function nsc.layer.update(delta, total)
end

function nsc.layer.render()
    for k, v in next, menuItems.items do
        local selected = k == menuIndex;

        nsc.graphics.setColor(255, 255, selected and 0 or 255, 255);
        nsc.graphics.drawString(v.text, 10, 10 + k * 30);
    end
end
