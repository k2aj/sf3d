#version 330 core

uniform vec4 attenuation;
uniform vec3 lightColor;
uniform vec3 ambientLightColor;
uniform mat4 model;

#define K_LINEAR attenuation.x
#define K_QUADRATIC attenuation.y
#define K_BIAS attenuation.z

vec3 light(vec3 position, out vec3 irradiance, out vec3 ambientLight)
{
    vec3 lightPosition = model[3].xyz;
    vec3 displacement = position - lightPosition;
    float a = max(0.0, 1.0 / (1.0 + K_LINEAR*length(displacement) + K_QUADRATIC*dot(displacement,displacement)) - K_BIAS);
    irradiance = a*lightColor;
    ambientLight = a*ambientLightColor;
    return normalize(displacement);
}