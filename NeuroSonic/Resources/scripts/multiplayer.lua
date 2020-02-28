
include "layerLayout";
include "linearToggle";
include "util.textures";

local textInput = include "util.textInput";

local bgName = "bgHighContrast";

--------------------------------------------------
-- Layer Data ------------------------------------
--------------------------------------------------
local currentState = 0;
local stateData = {
	[0] = { },
	create = { },
	rooms = { },
	roomsPassword = { },
	room = { },
};

local function invokeState(fnName, ...)
	local state = stateData[currentState];
	if (fnName == "setRooms") then print(state[fnName]); end
	if (state[fnName]) then
		state[fnName](state, ...);
	end
end

local function setState(nextState, ...)
	if (not stateData[nextState]) then
		print("Invalid state", nextState);
		return;
	end
	
	currentState = nextState;
	invokeState("init", ...);
end
--------------------------------------------------


--------------------------------------------------
-- Textures --------------------------------------
--------------------------------------------------
local textures =
{
	noise = { },
	numbers = { },
	legend = { },
	badges = { },
	chartFrame = { },
	infoPanel =
	{
		landscape = { },
		portrait = { },
	},
};

local currentNoiseTexture;
--------------------------------------------------


--------------------------------------------------
-- Audio -----------------------------------------
--------------------------------------------------
local audio =
{
	clicks = { },
};
--------------------------------------------------


local tcp;

local function pop()
	if (tcp) then
		tcp.close();
		tcp = nil;
	end

	theori.graphics.closeCurtain(0.25, theori.layer.pop);
end

local function playerName()
	return "NeuroSonic Client";
end

function theori.layer.doAsyncLoad()
    Layouts.Landscape.Background = theori.graphics.queueTextureLoad(bgName .. "_LS");
    Layouts.Portrait.Background = theori.graphics.queueTextureLoad(bgName .. "_PR");

    textures.cursor = theori.graphics.queueTextureLoad("chartSelect/cursor");
    textures.cursorOuter = theori.graphics.queueTextureLoad("chartSelect/cursorOuter");
    textures.levelBadge = theori.graphics.queueTextureLoad("chartSelect/levelBadge");
    textures.levelBadgeBorder = theori.graphics.queueTextureLoad("chartSelect/levelBadgeBorder");
    textures.levelBar = theori.graphics.queueTextureLoad("chartSelect/levelBar");
    textures.levelBarBorder = theori.graphics.queueTextureLoad("chartSelect/levelBarBorder");
    textures.levelText = theori.graphics.queueTextureLoad("chartSelect/levelText");

	local frameTextures = { "background", "border", "fill", "storageDevice", "trackDataLabel" };
	for _, texName in next, frameTextures do
		textures.chartFrame[texName] = theori.graphics.queueTextureLoad("chartSelect/chartFrame/" .. texName);
	end
	
	local infoPanelPortrait = { "border", "fill", "jacketBorder" };
	for _, texName in next, infoPanelPortrait do
		textures.infoPanel.portrait[texName] = theori.graphics.queueTextureLoad("chartSelect/infoPanel/portrait/" .. texName);
	end

    textures.noJacket = theori.graphics.queueTextureLoad("chartSelect/noJacket");
    textures.noJacketOverlay = theori.graphics.queueTextureLoad("chartSelect/noJacketOverlay");

    textures.infoPanel.landscape.background = theori.graphics.queueTextureLoad("chartSelect/landscapeInfoPanelBackground");

    textures.infoPanel.portrait.tempBackground = theori.graphics.queueTextureLoad("chartSelect/tempPortraitInfoPanelBackground");

	audio.clicks.primary = theori.audio.queueAudioLoad("chartSelect/click0");

    return true;
end

