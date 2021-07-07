using System;
using DGL;
using DGL.Model;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SF3D
{
    public class Entity
    {
        public bool IsAlive = true;
        public Transform3D Transform = Transform3D.Identity;
        public float LifeTime {get; private set;} = 0;
        public Vector3 Velocity = new(0);
        private readonly Model Model;
        private Scene.ObjectID objectID;
        public Entity(Model model) => Model = model;
        public virtual void OnSpawned(World world, Scene scene)
        {
            objectID = scene.Add(Model, Transform.ToMatrix());
        }
        public virtual void OnDespawned(World world, Scene scene)
        {
            scene.Remove(objectID);
        }
        public virtual void Update(World world, Scene scene, float dt)
        {
            Transform.Translation += Velocity * dt;
            LifeTime += dt;
        }
        public void UpdateModelMatrix(Scene scene)
        {
            scene.SetModelMatrix(objectID, Transform.ToMatrix());
        }
    }
}