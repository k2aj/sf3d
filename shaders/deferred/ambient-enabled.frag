#version 330 core

uniform vec3 ambientLightColor;

vec3 addAmbient(vec3 radiance, vec3 ambientColor) {
    return ambientLightColor * ambientColor + radiance;
}