function theori.layer.doAsyncFinalize()
	tcp = theori.net.createTcp("usc-multi.drewol.me", 39079);
	if (not tcp) then return true; end

	tcp.listenForTopic("server.rooms", function(data) invokeState("setRooms", data.rooms); end);
	tcp.listenForTopic("server.room.joined", function(data) invokeState("joinRoom", data.room); end);
	tcp.listenForTopic("room.update", function(data) invokeState("updateRoom", data); end);

	tcp.sendLine('{"topic":"user.auth","password":"","name":"Test NeuroSonic Client","version":"v0.17"}');
    
    fontSlant = theori.graphics.getStaticFont("slant");

	getLegends(textures.legend);
	getLegends(textures.badges);

	return true;
end

function theori.layer.init()
	if (not tcp) then
		pop();
		return;
	end

	tcp.startReceiving();

	theori.input.keyboard.pressed.connect(function(key) invokeState("keyPressed", key); end);
	theori.input.controller.pressed.connect(function(controller, button) invokeState("controllerPressed", controller, button); end);
	theori.input.controller.axisTicked.connect(function(controller, axis, dir) invokeState("controllerAxisTicked", controller, axis, dir); end);

	theori.graphics.openCurtain();
end

function theori.layer.resumed()
	theori.graphics.openCurtain();
end

function theori.layer.update(delta, total)
	if (not tcp) then
		return;
	end

	tcp.process();

    --Layout.Update(delta, total);
	invokeState("update", delta, total);
end

function theori.layer.render()
    Layout.CheckLayout();
    Layout.DoTransform();

    Layout.Render();
end

-- Landscape

function Layouts.Landscape.Render(self)
	invokeState("renderLandscape");
end

function Layouts.WideLandscape.Render(self)
	Layouts.WideLandscape.Render(self);
end

-- Portrait

function Layouts.Portrait.Render(self)
	invokeState("renderPortrait");
end

function Layouts.TallPortrait.Render(self)
	Layouts.Portrait.Render(self);
end

--------------------------------------------------
-- Default State ---------------------------------
--------------------------------------------------

local defaultState = stateData[0];

function defaultState.keyPressed(self, key)
	if (key == KeyCode.ESCAPE) then
		pop();
	end
end

function defaultState.controllerPressed(self, controller, button)
	if (button == "back") then
		pop();
	end
end

function defaultState.controllerAxisTicked(self, controller, axis, dir)
end

function defaultState.setRooms(self, rooms)
	stateData.rooms.rooms = rooms;
	setState("rooms");
end

function defaultState.renderLandscape(self)
    Layout.DrawBackgroundFilled(Layouts.Landscape.Background);
end

function defaultState.renderPortrait(self)
    Layout.DrawBackgroundFilled(Layouts.Portrait.Background);
end

--------------------------------------------------
-- Create Room State -----------------------------
--------------------------------------------------

local createState = stateData.create;

createState.index = 3;
createState.numOptions = 3;

function createState.back(self)
	if (textInput.isActive()) then
		textInput.stop();
	end
	setState("rooms");
end

