using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using DGL;
using DGL.Model;


namespace SF3D
{
    /// <summary>
    /// Responsible for efficient drawing of 3D objects
    /// </summary>
    public sealed class Scene
    {
        private Dictionary<ObjectID,(Model,Matrix4)> objects = new();
        private List<ObjectID> freeList = new();
        int curObjectID = 0;
        public struct ObjectID
        {
            private int value;
            public ObjectID(int value) => this.value = value;
        }
        public ObjectID Add(Model model, Matrix4 modelMatrix) 
        {
            ObjectID result;
            if(freeList.Count > 0)
            {
                result = freeList[freeList.Count-1];
                freeList.RemoveAt(freeList.Count-1);
            }
            else
                result = new(++curObjectID);
            objects[result] = (model, modelMatrix);
            return result;
        }
        public void Remove(ObjectID id)
        {
            if(objects.Remove(id))
                freeList.Add(id);
        }
        public void SetModelMatrix(ObjectID id, Matrix4 matrix) => objects[id] = (objects[id].Item1, matrix);

        public void Render(Matrix4 view, Matrix4 projection, bool shadow)
        {
            SceneShaderProgram vvv = shadow ? Shaders.Shadow : Shaders.GBufferVVV;

            vvv.Bind();
            GL.VertexAttrib4(AttribLocations.Specular, 0.5f, 0.5f, 0.5f, 0.5f);
            vvv.Projection = projection;
            vvv.View = view;
            foreach(var (_, (model, modelMatrix)) in objects)
            {
                vvv.Model = modelMatrix;
                model.Bind(shadow);
                model.Draw(shadow);
            }
        }
    }
}