using System;
using System.Collections.Generic;

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;

using DGL;
using DGL.Model;

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
        class Tetragon
        {
            private float angularVelocity;
            private float rotation = 0;
            private Vector3 rotationAxis;

            private Vector3 position;
            private float scale;
            private Scene.ObjectID id;
            private Model model;

            public Tetragon(Random rng, Model model) 
            {
                scale = rng.NextFloat() + 0.3f;
                angularVelocity = rng.NextFloat(-0.5f,0.5f);
                rotation = rng.NextFloat(-MathF.PI, MathF.PI);
                rotationAxis = rng.NextVector3(1);
                position = rng.NextVector3(7.5f);
                this.model = model;
            }
            public void OnSpawned(Scene scene) => id = scene.Add(model, Matrix4.Identity);
            public void Update(Scene scene, float dt)
            {
                rotation += angularVelocity * dt;
                var modelMatrix = 
                    Matrix4.CreateScale(scale) * 
                    Matrix4.CreateFromAxisAngle(rotationAxis, rotation) *
                    Matrix4.CreateTranslation(position);
                scene.SetModelMatrix(id, modelMatrix);
            }
        }

        private static Vector3[] positions = {
            new( 0, -0.544f,  1.155f),
            new( 0,  1.088f,  0),
            new( 1, -0.544f, -0.577f),

            new( 0, -0.544f,  1.155f),
            new(-1, -0.544f, -0.577f),
            new( 0,  1.088f,  0),

            new(-1, -0.544f, -0.577f),
            new( 1, -0.544f, -0.577f),
            new( 0,  1.088f,  0),

            new( 0, -0.544f,  1.155f),
            new( 1, -0.544f, -0.577f),
            new(-1, -0.544f, -0.577f),
        };
        private static Vector3[] normals = {
            
            new(0.866f, 0, 0.5f),
            new(0.866f, 0, 0.5f),
            new(0.866f, 0, 0.5f),

            new(-0.866f, 0, 0.5f),
            new(-0.866f, 0, 0.5f),
            new(-0.866f, 0, 0.5f),

            new(0,0,-1),
            new(0,0,-1),
            new(0,0,-1),

            new(0,-1,0),
            new(0,-1,0),
            new(0,-1,0),
        };
        private static Color4[] diffuse = {
            Color4.OrangeRed,
            Color4.DarkRed,
            Color4.IndianRed,
            Color4.ForestGreen,
            Color4.DarkGreen,
            Color4.LawnGreen,
            Color4.RoyalBlue,
            Color4.AliceBlue,
            Color4.Aquamarine,
            Color4.LightGoldenrodYellow,
            Color4.YellowGreen,
            Color4.Orange,
        };
        private static int[] indices = {
            0,1,2,
            3,4,5,
            6,7,8,
            9,10,11,
        };

        private Scene scene = new();
        private Camera camera = new(){Target = new(0.5f,0,0), Eye = new(0,0,-3)};
        private const float cameraVelocity = 3;
        private Model model;
        private GBuffer gBuffer;
        private Framebuffer shadowFbo;
        private Texture2D shadowMap;

        private Framebuffer hdrFbo;
        private Texture2D hdr;
        private List<Tetragon> tetragons = new();
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            UpdateFrequency = 60.0;

            model = new(indices.AsSpan(), positions.AsSpan(), normals.AsSpan(), diffuse.AsSpan());

            gBuffer = new(size: new(1920,1080));
            Shaders.Init();

            shadowMap = new Texture2D(PixelInternalFormat.DepthComponent24, new(2048,2048));
            shadowFbo = new((FramebufferAttachment.DepthAttachment, shadowMap));

            hdr = new Texture2D(PixelInternalFormat.Rgb16f, new(1920,1080));
            hdrFbo = new Framebuffer((FramebufferAttachment.ColorAttachment0, hdr));

            var rng = new Random();
            
            for(int i=0; i<100; ++i) 
            {
                var t = new Tetragon(rng,model);
                t.OnSpawned(scene);
                tetragons.Add(t);
            }
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
            lookPitch = (float) Math.Clamp(lookPitch + mouseDelta.Y/100, -Math.PI/2.01, Math.PI/2.01);
            lookYaw += mouseDelta.X/100;
            camera.LookDir = Matrix3.CreateRotationY(lookYaw)*Matrix3.CreateRotationX(-lookPitch)*Vector3.UnitZ;

            foreach(var t in tetragons)
                t.Update(scene, dt);

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

            float aspectRatio = Size.X / Size.Y;
            Matrix4.CreatePerspectiveFieldOfView(MathF.PI/2, aspectRatio, 0.1f, 20f, out Matrix4 projection);

            var lightPos = new Vector3(0,10,0);
            var lightProjection = Matrix4.CreateOrthographicOffCenter(-10, 10, -10, 10, 2, 20);
            var lightView = Matrix4.LookAt(lightPos, new Vector3(0.5f,0,0), Vector3.UnitY);

            GL.ClearColor(0,0,0,0);
            GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] {135/255f,162/255f,237/255f,1f}); //background color
            GL.ClearBuffer(ClearBuffer.Color, 1, new float[] {0,0,0,0}); //specular color - doesn't matter for background
            GL.ClearBuffer(ClearBuffer.Color, 2, new float[] {0.5f,0.5f,0.5f,0}); //normal vectors - should be 0,0,0 for background but we store them as (normal+1)/2, so 0.5,0.5,0.5 stands for 0,0,0
            GL.ClearBuffer(ClearBuffer.Color, 3, new float[] {0,0,0,0}); //positions - don't matter for background

            scene.Render(camera.ViewMatrix, projection, shadow: false);

            shadowFbo.Bind();
            GL.Viewport(0, 0, 2048, 2048);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            scene.Render(lightView, lightProjection, shadow: true);

            //hdrFbo.Bind();
            //GL.Viewport(0,0,1920,1080);
            Framebuffer.Default.Bind();
            GL.Viewport(0,0,Size.X,Size.Y);
            GL.Disable(EnableCap.DepthTest);            

            Texture2D.BindAll(
                (TextureUnit.Texture0, gBuffer.DiffuseMap),
                (TextureUnit.Texture1, gBuffer.SpecularMap),
                (TextureUnit.Texture2, gBuffer.NormalMap),
                (TextureUnit.Texture3, gBuffer.PositionMap),
                (TextureUnit.Texture4, shadowMap),
                (TextureUnit.Texture5, hdr)
            );
            
            Shaders.DeferredSunlight.Bind();

            Shaders.DeferredSunlight.DiffuseMap = TextureUnit.Texture0;
            Shaders.DeferredSunlight.SpecularMap = TextureUnit.Texture1;
            Shaders.DeferredSunlight.NormalMap = TextureUnit.Texture2;
            Shaders.DeferredSunlight.PositionMap = TextureUnit.Texture3;

            Shaders.DeferredSunlight.AmbientLightColor = new(0.1f);
            Shaders.DeferredSunlight.LightDirection = new(0,-1,0);
            Shaders.DeferredSunlight.LightColor = new(1);
            Shaders.DeferredSunlight.CameraPosition = camera.Eye;

            Shaders.DeferredSunlight.ShadowMap = TextureUnit.Texture4;
            Shaders.DeferredSunlight.ShadowView = lightView;
            Shaders.DeferredSunlight.ShadowProjection = lightProjection;
            
            VAO.Empty.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

            /*Framebuffer.Default.Bind();
            GL.Viewport(0,0,Size.X,Size.Y);
            Shaders.ToneMapping.Bind();
            Shaders.ToneMapping.Texture = TextureUnit.Texture5;
            Shaders.ToneMapping.Exposure = 0.25f;
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);*/

            SwapBuffers();
            base.OnRenderFrame(e);
        }
    }
}
