#version 330 core

in vec3 vDiffuse;
in vec4 vSpecular;
in vec3 vNormal;
in vec3 vPosition;

layout(location=0) out vec3 fDiffuse;
layout(location=1) out vec4 fSpecular;
layout(location=2) out vec3 fNormal;
layout(location=3) out vec3 fPosition;

void main()
{
    fDiffuse = vDiffuse;
    //fSpecular = vSpecular;
    fSpecular = vec4(1.0, 1.0, 1.0, 0.5);
    fNormal = normalize(vNormal)*0.5+0.5;
    fPosition = vPosition;
}