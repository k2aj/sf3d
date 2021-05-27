using System;
using System.IO;
using System.Runtime.InteropServices;

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;

using DGL;

namespace SF3D
{
    sealed class ExampleShaderProgram : ShaderProgram
    {
        private int uModel;
        public ExampleShaderProgram(params Shader[] shaders) : base(shaders) => uModel = GetUniformLocation("model");
        public Matrix4 Model {set {EnsureBound(); GL.UniformMatrix4(uModel, false, ref value);}}
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Vertex 
    {
        [layout(location=0)] Vector2 Position;
        [layout(location=1)] Color4 Color;

        public Vertex(Vector2 position, Color4 color) => (Position, Color) = (position, color);
    }

    class Game : GameWindow
    {
        private static Vertex[] vertices = {
            new(new(-1,-1), Color4.OrangeRed),
            new(new(1,-1), Color4.ForestGreen),
            new(new(1,1), Color4.RoyalBlue),
            new(new(-1,1), Color4.LightGoldenrodYellow)
        };
        
        private VBO vbo;
        private VAO vao;
        private Shader vShader, fShader;
        private ExampleShaderProgram program;
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            UpdateFrequency = 60.0;

            vbo = new();
            vbo.Bind(BufferTarget.ArrayBuffer);
            vbo.Allocate<Vertex>(vertices.Length);
            vbo.Upload(vertices.AsSpan());

            vao = new();
            vao.Bind();
            vao.AttachAttribs<Vertex>(vbo);

            vShader = new(ShaderType.VertexShader, @"
                #version 330 core
                layout(location=0) in vec2 position;
                layout(location=1) in vec4 color;

                out vec4 vColor;
                uniform mat4 model;

                void main() {
                    gl_Position = model*vec4(position, 0.5, 1.0);
                    vColor = color;
                }
            ");
            fShader = new(ShaderType.FragmentShader, @"
                #version 330 core
                in vec4 vColor;
                out vec4 fColor;
                void main() {fColor = vColor;}
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

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            var input = KeyboardState;
            if(input.IsKeyDown(Keys.Escape))
                Close();

            base.OnUpdateFrame(e);
        }

        private float t = 0;

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            t += (float) args.Time;

            GL.Viewport(0, 0, Size.X, Size.Y);
            GL.ClearColor(new Color4(252,136,231,255));
            GL.Clear(ClearBufferMask.ColorBufferBit);

            

            program.Bind();
            program.Model = Matrix4.CreateScale(0.5f,0.2f,1)*Matrix4.CreateRotationZ(t)*Matrix4.CreateTranslation(0.5f,0,0);
            vao.Bind();
            vao.Draw(PrimitiveType.TriangleFan, 0, vertices.Length);

            SwapBuffers();
            base.OnRenderFrame(args);
        }
    }
}
