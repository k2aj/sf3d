using System.IO;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using DGL;

#nullable disable

namespace SF3D
{
    public static class Shaders
    {
        /// <summary>
        ///  GBuffer-filling shader program. Diffuse color, specular color and normal vectors are stored in model vertices.
        /// </summary>
        public static AtlasShaderProgram GBufferVVV;
        public static SceneShaderProgram Shadow;
        public static DeferredSunlightShaderProgram DeferredSunlight;
        public static DeferredOmniShaderProgram DeferredOmni;
        public static PostProcessingEffect Identity;
        public static FogEffect Fog;
        public static ToneMappingEffect ToneMapping;
        public static FilterGreaterEffect FilterGreater;
        public static Kernel1DEffect Kernel1D;

        public static void Init()
        {
            using(SF3DShaderLoader loader = new())
            {
                GBufferVVV = new(loader.Get("shaders/vvv/common.vert"), loader.Get("shaders/vvv/gbuffer.frag"));
                Shadow = new(loader.Get("shaders/shadow.vert"), loader.Get("shaders/empty.frag"));
                DeferredSunlight = new(loader.GetDeferred(type: "directional", brdf: "blinn-phong", shadows: "pcf", background: true));
                DeferredOmni = new(loader.GetDeferred(type: "omni", brdf: "blinn-phong"));
                Identity = new(loader.GetPostProcessing("identity"));
                Fog = new(loader.GetPostProcessing("fog"));
                ToneMapping = new(loader.GetPostProcessing("tone-mapping"));
                FilterGreater = new(loader.GetPostProcessing("filter-greater"));
                Kernel1D = new(loader.GetPostProcessing("kernel1d"));
            }
        }
        public static void Dispose()
        {
            GBufferVVV.Dispose();
            Shadow.Dispose();
            DeferredSunlight.Dispose();
            DeferredOmni.Dispose();
            Identity.Dispose();
            Fog.Dispose();
            ToneMapping.Dispose();
            FilterGreater.Dispose();
            Kernel1D.Dispose();
        }
    }
    public class SceneShaderProgram : ShaderProgram
    {
        private int uModel, uView, uProjection;
        public SceneShaderProgram(params Shader[] shaders) : base(shaders) 
        {
            uModel = GetUniformLocation("model");
            uView = GetUniformLocation("view");
            uProjection = GetUniformLocation("projection");
        }
        public Matrix4 Model {set {EnsureBound(); GL.UniformMatrix4(uModel, false, ref value);}}
        public Matrix4 View {set {EnsureBound(); GL.UniformMatrix4(uView, false, ref value);}}
        public Matrix4 Projection {set {EnsureBound(); GL.UniformMatrix4(uProjection, false, ref value);}}
    }
    public class AtlasShaderProgram : SceneShaderProgram
    {
        private int uAtlas, uInvAtlasSize;
        public AtlasShaderProgram(params Shader[] shaders) : base(shaders) 
        {
            uAtlas = GetUniformLocation("atlas");
            uInvAtlasSize = GetUniformLocation("invAtlasSize");
        }
        public TextureUnit Atlas {set {EnsureBound(); GL.Uniform1(uAtlas, (int) value - (int) TextureUnit.Texture0);}}
        public Vector2 AtlasSize {set {EnsureBound(); GL.Uniform2(uInvAtlasSize, new Vector2(1/value.X, 1/value.Y));}}
    }

