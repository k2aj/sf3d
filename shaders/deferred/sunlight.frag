#version 330 core

in vec2 uv;

uniform sampler2D diffuseMap;
uniform sampler2D specularMap;
uniform sampler2D normalMap;
uniform sampler2D positionMap;

uniform vec3 lightDirection;
uniform vec3 lightColor;
uniform vec3 ambientLightColor;
uniform vec3 cameraPosition;

out vec3 fragColor;

vec3 phong(vec3 diffuseColor, vec3 specularColor, float specularExponent, vec3 normal, vec3 position)
{
    vec3 diffuseComponent = max(ambientLightColor, dot(-lightDirection, normal)*lightColor) * diffuseColor;

    vec3 toCamera = normalize(cameraPosition - position);
    vec3 reflectedDir = reflect(lightDirection, normal);
    vec3 specularComponent = pow(max(dot(reflectedDir, toCamera), 0), specularExponent) * specularColor;

    return diffuseComponent + specularComponent;
}

void main() 
{
    //Extract data from gbuffer
    vec3 diffuseColor = texture(diffuseMap,uv).rgb;

    vec4 rawSpecular = texture(specularMap,uv);
    vec3 specularColor = rawSpecular.rgb;
    float specularExponent = rawSpecular.a*255.0;

    vec3 normal = texture(normalMap,uv).xyz*2.0-1.0;
    vec3 position = texture(positionMap,uv).xyz;

    //If normal vector=0, discard results from lighting and render diffuse instead   
    float useLighting = dot(normal,normal); 
    fragColor = mix(
        diffuseColor, 
        clamp(phong(diffuseColor,specularColor,specularExponent,normal,position),0.0,1.0), 
        useLighting
    );
}