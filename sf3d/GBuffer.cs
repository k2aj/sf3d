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
        /// <summary> Normalized surface normals in world space. </summary>
        public Texture2D NormalMap {get; private set;}
        /// <summary> Fragment positions in world space. </summary>
        //TODO remove this later and recompute positions from projection+view matrix and ZBuffer instead
        public Texture2D PositionMap {get; private set;}
        public Texture2D ZBuffer {get; private set;}

        public GBuffer(Vector2i size)
        {
            Framebuffer = new(
                (ColorAttachment0, DiffuseMap  = new(Rgb8,             size)),
                (ColorAttachment1, SpecularMap = new(Rgba8,            size)),
                // For some reason specular lighting flickers if the bitdepth for normals is too low
                // rgba8, rgb10 and r11g11b10f formats all experience flickering
                (ColorAttachment2, NormalMap   = new(Rgb16,            size)),
                (ColorAttachment3, PositionMap = new(Rgb32f,           size)),
                (DepthAttachment,  ZBuffer     = new(DepthComponent24, size))
            );
            GL.DrawBuffers(4, new DrawBuffersEnum[]{DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3});
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