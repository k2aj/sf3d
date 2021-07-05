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
    public static class RandomExtensions
    {
        public static float NextFloat(this Random random) => (float) random.NextDouble();
        public static float NextFloat(this Random random, float min, float max) => random.NextFloat() * (max-min) + min;
        public static Vector3 NextVector3(this Random random, float max) => new(random.NextFloat(-max,max), random.NextFloat(-max,max), random.NextFloat(-max,max));
    }
    class Game : GameWindow
    {
        private static float[] gaussianBlurKernel = {
            0.055f, 0.244f, 0.402f, 0.244f, 0.055f
        };

        private Scene scene = new();
        private Camera camera = new(){Target = new(0.5f,0,0), Eye = new(0,0,-3)};
        private const float cameraVelocity = 3;
        private GBuffer gBuffer;
        private Framebuffer shadowFbo, hdrFbo, bloomFbo1, bloomFbo2;
        private Texture2D shadowMap, hdr, bloom1, bloom2;
        private Sampler sNearest, sLinear;

        //, terrain;
        Scene.ObjectID objectID;
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            unsafe 
            {
                // Hide cursor because we're using a FPS-style camera
                GLFW.SetInputMode(WindowPtr, CursorStateAttribute.Cursor, CursorModeValue.CursorDisabled);
            }
            UpdateFrequency = 60.0;

            gBuffer = new(size: new(1920,1080));
            Shaders.Init();
            Models.Init();

            shadowMap = new Texture2D(PixelInternalFormat.DepthComponent32, new(2048,2048));
            shadowFbo = new((FramebufferAttachment.DepthAttachment, shadowMap));

            hdr = new Texture2D(PixelInternalFormat.Rgb16f, new(1920,1080));
            hdrFbo = new Framebuffer((FramebufferAttachment.ColorAttachment0, hdr));

            bloom1 = new Texture2D(PixelInternalFormat.Rgb16f, new(1920,1080));
            bloomFbo1 = new Framebuffer((FramebufferAttachment.ColorAttachment0, bloom1));

            bloom2 = new Texture2D(PixelInternalFormat.Rgb16f, new(1920,1080));
            bloomFbo2 = new Framebuffer((FramebufferAttachment.ColorAttachment0, bloom2));

            sNearest = new(){Wrap = TextureWrapMode.ClampToEdge, MinFilter = TextureMinFilter.Nearest, MagFilter = TextureMagFilter.Nearest};
            sLinear = new(){Wrap = TextureWrapMode.ClampToEdge, MinFilter = TextureMinFilter.Linear, MagFilter = TextureMagFilter.Linear};

            //terrain = WavefrontOBJ.Parse("models/terrain/terrain.obj").ToModel(atlas);

            var rng = new Random();
            objectID = scene.Add(Models.Plane, Matrix4.Identity);
            //scene.Add(terrain, Matrix4.CreateScale(0.25f));
            var light = new OmniLight{Intensity = new(1), Position = new(0,1.5f,1)};
            scene.Add(Models.OmniLight, Matrix4.CreateTranslation(0,0,5));
            for(int i=0; i<10; ++i)
            {
                scene.Add(Models.Tree, Matrix4.CreateRotationY(rng.NextFloat()*6.28f) * Matrix4.CreateTranslation((rng.NextFloat()-0.5f)*20,0,(rng.NextFloat()-0.5f)*20));
            }
            scene.Add(light);

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
        float lookPitch, lookYaw;
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            float dt = (float) e.Time;
            t += dt;
            var input = KeyboardState;
            if(input.IsKeyDown(Keys.Escape))
                Close();

            scene.SetModelMatrix(objectID, Matrix4.CreateFromAxisAngle(new Vector3(0,1,0), 0.25f*t)*Matrix4.CreateTranslation(0,1,0));

            // Naive implementation of flying FPS-style camera
            Vector3 cameraForward = new(camera.LookDir.X, 0, camera.LookDir.Z);
            cameraForward.Normalize();
            Vector3 cameraRight = Matrix3.CreateRotationY(MathF.PI/2) * cameraForward;
            if(input.IsKeyDown(Keys.A))
                camera.Eye -= cameraRight*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.D))
                camera.Eye += cameraRight*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.W))
                camera.Eye += cameraForward*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.S))
                camera.Eye -= cameraForward*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.Space))
                camera.Eye += Vector3.UnitY*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.LeftShift))
                camera.Eye -= Vector3.UnitY*cameraVelocity*dt;

            var mouseDelta = MouseState.Position - MouseState.PreviousPosition;
            mouseDelta = mouseDelta / (float)Math.Clamp(e.Time, 1/100f, 1/5f) / 60;
            // We clamp to +-Math.PI/2.01 instead of +-Math.PI/2 to prevent the user from looking straight up/straight down,
            // because it breaks Camera's view matrix because of some issues with Matrix4.LookAt
            lookPitch = (float) Math.Clamp(lookPitch + mouseDelta.Y/100, -Math.PI/2.01, Math.PI/2.01);
            lookYaw += mouseDelta.X/100;
            camera.LookDir = Matrix3.CreateRotationY(lookYaw)*Matrix3.CreateRotationX(-lookPitch)*Vector3.UnitZ;

            base.OnUpdateFrame(e);
        }
        protected override void OnResize(ResizeEventArgs e)
        {
            gBuffer.Size = e.Size;
            base.OnResize(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            gBuffer.Framebuffer.Bind();
            GL.Viewport(0, 0, gBuffer.Size.X, gBuffer.Size.Y);
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Blend);

            float aspectRatio = Size.X / Size.Y;
            var projection = camera.ProjectionMatrix;
            //Matrix4.CreatePerspectiveFieldOfView(MathF.PI/2, aspectRatio, 0.25f, 40f, out Matrix4 projection);

            // Bind textures
            Texture2D.BindAll(
                (TextureUnit.Texture0, gBuffer.DiffuseMap, sNearest),
                (TextureUnit.Texture1, gBuffer.SpecularMap, sNearest),
                (TextureUnit.Texture2, gBuffer.NormalMap, sNearest),
                (TextureUnit.Texture3, gBuffer.PositionMap, sNearest),
                (TextureUnit.Texture4, shadowMap, sNearest),
                (TextureUnit.Texture5, hdr, sLinear),
                (TextureUnit.Texture6, bloom1, sLinear),
                (TextureUnit.Texture7, bloom2, sLinear),
                (TextureUnit.Texture8, Models.Atlas.Texture, sLinear)
            );

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
            var lightPos = new Vector3(0,10,0);
            var lightProjection = Matrix4.CreateOrthographicOffCenter(-40, 40, -40, 40, 2, 20);
            var lightView = Matrix4.LookAt(lightPos, new Vector3(0.5f,0,0), Vector3.UnitY);

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

            Shaders.DeferredSunlight.AmbientLightColor = new(0.15f);
            Shaders.DeferredSunlight.LightDirection = new(0,-1,0);
            Shaders.DeferredSunlight.LightColor = new(0);
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

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            scene.RenderLights(camera);
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

            SwapBuffers();
            base.OnRenderFrame(e);
        }
    }
}
