#version 330
#extension GL_ARB_separate_shader_objects : enable

layout (location = 1) in vec2 frag_TexCoord;
layout (location = 0) out vec4 target;

uniform sampler2D MainTexture;

uniform vec3 LeftColor;
uniform vec3 RightColor;
uniform float Hidden;

void main()
{	
	vec4 mainColor = texture(MainTexture, vec2(frag_TexCoord.x, 0.75));
	vec4 replaceColor = texture(MainTexture, vec2(frag_TexCoord.x, 0.25));
	
    vec4 col = mainColor;

    if(frag_TexCoord.y > Hidden * 0.6)
    {
		vec4 overlay = vec4(0, 0, 0, replaceColor.w);
		
        //Red channel to color right lane
        overlay.xyz += vec3(.9) * RightColor * vec3(replaceColor.x);

        //Blue channel to color left lane
        overlay.xyz += vec3(.9) * LeftColor * vec3(replaceColor.z);

        //Color green channel white
        overlay.xyz += vec3(.6) * vec3(replaceColor.y);
		
		float alpha = max(col.w, overlay.w);
		col = vec4(col.xyz * (1 - overlay.w) + overlay.xyz * overlay.w, alpha);
    }
    else
    {
        col.xyz = vec3(0);
        col.a = col.a > 0.0 ? 0.3 : 0.0;
    }
	
    target = col;
}