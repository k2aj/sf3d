using DGL.Model;
using DGL;

namespace SF3D
{
    public sealed class Particle : Entity
    {
        private readonly float MaxLifetime;
        private readonly float Size;
        public Particle(Model model, float size, float maxLifetime) : base(model) 
        {
            Transform.Scale = size;
            Size = size;
            MaxLifetime = maxLifetime;
        }

        public override void OnSpawned(World world, Scene scene)
        {
            base.OnSpawned(world, scene);
            UpdateModelMatrix(scene);
        }

        public override void Update(World world, Scene scene, float dt)
        {
            base.Update(world, scene, dt);
            Transform.Scale = Size*(1-LifeTime/MaxLifetime).Clamp(0,1);
            UpdateModelMatrix(scene);
            if(LifeTime > MaxLifetime) IsAlive = false;
        }
    }
}