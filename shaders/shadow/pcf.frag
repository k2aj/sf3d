#version 330 core

uniform mat4 shadowViewProjection;
uniform sampler2D shadowMap;
uniform float shadowBias = 0.02;

vec3 applyShadow(vec3 irradiance, vec3 position)
{
    vec2 uvOffset = vec2(1.0)/textureSize(shadowMap,0);
    float numInShadow = 0;

    for(int x=-2; x<=2; ++x)
        for(int y=-2; y<=2; ++y)
        {
            vec4 lightSpacePosition = shadowViewProjection * vec4(position, 1.0);
            vec3 pos = lightSpacePosition.xyz / lightSpacePosition.w * 0.5 + 0.5;
            float shadowDepth = texture(shadowMap, pos.xy + uvOffset*vec2(x,y)).r;
            numInShadow += ((shadowDepth + shadowBias < pos.z) ? 1.0 : 0.0);
        }
    return irradiance*(1-numInShadow/25.0);
}