using System;
using DGL;
using DGL.Model;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SF3D
{
    public sealed class Aircraft : AerialEntity
    {
        //private OmniLight light = new(){Color = new(24,16,8), AmbientColor = new(1,0.66f,0.33f), Attenuation = new(0,0.01f,0.05f,0)};
        private bool firingMissile = false;
        private Vector3 aimDir = Vector3.UnitZ;

        public Aircraft(Model model) : base(model, new(-2,-1,-2,2,1,2)) 
        {
            CanLand = true;
            EngineLocations = new(){new(0.16f,0,-1.55f), new(-0.16f,0,-1.55f)};
        }

        private float reloadLeft = 0;
        private int missileBay = 0;

        public override void OnDespawned(World world, Scene scene)
        {
            //base.OnDespawned(world, scene);
        }

        public override void Update(World world, Scene scene, float dt)
        {
            base.Update(world, scene, dt);

            if(firingMissile && reloadLeft <= 0)
            {
                missileBay = (missileBay+1)%2;
                var missile = new Missile(Models.Missile, Transform.TransformPosition(new(2*missileBay-1,-0.3f,0)), aimDir*Velocity.Length*0.85f);
                missile.Transform.Orientation = Transform.TransformOffset(new (0,0,1)).RotateTowards(aimDir) * Transform.Orientation;
                world.Spawn(missile);
                reloadLeft = 0.2f;
            } else {
                reloadLeft -= dt;
            }
        }

        override public void Control(Input input)
        {
            base.Control(input);
            firingMissile = input.Mouse.IsButtonDown(MouseButton.Left);
            aimDir = input.LookDir;
        }
    }
}