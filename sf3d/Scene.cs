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
        private List<OmniLight> lights = new();
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
        public void Add(OmniLight light) => lights.Add(light);
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

        public void RenderLights(Camera camera)
        {
            Shaders.DeferredOmni.EnsureBound();
            Shaders.DeferredOmni.Projection = camera.ProjectionMatrix;
            Shaders.DeferredOmni.View = camera.ViewMatrix;
            Shaders.DeferredOmni.ZNear = camera.ZNear;

            Models.OmniLight.Bind(shadow: true);

            var clippingPlaneNormal = camera.LookDir.Normalized();
            foreach(var light in lights)
            {
                // Cull lights which don't affect anything visible on screen
                //var distanceFromNearPlane = Vector3.Dot((light.Position - camera.Eye), clippingPlaneNormal) - camera.ZNear;
                //if(distanceFromNearPlane >= -light.Range) 
                {
                    Shaders.DeferredOmni.Light = light;
                    Models.OmniLight.Draw(shadow: true);
                }

            }
        }
    }

    public sealed class OmniLight
    {
        public Vector3 Position = Vector3.Zero;
        public Vector3 Color = Vector3.One;
        public Vector3 AmbientColor = new(0.1f);
        public float Range {
            get
            {
                float delta = Attenuation.X*Attenuation.X - 4*Attenuation.Y*(1-1/Attenuation.Z);
                return (-Attenuation.X+MathF.Sqrt(delta))/(2*Attenuation.Y);
            }
        }
        // const coefficient is always 1
        // X - linear coefficient
        // Y - quadratic coefficient
        // Z - bias (gets subtracted from attenuation value, used to limit range of the light source)
        // W - reserved for future use
        public Vector4 Attenuation = new(0.1f);
    }
}