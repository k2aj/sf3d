using System;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace DGL {

    /// <summary>
    /// Represents an OpenGL buffer object.
    /// </summary>
    public sealed class VBO : IDisposable {
        private int handle = 0;
        public int Capacity {get; private set;}
        public VBO() => handle = GL.GenBuffer();
        public void Bind(BufferTarget target = BufferTarget.ArrayBuffer) => GL.BindBuffer(target, handle);
        internal void EnsureBound(BufferTarget target) => Bind(target);

        public void Allocate<T>(int count, BufferUsageHint usage = BufferUsageHint.StaticDraw, BufferTarget target = BufferTarget.ArrayBuffer) where T : struct
        {
            EnsureBound(target);
            GL.BufferData(target, Capacity = Marshal.SizeOf<T>() * count, new IntPtr(0), usage);
        }

        public void Upload<T>(Span<T> data, int index = 0, BufferTarget target = BufferTarget.ArrayBuffer) where T : struct {
            int start = Marshal.SizeOf<T>() * index;
            int size = Marshal.SizeOf<T>() * data.Length;
            if(start < 0 || start+size > Capacity)
                throw new IndexOutOfRangeException("Upload into buffer object out of bounds.");
            EnsureBound(target);
            GL.BufferSubData<T>(target, new IntPtr(start), size, ref MemoryMarshal.AsRef<T>(MemoryMarshal.AsBytes(data)));
        }
            
        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteBuffer(handle);
                Capacity = 0;
                handle = 0;
            }
        }
    }

    public sealed class VAO : IDisposable 
    {
        private int handle;

        public VAO() => handle = GL.GenVertexArray();
        private VAO(int handle) => this.handle = handle;

        public void Bind() => GL.BindVertexArray(handle);
        internal void EnsureBound() => Bind();

        public void VertexAttribPointer(VBO vbo, int attrib, int size, VertexAttribPointerType type, bool normalized, int stride, IntPtr offset)
        {
            vbo.EnsureBound(BufferTarget.ArrayBuffer);
            EnsureBound();
            GL.EnableVertexAttribArray(attrib);
            GL.VertexAttribPointer(attrib, size, type, normalized, stride, offset);
        }

        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteVertexArray(handle);
                handle = 0;
            }
        }

        public void Draw(PrimitiveType mode, int first, int count)
        {
            EnsureBound();
            GL.DrawArrays(mode, first, count);
        }
    }
}