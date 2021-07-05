using System;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace DGL.Model
{
    public sealed class Model : IDisposable
    {
        private VBO indices = new(), positions = new(), normals = new(), diffuseUv = new(), specularUv = new();
        private VAO vao = new(), shadowVao = new();
        private int indexCount;

        public Model(Span<int> indices, Span<Vector3> positions, Span<Vector3> normals, Span<Vector2> diffuseUvs, Span<Vector2> specularUvs)
        {
            vao.Bind();

            vao.AttachIndices(this.indices);
            this.indices.Allocate(indices, target: BufferTarget.ElementArrayBuffer);
            indexCount = indices.Length;

            this.positions.Bind();
            this.positions.Allocate(positions);
            vao.AttachAttribs<Vector3>(this.positions, AttribLocations.Position);

            this.normals.Bind();
            this.normals.Allocate(normals);
            vao.AttachAttribs<Vector3>(this.normals, AttribLocations.Normal);

            this.diffuseUv.Bind();
            this.diffuseUv.Allocate(diffuseUvs);
            vao.AttachAttribs<Vector2>(this.diffuseUv, AttribLocations.Diffuse);

            this.specularUv.Bind();
            this.specularUv.Allocate(specularUvs);
            vao.AttachAttribs<Vector2>(this.specularUv, AttribLocations.Specular);

            shadowVao.Bind();
            shadowVao.AttachIndices(this.indices);
            this.positions.Bind();
            shadowVao.AttachAttribs<Vector3>(this.positions, AttribLocations.Position);
        }

        public void Bind(bool shadow = false) => 
            (shadow ? shadowVao : vao).Bind();
        internal bool IsBound(bool shadow = false) => (shadow ? shadowVao : vao).IsBound();
        public void EnsureBound(bool shadow) 
        {
            #if DGL_LOG_MISSING_BIND
            if(!IsBound(shadow))
                DebugUtils.LogUniqueStackTrace("WARNING: Model not bound.");
            #endif
            #if DGL_AUTOBIND
                Bind(shadow);
            #endif
        }

        public void Draw(bool shadow = false)
        {
            EnsureBound(shadow);
            (shadow ? shadowVao : vao).DrawIndexed(PrimitiveType.Triangles, 0, indexCount, DrawElementsType.UnsignedInt);
        }

        public void Dispose()
        {
            vao.Dispose();
            shadowVao.Dispose();
            indices.Dispose();
            positions.Dispose();
            normals.Dispose();
            diffuseUv.Dispose();
            specularUv.Dispose();
        }
    }
}