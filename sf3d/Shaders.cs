using System.IO;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using DGL;

#nullable disable

namespace SF3D
{
    public sealed class Shaders
    {
        /// <summary>
        ///  GBuffer-filling shader program. Diffuse color, specular color and normal vectors are stored in model vertices.
        /// </summary>
        public static SceneShaderProgram GBufferVVV, Shadow;
        public static DeferredSunlightShaderProgram DeferredSunlight;
        public static IdentityEffect Identity;
        public static ToneMappingEffect ToneMapping;
        public static FilterGreaterEffect FilterGreater;
        public static Kernel1DEffect Kernel1D;

        public static void Init()
        {
            using(ShaderLoader loader = new())
            {
                GBufferVVV = new(loader.Get("shaders/vvv/common.vert"), loader.Get("shaders/vvv/gbuffer.frag"));
                Shadow = new(loader.Get("shaders/shadow.vert"), loader.Get("shaders/empty.frag"));
                DeferredSunlight = new(loader.Get("shaders/postprocessing/base.vert"), loader.Get("shaders/deferred/sunlight.frag"));
                Identity = new(loader.Get("shaders/postprocessing/base.vert"), loader.Get("shaders/postprocessing/identity.frag"));
                ToneMapping = new(loader.Get("shaders/postprocessing/base.vert"), loader.Get("shaders/postprocessing/tone-mapping.frag"));
                FilterGreater = new(loader.Get("shaders/postprocessing/base.vert"), loader.Get("shaders/postprocessing/filter-greater.frag"));
                Kernel1D = new(loader.Get("shaders/postprocessing/base.vert"), loader.Get("shaders/postprocessing/kernel1d.frag"));
            }
        }
        public static void Dispose()
        {
            GBufferVVV.Dispose();
            Shadow.Dispose();
            DeferredSunlight.Dispose();
            Identity.Dispose();
        }
    }
    public sealed class SceneShaderProgram : ShaderProgram
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

    public class DeferredShaderProgram : ShaderProgram
    {
        private int uDiffuseMap, uSpecularMap, uNormalMap, uPositionMap, uCameraPosition;
        public DeferredShaderProgram(params Shader[] shaders) : base(shaders)
        {
            uDiffuseMap = GetUniformLocation("diffuseMap");
            uSpecularMap = GetUniformLocation("specularMap");
            uNormalMap = GetUniformLocation("normalMap");
            uPositionMap = GetUniformLocation("positionMap");
            uCameraPosition = GetUniformLocation("cameraPosition");
        }
        public TextureUnit DiffuseMap {set {EnsureBound(); GL.Uniform1(uDiffuseMap, (int) value - (int) TextureUnit.Texture0);}}
        public TextureUnit SpecularMap {set {EnsureBound(); GL.Uniform1(uSpecularMap, (int) value - (int) TextureUnit.Texture0);}}
        public TextureUnit NormalMap {set {EnsureBound(); GL.Uniform1(uNormalMap, (int) value - (int) TextureUnit.Texture0);}}
        public TextureUnit PositionMap {set {EnsureBound(); GL.Uniform1(uPositionMap, (int) value - (int) TextureUnit.Texture0);}}
        public Vector3 CameraPosition {set {EnsureBound(); GL.Uniform3(uCameraPosition, value);}}
    }

    public sealed class DeferredSunlightShaderProgram : DeferredShaderProgram
    {
        private int uLightDirection, uLightColor, uAmbientLightColor, uShadowMap, uShadowView, uShadowProjection, uShadowBias;
        public DeferredSunlightShaderProgram(params Shader[] shaders) : base(shaders)
        {
            uLightDirection = GetUniformLocation("lightDirection");
            uLightColor = GetUniformLocation("lightColor");
            uAmbientLightColor = GetUniformLocation("ambientLightColor");
            uShadowMap = GetUniformLocation("shadowMap");
            uShadowProjection = GetUniformLocation("shadowProjection");
            uShadowView = GetUniformLocation("shadowView");
            uShadowBias = GetUniformLocation("shadowBias");
        }
        public Vector3 LightDirection {set {EnsureBound(); GL.Uniform3(uLightDirection, value);}}
        public Vector3 LightColor {set {EnsureBound(); GL.Uniform3(uLightColor, value);}}
        public Vector3 AmbientLightColor {set {EnsureBound(); GL.Uniform3(uAmbientLightColor, value);}}

        public TextureUnit ShadowMap {set {EnsureBound(); GL.Uniform1(uShadowMap, (int) value - (int) TextureUnit.Texture0);}}
        public Matrix4 ShadowView {set {EnsureBound(); GL.UniformMatrix4(uShadowView, false, ref value);}}
        public Matrix4 ShadowProjection {set {EnsureBound(); GL.UniformMatrix4(uShadowProjection, false, ref value);}}
        public float ShadowBias {set {EnsureBound(); GL.Uniform1(uShadowBias, value);}}
    }

    public sealed class IdentityEffect : ShaderProgram
    {
        private int uTex;
        public IdentityEffect(params Shader[] shaders) : base(shaders) => uTex = GetUniformLocation("tex");
        public TextureUnit Texture {set {EnsureBound(); GL.Uniform1(uTex, (int) value - (int) TextureUnit.Texture0);}}
    }

    public sealed class ToneMappingEffect : ShaderProgram
    {
        private int uTex, uBloomMap, uExposure;
        public ToneMappingEffect(params Shader[] shaders) : base(shaders)
        {
            uTex = GetUniformLocation("tex");
            uExposure = GetUniformLocation("exposure");
            uBloomMap = GetUniformLocation("bloomMap");
        }
        public TextureUnit Texture {set {EnsureBound(); GL.Uniform1(uTex, (int) value - (int) TextureUnit.Texture0);}}
        public TextureUnit BloomMap {set {EnsureBound(); GL.Uniform1(uBloomMap, (int) value - (int) TextureUnit.Texture0);}}
        public float Exposure {set {EnsureBound(); GL.Uniform1(uExposure, value);}}
    }

    public sealed class FilterGreaterEffect : ShaderProgram
    {
        private int uTex, uThreshold;
        public FilterGreaterEffect(params Shader[] shaders) : base(shaders)
        {
            uTex = GetUniformLocation("tex");
            uThreshold = GetUniformLocation("threshold");
        }
        public TextureUnit Texture {set {EnsureBound(); GL.Uniform1(uTex, (int) value - (int) TextureUnit.Texture0);}}
        public float Threshold {set {EnsureBound(); GL.Uniform1(uThreshold, value);}}
    }

    public sealed class Kernel1DEffect : ShaderProgram
    {
        private int uTex, uKernel, uKernelLength, uKernelStep, uKernelOffset;
        public Kernel1DEffect(params Shader[] shaders) : base(shaders)
        {
            uTex = GetUniformLocation("tex");
            uKernel = GetUniformLocation("kernel");
            uKernelLength = GetUniformLocation("kernelLength");
            uKernelOffset = GetUniformLocation("kernelOffset");
            uKernelStep = GetUniformLocation("kernelStep");
        }
        public TextureUnit Texture {set {EnsureBound(); GL.Uniform1(uTex, (int) value - (int) TextureUnit.Texture0);}}
        public float[] Kernel {set {EnsureBound(); GL.Uniform1(uKernel, value.Length, value); GL.Uniform1(uKernelLength, value.Length);}}
        public Vector2 KernelStep {set {EnsureBound(); GL.Uniform2(uKernelStep, value);}}
        public Vector2 KernelOffset {set {EnsureBound(); GL.Uniform2(uKernelOffset, value);}}
    }
}