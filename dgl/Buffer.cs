using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

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
        public void AttachAttribs<Vertex>(VBO vbo) where Vertex : struct
        {
            EnsureBound();
            vbo.EnsureBound(BufferTarget.ArrayBuffer);
            AttachAttribs(typeof(Vertex), vbo, 0, Marshal.SizeOf<Vertex>(), 0);
        }

        private void AttachAttribs(Type T, VBO vbo, int attrib, int stride, int offset)
        {
            var fields = T.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);
            foreach(var field in fields)
            {
                foreach(var layoutAttrib in field.GetCustomAttributes<layout>())
                {
                    if(attribTypes.ContainsKey(field.FieldType))
                    {
                        var typeDescriptor = attribTypes[field.FieldType];
                        for(int i=0; i<typeDescriptor.attribCount; ++i)
                        {
                            GL.EnableVertexAttribArray(attrib+layoutAttrib.location+i);
                            GL.VertexAttribPointer(
                                attrib+layoutAttrib.location+i, 
                                typeDescriptor.count, 
                                typeDescriptor.type, 
                                layoutAttrib.normalized, 
                                stride, 
                                offset + Marshal.OffsetOf(T,field.Name).ToInt32() + Marshal.SizeOf(T)*i/typeDescriptor.attribCount
                            );
                        }
                    }
                    else if(field.GetType().IsValueType)
                        AttachAttribs(
                            field.FieldType, 
                            vbo, 
                            attrib+layoutAttrib.location, 
                            stride, 
                            offset+Marshal.OffsetOf(T,field.Name).ToInt32()
                        );
                    else
                    {
                        //ignore fields without layout attribute for now
                        //probably would be a good idea to log this or something (TODO)
                    }
                }
            }
        }

        private static readonly Dictionary<Type, (VertexAttribPointerType type, int count, int attribCount)> attribTypes = new();
        public static void RegisterAttribType<T>(VertexAttribPointerType type, int count, int attribCount) => attribTypes[typeof(T)] = (type,count,attribCount);

        static VAO()
        {
            RegisterAttribType<sbyte>(VertexAttribPointerType.Byte, 1, 1);
            RegisterAttribType<byte>(VertexAttribPointerType.UnsignedByte, 1, 1);
            RegisterAttribType<short>(VertexAttribPointerType.Short, 1, 1);
            RegisterAttribType<ushort>(VertexAttribPointerType.UnsignedShort, 1, 1);
            RegisterAttribType<int>(VertexAttribPointerType.Int, 1, 1);
            RegisterAttribType<uint>(VertexAttribPointerType.UnsignedInt, 1, 1);

            RegisterAttribType<OpenTK.Mathematics.Half>(VertexAttribPointerType.HalfFloat, 1, 1);
            RegisterAttribType<float>(VertexAttribPointerType.Float, 1, 1);

            RegisterAttribType<Vector2i>(VertexAttribPointerType.Int, 2, 1);
            RegisterAttribType<Vector3i>(VertexAttribPointerType.Int, 3, 1);
            RegisterAttribType<Vector4i>(VertexAttribPointerType.Int, 4, 1);

            RegisterAttribType<Vector2h>(VertexAttribPointerType.HalfFloat, 2, 1);
            RegisterAttribType<Vector3h>(VertexAttribPointerType.HalfFloat, 3, 1);
            RegisterAttribType<Vector4h>(VertexAttribPointerType.HalfFloat, 4, 1);

            RegisterAttribType<Vector2>(VertexAttribPointerType.Float, 2, 1);
            RegisterAttribType<Vector3>(VertexAttribPointerType.Float, 3, 1);
            RegisterAttribType<Vector4>(VertexAttribPointerType.Float, 4, 1);

            RegisterAttribType<Matrix2>(VertexAttribPointerType.Float, 2, 2);
            RegisterAttribType<Matrix3>(VertexAttribPointerType.Float, 3, 3);
            RegisterAttribType<Matrix4>(VertexAttribPointerType.Float, 4, 4);

            RegisterAttribType<Color4>(VertexAttribPointerType.Float, 4, 1);
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

    [AttributeUsage(AttributeTargets.Field)]
    public class layout : Attribute 
    {
        public int location {get; init;}
        public bool normalized {get; init;} = false;
    }
}