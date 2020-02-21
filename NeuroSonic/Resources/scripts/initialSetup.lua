
local fontSlant;

local function createChoice(name, callback)
end

local choices = {
    createChoice("CONFIG", function()
        theori.craphics.closeCurtain(0.2, function() theori.layer.push("userConfig"); end);
    end),

    createChoice("CONTINUE", function()
        theori.craphics.closeCurtain(0.25, function() theori.layer.push("titleScreen"); end);
    end),
};

function theori.layer.resume()
    theori.graphics.openCurtain();
end

function theori.layer.init()
    fontSlant = theori.graphics.getStaticFont("slant");
    
    theori.graphics.openCurtain();
end

function theori.layer.update(delta, total)
end

function theori.layer.render()
end
