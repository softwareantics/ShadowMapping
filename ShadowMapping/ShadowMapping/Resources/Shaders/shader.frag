#version 460

layout (location = 0) in vec3 in_color;
layout (location = 1) in vec2 in_texCoord;

layout (location = 0) out vec4 out_color;

uniform sampler2D u_texture;

void main()
{
    out_color = texture(u_texture, in_texCoord).rgba * vec4(in_color, 1.0);
}
