#version 330 core

layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aTex;
layout(location = 2) in vec4 aCol;

uniform mat4 uMatrix;

out vec2 vTex;
out vec4 vCol;

void main()
{
    gl_Position = vec4(aPos, 0.0, 1.0) * uMatrix;
    vTex = aTex;
    vCol = aCol;
}
