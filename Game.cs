using System;
using System.IO;
using System.Runtime.InteropServices;

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;

using DGL;
using DGL.Model;

namespace SF3D
{
    class Game : GameWindow
    {
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

        private Camera camera = new(){Target = new(0.5f,0,0), Eye = new(0,0,-3)};
        private const float cameraVelocity = 3;
        private Model model;
        private GBuffer gBuffer;
        private Framebuffer shadowFbo;
        private Texture2D shadowMap;
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            UpdateFrequency = 60.0;

            model = new(indices.AsSpan(), positions.AsSpan(), normals.AsSpan(), diffuse.AsSpan());

            gBuffer = new(size: new(1920,1080));
            Shaders.Init();

            shadowMap = new Texture2D(PixelInternalFormat.DepthComponent24, new(2048,2048));
            shadowFbo = new((FramebufferAttachment.DepthAttachment, shadowMap));
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
            base.OnUpdateFrame(e);
        }

        private void RenderScene(SceneShaderProgram program, bool shadow)
        {
            model.Bind();
            
            program.Model = Matrix4.CreateScale(1f)*Matrix4.CreateFromAxisAngle(new(1,-1,1), 1.3f*t+0.5f)*Matrix4.CreateTranslation(0.5f,-3,0);
            model.Draw();

            program.Model = Matrix4.CreateScale(0.5f)*Matrix4.CreateFromAxisAngle(new(1,1,1), t)*Matrix4.CreateTranslation(0.5f,0,0);
            model.Draw();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            gBuffer.Framebuffer.Bind();
            GL.Viewport(0, 0, 1920, 1080);
            GL.Enable(EnableCap.DepthTest);

            float aspectRatio = Size.X / Size.Y;
            Matrix4.CreatePerspectiveFieldOfView(MathF.PI/2, aspectRatio, 0.1f, 20f, out Matrix4 projection);

            var lightPos = new Vector3(0,10,0);
            var lightProjection = Matrix4.CreateOrthographicOffCenter(-10, 10, -10, 10, 2, 20);
            var lightView = Matrix4.LookAt(lightPos, new Vector3(0.5f,0,0), Vector3.UnitY);

            GL.ClearColor(0,0,0,0);
            GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] {252/255f,136/255f,231/255f,1f}); //background color
            GL.ClearBuffer(ClearBuffer.Color, 1, new float[] {0,0,0,0}); //specular color - doesn't matter for background
            GL.ClearBuffer(ClearBuffer.Color, 2, new float[] {0.5f,0.5f,0.5f,0}); //normal vectors - should be 0,0,0 for background but we store them as (normal+1)/2, so 0.5,0.5,0.5 stands for 0,0,0
            GL.ClearBuffer(ClearBuffer.Color, 3, new float[] {0,0,0,0}); //positions - don't matter for background

            Shaders.GBufferVVV.Bind();
            Shaders.GBufferVVV.View = camera.ViewMatrix;
            Shaders.GBufferVVV.Projection = projection;
            RenderScene(Shaders.GBufferVVV, shadow: false);

            shadowFbo.Bind();
            GL.Viewport(0, 0, 2048, 2048);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            

            Shaders.Shadow.Bind();
            Shaders.Shadow.View = lightView;
            Shaders.Shadow.Projection = lightProjection;
            RenderScene(Shaders.Shadow, shadow: true);

            Framebuffer.Default.Bind();
            GL.Viewport(0,0,Size.X,Size.Y);
            GL.Disable(EnableCap.DepthTest);

            Texture2D.BindAll(
                (TextureUnit.Texture0, gBuffer.DiffuseMap),
                (TextureUnit.Texture1, gBuffer.SpecularMap),
                (TextureUnit.Texture2, gBuffer.NormalMap),
                (TextureUnit.Texture3, gBuffer.PositionMap),
                (TextureUnit.Texture4, shadowMap)
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

            SwapBuffers();
            base.OnRenderFrame(e);
        }
    }
}
