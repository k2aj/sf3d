using System;
using OpenTK;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;

namespace SF3D
{
    class Game : GameWindow
    {
        public Game() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            UpdateFrequency = 60.0;
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
            GL.ClearColor(new Color4(252,136,231,255));
            GL.Clear(ClearBufferMask.ColorBufferBit);

            SwapBuffers();
            base.OnRenderFrame(args);
        }
    }
}
