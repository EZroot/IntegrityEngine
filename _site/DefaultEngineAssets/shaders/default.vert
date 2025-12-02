#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoord;

// Per-instance attributes for the Model Matrix
// location = 2, 3, 4, 5 are used for the 4 vec4 columns of the mat4
layout (location = 2) in vec4 instModelCol1;
layout (location = 3) in vec4 instModelCol2;
layout (location = 4) in vec4 instModelCol3;
layout (location = 5) in vec4 instModelCol4;

out vec2 TexCoord;

// Reconstruct the Model matrix from the columns


uniform mat4 projection;
void main()
{
    mat4 instModel = mat4(
        instModelCol1,
        instModelCol2,
        instModelCol3,
        instModelCol4
    );

    // Apply the per-instance model matrix and then the projection matrix
    gl_Position = projection * instModel * vec4(aPos, 0.0, 1.0);
    TexCoord = aTexCoord;
}
