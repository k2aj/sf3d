using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;

// This is only used for loading images
using System.Drawing;

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

        public static void BindAll(params (TextureUnit target, Texture2D texture, Sampler sampler)[] textures) 
        {
            foreach(var t in textures)
            {
                GL.ActiveTexture(t.target);
                t.texture.Bind();
                t.sampler.Bind((TextureUnit)(t.target - TextureUnit.Texture0));
            }
        }
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
            {PixelInternalFormat.Depth24Stencil8, (PixelFormat.DepthStencil,PixelType.UnsignedInt248)},
            {PixelInternalFormat.DepthComponent24, (PixelFormat.DepthComponent,PixelType.UnsignedInt)},
            {PixelInternalFormat.DepthComponent32, (PixelFormat.DepthComponent,PixelType.UnsignedInt)}
        };
        public void Allocate(Vector2i size)
        {
            EnsureBound();
            var allocVars = allocDefaults.GetValueOrDefault(Format, (format: PixelFormat.Rgba, type: PixelType.Float));
            GL.TexImage2D(TextureTarget.Texture2D, 0, Format, size.X, size.Y, 0, allocVars.format, allocVars.type, new IntPtr(0));
            Size = size;
        }

        public void Upload(Bitmap bitmap, Vector2i offset)
        {
            EnsureBound();
            var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            if(data.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                throw new FormatException($"Pixel format not supported: {data.PixelFormat}.");
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, offset.X, offset.Y, bitmap.Width, bitmap.Height, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);
        }

        public void Allocate(Bitmap bitmap)
        {
            EnsureBound();
            Allocate(new Vector2i(bitmap.Width, bitmap.Height));
            Upload(bitmap, Vector2i.Zero);
        }
    }

    public class CubeMap : IDisposable
    {
        private int handle;

        public CubeMap(Bitmap[] bitmaps)
        {
            handle = GL.GenTexture();
            Bind();
            for(int i=0; i<6; ++i)
            {
                var data = bitmaps[i].LockBits(new Rectangle(0, 0, bitmaps[i].Width, bitmaps[i].Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                if(data.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                    throw new FormatException($"Pixel format not supported: {data.PixelFormat}.");
                GL.TexImage2D((TextureTarget)(TextureTarget.TextureCubeMapPositiveX+i), 0, PixelInternalFormat.Rgb8, bitmaps[i].Width, bitmaps[i].Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
                bitmaps[i].UnlockBits(data);
            }
            
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear); 
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear); 
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge); 
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge); 
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int) TextureWrapMode.ClampToEdge); 
        }
        public CubeMap(string path) : this(
            (new string[]{"right","left","top","bottom","front","back"})
            .Select(x => new Bitmap(Path.Combine(path,x+".png")))
            .ToArray()
        ) {}

        public void Bind() => GL.BindTexture(TextureTarget.TextureCubeMap, handle);
        internal bool IsBound() => GL.GetInteger(GetPName.TextureBindingCubeMap) == handle;
        internal void EnsureBound()
        {
            #if DGL_LOG_MISSING_BIND
            if(!IsBound())
                DebugUtils.LogUniqueStackTrace("WARNING: Skybox not bound.", 1);
            #endif
            #if DGL_AUTOBIND
                Bind();
            #endif
        }

        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteTexture(handle);
                handle = 0;
            }
        }
    }

    public sealed class Sampler : IDisposable
    {
        private int handle;

        public TextureMinFilter MinFilter {set => GL.SamplerParameter(handle, SamplerParameterName.TextureMinFilter, (int) value);}
        public TextureMagFilter MagFilter {set => GL.SamplerParameter(handle, SamplerParameterName.TextureMagFilter, (int) value);}

        public TextureWrapMode WrapS {set => GL.SamplerParameter(handle, SamplerParameterName.TextureWrapS, (int) value);}
        public TextureWrapMode WrapT {set => GL.SamplerParameter(handle, SamplerParameterName.TextureWrapT, (int) value);}
        public TextureWrapMode Wrap {set {WrapS = value; WrapT = value;}}

        public Sampler() => handle = GL.GenSampler();

        public void Bind(TextureUnit target) => GL.BindSampler((int)target, handle);

        public void Dispose()
        {
            if(handle != 0)
            {
                GL.DeleteSampler(handle);
                handle = 0;
            }
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

            // At this point we know all attached textures have the same size,
            // because otherwise this fbo would be incomplete and code above would throw
            Size = attachments[0].texture.Size;
        }
        private Framebuffer(int handle) => this.handle = handle;

        public Vector2i Size {get; private set;}

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

    public record AtlasSlice(Atlas Atlas, Box2i Area);

    public sealed class Atlas : IDisposable {
        public Texture2D Texture {get; private set;}
        public Atlas(Vector2i initialSizeHint)
        {
            int log2InitialSize = CeilLog2(Math.Max(initialSizeHint.X, initialSizeHint.Y));
            freeBlocks = new();
            for(int i=0; i<=log2InitialSize; ++i)
                freeBlocks.Add(new());

            int size = 1<<log2InitialSize;
            Texture = new(size: new Vector2i(size));

            freeBlocks[log2InitialSize].Add(new(0,0,size,size));
        }
        public Atlas() : this(new(1024, 1024)) {}
        private List<List<Box2i>> freeBlocks;

        private void Expand() {
            Texture2D newTexture = new Texture2D(size: Texture.Size*2); //double size of original texture
            freeBlocks[^1].Add(new(Texture.Size.X,0,2*Texture.Size.X,Texture.Size.Y)); //add empty blocks corresponding to new free space
            freeBlocks[^1].Add(new(0,Texture.Size.Y,Texture.Size.X,2*Texture.Size.Y)); 
            freeBlocks[^1].Add(new(Texture.Size.X,Texture.Size.Y,2*Texture.Size.X,2*Texture.Size.Y));
            freeBlocks.Add(new());

            // copy contents from old texture to new texture via framebuffer blit (not the most elegant solution, I know, but I don't have time)
            using(
                Framebuffer srcFbo = new Framebuffer((FramebufferAttachment.ColorAttachment0, Texture)),
                            dstFbo = new Framebuffer((FramebufferAttachment.ColorAttachment0, newTexture))
            ) {
                srcFbo.Bind(FramebufferTarget.ReadFramebuffer);
                dstFbo.Bind(FramebufferTarget.DrawFramebuffer);
                GL.BlitFramebuffer(0, 0, Texture.Size.X, Texture.Size.Y, 0, 0, Texture.Size.X, Texture.Size.Y, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);
            }
            Texture.Dispose();
            Texture = newTexture;
        }

        public AtlasSlice Allocate(Vector2i size)
        {
            int log2BlockSize = CeilLog2(Math.Max(size.X, size.Y));
            return new(this, Allocate(log2BlockSize));
        }

        public AtlasSlice Allocate(Bitmap bitmap)
        {
            var size = new Vector2i(bitmap.Width, bitmap.Height);
            var block = Allocate(size).Area;
            var topLeft = block.Min + (block.Size - size)/2;
            Texture.Bind();
            Texture.Upload(bitmap, topLeft);
            return new(this, new(topLeft, topLeft+new Vector2i(bitmap.Width, bitmap.Height)));
        }

        private Box2i Allocate(int log2BlockSize)
        {
            if(freeBlocks.Count > log2BlockSize && freeBlocks[log2BlockSize].Any()) {
                var result = freeBlocks[log2BlockSize].Last();
                freeBlocks[log2BlockSize].RemoveAt(freeBlocks[log2BlockSize].Count-1);
                return result;
            } else {
                // Ensure we have blocks big enough to subdivide
                while(log2BlockSize+2 >= freeBlocks.Count) Expand();
                // Allocate 2x bigger block than necessary and subdivide it into smaller blocks
                var blk = Allocate(log2BlockSize+1);
                freeBlocks[log2BlockSize].Add(new(blk.Min + new Vector2i(blk.Size.X/2,0), blk.Min + new Vector2i(blk.Size.X,blk.Size.Y/2)));
                freeBlocks[log2BlockSize].Add(new(blk.Min + new Vector2i(0,blk.Size.Y/2), blk.Min + new Vector2i(blk.Size.X/2,blk.Size.Y)));
                freeBlocks[log2BlockSize].Add(new(blk.Min+blk.HalfSize, blk.Max));
                return new Box2i(blk.Min, blk.Min + blk.HalfSize);
            }
        }

        public void Dispose() {
            Texture.Dispose();
        }

        private static int CeilLog2(int n)
        {
            int r = Math.ILogB(n);
            if(n > 1<<r) ++r;
            return r;
        }
    }
}