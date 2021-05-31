#version 330 core

in vec2 uv;

uniform sampler2D tex;

uniform float kernel[32];
uniform int kernelLength;
uniform vec2 kernelOffset = vec2(0.0);
uniform vec2 kernelStep;

out vec4 fColor;

void main()
{
    vec2 texelSize = vec2(1.0) / textureSize(tex, 0);

    vec4 sum = vec4(0.0);
    for(int i=0; i<kernelLength; ++i) 
    {
        float r = i-(kernelLength-1)*0.5f;
        sum += texture(tex, uv+(kernelOffset+r*kernelStep)*texelSize)*kernel[i];
    }
    fColor = sum;
}