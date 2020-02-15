
local textInput = { };

function textInput.new()
	local result = setmetatable({ }, { __index = textInput });
	result.text = "";
	result.focused = false;
	return result;
end

function textInput.begin(self)
	self.focused = true;
end

function textInput.end(self)
	self.focused = false;
end

return textInput;
