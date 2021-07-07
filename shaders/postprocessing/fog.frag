#version 330 core

in vec2 uv;

uniform samplerCube cubemap;
uniform sampler2D tex; //zbuffer

uniform mat4 invViewProj;
uniform mat4 invModel;
uniform vec3 cameraPosition;
uniform vec2 fogRadii;
uniform vec3 fogColor;

#define FOG_RADIUS_MIN fogRadii.x   //Distance from camera at which fog starts
#define FOG_RADIUS_MAX fogRadii.y   //Distance from camera at which fog completely hides terrain

out vec4 fragColor;

void main()
{
    // Reconstruct world space position of fragment using zBuffer
    vec4 clipSpacePos = vec4(
        uv*2-1,
        texture(tex, uv).x*2-1,
        1
    );
    vec4 homogenousPos = invViewProj * clipSpacePos;
    vec3 fWorldPos = homogenousPos.xyz / homogenousPos.w;

    vec3 displacement = fWorldPos - cameraPosition;
    vec3 cubemapTexCoord = (invModel*vec4(displacement,1.0)).xyz;

    // Apply fog color below the camera & skybox above the camera
    float bgStrength = max(dot(normalize(displacement), vec3(0,1,0)), 0);
    vec3 bgFogColor = mix(fogColor, texture(cubemap, cubemapTexCoord).rgb, bgStrength);

    // Fog/background intensifies as distance from camera increases
    float dist = length(displacement);
    float fogStrength = clamp((dist - FOG_RADIUS_MIN) / (FOG_RADIUS_MAX - FOG_RADIUS_MIN), 0, 1);
    fragColor = vec4(bgFogColor, fogStrength); //apply fog using hardware blending
}