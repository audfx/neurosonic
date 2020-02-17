
local textInput = { };

local text = "";

theori.input.textInput.connect(function(composition)
	if (not theori.input.isTextInputActive()) then return; end
	text = text .. composition;
end);

theori.input.keyboard.pressed.connect(function(key)
	if (not theori.input.isTextInputActive()) then return; end

	if (key == KeyCode.BACKSPACE) then
		if (#text == 0) then return; end
		text = string.sub(text, 1, #text - 1);
	else
		return;
	end
end);

function textInput.start(startText)
	if (theori.input.isTextInputActive()) then return; end
	
	text = startText or "";
	theori.input.startTextEditing();
end

function textInput.stop()
	if (not theori.input.isTextInputActive()) then return; end

	text = "";
	theori.input.stopTextEditing();
end

function textInput.isActive()
	return theori.input.isTextInputActive();
end

function textInput.getText()
	return text;
end

return textInput;
