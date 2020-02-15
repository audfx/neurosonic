
local textInput = { };

function textInput.new()
	local result = setmetatable({ }, { __index = textInput });
	result.text = "";
	result.focused = false;
	return result;
end

function textInput.start(self)
	self.focused = true;
end

function textInput.stop(self)
	self.focused = false;
end

return textInput;
