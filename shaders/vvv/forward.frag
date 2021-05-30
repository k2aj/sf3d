#version 330 core

in vec3 vDiffuse;
in vec3 vSpecular;
in vec3 vNormal;
in vec3 vPosition;

uniform vec3 ambientLightColor;
uniform vec3 lightDirection;
uniform vec3 lightColor;
uniform vec3 cameraPosition;

out vec3 fragColor;

void main() 
{
    vec3 norm = normalize(vNormal);
    vec3 dirToCamera = normalize(cameraPosition - vPosition);
    
    vec3 diffuseLight = max(ambientLightColor, dot(-lightDirection,norm) * lightColor) ;

    vec3 reflectedDir = reflect(-lightDirection, norm);
    vec3 specularLight = pow(max(dot(reflectedDir, dirToCamera), 0), 32) * lightColor * 0.5;

    fColor = diffuseLight * vDiffuse + specularLight * vDiffuse;
}