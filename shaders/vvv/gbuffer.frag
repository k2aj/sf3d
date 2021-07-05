#version 330 core

in vec2 vDiffuseUv;
in vec2 vSpecularUv;
in vec3 vNormal;
in vec3 vPosition;

uniform sampler2D atlas;

layout(location=0) out vec3 fDiffuse;
layout(location=1) out vec4 fSpecular;
layout(location=2) out vec3 fNormal;
layout(location=3) out vec3 fPosition;

void main()
{
    //fDiffuse = vDiffuse;
    fDiffuse = texture(atlas, vDiffuseUv).rgb;
    fSpecular = vec4(texture(atlas, vSpecularUv).rgb, 0.5);

    //fSpecular = vSpecular;
    fNormal = normalize(vNormal)*0.5+0.5;
    fPosition = vPosition;
}