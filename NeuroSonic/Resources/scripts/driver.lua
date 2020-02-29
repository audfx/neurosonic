
local titleLoop;

local fontSlant;

function theori.layer.doAsyncLoad()
    theori.graphics.queueStaticTextureLoad("title");
    theori.graphics.queueStaticTextureLoad("audfx-text-large");

    theori.graphics.queueStaticTextureLoad("legend/bt/a");
    theori.graphics.queueStaticTextureLoad("legend/bt/b");
    theori.graphics.queueStaticTextureLoad("legend/bt/c");
    theori.graphics.queueStaticTextureLoad("legend/bt/d");
    theori.graphics.queueStaticTextureLoad("legend/fx/l");
    theori.graphics.queueStaticTextureLoad("legend/fx/r");
    theori.graphics.queueStaticTextureLoad("legend/fx/lr");
    theori.graphics.queueStaticTextureLoad("legend/start");
    
	theori.graphics.queueStaticTextureLoad("badges/Perfect");
	theori.graphics.queueStaticTextureLoad("badges/S");
	theori.graphics.queueStaticTextureLoad("badges/AAAX");
	theori.graphics.queueStaticTextureLoad("badges/AAA");
	theori.graphics.queueStaticTextureLoad("badges/AAX");
	theori.graphics.queueStaticTextureLoad("badges/AA");
	theori.graphics.queueStaticTextureLoad("badges/AX");
	theori.graphics.queueStaticTextureLoad("badges/A");
	theori.graphics.queueStaticTextureLoad("badges/B");
	theori.graphics.queueStaticTextureLoad("badges/C");
	theori.graphics.queueStaticTextureLoad("badges/F");

    titleLoop = theori.audio.queueStaticAudioLoad("launchtower-title-loop");
    
    fontSlant = theori.graphics.createStaticFont("slant");

    return theori.doStaticLoadsAsync();
end

function theori.layer.doAsyncFinalize()
    --[[
    local loadChars = " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789~`!@#$%^&*()_=[{]}\\|:;\"'<,>.?/";
    local scales = { 8, 12, 16, 24, 32 };
    for _, scale in next, scales do
        fontSlant.preFlattenToScales(loadChars, scales);
    end
    --]]

    return theori.finalizeStaticLoads();
end

-- on first startup, push the splash screen
function theori.layer.init()
	-- ensure that even if there are 0 charts in the collection, the
	--  Favorites collection always exists.
	theori.charts.createCollection("Favorites");

    local titleLoopBeatDuration = 60.0 / 132;
    
    titleLoop.volume = 0.3;
    titleLoop.setLoopArea(titleLoopBeatDuration * 4, titleLoopBeatDuration * 68);

    -- TODO(local): this needs to be readily callable, ideally
    theori.charts.setDatabaseToClean(function()
        theori.charts.setDatabaseToPopulate(function() print("Populate (from driver) finished."); end);
    end);

    --theori.layer.push("chartSelect");
    --theori.layer.push("multiplayer");
    theori.layer.push("splashScreen");
    --theori.graphics.closeCurtain(0.25, function() theori.layer.push("userConfig"); end);
    --theori.graphics.closeCurtain(0.25, function() theori.layer.push("chartSelect"); end);
end

-- any time someone gets back down to the driver, push a new splash screen
function theori.layer.resumed()
    theori.graphics.openCurtain();
    theori.layer.push("splashScreen");
end