    public class DeferredShaderProgram : ShaderProgram
    {
        private int uDiffuseMap, uSpecularMap, uNormalMap, uPositionMap, uCameraPosition, uLightColor, uAmbientLightColor;
        public DeferredShaderProgram(params Shader[] shaders) : base(shaders)
        {
            uDiffuseMap = GetUniformLocation("diffuseMap");
            uSpecularMap = GetUniformLocation("specularMap");
            uNormalMap = GetUniformLocation("normalMap");
            uPositionMap = GetUniformLocation("positionMap");
            uCameraPosition = GetUniformLocation("cameraPosition");
            uLightColor = GetUniformLocation("lightColor");
            uAmbientLightColor = GetUniformLocation("ambientLightColor");
        }
        public TextureUnit DiffuseMap {set {EnsureBound(); GL.Uniform1(uDiffuseMap, (int) value - (int) TextureUnit.Texture0);}}
        public TextureUnit SpecularMap {set {EnsureBound(); GL.Uniform1(uSpecularMap, (int) value - (int) TextureUnit.Texture0);}}
        public TextureUnit NormalMap {set {EnsureBound(); GL.Uniform1(uNormalMap, (int) value - (int) TextureUnit.Texture0);}}
        public TextureUnit PositionMap {set {EnsureBound(); GL.Uniform1(uPositionMap, (int) value - (int) TextureUnit.Texture0);}}
        public Vector3 CameraPosition {set {EnsureBound(); GL.Uniform3(uCameraPosition, value);}}
        public Vector3 LightColor {set {EnsureBound(); GL.Uniform3(uLightColor, value);}}
        public Vector3 AmbientLightColor {set {EnsureBound(); GL.Uniform3(uAmbientLightColor, value);}}
    }

    public sealed class DeferredSunlightShaderProgram : DeferredShaderProgram
    {
        private int uLightDirection, uShadowMap, uShadowViewProjection, uShadowBias;
        public DeferredSunlightShaderProgram(params Shader[] shaders) : base(shaders)
        {
            uLightDirection = GetUniformLocation("lightDirection");
            uShadowMap = GetUniformLocation("shadowMap");
            uShadowViewProjection = GetUniformLocation("shadowViewProjection");
            uShadowBias = GetUniformLocation("shadowBias");
        }
        public Vector3 LightDirection {set {EnsureBound(); GL.Uniform3(uLightDirection, value);}}
        public TextureUnit ShadowMap {set {EnsureBound(); GL.Uniform1(uShadowMap, (int) value - (int) TextureUnit.Texture0);}}
        public Matrix4 ShadowViewProjection {set {EnsureBound(); GL.UniformMatrix4(uShadowViewProjection, false, ref value);}}
        public float ShadowBias {set {EnsureBound(); GL.Uniform1(uShadowBias, value);}}
    }

    public sealed class DeferredOmniShaderProgram : DeferredShaderProgram
    {
        private int uModel, uView, uProjection, uLightPosition, uAttenuation, uZNear;
        public DeferredOmniShaderProgram(params Shader[] shaders) : base(shaders)
        {
            uModel = GetUniformLocation("model");
            uView = GetUniformLocation("view");
            uProjection = GetUniformLocation("projection");
            uLightPosition = GetUniformLocation("lightPosition");
            uAttenuation = GetUniformLocation("attenuation");
            uZNear = GetUniformLocation("zNear");
        }
        public Matrix4 Model {set {EnsureBound(); GL.UniformMatrix4(uModel, false, ref value);}}
        public Matrix4 View {set {EnsureBound(); GL.UniformMatrix4(uView, false, ref value);}}
        public Matrix4 Projection {set {EnsureBound(); GL.UniformMatrix4(uProjection, false, ref value);}}
        public Vector4 Attenuation {set {EnsureBound(); GL.Uniform4(uAttenuation, value);}}
        public OmniLight Light
        {
            set
            {
                Model = Matrix4.CreateScale(value.Range) * Matrix4.CreateTranslation(value.Position);
                LightColor = value.Color;
                AmbientLightColor = value.AmbientColor;
                Attenuation = value.Attenuation;
            }
        }
        public float ZNear {set {EnsureBound(); GL.Uniform1(uZNear, value);}}
    }

    public class PostProcessingEffect : ShaderProgram
    {
        private int uTex;
        public PostProcessingEffect(params Shader[] shaders) : base(shaders) => uTex = GetUniformLocation("tex");

        public void Apply()
        {
            EnsureBound();
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }
        public TextureUnit Texture {set {EnsureBound(); GL.Uniform1(uTex, (int) value - (int) TextureUnit.Texture0);}}
    }

