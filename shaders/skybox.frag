#version 330 core

in vec3 vPosition;
uniform samplerCube cubemap;

layout(location=0) out vec3 fDiffuse;
layout(location=1) out vec4 fSpecular;
layout(location=2) out vec3 fNormal;
layout(location=3) out vec3 fPosition;

void main()
{
    fDiffuse = texture(cubemap, vPosition).rgb;
    fSpecular = vec4(0.0,0.0,0.0,0.25);
    fNormal = vec3(0.5); //remember 0.5 in fbo = 0 in normal vector
    fPosition = vec3(0.0);
}