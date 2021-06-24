#version 330 core

uniform vec3 lightColor;
uniform vec3 lightDirection;

vec3 light(vec3 position, out vec3 direction)
{
    direction = lightDirection;
    return lightColor;
}