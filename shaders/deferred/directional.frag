#version 330 core

uniform vec3 lightColor;
uniform vec3 ambientLightColor;
uniform vec3 lightDirection;

vec3 light(vec3 position, out vec3 irradiance, out vec3 ambientLight)
{
    irradiance = lightColor;
    ambientLight = ambientLightColor;
    return lightDirection;
}