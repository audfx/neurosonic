#version 330
#extension GL_ARB_separate_shader_objects : enable

layout (location = 1) in vec2 frag_TexCoord;
layout (location = 0) out vec4 target;

uniform sampler2D MainTexture;

uniform vec3 LaserColor;
uniform vec3 HiliteColor;

uniform float Glow;
uniform int GlowState;

vec3 rgb2hsv(vec3 c)
{
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c)
{
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

void main()
{	
	float x = float(GlowState) * 0.25;

	vec3 s = texture(MainTexture, vec2(frag_TexCoord.x * 0.25 + x, frag_TexCoord.y)).rgb;
	
	vec3 hsvLaser = rgb2hsv(LaserColor);
	
	vec3 hsvBase = vec3(hsvLaser.x + (1 - s.g) * 15.0 / 360, 1, 1);

	float m = abs((hsvLaser.x - 0.3));
	vec3 hsvHilite = vec3(mod(m * 0.5, 1), 1, 1);

	vec3 baseColor = hsv2rgb(hsvBase);
	vec3 hiliteColor = hsv2rgb(hsvHilite);

	vec3 color = s.g * (1 - s.r * 0.5) * baseColor + s.r * hiliteColor * 0.6;

	target = vec4(color, 1);
}