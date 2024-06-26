#ifndef RADIUS_CURSOR_DECALS_H

#define RADIUS_CURSOR_DECALS_H

struct RadiusCursorDecal
{
    vec2 BottomLeftCornerPosition;
    float Diameter;
    uint DecalTextureIndex;
    vec3 _Padding;
    float Opacity;
};

layout(set = PASS_CONSTANTS_RESOURCE_SET, binding = 7) uniform texture2DArray RadiusCursorDecalTextures;

layout(set = PASS_CONSTANTS_RESOURCE_SET, binding = 8) uniform sampler RadiusCursorDecalSampler;

layout(set = PASS_CONSTANTS_RESOURCE_SET, binding = 9) uniform RadiusCursorDecalConstants
{
    vec3 _Padding;
    uint NumRadiusCursorDecals;
} _RadiusCursorDecalConstants;

layout(std430, set = 1, binding = 10) readonly buffer RadiusCursorDecals
{
    RadiusCursorDecal _RadiusCursorDecals[];
};

vec3 GetRadiusCursorDecalColor(vec3 worldPosition)
{
    vec3 result = vec3(0, 0, 0);

    for (int i = 0; i < _RadiusCursorDecalConstants.NumRadiusCursorDecals; i++)
    {
        // Can't do this because SPIRV-Cross doesn't support it yet:
        // RadiusCursorDecal decal = _RadiusCursorDecals[i];

        uint decalTextureIndex = _RadiusCursorDecals[i].DecalTextureIndex;
        vec2 decalBottomLeftPosition = _RadiusCursorDecals[i].BottomLeftCornerPosition;
        float decalDiameter = _RadiusCursorDecals[i].Diameter;
        float decalOpacity = _RadiusCursorDecals[i].Opacity;

        float decalU = (worldPosition.x - decalBottomLeftPosition.x) / decalDiameter;
        float decalV = (worldPosition.y - decalBottomLeftPosition.y) / decalDiameter;

        vec2 decalUV = vec2(decalU, 1 - decalV);

        vec4 decalColor = texture(
            sampler2DArray(RadiusCursorDecalTextures, RadiusCursorDecalSampler),
            vec3(decalUV, decalTextureIndex));

        result += decalColor.xyz * decalColor.a * decalOpacity;
    }

    return result;
}

#endif