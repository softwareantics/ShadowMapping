#version 460

layout (location = 0) in vec2 in_texCoord;

layout (location = 0) out vec4 out_color;

uniform sampler2D depthMap;

void main()
{             
    float depthValue = texture(depthMap, in_texCoord).r;
    out_color = vec4(vec3(depthValue), 1.0);
}

