#version 330 core

noperspective out vec2 uv;

const vec2 model[6] = vec2[6](
    vec2(-1.0,-1.0),
    vec2(1.0,-1.0),
    vec2(1.0,1.0),
    vec2(-1.0,-1.0),
    vec2(1.0,1.0),
    vec2(-1.0,1.0)
);

void main() {
    vec2 position = model[gl_VertexID];
    gl_Position = vec4(position, 0.5, 1.0);
    uv = position*0.5+0.5;
}