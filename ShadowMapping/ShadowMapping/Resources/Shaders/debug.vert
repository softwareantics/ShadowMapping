#version 460

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec2 in_texCoord;

layout (location = 0) out vec2 out_texCoord;

void main()
{
    out_texCoord = in_texCoord;
    gl_Position = vec4(in_position, 1.0);
}

