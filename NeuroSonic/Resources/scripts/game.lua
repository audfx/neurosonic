
-- controls chart playback (with highway params),
--  audio playback and judgement
local gameSystem;
-- renderable highway
local highway;
-- background is another lua script
local background;

function nsc.layer.construct()
    local chart = nsc.charts.loadChart();
    gameSystem = nsc.game.newGameSystem(chart);
    --highway = nsc.graphics.newHighway();
    highway = gameSystem.createHighway();
    background = include("game/backgrounds/stars");
end

function nsc.layer.doAsyncLoad()
    if (not gameSystem.doAsyncLoad()) then
        return false;
    end

    if (not highway.doAsyncLoad()) then
        return false;
    end

    if (not background.doAsyncLoad()) then
        return false;
    end

    return true;
end

function nsc.layer.doAsyncFinalize()
    if (not gameSystem.doAsyncFinalize()) then
        return false;
    end

    if (not background.doAsyncFinalize()) then
        return false;
    end

    if (not highway.doAsyncFinalize()) then
        return false;
    end

    return true;
end

function nsc.layer.init()
    gameSystem.init();
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

    gameSystem.update(delta, total);

    gameSystem.applyToHighway(highway);
    gameSystem.applyToBackground(background);

    highway.update(delta, total);
    background.horizonHeight = highway.horizonHeight;

    background.update(delta, total);
end

function nsc.layer.render()
    background.render();
    nsc.graphics.flush();

    highway.render();
end
