
local function outBack(t, b, c, d, s)
  if not s then s = 1.70158 end
  t = t / (d or 1) - 1
  return c * (t * t * ((s + 1) * t + s) + 1) + b
end

function linearToggle()
	return {
		alpha = 1, from = 0, to = 0,

		toggle = function(self)
			self.from = self:sample();
			self.to = 1 - self.to;
			self.alpha = 0;
		end,

		update = function(self, delta)
			self.alpha = math.min(1, self.alpha + delta);
		end,

		sample = function(self) return outBack(self.alpha, self.from, (self.to - self.from)); end,
		sampleLinear = function(self) return self.from + self.alpha * (self.to - self.from); end,
	};
end
