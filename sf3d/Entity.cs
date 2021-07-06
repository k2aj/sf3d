using System;
using DGL;
using DGL.Model;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SF3D
{
    public class Entity
    {
        public Transform3D Transform = Transform3D.Identity;
        public Vector3 Velocity = new(0);
        private readonly Model Model;
        private Scene.ObjectID objectID;
        public Entity(Model model) => Model = model;
        public virtual void OnSpawned(Scene scene)
        {
            objectID = scene.Add(Model, Transform.ToMatrix());
        }
        public virtual void OnDespawned(Scene scene)
        {
            scene.Remove(objectID);
        }
        public virtual void Update(Scene scene, float dt)
        {
            Transform.Translation += Velocity * dt;
            scene.SetModelMatrix(objectID, Transform.ToMatrix());
        }
    }
}