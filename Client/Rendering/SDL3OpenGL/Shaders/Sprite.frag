#version 330 core

in vec2 vTex;
in vec4 vCol;

uniform sampler2D uTexture;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(uTexture, vTex);
    FragColor = texColor * vCol;
}
