using OpenTK.Mathematics;

namespace DGL
{
    public sealed class Camera
    {
        public Vector3 Eye = Vector3.Zero, 
                       LookDir = Vector3.UnitZ, 
                       Up = Vector3.UnitY;

        public Matrix4 ViewMatrix 
        {
            get => Matrix4.LookAt(Eye, Target, Up);
        }
        public Vector3 Target
        {
            get => Eye + LookDir;
            set => LookDir = Target - Eye;
        }
    }
}