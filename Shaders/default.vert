#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoord;

layout (location = 2) in vec4 instModelCol1;
layout (location = 3) in vec4 instModelCol2;
layout (location = 4) in vec4 instModelCol3;
layout (location = 5) in vec4 instModelCol4;

layout (location = 6) in vec4 uvLocation;

out vec2 TexCoord;

uniform mat4 projection;
void main()
{
    mat4 instModel = mat4(
        instModelCol1,
        instModelCol2,
        instModelCol3,
        instModelCol4
    );

    gl_Position = projection * instModel * vec4(aPos, 0.0, 1.0);
    TexCoord = uvLocation.xy + aTexCoord * uvLocation.zw;
}
