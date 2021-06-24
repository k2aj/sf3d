#version 330 core
vec3 brdf(
    vec3 lightDirection, vec3 toCamera, vec3 surfaceNormal,
    vec3 diffuseColor, vec3 specularColor, float specularExponent
) {
    vec3 halfway = normalize(toCamera - lightDirection);
    vec3 specularComponent = pow(max(dot(halfway, surfaceNormal), 0.0), specularExponent) * specularColor;
    return diffuseColor + specularComponent;
}