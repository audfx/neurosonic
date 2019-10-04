
function nsc.layer.doAsyncLoad()
    nsc.graphics.loadStaticTextureAsync("title");
    nsc.graphics.loadStaticTextureAsync("audfx-text-large");

    return nsc.graphics.doStaticTextureLoadsAsync();
end

function nsc.layer.doAsyncFinalize()
    return nsc.graphics.finalizeStaticTextureLoads();
end

-- on first startup, push the splash screen
function nsc.layer.init()
    --nsc.charts.setDatabaseToPopulate();
    nsc.layer.push("splashScreen");
end

-- any time someone gets back down to the driver, push a new splash screen
function nsc.layer.resumed()
    nsc.openCurtain();
    nsc.layer.push("splashScreen");
end
