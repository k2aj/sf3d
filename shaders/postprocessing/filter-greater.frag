#version 330 core

in vec2 uv;

uniform sampler2D tex;
uniform float threshold;

out vec4 fColor;

void main()
{
    fColor = max(texture(tex,uv)-threshold, vec4(0.0));
}