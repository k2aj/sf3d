#version 330 core

in vec2 uv;

uniform sampler2D tex;
uniform sampler2D bloomMap;
uniform float exposure;

out vec3 fColor;

void main()
{
    vec3 color = texture(tex, uv).rgb + texture(bloomMap, uv).rgb;
    //fColor = vec3(1.0) - exp(-color*exposure);
    //fColor = pow(color / (vec3(1.0) + color), vec3(0.75));
    fColor = color / (vec3(1.0) + color);
}