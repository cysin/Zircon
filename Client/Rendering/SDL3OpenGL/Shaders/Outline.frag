#version 330 core

in vec2 vTex;
in vec4 vCol;

uniform sampler2D uTexture;
uniform vec4 uOutlineColor;
uniform vec2 uTextureSize;
uniform float uOutlineThickness;
uniform vec4 uSourceUV; // xy = min, zw = max

out vec4 FragColor;

vec4 SampleSprite(vec2 uv)
{
    if (any(lessThan(uv, uSourceUV.xy)) || any(greaterThan(uv, uSourceUV.zw)))
        return vec4(0.0);
    return texture(uTexture, uv);
}

void main()
{
    vec2 texelSize = 1.0 / uTextureSize;

    vec4 texColor = SampleSprite(vTex) * vCol;
    float alpha = texColor.a;

    bool hasNeighbour = false;
    float minDist = uOutlineThickness + 1.0;
    int radius = int(ceil(uOutlineThickness));

    for (int x = -radius; x <= radius; ++x)
    {
        for (int y = -radius; y <= radius; ++y)
        {
            if (x == 0 && y == 0)
                continue;

            vec2 offset = vec2(float(x), float(y)) * texelSize;
            float nAlpha = SampleSprite(vTex + offset).a;

            if (nAlpha > 0.05)
            {
                hasNeighbour = true;
                float d = length(vec2(float(x), float(y)));
                minDist = min(minDist, d);
            }
        }
    }

    if (alpha <= 0.05 && hasNeighbour)
    {
        float falloff = (uOutlineThickness <= 1.0) ? 0.0 :
            clamp((minDist - 1.0) / max(1.0, uOutlineThickness - 1.0), 0.0, 1.0);
        float outlineAlpha = mix(1.0, 0.5, falloff);
        FragColor = vec4(uOutlineColor.rgb, outlineAlpha);
    }
    else
    {
        FragColor = vec4(0.0);
    }
}
