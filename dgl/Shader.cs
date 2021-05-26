using System;
using System.Linq;
using OpenTK.Graphics.OpenGL;

namespace DGL {

    public sealed class Shader : IDisposable 
    {
        internal int handle;
        public ShaderType Type {get; init;}

        public Shader(ShaderType type, string sourceCode)
        {
            Type = type;
            handle = GL.CreateShader(type);
            GL.ShaderSource(handle, sourceCode);
            GL.CompileShader(handle);
            GL.GetShader(handle, ShaderParameter.CompileStatus, out int compileStatus);
            if(compileStatus == 0) {
                var ex = new ArgumentException("Failed to compile shader:\n"+GL.GetShaderInfoLog(handle));
                Dispose();
                throw ex;
            }
        }

        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteShader(handle);
                handle = 0;
            }
        }
    }

    public sealed class ShaderProgram : IDisposable
    {
        internal int handle;

        public ShaderProgram(params Shader[] shaders)
        {
            handle = GL.CreateProgram();
            foreach(var shader in shaders)
                GL.AttachShader(handle, shader.handle);
            GL.LinkProgram(handle);
            GL.GetProgram(handle, GetProgramParameterName.LinkStatus, out int linkStatus);
            if(linkStatus == 0) {
                var ex = new ArgumentException("Failed to link shader program:\n"+GL.GetProgramInfoLog(handle));
                Dispose();
                throw ex;
            }
        }

        public void Bind() => GL.UseProgram(handle);
        internal void EnsureBound() => Bind();

        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteProgram(handle);
                handle = 0;
            }
        }
    }
}