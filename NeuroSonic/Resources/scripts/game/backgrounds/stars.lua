
local CenterpieceTex;
local ParticlesTex = { };

local ParticleSpawners = { };
local Particles = { };

local function ShallowCopy(value)
	if (type(value) == "table") then
		local result = { };
		for k, v in next, value do
			result[k] = v;
		end
		return result;
	end
	
	return value;
end

local function DeepCopy(value)
	if (type(value) == "table") then
		local result = { };
		for k, v in next, value do
			result[DeepCopy(k)] = DeepCopy(v);
		end
		return result;
	end
	
	return value;
end

local function CreateParticleSpawner(frequency, spawnOffset, texture, size, posx, posy, distance, lifetime)
	local spawner = {
		Frequency = frequency,
		Timer = frequency + spawnOffset,

		Prototype = {
			Texture = texture,
			Size = size,
			Position = { X = posx, Y = posy },
			MaxDistance = distance,
			Distance = distance,
			Lifetime = lifetime,
		},

		SpawnParticle = function(self)
			return DeepCopy(self.Prototype);
		end,
	};
	return spawner;
end

function asyncLoad()
	CenterpieceTex = theori.graphics.queueTextureLoad("game_bg/centerpiece");
	ParticlesTex[1] = theori.graphics.queueTextureLoad("game_bg/particle0");
	ParticlesTex[2] = theori.graphics.queueTextureLoad("game_bg/particle1");
	ParticlesTex[3] = theori.graphics.queueTextureLoad("game_bg/particle2");

	return true;
end

function asyncFinalize()
	return true;
end

function initialize()
	for i = 1, 2 do
		local side = (i == 1 and -1 or 1);
		
		table.insert(ParticleSpawners, CreateParticleSpawner(0.50, 0.35, ParticlesTex[1],  95, side * 0.35, -0.35, 20, 1.00));
		table.insert(ParticleSpawners, CreateParticleSpawner(0.65, 0.00, ParticlesTex[1], 125, side * 0.50,  0.55, 18, 1.00));
		
		table.insert(ParticleSpawners, CreateParticleSpawner(0.65, 0.15, ParticlesTex[2], 120, side * 0.75,  0.95, 22, 1.00));
		
		table.insert(ParticleSpawners, CreateParticleSpawner(0.55, 0.35, ParticlesTex[3], 110, side * 0.70,  0.75, 18, 0.90));
		table.insert(ParticleSpawners, CreateParticleSpawner(0.55, 0.20, ParticlesTex[3],  90, side * 0.90,  1.25, 20, 1.00));
		table.insert(ParticleSpawners, CreateParticleSpawner(0.55, 0.05, ParticlesTex[3],  75, side * 0.80, -0.15, 22, 1.10));
		
		for i = 1, 10 do
			local y = -3.0 + (i - 1) * 0.6;
			local x = -4.0 - 0.5 * (5 - i);
			if (i % 2 == 0) then
				x = x + 0.35;
			end

			table.insert(ParticleSpawners, CreateParticleSpawner(0.7, (i % 3) * 0.15, ParticlesTex[3], 85, side * x, y, 25, 2.00 - (i % 4) * 0.1));
		end
	end
end

function update(delta, total)
	for _, spawner in next, ParticleSpawners do
		spawner.Timer = spawner.Timer - delta;
		if (spawner.Timer <= 0) then
			spawner.Timer = spawner.Frequency;

			local particle = spawner:SpawnParticle();
			table.insert(Particles, particle);
		end
	end
	
	for k, particle in next, Particles do
		particle.Distance = particle.Distance - delta * (particle.MaxDistance / particle.Lifetime);
		if (particle.Distance <= 0) then
			table.remove(Particles, k);
		end
	end
end

function render()
	local width, height = theori.graphics.getViewportSize();
	local originx, originy = width / 2, height * horizonHeight;

	local centerSize = width * 0.6;

	--local spins = -math.min(1, spinTimer * 2) * 720 - math.min(1, swingTimer * 2) * 360;

	theori.graphics.saveTransform();
	theori.graphics.rotate(-combinedTilt * 0.25);
	theori.graphics.translate(originx, originy);

	theori.graphics.setFillToTexture(CenterpieceTex, 255, 255, 255, 255);
	theori.graphics.fillRect(-centerSize / 2, -centerSize * 0.8, centerSize, centerSize);

	theori.graphics.restoreTransform();

	theori.graphics.saveTransform();
	theori.graphics.rotate(-combinedTilt * 0.25);
	theori.graphics.translate(originx, originy);

	for _, particle in next, Particles do
		local posx = width * (particle.Position.X / particle.Distance);
		local posy = height * (particle.Position.Y / particle.Distance);
		local size = particle.Size / particle.Distance;
		
		local alpha = 1;
		if (particle.Distance > particle.MaxDistance - 4) then	
			alpha = 1 - math.min(4, particle.Distance - (particle.MaxDistance - 4)) / 4;
		elseif (particle.Distance < 4) then
			alpha = particle.Distance / 4;
		end

		theori.graphics.setFillToTexture(particle.Texture, 255, 255, 255, math.floor(255 * alpha));
		theori.graphics.fillRect(posx - size / 2, posy - size / 2, size, size);
	end
	
	theori.graphics.restoreTransform();
end
