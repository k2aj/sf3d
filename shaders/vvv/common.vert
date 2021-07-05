#version 330 core

layout(location=0) in vec3 position;
layout(location=1) in vec3 normal;
layout(location=2) in vec2 diffuseUv;
layout(location=3) in vec2 specularUv;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec2 invAtlasSize;

out vec3 vPosition;
out vec3 vNormal;
out vec2 vDiffuseUv;
out vec2 vSpecularUv;

void main()
{
    vec3 worldPosition = (model * vec4(position, 1.0)).xyz;
    gl_Position = projection * view * vec4(worldPosition, 1.0);

    vDiffuseUv = diffuseUv * invAtlasSize;
    vSpecularUv = specularUv * invAtlasSize;
    vNormal = mat3(transpose(inverse(model))) * normal;
    vPosition = worldPosition;
}