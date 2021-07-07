using System;
using System.Linq;
using System.Collections.Generic;
using DGL;
using DGL.Model;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SF3D
{
    public class AerialEntity : Entity
    {
        protected float thrust = 0, pitchCtrl = 0, rollCtrl = 0;
        public float GravityMultiplier {get; init;} = 1;
        public float EnginePower {get; init;} = 20;
        public float PitchMobility {get; init;} = 1;
        public float RollMobility {get; init;} = 1;
        public float LiftStrength {get; init;} = 0.4f;
        public Vector3 DragCoefficient {get; init;} = new(0.1f, 0.4f, 0.05f);
        public bool CanLand {get; init;} = false;
        public Camera Camera = new(){Eye = Vector3.Zero};
        public Box3 Hitbox;
        public List<Vector3> EngineLocations = new();
        private List<OmniLight> engineLights = new();
        private float engineBrightnessFactor = 0.1f;

        public AerialEntity(Model model, Box3 hitbox) : base(model)
        {
            Hitbox = hitbox;
        }

        public override void OnSpawned(World world, Scene scene)
        {
            base.OnSpawned(world, scene);
            var lightColor = new Vector3(3,2,1) * MathF.Sqrt(EnginePower/EngineLocations.Count); 

            foreach(var pos in EngineLocations)
            {
                engineLights.Add(new OmniLight(){Color = lightColor, AmbientColor = lightColor/20, Attenuation = new(0,0.02f,0.05f,0)});
                scene.Add(engineLights[^1]);
            }
        }
        public override void OnDespawned(World world, Scene scene)
        {
            base.OnDespawned(world, scene);
            foreach(var light in engineLights)
                light.IsAlive = false;
        }

        public virtual void Crash(World world)
        {
            world.Spawn(new Explosion(Transform.Translation));
            IsAlive = false;
        }

        override public void Update(World world, Scene scene, float dt)
        {
            var forward = Transform.TransformOffset(new (0,0,1));
            var up = Transform.TransformOffset(new (0,1,0));
            var right = Transform.TransformOffset(new (1,0,0));

            // Apply thrust
            float airDensityFactor = 1000/(1000+Transform.Translation.Y);
            float vForward = Vector3.Dot(Velocity, forward);
            Velocity -= vForward * forward;
            vForward = Math.Max(0, vForward+thrust*EnginePower*airDensityFactor*dt); //we use this so that thrust can not cause the plane to go fly backward
            Velocity += vForward * forward;

            // Steering affects velocity, velocity affects orientation (but that comes later)
            var extraPitch = Quaternion.FromAxisAngle(right, pitchCtrl*PitchMobility*dt);
            var extraRoll = Quaternion.FromAxisAngle(forward, rollCtrl*RollMobility*dt);
            Velocity = extraPitch*extraRoll*Velocity;

            // Apply lift & drag
            Vector3 lift = Vector3.Cross(Velocity, right) * LiftStrength * dt * airDensityFactor; //lift implementation
            Vector3 drag = new Vector3(
                DragCoefficient.X*dt*Vector3.Dot(Velocity, right),
                DragCoefficient.Y*dt*Vector3.Dot(Velocity, up),
                DragCoefficient.Z*dt*Vector3.Dot(Velocity, forward)
            );
            Velocity -= Transform.TransformOffset(drag);

            // Rotate plane so that orientation matches velocity
            var tgtForward = Velocity.LengthSquared < 0.01f ? forward : Velocity;
            Transform.Orientation = extraRoll*forward.RotateTowards(tgtForward,dt)*Transform.Orientation;

            base.Update(world, scene, dt);
            Hitbox.Center = Transform.Translation;

            // Landing shenanigans
            if(Hitbox.Min.Y <= 0)
            {
                var cosLandingAngle = up.Y;
                if(CanLand && cosLandingAngle >= 0.9f && Velocity.Y >= -10 & world.GetNearbyLandingAreas(Transform.Translation).Any(a => a.Intersects(Hitbox)))
                {
                    //Console.WriteLine("Landing ok.");
                    Transform.Translation.Y = Hitbox.HalfSize.Y;
                    Velocity.Y = MathF.Max(Velocity.Y, 0);
                    Transform.Orientation = Transform.TransformOffset(new (0,1,0)).RotateTowards(new Vector3(0,1,0),dt)*Transform.Orientation;
                }
                else Crash(world);
            }
            if(world.GetNearbyHitboxes(Transform.Translation).Any(h => h.Intersects(Hitbox)))
                Crash(world);

            // Reduce lift & gravity at low altitudes because it causes microstuttering
            float liftGravityFactor = CanLand ? Math.Max(0, Math.Min(1, (Hitbox.Min.Y-0.0001f)/10)) : 1;
            lift.Y = MathF.Min(lift.Y, 20*dt); //"lift compensation"
            Velocity += liftGravityFactor*lift;
            Velocity.Y -= liftGravityFactor*20*dt*GravityMultiplier; //gravity implementation

            UpdateModelMatrix(scene);
            //recalculate vectors needed for camera to prevent microstuttering
            forward = Transform.TransformOffset(new (0,0,1));
            up = Transform.TransformOffset(new (0,1,0));
            right = Transform.TransformOffset(new (1,0,0));

            engineBrightnessFactor = (engineBrightnessFactor + (thrust > 0 ? dt : -dt)).Clamp(0.25f,1);
            var lightColor = new Vector3(3,2,1) * MathF.Sqrt(EnginePower/EngineLocations.Count)*engineBrightnessFactor;
            foreach(var (light,pos) in engineLights.Zip(EngineLocations))
            {
                light.Position = Transform.TransformPosition(pos);
                light.Color = lightColor;
                light.AmbientColor = light.Color/20;
            }
    
            Camera.LookDir = forward;
            Camera.Eye = Transform.Translation - 4*forward + 1.25f*up;
            Camera.Up = up;
        }

        public virtual void Control(Input input)
        {
            pitchCtrl = 0;
            rollCtrl = 0;
            thrust = 0;
            if(input.Keyboard.IsKeyDown(Keys.A)) rollCtrl -= 1;
            if(input.Keyboard.IsKeyDown(Keys.D)) rollCtrl += 1;
            if(input.Keyboard.IsKeyDown(Keys.W)) pitchCtrl += 1;
            if(input.Keyboard.IsKeyDown(Keys.S)) pitchCtrl -= 1;
            if(input.Keyboard.IsKeyDown(Keys.Space)) thrust = 1;
            if(input.Keyboard.IsKeyDown(Keys.LeftShift)) thrust = -1;
        }
    }
}