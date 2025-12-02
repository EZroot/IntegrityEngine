#version 330 core

in vec2 frag_uv;
in vec4 frag_color;

uniform sampler2D textureSampler;

out vec4 out_color;

void main()
{
    // Sample the font/texture and multiply by the vertex color
    out_color = frag_color * texture(textureSampler, frag_uv);
}