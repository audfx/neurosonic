#version 330
#extension GL_ARB_separate_shader_objects : enable

layout (location = 1) in vec2 frag_TexCoord;
layout (location = 0) out vec4 target;

uniform sampler2D MainTexture;

uniform float Glow;
uniform int GlowState;

void main()
{	
	float x = float(GlowState) * 0.25;
	vec4 mainColor = texture(MainTexture, vec2(frag_TexCoord.x * 0.25 + x, frag_TexCoord.y));
	target = mainColor;
}
