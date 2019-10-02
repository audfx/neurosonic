
-- chart data, audio, and judgement
local chart, audio, judge;
-- playback for visuals and for judgements
local playbackVisual, playbackInput;
-- renderable highway and its parameters
local highway, highwayParams;
-- background is another lua script
local background;

function nsc.layer.construct()
    background = include("game/backgrounds/stars");
end

function nsc.layer.doAsyncLoad()
    if (not background.doAsyncLoad()) then
        return false;
    end

    highway = nsc.game.newHighway();
    if (not highway.doAsyncLoad()) then
        return false;
    end

    --[[
    highwayParams = nsc.game.newHighwayParams();
    if (not highwayParams.doAsyncLoad()) then
        return false;
    end
    --]]

    return true;
end

function nsc.layer.doAsyncFinalize()
    if (not background.doAsyncFinalize()) then
        return false;
    end

    if (not highway.doAsyncFinalize()) then
        return false;
    end
    
    --[[
    if (not highwayParams.doAsyncFinalize()) then
        return false;
    end
    --]]

    return true;
end

function nsc.layer.init()
    background.init();

    nsc.openCurtain();

    nsc.input.controller.pressed:connect(function(button)
        if (button == ControllerInput.Back) then
            nsc.closeCurtain(0.2, nsc.layer.pop);
        end
    end);
end

function nsc.layer.update(delta, total)
    local w, h = nsc.graphics.getViewportSize();

    if (w > h) then
        highway.setViewport((w - h * 0.95) / 2, 0, h * 0.95);
    else
        highway.setViewport(0, (h - w) / 2 - w / 5, w);
    end

    highway.update(delta, total);

    background.horizonHeight = highway.horizonHeight;
    background.combinedTilt = 0;
    background.effectRotation = 0;
    background.spinTimer = 0;
    background.swingTimer = 0;

    background.update(delta, total);
end

function nsc.layer.render()
    background.render();
    nsc.graphics.flush();

    highway.render();
end
