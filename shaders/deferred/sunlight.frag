#version 330 core

in vec2 uv;

uniform sampler2D diffuseMap;
uniform sampler2D specularMap;
uniform sampler2D normalMap;
uniform sampler2D positionMap;

uniform sampler2D shadowMap;
uniform mat4 shadowView;
uniform mat4 shadowProjection;
uniform float shadowBias = 0.01;

uniform vec3 lightDirection;
uniform vec3 lightColor;
uniform vec3 ambientLightColor;
uniform vec3 cameraPosition;

out vec3 fragColor;

float inShadow(vec3 position)
{
    vec2 uvOffset = vec2(1.0)/textureSize(shadowMap,0);
    float numInShadow = 0;

    for(int x=-2; x<=2; ++x)
        for(int y=-2; y<=2; ++y)
        {
            vec4 lightSpacePosition = shadowProjection * shadowView * vec4(position, 1.0);
            vec3 pos = lightSpacePosition.xyz / lightSpacePosition.w * 0.5 + 0.5;
            float shadowDepth = texture(shadowMap, pos.xy + uvOffset*vec2(x,y)).r;
            numInShadow += ((shadowDepth + shadowBias < pos.z) ? 1.0 : 0.0);
        }
    return numInShadow/25.0;
}

vec3 phong(vec3 diffuseColor, vec3 specularColor, float specularExponent, vec3 normal, vec3 position)
{
    vec3 diffuseComponent = max(ambientLightColor, dot(-lightDirection, normal)*lightColor) * diffuseColor;

    vec3 toCamera = normalize(cameraPosition - position);
    vec3 reflectedDir = reflect(lightDirection, normal);
    vec3 specularComponent = pow(max(dot(reflectedDir, toCamera), 0), specularExponent) * lightColor * specularColor;

    return mix(
        diffuseComponent + specularComponent, 
        diffuseColor*ambientLightColor, 
        inShadow(position)
    );
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
        clamp(phong(diffuseColor,specularColor,specularExponent,normal,position),0.0,1000.0), 
        useLighting
    );
}