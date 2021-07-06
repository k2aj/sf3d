using System;
using DGL;
using DGL.Model;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SF3D
{
    public sealed class Aircraft : Entity
    {
        private const float maxSpeed = 100;
        private float thrust = 0, pitchCtrl = 0, rollCtrl = 0;

        public Camera Camera = new(){Eye = Vector3.Zero};
        private OmniLight light = new(){Color = new(24,16,8), AmbientColor = new(1,0.66f,0.33f), Attenuation = new(0,0.01f,0.05f,0)};

        public Aircraft(Model model) : base(model) {}
        override public void OnSpawned(Scene scene){
            base.OnSpawned(scene);
            scene.Add(light);
        }
        override public void Update(Scene scene, float dt)
        {
            var forward = Transform.TransformOffset(new (0,0,1));
            var up = Transform.TransformOffset(new (0,1,0));
            var right = Transform.TransformOffset(new (1,0,0));

            Velocity += forward*thrust*dt;

            var extraPitch = Quaternion.FromAxisAngle(right, pitchCtrl*dt);
            var extraRoll = Quaternion.FromAxisAngle(forward, rollCtrl*dt);

            Transform.Orientation = (extraPitch*extraRoll*Transform.Orientation).Normalized();
            Velocity = extraPitch*extraRoll*Velocity;
            if(Velocity.Length > maxSpeed) Velocity = Velocity.Normalized() * maxSpeed;

            base.Update(scene, dt);
            light.Position = Transform.Translation - 1.5f*forward;
            Camera.Eye = Transform.Translation - 4*forward + 1.25f*up;
            Camera.LookDir = forward;
            Camera.Up = up;
        }

        public void Control(KeyboardState keyboard, float dt)
        {
            pitchCtrl = 0;
            rollCtrl = 0;
            if(keyboard.IsKeyDown(Keys.A)) rollCtrl -= 1;
            if(keyboard.IsKeyDown(Keys.D)) rollCtrl += 1;
            if(keyboard.IsKeyDown(Keys.W)) pitchCtrl += 1;
            if(keyboard.IsKeyDown(Keys.S)) pitchCtrl -= 1;
            if(keyboard.IsKeyDown(Keys.Space)) thrust = 10;
            if(keyboard.IsKeyDown(Keys.LeftShift)) thrust = -10;
        }
    }
}