    public class FogEffect : PostProcessingEffect
    {
        private int uCubemap, uCameraPosition, uFogRadii, uInvViewProj, uInvModel, uFogColor;
        public FogEffect(params Shader[] shaders) : base(shaders)
        {
            uCubemap = GetUniformLocation("cubemap");
            uCameraPosition = GetUniformLocation("cameraPosition");
            uFogRadii = GetUniformLocation("fogRadii");
            uInvViewProj = GetUniformLocation("invViewProj");
            uInvModel = GetUniformLocation("invModel");
            uFogColor = GetUniformLocation("fogColor");
        }
        public TextureUnit CubeMap {set {EnsureBound(); GL.Uniform1(uCubemap, (int) value - (int) TextureUnit.Texture0);}}
        public Vector3 CameraPosition {set {EnsureBound(); GL.Uniform3(uCameraPosition, value);}}
        public Vector2 FogRadii {set {EnsureBound(); GL.Uniform2(uFogRadii, value);}}
        public Vector3 FogColor {set {EnsureBound(); GL.Uniform3(uFogColor, value);}}
        public Matrix4 InverseViewProjection {set {EnsureBound(); GL.UniformMatrix4(uInvViewProj, false, ref value);}}
        public Matrix4 InverseModel {set {EnsureBound(); GL.UniformMatrix4(uInvModel, false, ref value);}}
    }

    public sealed class ToneMappingEffect : PostProcessingEffect
    {
        private int uBloomMap, uExposure;
        public ToneMappingEffect(params Shader[] shaders) : base(shaders)
        {
            uExposure = GetUniformLocation("exposure");
            uBloomMap = GetUniformLocation("bloomMap");
        }
        public TextureUnit BloomMap {set {EnsureBound(); GL.Uniform1(uBloomMap, (int) value - (int) TextureUnit.Texture0);}}
        public float Exposure {set {EnsureBound(); GL.Uniform1(uExposure, value);}}
    }

    public sealed class FilterGreaterEffect : PostProcessingEffect
    {
        private int uThreshold;
        public FilterGreaterEffect(params Shader[] shaders) : base(shaders) => uThreshold = GetUniformLocation("threshold");
        public float Threshold {set {EnsureBound(); GL.Uniform1(uThreshold, value);}}
    }

    public sealed class Kernel1DEffect : PostProcessingEffect
    {
        private int uKernel, uKernelLength, uKernelStep, uKernelOffset;
        public Kernel1DEffect(params Shader[] shaders) : base(shaders)
        {
            uKernel = GetUniformLocation("kernel");
            uKernelLength = GetUniformLocation("kernelLength");
            uKernelOffset = GetUniformLocation("kernelOffset");
            uKernelStep = GetUniformLocation("kernelStep");
        }
        public float[] Kernel {set {EnsureBound(); GL.Uniform1(uKernel, value.Length, value); GL.Uniform1(uKernelLength, value.Length);}}
        public Vector2 KernelStep {set {EnsureBound(); GL.Uniform2(uKernelStep, value);}}
        public Vector2 KernelOffset {set {EnsureBound(); GL.Uniform2(uKernelOffset, value);}}
    }

    public sealed class SF3DShaderLoader : ShaderLoader
    {
        public Shader[] GetPostProcessing(string name) => new Shader[]{
            Get("shaders/postprocessing/base.vert"),
            Get($"shaders/postprocessing/{name}.frag")
        };

        /// <summary>
        /// Creates shaders for a deferred shading program.
        /// </summary>
        /// <param name="type">Type of the light source (directional).</param>
        /// <param name="brdf">BRDF function being used (phong/blinn-phong).</param>
        /// <param name="shadows">Shadow algorithm being used (pcf/none).</param>
        /// <param name="ambientLighting">Whether ambient lighting is enabled or disabled.</param>
        /// <returns></returns>
        public Shader[] GetDeferred(string type, string brdf, string shadows = "none", bool background = false) => new Shader[]{
            Get($"shaders/deferred/{type}.vert"),
            Get("shaders/deferred/base.frag"),
            Get($"shaders/deferred/{type}.frag"),
            Get($"shaders/brdf/{brdf}.frag"),
            Get($"shaders/shadow/{shadows}.frag"),
            Get($"shaders/deferred/background-{(background ? "enabled" : "disabled")}.frag")
        };
    }
}