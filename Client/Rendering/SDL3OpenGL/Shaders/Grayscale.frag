#version 330 core

in vec2 vTex;
in vec4 vCol;

uniform sampler2D uTexture;

out vec4 FragColor;

void main()
{
    vec4 texColor = texture(uTexture, vTex);
    float gray = dot(texColor.rgb, vec3(0.299, 0.587, 0.114));
    FragColor = vec4(gray * vCol.rgb, texColor.a);
}
