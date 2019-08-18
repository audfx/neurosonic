
local State;

local TransitionTime = 0.25;

local IntroTimer;
local OutroTimer;

local AnimTimer = 1.0;

function AsyncLoad()
	return true;
end

function AsyncFinalize()
	return true;
end

function Init()
	State = "Intro";

	IntroTimer = TransitionTime;
	OutroTimer = TransitionTime;
end

-- Return true while intro running, false otherwise.
function CheckIntro()
	return State == "Intro";
end

-- Return true while outro running, false otherwise.
function CheckOutro()
	return State == "Outro";
end

function TriggerOutro()
	State = "Outro";
end

function Update(delta, total)
	if (State == "Intro") then
		IntroTimer = IntroTimer - delta;
		AnimTimer = math.clamp(IntroTimer / TransitionTime, 0, 1);
		
		if (IntroTimer <= 0) then
			State = "Idle";
		end
	elseif (State == "Outro") then
		OutroTimer = OutroTimer - delta;
		AnimTimer = 1 - math.clamp(OutroTimer / TransitionTime, 0, 1);

		if (OutroTimer <= 0) then
			State = "Ended";
		end
	end
end

function Draw()
	local width, height = g2d.GetViewportSize();
	local originx, originy = width / 2, height / 2;

	local bgRotation = (1 - AnimTimer) * 45;
	local bgDist = (width / 2) * AnimTimer;
	local bgWidth = (width / 2) * 2;
	local bgHeight = height * 4;

	g2d.SaveTransform();
	g2d.Rotate(bgRotation);
	g2d.Translate(originx, originy);
	
	g2d.SetColor(255, 0, 255, 255);
	g2d.FillRect(bgDist, -bgHeight / 2, bgWidth, bgHeight);
	g2d.SetColor(0, 169, 255, 255);
	g2d.FillRect(-bgDist - bgWidth, -bgHeight / 2, bgWidth, bgHeight);

	g2d.RestoreTransform();
end
