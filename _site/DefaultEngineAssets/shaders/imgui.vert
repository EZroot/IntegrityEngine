#version 330 core

layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in vec4 in_color;

uniform mat4 projection;

out vec2 frag_uv;
out vec4 frag_color;

void main()
{
    frag_uv = in_uv;
    frag_color = in_color;
    // Apply the projection matrix to transform from screen coordinates to clip space
    gl_Position = projection * vec4(in_position.xy, 0.0, 1.0);
}