function createState.select(self)
	if (self.index == 1) then
		textInput.start(self.roomName);
	elseif (self.index == 2) then
		textInput.start(self.password);
	else
		local fields = '"name":"' .. self.roomName .. '"';
		if (self.password and #self.password > 0) then
			fields = fields .. ',"password":"' .. self.password .. '"';
		end
		tcp.sendLine('{"topic":"server.room.new",' .. fields .. '}');
	end
end

function createState.init(self)
	self.roomName = playerName() .. "'s Room";
	self.password = nil;

	self.index = 3; -- 1 is name, 2 is password, 3 is create
end

function createState.keyPressed(self, key)
	if (textInput.isActive()) then
		if (key == KeyCode.ESCAPE or key == KeyCode.RETURN) then
			textInput.stop();
		end
	else
		if (key == KeyCode.ESCAPE) then
			self:back();
		elseif (key == KeyCode.RETURN) then
			self:select();
		elseif (key == KeyCode.UP) then
			self.index = 1 + ((self.index - 1) + -1) % self.numOptions;
		elseif (key == KeyCode.DOWN) then
			self.index = 1 + ((self.index - 1) +  1) % self.numOptions;
		end
	end
end

function createState.controllerPressed(self, controller, button)
	if (textInput.isActive()) then
		if (button == "back" or button == "start") then
			textInput.stop();
		end
	else
		if (button == "back") then
			self:back();
		elseif (button == "start") then
			self:select();
		elseif (button == 4) then
			self.index = 1;
			textInput.start(self.roomName);
		elseif (button == 5) then
			self.index = 2;
			textInput.start(self.password or "");
		end
	end
end

function createState.controllerAxisTicked(self, controller, axis, dir)
	if (not textInput.isActive()) then
		self.index = 1 + ((self.index - 1) + dir) % self.numOptions;
	end
end

function createState.joinRoom(self, room)
	stateData.room.room = room;
	setState("room");
end

function createState.update(self, delta, total)
	if (textInput.isActive()) then
		local text = textInput.getText();
		if (self.index == 1) then
			self.roomName = text;
		elseif (self.index == 2) then
			self.password = text;
		end
	end
end

function createState.renderLandscape(self)
    Layout.DrawBackgroundFilled(Layouts.Landscape.Background);

	local tbMargin = LayoutHeight * 0.1;

	theori.graphics.setFillToColor(0, 0, 0, 150);
	theori.graphics.fillRect(0, 0, LayoutWidth, tbMargin);
	theori.graphics.fillRect(0, LayoutHeight - tbMargin, LayoutWidth, tbMargin);

	local textBoxWidth = LayoutWidth * 0.5;
	local textBoxHeight = textBoxWidth * 0.05;

	if (self.index == 1) then
		if (textInput.isActive()) then
			theori.graphics.setFillToColor(90, 90, 160, 255);
		else
			theori.graphics.setFillToColor(127, 127, 127, 255);
		end
	else
		theori.graphics.setFillToColor(70, 70, 70, 255);
	end
	theori.graphics.fillRoundedRect((LayoutWidth - textBoxWidth) / 2, LayoutHeight * 0.4, textBoxWidth, textBoxHeight, textBoxHeight / 2);
	
	if (self.index == 2) then
		if (textInput.isActive()) then
			theori.graphics.setFillToColor(90, 90, 160, 255);
		else
			theori.graphics.setFillToColor(127, 127, 127, 255);
		end
	else
		theori.graphics.setFillToColor(70, 70, 70, 255);
	end
	theori.graphics.fillRoundedRect((LayoutWidth - textBoxWidth) / 2, LayoutHeight * 0.6 - textBoxHeight, textBoxWidth, textBoxHeight, textBoxHeight / 2);

	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.setFont(nil);
	theori.graphics.setFontSize(textBoxHeight);
	theori.graphics.setTextAlign(Anchor.MiddleCenter);

	theori.graphics.fillString(self.roomName or "", LayoutWidth / 2, LayoutHeight * 0.4 + textBoxHeight * 0.4);
	theori.graphics.fillString(self.password or "", LayoutWidth / 2, LayoutHeight * 0.6 - textBoxHeight * 0.6);
	
	if (self.index == 3) then
		theori.graphics.setFillToColor(127, 127, 127, 255);
	else
		theori.graphics.setFillToColor(70, 70, 70, 255);
	end
	theori.graphics.fillRoundedRect(LayoutWidth * 0.35, LayoutHeight * 0.7, LayoutWidth * 0.3, LayoutWidth * 0.05, LayoutWidth * 0.0125);
	
	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.setFontSize(LayoutWidth * 0.04);
	theori.graphics.fillString("Create Room", LayoutWidth / 2, LayoutHeight * 0.7 + LayoutWidth * 0.02);

	local helpSize = tbMargin * 0.8;
	local helpTextSize = tbMargin - helpSize;

	theori.graphics.setFillToTexture(textures.legend.l, 255, 255, 255, 255);
	theori.graphics.fillRect(LayoutWidth / 2 - helpSize * 3.5, LayoutHeight - tbMargin, helpSize, helpSize);

	theori.graphics.setFillToTexture(textures.legend.start, 255, 255, 255, 255);
	theori.graphics.fillRect(LayoutWidth / 2 - helpSize * 0.5, LayoutHeight - tbMargin, helpSize, helpSize);

	theori.graphics.setFillToTexture(textures.legend.r, 255, 255, 255, 255);
	theori.graphics.fillRect(LayoutWidth / 2 + helpSize * 2.5, LayoutHeight - tbMargin, helpSize, helpSize);

	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.setFont(nil);
	theori.graphics.setFontSize(helpTextSize);
	theori.graphics.setTextAlign(Anchor.BottomCenter);
	theori.graphics.fillString("Edit Name", LayoutWidth / 2 - helpSize * 3, LayoutHeight - tbMargin * 0.1);
	theori.graphics.fillString("Select", LayoutWidth / 2, LayoutHeight - tbMargin * 0.1);
	theori.graphics.fillString("Edit Password", LayoutWidth / 2 + helpSize * 3, LayoutHeight - tbMargin * 0.1);
end

function createState.renderPortrait(self)
    Layout.DrawBackgroundFilled(Layouts.Portrait.Background);
end

--------------------------------------------------
-- Room State ------------------------------------
--------------------------------------------------

local roomsState = stateData.rooms;

roomsState.rooms = { };
roomsState.index = 1;

function roomsState.back(self)
	pop();
end

function roomsState.select(self)
	if (not self.rooms or #self.rooms == 0) then
		return;
	end

	local room = self.rooms[self.index];

	local json = '{"topic":"server.room.join","id":"%s","password":"%s"}';
	tcp.sendLine(string.format(json, room.id, ""));
end

function roomsState.keyPressed(self, key)
	if (key == KeyCode.ESCAPE) then
		self:back();
	elseif (key == KeyCode.RETURN) then
		self:select();
	elseif (button == KeyCode.C) then
		setState("create");
	end
end

function roomsState.controllerPressed(self, controller, button)
	if (button == "back") then
		self:back();
	elseif (button == "start") then
		self:select();
	elseif (button == 4) then
		setState("create");
	end
end

function roomsState.controllerAxisTicked(self, controller, axis, dir)
	self.index = 1 + ((self.index - 1) + dir) % #self.rooms;
end

function roomsState.setRooms(self, rooms)
	local currentRoomUuid = (self.rooms and #self.rooms > 0) and self.rooms[self.index];
	stateData.rooms.rooms = rooms;
	
	self.index = 1;
	if (currentRoomUuid and rooms) then
		for i, v in next, rooms do
			if (v.id == currentRoomUuid) then
				self.index = i;
			end
		end
	end
end

function roomsState.joinRoom(self, room)
	stateData.room.room = room;
	setState("room");
end

function roomsState.drawRooms(self, x, y, w, h)
	if (not self.rooms or #self.rooms == 0) then
		return;
	end

	theori.graphics.saveScissor();
	theori.graphics.scissor(x * LayoutScale, y * LayoutScale, w * LayoutScale, h * LayoutScale);

	local cx = x + w / 2;
	local cy = y + h / 2;

	local optWidth = w;
	local optHeight = w * 0.2;

	for i, v in next, self.rooms do
		if (i == self.index) then
			theori.graphics.setFillToColor(170, 210, 255, 255);
		else
			theori.graphics.setFillToColor(255, 255, 255, 255);
		end

		theori.graphics.setFont(nil);
		theori.graphics.setFontSize(optHeight * 0.45);
		theori.graphics.setTextAlign(Anchor.BottomCenter);
		theori.graphics.fillString(v.name, cx, y + (i + 0.55 - 1) * optHeight);
		
		local desc = string.format("%d/%d - %s", v.current, v.max, v.ingame and "In Game" or "Selecting Song");

		theori.graphics.setFontSize(optHeight * 0.2);
		theori.graphics.setTextAlign(Anchor.TopCenter);
		theori.graphics.fillString(desc, cx, y + (i + 0.55 - 1) * optHeight);
	end

	theori.graphics.restoreScissor();
end

function roomsState.renderLandscape(self)
    Layout.DrawBackgroundFilled(Layouts.Landscape.Background);

	local tbMargin = LayoutHeight * 0.1;

	self:drawRooms(LayoutWidth * 0.3, tbMargin, LayoutWidth * 0.4, LayoutHeight - 2 * tbMargin);

	theori.graphics.setFillToColor(0, 0, 0, 150);
	theori.graphics.fillRect(0, 0, LayoutWidth, tbMargin);
	theori.graphics.fillRect(0, LayoutHeight - tbMargin, LayoutWidth, tbMargin);

	local helpSize = tbMargin * 0.8;
	local helpTextSize = tbMargin - helpSize;

	theori.graphics.setFillToTexture(textures.legend.l, 255, 255, 255, 255);
	theori.graphics.fillRect(LayoutWidth / 2 - helpSize * 3.5, LayoutHeight - tbMargin, helpSize, helpSize);

	theori.graphics.setFillToTexture(textures.legend.start, 255, 255, 255, 255);
	theori.graphics.fillRect(LayoutWidth / 2 - helpSize * 0.5, LayoutHeight - tbMargin, helpSize, helpSize);

	theori.graphics.setFillToTexture(textures.legend.r, 255, 255, 255, 255);
	theori.graphics.fillRect(LayoutWidth / 2 + helpSize * 2.5, LayoutHeight - tbMargin, helpSize, helpSize);

	theori.graphics.setFillToColor(255, 255, 255, 255);
	theori.graphics.setFont(nil);
	theori.graphics.setFontSize(helpTextSize);
	theori.graphics.setTextAlign(Anchor.BottomCenter);
	theori.graphics.fillString("Create Room", LayoutWidth / 2 - helpSize * 3, LayoutHeight - tbMargin * 0.1);
	theori.graphics.fillString("Join Room", LayoutWidth / 2, LayoutHeight - tbMargin * 0.1);
	theori.graphics.fillString("Something Else", LayoutWidth / 2 + helpSize * 3, LayoutHeight - tbMargin * 0.1);
end

function roomsState.renderPortrait(self)
    Layout.DrawBackgroundFilled(Layouts.Portrait.Background);

	self:drawRooms(0, LayoutHeight * 0.25, LayoutWidth, LayoutHeight * 0.5);
end

--------------------------------------------------
-- Room State ------------------------------------
--------------------------------------------------

local roomState = stateData.room;
roomState.room = { };
roomState.chart = nil;
roomState.chartInfo = nil;
roomState.players = { };
roomState.host = nil;
roomState.owner = nil;
roomState.hardMode = false;
roomState.mirrorMode = false;
roomState.doRotate = false;
roomState.startGameSoon = false;

function roomState.back(self)
	tcp.sendLine('{"topic":"room.leave"}');
	setState("rooms");
	tcp.sendLine('{"topic":"server.rooms"}');
end

function roomState.select(self)
end

function roomState.keyPressed(self, key)
	if (key == KeyCode.ESCAPE) then
		self:back();
	elseif (key == KeyCode.RETURN) then
		self:select();
	end
end

function roomState.controllerPressed(self, controller, button)
	if (button == "back") then
		self:back();
	elseif (button == "start") then
		self:select();
	end
end

function roomState.controllerAxisTicked(self, controller, axis, dir)
end

function roomState.updateRoom(self, data)
	self.players = data.users;
	self.chartInfo = { folderName = data.song, diffIndex = data.diff, level = data.level, hash = data.hash };
	self.host = data.host;
	self.owner = data.owner or data.host;
	self.hardMode = data.hard_mode;
	self.mirrorMode = data.mirror_mode;
	self.doRotate = data.do_rotate;
	self.startGameSoon = data.start_soon;
end

function roomState.drawSelectedChart(self, x, y, w, h)
end

function roomState.drawPlayerList(self, x, y, w, h)
	local titleHeight = h * 0.1;
	local itemHeight = (h - titleHeight) / 8;

	theori.graphics.setFillToColor(255, 255, 255, 255);

	theori.graphics.setFont(fontSlant);
	theori.graphics.setFontSize(titleHeight);
	theori.graphics.setTextAlign(Anchor.BottomLeft);
	theori.graphics.fillString("Players", x + w * 0.1, y + titleHeight * 0.85);

	if (self.players) then
		for i, v in next, self.players do
			local playerWidth;
			if (v.ready) then
				playerWidth = w * 0.7;
				theori.graphics.setFillToColor(50, 200, 50, 255);
			elseif (v.missing_map) then
				playerWidth = w * 0.5;
				theori.graphics.setFillToColor(160, 80, 70, 255);
			else
				playerWidth = w * 0.6;
				theori.graphics.setFillToColor(127, 127, 127, 255);
			end

			theori.graphics.fillRoundedRectVarying(x, y + titleHeight + (i - 1) * itemHeight, playerWidth, itemHeight,
				0, itemHeight * 0.1, itemHeight * 0.1, 0);
			
			if (v.id == self.owner) then
				theori.graphics.setFillToColor(220, 200, 50, 255);
			elseif (v.id == self.host) then
				theori.graphics.setFillToColor(220, 220, 220, 255);
			else
				theori.graphics.setFillToColor(100, 100, 100, 255);
			end
			theori.graphics.fillRoundedRectVarying(x + playerWidth, y + titleHeight + (i - 0.9) * itemHeight, w - playerWidth, itemHeight * 0.8,
				0, itemHeight * 0.15, itemHeight * 0.15, 0);
				
			theori.graphics.setFillToColor(255, 255, 255, 255);

			local fontSize = itemHeight * 0.9;
			theori.graphics.setFont(fontSlant);
			theori.graphics.setFontSize(fontSize);
			theori.graphics.setTextAlign(Anchor.MiddleLeft);

			local nameWidth, _ = theori.graphics.measureString(v.name);
			if (nameWidth > playerWidth - w * 0.02) then
				fontSize = fontSize * (playerWidth - w * 0.02) / nameWidth;
				theori.graphics.setFontSize(fontSize);
			end

			theori.graphics.fillString(v.name, x + w * 0.01, y + titleHeight + (i - 0.5) * itemHeight - (itemHeight - fontSize * 0.75) / 2);
		end
	end
end

function roomState.renderLandscape(self)
    Layout.DrawBackgroundFilled(Layouts.Landscape.Background);

	local tbMargin = LayoutHeight * 0.1;

	theori.graphics.setFillToColor(0, 0, 0, 150);
	theori.graphics.fillRect(0, 0, LayoutWidth, tbMargin);
	theori.graphics.fillRect(0, LayoutHeight - tbMargin, LayoutWidth, tbMargin);

	theori.graphics.setFillToColor(255, 255, 255, 255);

	theori.graphics.setFont(fontSlant);
	theori.graphics.setFontSize(tbMargin);
	theori.graphics.setTextAlign(Anchor.BottomLeft);
	theori.graphics.fillString(self.room.name, tbMargin * 0.1, tbMargin * 0.9);

	local songPanelSize = LayoutHeight - 2 * tbMargin;
	self:drawPlayerList(0, tbMargin, LayoutWidth - songPanelSize, LayoutHeight - 2 * tbMargin);
	self:drawSelectedChart(LayoutWidth - songPanelSize, tbMargin, songPanelSize, LayoutHeight - 2 * tbMargin);
end

function roomState.renderPortrait(self)
    Layout.DrawBackgroundFilled(Layouts.Portrait.Background);
end
