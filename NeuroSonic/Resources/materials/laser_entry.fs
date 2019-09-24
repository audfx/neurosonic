#version 330
#extension GL_ARB_separate_shader_objects : enable

layout (location = 1) in vec2 frag_TexCoord;
layout (location = 0) out vec4 target;

uniform sampler2D MainTexture;

uniform vec3 LaserColor;
uniform vec3 HiliteColor;

uniform float Glow;
uniform int GlowState;

void main()
{	
	vec4 overlay = texture(MainTexture, vec2(frag_TexCoord.x * 0.5, frag_TexCoord.y));
	vec3 s = texture(MainTexture, vec2(0.5 + frag_TexCoord.x * 0.5, frag_TexCoord.y)).rgb;
	vec3 color = overlay.rgb * overlay.a + mix(s.g * LaserColor, HiliteColor, s.r) * (1 - overlay.a);

	target = vec4(color, 1);
}