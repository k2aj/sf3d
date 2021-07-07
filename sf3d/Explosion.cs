using System;
using System.Linq;
using DGL.Model;
using OpenTK.Mathematics;
using DGL;

namespace SF3D
{
    public sealed class Explosion : Entity
    {
        public float MaxLifeTime {get; init;} = 0.5f;
        public float MaxSize {get; init;} = 10;
        public Vector3 Color {get; init;} = new(1200,800,200);
        private OmniLight light;
        public Explosion(Vector3 position) : base(Models.OmniLight) 
        {
            Transform.Translation = position;
            light = new(){Position = position, Attenuation = new(0,0.1f,0.05f,0)};
        }
        public override void OnSpawned(World world, Scene scene)
        {
            base.OnSpawned(world, scene);
            scene.Add(light);

            var box = new Box3(Transform.Translation-new Vector3(MaxSize), Transform.Translation+new Vector3(MaxSize));
            if(Transform.Translation.Y <= MaxSize || world.GetNearbyHitboxes(Transform.Translation).Any(h => h.Intersects(box)))
            {
                var rng = new Random();
                int nDebris = rng.Next()%(int)(3*MaxSize)+(int)(MaxSize);
                float v = rng.NextFloat(MaxSize,3*MaxSize);
                for(int i=0; i<nDebris; ++i)
                {
                    world.Spawn(new Debris(rng.NextFloat() < 0.3f ? Models.Rock : Models.DirtClump, rng.NextFloat(0.2f,0.6f), Transform.Translation, rng.NextVector3(v/2)+new Vector3(0,v,0), rng.NextFloat(-3,3)));
                }
            }
        }
        public override void OnDespawned(World world, Scene scene)
        {
            base.OnDespawned(world, scene);
            light.IsAlive = false;
        }
        public override void Update(World world, Scene scene, float dt)
        {
            base.Update(world, scene, dt);
            float sizeFactor = ((MaxLifeTime - 2*MathF.Abs(MaxLifeTime/2 - LifeTime))/MaxLifeTime).Clamp(0,1);
            Transform.Scale = sizeFactor*MaxSize;
            UpdateModelMatrix(scene);
            light.Color = Color*sizeFactor*sizeFactor;
            light.AmbientColor = Color*sizeFactor*sizeFactor;
            light.Position = Transform.Translation;

            if(LifeTime >= MaxLifeTime)
                IsAlive = false;
        }
    }
}