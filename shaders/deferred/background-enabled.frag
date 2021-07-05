#version 330 core

vec3 applyBackground(vec3 backgroundColor, vec3 lightingResult, float mixingFactor)
{
    return mix(backgroundColor, lightingResult, mixingFactor);
}