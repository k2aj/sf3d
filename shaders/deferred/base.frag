#version 330 core

// we don't want perspective-correct interpolation because the texture is applied to a framebuffer, not a model
noperspective in vec2 uv;

uniform sampler2D diffuseMap;
uniform sampler2D specularMap;
uniform sampler2D normalMap;
uniform sampler2D positionMap;

uniform vec3 cameraPosition;

out vec3 fragColor;

/*===============================SHADER MODULES========================================
   The functions in this section have to be linked from other shaders.
*/

/* Used for turning shadows on/off and switching between different shadow mapping algorithms.
   Can be provided by any of the shaders in shadow/ directory.

   Returns 0 if position is in shadow.
   Returns irradiance if position is not in shadow.
   Values between 0 and irradiance indicate soft shadows.
*/
vec3 applyShadow(vec3 irradiance, vec3 position);

/* Used for changing the BRDF function being used.
   Can be provided by any of the shaders in shadow/ directory.

   Computes the ratio between outgoing radiance and irradiance from the light source.
*/
vec3 brdf(
    vec3 lightDirection, vec3 toCamera, vec3 surfaceNormal,
    vec3 diffuseColor, vec3 specularColor, float specularExponent
);

/* Used for switching the type of the light source (directional/omni/cone/etc.)

   Can be provided by any fragment shader from deferred/ directory (except ambient-* and base.frag).
   You also have to link a vertex shader for the specific light source type (so if you're using directional.frag, you
   also have to use directional.vert).

   Returns:
   - Direction of light rays coming from the light source
   - Irradiance from the light source
   - Ambient light coming from the light source
*/
vec3 light(vec3 position, out vec3 irradiance, out vec3 ambientLight);

/* Used for displaying background.
   Background ignores lighting and is recognised by having normal vectors equal to 0.
   Link this with background-enabled.frag or background-disabled.frag. 
   (background-disabled.frag is useful for additive blending, to prevent the background from being applied
   multiple times)
*/
vec3 applyBackground(vec3 backgroundColor, vec3 lightingResult, float mixingFactor);

//======================================================================================

void main() 
{
   //#define uv (gl_FragCoord.xy/gl_FragCoord.w*0.5+0.5)
    //Extract data from gbuffer
    vec3 diffuseColor = texture(diffuseMap,uv).rgb;

    vec4 rawSpecular = texture(specularMap,uv);
    vec3 specularColor = rawSpecular.rgb;
    float specularExponent = rawSpecular.a*255.0;

    vec3 normal = texture(normalMap,uv).xyz*2.0-1.0;
    vec3 position = texture(positionMap,uv).xyz;

    // Compute irradiance (taking shadows into account) and light direction
    vec3 irradiance;
    vec3 ambientLight;
    vec3 lightDirection = light(position, irradiance, ambientLight);
    irradiance = applyShadow(irradiance, position);

    // Compute outgoing radiance using BRDF, and add ambient lighting
    vec3 toCamera = normalize(cameraPosition - position);
    vec3 brdfVal = brdf(lightDirection, toCamera, normal, diffuseColor, specularColor, specularExponent);
    vec3 outgoingRadiance = clamp(brdfVal * dot(-lightDirection, normal) * irradiance, 0.0, 1000.0) + diffuseColor*ambientLight;

    //Dumb hack to disable lighting for background
    //If normal vector=0, discard results from lighting and copy-paste diffuse color instead   
    float useLighting = dot(normal,normal); 
    fragColor = applyBackground(
        diffuseColor, 
        outgoingRadiance,
        useLighting
    );
}