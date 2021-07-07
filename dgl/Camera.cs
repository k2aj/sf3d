using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DGL
{
    public sealed class Camera
    {
        public Vector3 Eye = Vector3.Zero, 
                       LookDir = Vector3.UnitZ, 
                       Up = Vector3.UnitY;
        public float ZNear = 0.25f, ZFar = 100, AspectRatio = 16/9f, FOV = MathF.PI/2;
        public Matrix4 ViewMatrix 
        {
            get => Matrix4.LookAt(Eye, Target, Up);
        }
        public Matrix4 ProjectionMatrix
        {
            get => Matrix4.CreatePerspectiveFieldOfView(FOV, AspectRatio, ZNear, ZFar);
        }
        public Vector3 Target
        {
            get => Eye + LookDir;
            set => LookDir = Target - Eye;
        }

        /*public void ControlFPS(KeyboardState input)
        {
            // Naive implementation of flying FPS-style camera
            Vector3 cameraForward = new(LookDir.X, 0, LookDir.Z);
            cameraForward.Normalize();
            Vector3 cameraRight = Matrix3.CreateRotationY(MathF.PI/2) * cameraForward;
            if(input.IsKeyDown(Keys.A))
                Eye -= cameraRight*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.D))
                Eye += cameraRight*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.W))
                Eye += cameraForward*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.S))
                Eye -= cameraForward*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.Space))
                Eye += Vector3.UnitY*cameraVelocity*dt;
            if(input.IsKeyDown(Keys.LeftShift))
                Eye -= Vector3.UnitY*cameraVelocity*dt;

            var mouseDelta = MouseState.Position - MouseState.PreviousPosition;
            mouseDelta = mouseDelta / (float)Math.Clamp(e.Time, 1/100f, 1/5f) / 60;
            // We clamp to +-Math.PI/2.01 instead of +-Math.PI/2 to prevent the user from looking straight up/straight down,
            // because it breaks Camera's view matrix because of some issues with Matrix4.LookAt
            lookPitch = (float) Math.Clamp(lookPitch + mouseDelta.Y/100, -Math.PI/2.01, Math.PI/2.01);
            lookYaw += mouseDelta.X/100;
            LookDir = Matrix3.CreateRotationY(lookYaw)*Matrix3.CreateRotationX(-lookPitch)*Vector3.UnitZ;
        }*/
    }
}