using System;
using OpenTK.Mathematics;

namespace DGL
{
    public sealed class Camera
    {
        public Vector3 Eye = Vector3.Zero, 
                       LookDir = Vector3.UnitZ, 
                       Up = Vector3.UnitY;
        public float ZNear = 0.25f, ZFar = 40, AspectRatio = 16/9f, FOV = MathF.PI/2;
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
    }
}