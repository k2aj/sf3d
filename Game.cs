using System;
using System.Collections.Generic;
using System.IO;

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;

using DGL;
using DGL.Model;

using System.Drawing;

namespace SF3D
{
    class Game : GameWindow
    {
        private static float[] gaussianBlurKernel = {
            0.055f, 0.244f, 0.402f, 0.244f, 0.055f
        };

        private Scene scene = new();
        private World world = new();
        private GBuffer gBuffer;
        private Framebuffer shadowFbo, hdrFbo, bloomFbo1, bloomFbo2;
        private Texture2D shadowMap, hdr, bloom1, bloom2;
        private Sampler sNearest, sLinear, sShadow;
        private CubeMap skybox;
        private Aircraft aircraft;
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            UpdateFrequency = 60.0;

            gBuffer = new(size: Size);
            Shaders.Init();
            Models.Init();

            shadowMap = new Texture2D(PixelInternalFormat.DepthComponent32, new(4096,4096));
            shadowFbo = new((FramebufferAttachment.DepthAttachment, shadowMap));

            hdr = new Texture2D(PixelInternalFormat.Rgb16f, Size);
            hdrFbo = new Framebuffer((FramebufferAttachment.ColorAttachment0, hdr));

            bloom1 = new Texture2D(PixelInternalFormat.Rgb16f, Size/2);
            bloomFbo1 = new Framebuffer((FramebufferAttachment.ColorAttachment0, bloom1));

            bloom2 = new Texture2D(PixelInternalFormat.Rgb16f, Size/2);
            bloomFbo2 = new Framebuffer((FramebufferAttachment.ColorAttachment0, bloom2));

            sNearest = new(){Wrap = TextureWrapMode.ClampToEdge, MinFilter = TextureMinFilter.Nearest, MagFilter = TextureMagFilter.Nearest};
            sLinear = new(){Wrap = TextureWrapMode.ClampToEdge, MinFilter = TextureMinFilter.Linear, MagFilter = TextureMagFilter.Linear};
            sShadow = new(){Wrap = TextureWrapMode.ClampToBorder, MinFilter = TextureMinFilter.Nearest, MagFilter = TextureMagFilter.Nearest, BorderColor = new(1)};

            skybox = new("textures/alien-sky");

            var rng = new Random();

            var light = new OmniLight{Color = new(4), AmbientColor = new(1), Position = new(0,2f,1)};
            scene.Add(light);

            aircraft = new(Models.Plane);
            aircraft.Transform.Translation = new(0,1,0);
            aircraft.Camera.ZFar = 400;
            world.Spawn(aircraft);

            GL.Enable(EnableCap.CullFace);
        }

        static void Main(string[] args)
        {
            using(var game = new Game{Title = "SF3D", Size = new(1200,720)}) 
            {
                game.Run();
            }
        }

        private float t = 0;
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            float dt = (float) e.Time;
            t += dt;
            var lookDir = new Vector4(MousePosition.X/(float)Size.X*2-1, MousePosition.Y/(float)Size.Y*(-2)+1, 1, 1) * (aircraft.Camera.ViewMatrix * aircraft.Camera.ProjectionMatrix).Inverted();
            lookDir = lookDir / lookDir.W;
            Input input = new(KeyboardState, MouseState, (lookDir.Xyz - aircraft.Camera.Eye).Normalized());

            if(input.Keyboard.IsKeyDown(Keys.Escape))
                Close();
            if(input.Keyboard.IsKeyReleased(Keys.R) && aircraft.Transform.Translation != new Vector3(0,1,0))
            {
                aircraft = new Aircraft(Models.Plane);
                aircraft.Transform.Translation = new(0,1,0);
                aircraft.Camera.ZFar = 250;
                world.Spawn(aircraft);
            }
            aircraft.Control(input);
            world.Update(scene, aircraft.Camera.Eye, dt);
            base.OnUpdateFrame(e);
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            gBuffer.Size = e.Size;

            hdr.Dispose();
            bloom1.Dispose();
            bloom2.Dispose();
            hdrFbo.Dispose();
            bloomFbo1.Dispose();
            bloomFbo2.Dispose();

