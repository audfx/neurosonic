
local titleLoop;

function nsc.layer.doAsyncLoad()
    nsc.graphics.queueStaticTextureLoad("title");
    nsc.graphics.queueStaticTextureLoad("audfx-text-large");

    titleLoop = nsc.audio.queueStaticAudioLoad("launchtower-title-loop");

    return nsc.doStaticLoadsAsync();
end

function nsc.layer.doAsyncFinalize()
    return nsc.finalizeStaticLoads();
end

-- on first startup, push the splash screen
function nsc.layer.init()
    local titleLoopBeatDuration = 60.0 / 132;
    
    titleLoop.volume = 0.7;
    titleLoop.setLoopArea(titleLoopBeatDuration * 4, titleLoopBeatDuration * 68);

    --[ [
    nsc.charts.setDatabaseToClean(function()
        nsc.charts.setDatabaseToPopulate(function() print("Populate (from driver) finished."); end);
    end);
    --]]

    nsc.layer.push("splashScreen");
end

-- any time someone gets back down to the driver, push a new splash screen
function nsc.layer.resumed()
    nsc.openCurtain();
    nsc.layer.push("splashScreen");
end
