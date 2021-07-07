using OpenTK.Mathematics;
using DGL.Model;
using DGL;
using System;

namespace SF3D
{
    public sealed class Debris : Entity
    {
        private readonly Vector3 RotationAxis; 
        private readonly float AngularVelocity;       
        public Debris(Model model, float size, Vector3 position, Vector3 velocity, float angularVelocity) : base(model)
        {
            var rng = new Random();
            RotationAxis = (new Vector3(rng.NextFloat(), rng.NextFloat(), rng.NextFloat()) - new Vector3(0.5f)).Normalized();
            Velocity = velocity;
            AngularVelocity = angularVelocity;
            Transform.Scale = size;
            Transform.Translation = position;
        }
        public override void Update(World world, Scene scene, float dt)
        {
            base.Update(world, scene, dt);
            Velocity.Y -= 20*dt; //gravity
            IsAlive = Velocity.Y > 0 || Transform.Translation.Y > 0;
            Transform.Orientation = Quaternion.FromAxisAngle(RotationAxis, AngularVelocity*LifeTime);
            UpdateModelMatrix(scene);
        }
    }
}