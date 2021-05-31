#version 330 core

in vec2 uv;

uniform sampler2D tex;

uniform float kernel[32];
uniform int kernelLength;
uniform vec2 kernelOffset;
uniform vec2 kernelStep;

out vec4 fColor;

void main()
{
    vec4 sum = vec4(0.0);
    for(int i=0; i<kernelLength; ++i)
        sum += texture(tex, uv+kernelOffset+i*kernelStep)*kernel[i];
    fColor = sum;
}