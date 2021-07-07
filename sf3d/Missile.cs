using OpenTK.Mathematics;
using DGL.Model;
using System;
using System.Linq;
using DGL;

namespace SF3D
{
    public sealed class Missile : AerialEntity
    {
        private OmniLight light = new(){Color = new(12,8,4), AmbientColor = new(1,0.66f,0.33f), Attenuation = new(0,0.01f,0.05f,0)};

        public Missile(Model model, Vector3 position, Vector3 velocity) : base(model, new(-1,-1,-1,1,1,1)) 
        {
            LiftStrength = 0;
            EnginePower = 40;
            DragCoefficient = new(0.3f, 0.3f, 0.05f);
            GravityMultiplier = 0;
            thrust = 1;
            Velocity = velocity;
            Transform.Translation = position; 
            EngineLocations = new(){new(0,0,-1.5f)};
        }

        public override void Update(World world, Scene scene, float dt)
        {
            base.Update(world, scene, dt);
            var box = new Box3(Transform.Translation-new Vector3(3), Transform.Translation+new Vector3(3));
            if(Transform.Translation.Y <= 3 || LifeTime >= 5 || world.GetNearbyHitboxes(Transform.Translation).Any(h => h.Intersects(box)))
            {
                IsAlive = false;
                world.Spawn(new Explosion(Transform.Translation));
            }
        }
    }
}