using System;
using System.Runtime.InteropServices;

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL;

using DGL;

namespace SF3D
{
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
        private ShaderProgram program;
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            UpdateFrequency = 60.0;

            vbo = new();
            vbo.Allocate<Vertex>(vertices.Length);
            vbo.Upload(vertices.AsSpan());

            vao = new();
            vao.AttachAttribs<Vertex>(vbo);

            vShader = new(ShaderType.VertexShader, @"
                #version 330 core
                layout(location=0) in vec2 position;
                layout(location=1) in vec4 color;

                out vec4 vColor;

                void main() {
                    gl_Position = vec4(position, 0.5, 1.0);
                    vColor = color;
                }
            ");
            fShader = new(ShaderType.FragmentShader, @"
                #version 330 core
                in vec4 vColor;
                out vec4 fColor;
                void main() {fColor = vColor;}
            ");
            program = new(vShader, fShader);
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

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            GL.Viewport(0, 0, Size.X, Size.Y);
            GL.ClearColor(new Color4(252,136,231,255));
            GL.Clear(ClearBufferMask.ColorBufferBit);

            program.Bind();
            vao.Draw(PrimitiveType.TriangleFan, 0, vertices.Length);

            SwapBuffers();
            base.OnRenderFrame(args);
        }
    }
}
