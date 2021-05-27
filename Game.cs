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
    sealed class ExampleShaderProgram : ShaderProgram
    {
        private int uModel, uView, uProjection, uLightDirection, uLightColor, uCameraPosition, uAmbientLight, uNormalMatrix;
        public ExampleShaderProgram(params Shader[] shaders) : base(shaders)
        {
            uModel = GetUniformLocation("model");
            uView = GetUniformLocation("view");
            uProjection = GetUniformLocation("projection");

            uLightDirection = GetUniformLocation("lightDirection");
            uLightColor = GetUniformLocation("lightColor");
            uAmbientLight = GetUniformLocation("ambientLight");
            uCameraPosition = GetUniformLocation("cameraPosition");
            uNormalMatrix = GetUniformLocation("normalMatrix");
        }
        public Matrix4 Model 
        {
            set 
            {
                EnsureBound(); 
                GL.UniformMatrix4(uModel, false, ref value);
                var normalMatrix = new Matrix3(value);
                normalMatrix.Invert();
                normalMatrix.Transpose();
                GL.UniformMatrix3(uNormalMatrix, false, ref normalMatrix);
            }
        }
        public Matrix4 View {set {EnsureBound(); GL.UniformMatrix4(uView, false, ref value);}}
        public Matrix4 Projection {set {EnsureBound(); GL.UniformMatrix4(uProjection, false, ref value);}}

        public Vector3 LightDirection {set {EnsureBound(); GL.Uniform3(uLightDirection, value.Normalized());}}
        public Vector3 LightColor {set {EnsureBound(); GL.Uniform3(uLightColor, value);}}
        public Vector3 CameraPosition {set {EnsureBound(); GL.Uniform3(uCameraPosition, value);}}
        public Vector3 AmbientLight {set {EnsureBound(); GL.Uniform3(uAmbientLight, value);}}
    }

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

        private Camera camera = new(){Target = new(0.5f,0,0), Eye = new(0,0,-10)};
        private const float cameraVelocity = 3;
        private Model model;
        private Shader vShader, fShader;
        private ExampleShaderProgram program;
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            UpdateFrequency = 60.0;

            model = new(indices.AsSpan(), positions.AsSpan(), normals.AsSpan(), diffuse.AsSpan());

            vShader = new(ShaderType.VertexShader, @"
                #version 330 core
                layout(location=0) in vec3 position;
                layout(location=2) in vec3 normal;
                layout(location=3) in vec4 diffuse;

                out vec3 vDiffuse, vNormal, vPosition;
                uniform mat4 model, view, projection;

                void main() 
                {
                    vPosition = (model*vec4(position,1.0)).xyz;
                    gl_Position = projection*view*model*vec4(position, 1.0);
                    vDiffuse = diffuse.rgb;
                    vNormal = normal;
                }
            ");
            fShader = new(ShaderType.FragmentShader, @"
                #version 330 core

                in vec3 vDiffuse, vNormal, vPosition;
                uniform vec3 ambientLight, lightDirection, lightColor, cameraPosition;
                uniform mat3 normalMatrix;
                out vec3 fragColor;

                void main() 
                {
                    vec3 norm = normalize(normalMatrix * vNormal);
                    vec3 dirToCamera = normalize(cameraPosition - vPosition);
                    
                    vec3 diffuseLight = max(ambientLight, dot(-lightDirection,norm) * lightColor) ;

                    vec3 reflectedDir = reflect(-lightDirection, norm);
                    vec3 specularLight = pow(max(dot(reflectedDir, dirToCamera), 0), 32) * lightColor * 0.5;

                    fragColor = diffuseLight * vDiffuse + specularLight * vDiffuse;
                }
            ");
            program = new ExampleShaderProgram(vShader, fShader);
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

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Viewport(0, 0, Size.X, Size.Y);
            float aspectRatio = Size.X / Size.Y;
            Matrix4.CreatePerspectiveFieldOfView(MathF.PI/2, aspectRatio, 0.1f, 10f, out Matrix4 projection);

            GL.ClearColor(new Color4(252,136,231,255));
            GL.Enable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);

            program.Bind();
            program.Model = Matrix4.CreateScale(0.5f)*Matrix4.CreateFromAxisAngle(new(1,1,1), t)*Matrix4.CreateTranslation(0.5f,0,0);
            program.View = camera.ViewMatrix;
            program.Projection = projection;

            program.AmbientLight = new(0.1f);
            program.LightDirection = new(0,-10,0);
            program.LightColor = new(1);
            program.CameraPosition = camera.Eye;

            model.Bind();
            model.Draw();

            SwapBuffers();
            base.OnRenderFrame(e);
        }
    }
}
