#version 330 core

in vec2 vTex;
in vec4 vCol;

uniform vec2 uImgMin;
uniform vec2 uImgMax;
uniform float uShadowSize;
uniform float uMaxAlpha;
uniform float uViewportHeight;

out vec4 FragColor;

void main()
{
    // gl_FragCoord.y is bottom-up in OpenGL; flip to match top-down screen coords
    vec2 position = vec2(gl_FragCoord.x, uViewportHeight - gl_FragCoord.y);

    float distLeft = uImgMin.x - position.x;
    float distTop = uImgMin.y - position.y;
    float distRight = position.x - uImgMax.x;
    float distBottom = position.y - uImgMax.y;

    float shadowDistance = max(max(distLeft, distTop), max(distRight, distBottom));

    if (shadowDistance <= 0.0)
        discard;

    float alpha = clamp(1.0 - shadowDistance / max(uShadowSize, 0.0001), 0.0, 1.0) * uMaxAlpha;
    FragColor = vec4(0.0, 0.0, 0.0, alpha);
}
