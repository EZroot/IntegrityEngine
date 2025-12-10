#version 330 core
out vec4 FragColor;

in vec2 TexCoord;
in vec4 TintColor; 

uniform sampler2D textureSampler;

void main()
{
    vec4 textureColor = texture(textureSampler, TexCoord);
    FragColor = textureColor * TintColor;
    
    if (FragColor.a < 0.01) {
        discard;
    }
}