            hdr = new Texture2D(PixelInternalFormat.Rgb16f, e.Size);
            hdrFbo = new Framebuffer((FramebufferAttachment.ColorAttachment0, hdr));

            bloom1 = new Texture2D(PixelInternalFormat.Rgb16f, e.Size/2);
            bloomFbo1 = new Framebuffer((FramebufferAttachment.ColorAttachment0, bloom1));

            bloom2 = new Texture2D(PixelInternalFormat.Rgb16f, e.Size/2);
            bloomFbo2 = new Framebuffer((FramebufferAttachment.ColorAttachment0, bloom2));

            base.OnResize(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            var camera = aircraft.Camera;

            gBuffer.Framebuffer.Bind();
            GL.Viewport(0, 0, gBuffer.Size.X, gBuffer.Size.Y);
            GL.Enable(EnableCap.DepthTest);

            float aspectRatio = Size.X / Size.Y;
            var projection = camera.ProjectionMatrix;

            // Bind textures
            Texture2D.BindAll(
                (TextureUnit.Texture0, gBuffer.DiffuseMap, sNearest),
                (TextureUnit.Texture1, gBuffer.SpecularMap, sNearest),
                (TextureUnit.Texture2, gBuffer.NormalMap, sNearest),
                (TextureUnit.Texture3, gBuffer.PositionMap, sNearest),
                (TextureUnit.Texture4, shadowMap, sShadow),
                (TextureUnit.Texture5, hdr, sLinear),
                (TextureUnit.Texture6, bloom1, sLinear),
                (TextureUnit.Texture7, bloom2, sLinear),
                (TextureUnit.Texture8, Models.Atlas.Texture, sLinear),
                (TextureUnit.Texture10, gBuffer.ZBuffer, sNearest)
            );
            GL.ActiveTexture(TextureUnit.Texture9);
            skybox.Bind();

            // Render scene to gbuffer
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] {135/255f,162/255f,237/255f,1f}); //background color
            GL.ClearBuffer(ClearBuffer.Color, 1, new float[] {0,0,0,0}); //specular color - doesn't matter for background
            GL.ClearBuffer(ClearBuffer.Color, 2, new float[] {0.5f,0.5f,0.5f,0}); //normal vectors - should be 0,0,0 for background but we store them as (normal+1)/2, so 0.5,0.5,0.5 stands for 0,0,0
            GL.ClearBuffer(ClearBuffer.Color, 3, new float[] {0,0,0,0}); //positions - don't matter for background

            Shaders.GBufferVVV.Bind();
            Shaders.GBufferVVV.Atlas = TextureUnit.Texture8;
            Shaders.GBufferVVV.AtlasSize = Models.Atlas.Texture.Size;
            scene.Render(camera.ViewMatrix, projection, shadow: false);

            // Render scene to shadow map
            var lightModelMatrix = Matrix4.CreateRotationX(MathF.PI/4)*Matrix4.CreateRotationY(2*MathF.PI*t/480);
            var lightPos = new Vector3(camera.Eye.X,0,camera.Eye.Z)+(new Vector4(0,camera.ZFar,0,0)*lightModelMatrix).Xyz;
            var lightProjection = Matrix4.CreateOrthographicOffCenter(-camera.ZFar, camera.ZFar, -camera.ZFar, camera.ZFar, 2, 1000);
            var lightView = Matrix4.LookAt(lightPos, new Vector3(camera.Eye.X,0,camera.Eye.Z), Vector3.UnitY);

            shadowFbo.Bind();
            GL.Viewport(0, 0, shadowFbo.Size.X, shadowFbo.Size.Y);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            scene.Render(lightView, lightProjection, shadow: true);

            // Apply lighting
            hdrFbo.Bind();
            GL.Viewport(0,0,hdrFbo.Size.X,hdrFbo.Size.Y);
            GL.Disable(EnableCap.DepthTest);            
            
            Shaders.DeferredSunlight.Bind();

            Shaders.DeferredSunlight.DiffuseMap = TextureUnit.Texture0;
            Shaders.DeferredSunlight.SpecularMap = TextureUnit.Texture1;
            Shaders.DeferredSunlight.NormalMap = TextureUnit.Texture2;
            Shaders.DeferredSunlight.PositionMap = TextureUnit.Texture3;

            Shaders.DeferredSunlight.AmbientLightColor = new(0.1f);
            Shaders.DeferredSunlight.LightDirection = new(0,-1,0);
            Shaders.DeferredSunlight.LightColor = new(0.3f);
            Shaders.DeferredSunlight.CameraPosition = camera.Eye;

            Shaders.DeferredSunlight.ShadowMap = TextureUnit.Texture4;
            Shaders.DeferredSunlight.ShadowViewProjection = lightView*lightProjection;
            
            VAO.Empty.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            Shaders.DeferredOmni.Bind();
            Shaders.DeferredOmni.DiffuseMap = TextureUnit.Texture0;
            Shaders.DeferredOmni.SpecularMap = TextureUnit.Texture1;
            Shaders.DeferredOmni.NormalMap = TextureUnit.Texture2;
            Shaders.DeferredOmni.PositionMap = TextureUnit.Texture3;
            Shaders.DeferredOmni.CameraPosition = camera.Eye;

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            scene.RenderLights(camera);

            // Apply fog and skybox
            // this also needs blending
            Shaders.Fog.Bind();
            Shaders.Fog.CameraPosition = camera.Eye;
            Shaders.Fog.InverseViewProjection = Matrix4.Invert(camera.ViewMatrix * camera.ProjectionMatrix);
            Shaders.Fog.InverseModel = lightModelMatrix.Inverted() * Matrix4.CreateRotationX(1.2f) * Matrix4.CreateRotationY(-0.55f);
            Shaders.Fog.FogRadii = new(0, camera.ZFar);
            Shaders.Fog.FogColor = new(0.2f,0.2f,0.2f);
            Shaders.Fog.CubeMap = TextureUnit.Texture9;
            Shaders.Fog.Texture = TextureUnit.Texture10;
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            Shaders.Fog.Apply();

            GL.Disable(EnableCap.Blend);

            // Extract bright fragments into bloomFbo1
            bloomFbo1.Bind();
            GL.Viewport(0, 0, bloomFbo1.Size.X, bloomFbo1.Size.Y);
            Shaders.FilterGreater.Bind();
            Shaders.FilterGreater.Threshold = 1.0f;
            Shaders.FilterGreater.Texture = TextureUnit.Texture5;
            Shaders.FilterGreater.Apply();

            // Run multiple alternating passes of horizontal & vertical gaussian blur on the bloom map
            //(ping pong between bloomFbo1 and bloomFbo2)
            Shaders.Kernel1D.Bind();
            Shaders.Kernel1D.Kernel = KernelEffects.CreateGaussian1D(length: 12, sd: 4);
            for(int i=0; i<4; ++i)
            {
                bloomFbo2.Bind();
                Shaders.Kernel1D.Texture = TextureUnit.Texture6;
                Shaders.Kernel1D.KernelStep = Vector2.UnitX;
                Shaders.Kernel1D.Apply();

                bloomFbo1.Bind();
                Shaders.Kernel1D.Texture = TextureUnit.Texture7;
                Shaders.Kernel1D.KernelStep = Vector2.UnitY;
                Shaders.Kernel1D.Apply();
            }

            // Run postprocessing (tone mapping & apply bloom)
            Framebuffer.Default.Bind();
            GL.Viewport(0,0,Size.X,Size.Y);
            Shaders.ToneMapping.Bind();
            Shaders.ToneMapping.Texture = TextureUnit.Texture5;
            Shaders.ToneMapping.BloomMap = TextureUnit.Texture6;
            Shaders.ToneMapping.Exposure = 0.25f;
            Shaders.ToneMapping.Apply();

            if(KeyboardState.IsKeyDown(Keys.T))
                Models.Atlas.Texture.CopyTo(Vector2i.Zero, Models.Atlas.Texture.Size, Framebuffer.Default, new(128), new(512));

            SwapBuffers();
            base.OnRenderFrame(e);
        }
    }
}
