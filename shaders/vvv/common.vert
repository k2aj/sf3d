#version 330 core

layout(location=0) in vec3 position;
layout(location=1) in vec2 uv;
layout(location=2) in vec3 normal;
layout(location=3) in vec3 diffuse;
layout(location=4) in vec4 specular;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec2 uvOffset;
uniform vec2 uvScale;

out vec3 vDiffuse;
out vec4 vSpecular;
out vec3 vNormal;
out vec3 vPosition;
out vec2 vUv;

void main()
{
    vec3 worldPosition = (model * vec4(position, 1.0)).xyz;
    gl_Position = projection * view * vec4(worldPosition, 1.0);

    vDiffuse = diffuse;
    vSpecular = specular;
    vNormal = mat3(transpose(inverse(model))) * normal;
    vPosition = worldPosition;
    vUv = uv*uvScale+uvOffset;
}