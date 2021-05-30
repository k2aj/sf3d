using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Collections.Generic;
namespace DGL 
{
    public sealed class Texture2D : IDisposable
    {
        internal int handle;
        public Texture2D(PixelInternalFormat format = PixelInternalFormat.Rgba8, Vector2i? size = null)
        {
            Format = format;
            handle = GL.GenTexture();
            Bind();
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest); 
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest); 
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.MirroredRepeat); 
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.MirroredRepeat); 
            if(size is Vector2i s)
                Allocate(s);
        }
        public void Bind() => GL.BindTexture(TextureTarget.Texture2D, handle);
        public Vector2i Size {get; private set;} = Vector2i.Zero;
        public PixelInternalFormat Format {get; private set;}
        internal bool IsBound() => GL.GetInteger(GetPName.TextureBinding2D) == handle;
        internal void EnsureBound()
        {
            #if DGL_LOG_MISSING_BIND
            if(!IsBound())
                DebugUtils.LogUniqueStackTrace("WARNING: 2D Texture not bound.", 1);
            #endif
            #if DGL_AUTOBIND
                Bind();
            #endif
        }
        public void GenerateMipmaps()
        {
            EnsureBound();
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteTexture(handle);
                handle = 0;
            }
        }

        private static Dictionary<PixelInternalFormat,(PixelFormat format,PixelType type)> allocDefaults = new Dictionary<PixelInternalFormat,(PixelFormat format,PixelType type)>{
            {PixelInternalFormat.Depth24Stencil8, (PixelFormat.DepthStencil,PixelType.UnsignedInt248)}
        };
        public void Allocate(Vector2i size)
        {
            EnsureBound();
            var allocVars = allocDefaults.GetValueOrDefault(Format, (format: PixelFormat.Rgba, type: PixelType.Float));
            GL.TexImage2D(TextureTarget.Texture2D, 0, Format, size.X, size.Y, 0, allocVars.format, allocVars.type, new IntPtr(0));
            Size = size;
        }
    }

    public sealed class Framebuffer : IDisposable
    {
        public static readonly Framebuffer Default = new Framebuffer(0);
        private int handle;

        public Framebuffer(params (FramebufferAttachment attachmentPoint, Texture2D texture)[] attachments)
        {
            handle = GL.GenFramebuffer();
            Bind(FramebufferTarget.Framebuffer);
            foreach(var attachment in attachments) 
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment.attachmentPoint, TextureTarget.Texture2D, attachment.texture.handle, 0);
            
            var errorCode = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if(errorCode != FramebufferErrorCode.FramebufferComplete)
                throw new ArgumentException($"Incomplete framebuffer: {errorCode}");
        }
        private Framebuffer(int handle) => this.handle = handle;

        public Vector2i Size {get; private set;} = Vector2i.Zero;

        public void Bind(FramebufferTarget target = FramebufferTarget.DrawFramebuffer) => GL.BindFramebuffer(target,handle);
        internal bool IsBound(FramebufferTarget target)
        {
            switch(target) {
                case FramebufferTarget.ReadFramebuffer: return GL.GetInteger(GetPName.ReadFramebufferBinding) == handle;
                case FramebufferTarget.DrawFramebuffer: return GL.GetInteger(GetPName.DrawFramebufferBinding) == handle;
                case FramebufferTarget.Framebuffer: return GL.GetInteger(GetPName.FramebufferBinding) == handle;
                default: throw new NotImplementedException();
            }
        }
        internal void EnsureBound(FramebufferTarget target)
        {
            #if DGL_LOG_MISSING_BIND
            if(!IsBound(target))
                DebugUtils.LogUniqueStackTrace("WARNING: Framebuffer not bound.", 1);
            #endif
            #if DGL_AUTOBIND
                Bind(target);
            #endif
        }

        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteFramebuffer(handle);
                handle = 0;
            }
        }
    }
}