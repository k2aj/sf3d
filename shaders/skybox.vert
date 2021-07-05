#version 330 core

layout(location=0) in vec3 position;

out vec3 vPosition;

uniform mat4 model, view, projection;

void main()
{
    gl_Position = projection*view*model*vec4(position,1.0);
    vPosition = position;
}