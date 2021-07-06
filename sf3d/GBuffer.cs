using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using DGL;

using static OpenTK.Graphics.OpenGL.FramebufferAttachment;
using static OpenTK.Graphics.OpenGL.PixelInternalFormat;


namespace SF3D
{
    /// <summary> GBuffer used for deferred shading in SF3D. </summary> 
    public sealed class GBuffer : IDisposable
    {
        public Framebuffer Framebuffer {get; private set;}
        public Texture2D DiffuseMap {get; private set;}
        /// <summary> RGB = specular color, alpha = specular exponent </summary>
        public Texture2D SpecularMap {get; private set;}
        /// <summary> 
        /// Normalized surface normals. 
        /// Vectors are stored as v*0.5+0.5 because the texture can only fit values in range [0,1] and I don't want to use float textures
        /// Normal=0 means the pixel is a background pixel and should not be shaded.
        /// </summary>
        public Texture2D NormalMap {get; private set;}
        /// <summary> Fragment positions in world space. </summary>
        //TODO remove this later and recompute positions from projection+view matrix and ZBuffer instead
        public Texture2D PositionMap {get; private set;}
        public Texture2D ZBuffer {get; private set;}

        private Vector2i size;
        public Vector2i Size
        {
            get => size;
            set
            {
                //Resize all textures and recreate framebuffer
                size = value;
                Framebuffer.Dispose();
                DiffuseMap.Bind();
                DiffuseMap.Allocate(value);
                SpecularMap.Bind();
                SpecularMap.Allocate(value);
                NormalMap.Bind();
                NormalMap.Allocate(value);
                PositionMap.Bind();
                PositionMap.Allocate(value);
                ZBuffer.Bind();
                ZBuffer.Allocate(value);
                Framebuffer = CreateFramebuffer();
            }
        }

        private Framebuffer CreateFramebuffer()
        {
            var result = new Framebuffer(
                (ColorAttachment0, DiffuseMap),
                (ColorAttachment1, SpecularMap),
                (ColorAttachment2, NormalMap),
                (ColorAttachment3, PositionMap),
                (DepthAttachment,  ZBuffer)
            );
            GL.DrawBuffers(4, new DrawBuffersEnum[]{DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3});
            return result;
        }

        public GBuffer(Vector2i size)
        {
            DiffuseMap  = new(Rgb8, size);
            SpecularMap = new(Rgba8, size);
            // For some reason specular lighting flickers if the bitdepth for normals is too low
            // rgba8, rgb10 and r11g11b10f formats all experience flickering
            NormalMap = new(Rgb16, size);
            PositionMap = new(Rgb32f, size);
            ZBuffer = new(DepthComponent24, size);
            Framebuffer = CreateFramebuffer();
        }

    

        public void Dispose()
        {   
            Framebuffer.Dispose();
            DiffuseMap.Dispose();
            SpecularMap.Dispose();
            NormalMap.Dispose();
            PositionMap.Dispose();
            ZBuffer.Dispose();
        }
    }  
}