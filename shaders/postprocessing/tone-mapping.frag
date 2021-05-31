#version 330 core

in vec2 uv;

uniform sampler2D tex;
uniform float exposure = 1.0;

out vec3 fColor;

void main()
{
    const float r_gamma = 1.0/2.2;
    vec3 color = texture(tex, uv).rgb;
    fColor = pow(vec3(1.0) - exp(-color*exposure), vec3(r_gamma));
    //fColor = color/(vec3(1.0)+color);
}