using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace DGL 
{
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

    public class ShaderProgram : IDisposable
    {
        internal int handle;
        private Dictionary<string,int> uniformLocations = new();

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
        internal bool IsBound() => GL.GetInteger(GetPName.CurrentProgram) == handle;
        internal void EnsureBound()
        {
            #if DGL_LOG_MISSING_BIND
            if(!IsBound())
                DebugUtils.LogUniqueStackTrace("WARNING: Shader program not bound.", 1);
            #endif
            #if DGL_AUTOBIND
                Bind();
            #endif
        }

        public int GetUniformLocation(string name)
        {
            if(uniformLocations.TryGetValue(name, out int l))
                return l;
            int location = GL.GetUniformLocation(handle, name);
            uniformLocations[name] = location;
            return location;
        }

        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteProgram(handle);
                handle = 0;
            }
        }
    }

    public sealed class ShaderLoader : IDisposable
    {
        private Dictionary<string,Shader> shaders = new();

        private void Load(string path)
        {
            string extension = path.Split('.')[^1];
            ShaderType type;
            switch(extension)
            {
                case "vert": 
                case "v": 
                type = ShaderType.VertexShader; 
                break;

                case "frag":
                case "f":
                type = ShaderType.FragmentShader;
                break;

                case "geom":
                case "g":
                type = ShaderType.GeometryShader;
                break;

                default: throw new NotImplementedException($"Extension .{extension} not supported.");
            }
            shaders[path] = new Shader(type, File.ReadAllText(path));
        }

        public Shader Get(string path)
        {
            path = Path.GetFullPath(path);
            if(!shaders.ContainsKey(path)) Load(path);
            return shaders[path];
        }

        public void Dispose()
        {
            foreach(var (_, shader) in shaders)
                shader.Dispose();
        }
    }
}