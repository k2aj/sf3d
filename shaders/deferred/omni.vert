#version 330 core

layout(location=0) in vec3 position;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform float zNear;

noperspective out vec2 uv;

void main()
{
    vec4 eyeSpacePosition = view * model * vec4(position, 1.0);
    // Project the vertices which are in front of near clipping plane into that plane
    // (a little bit of bias is added because when z=zNear the vertices are still clipped away)
    vec4 clipSpacePosition = projection * vec4(eyeSpacePosition.xy, min(eyeSpacePosition.z, -zNear-0.001), eyeSpacePosition.w);
    gl_Position = clipSpacePosition;
    uv = clipSpacePosition.xy*0.5/clipSpacePosition.w+0.5;
}