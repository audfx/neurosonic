
local globalTimer = 0

function AsyncLoad()
	return true;
end

function AsyncFinalize()
	return true;
end

function Init()
end


function Update(delta, total)
    globalTimer = globalTimer + delta;
end

function Draw()
	g2d.SetColor(255, 0, 0, 255);
	g2d.FillRect(10, 10, 255, 255);
end
