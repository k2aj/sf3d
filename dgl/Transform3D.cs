using OpenTK.Mathematics;

namespace DGL
{
    public struct Transform3D
    {
        public Quaternion Orientation;
        public float Scale;
        public Vector3 Translation;

        public static Transform3D Identity = new()
        {
            Orientation = Quaternion.Identity,
            Scale = 1,
            Translation = Vector3.Zero
        };

        public Vector3 TransformOffset(Vector3 offset) => (Orientation * (new Quaternion(offset,0)) * Quaternion.Conjugate(Orientation) * (1/Orientation.LengthSquared)).Xyz * Scale;
        public Vector3 TransformPosition(Vector3 position) => TransformOffset(position) + Translation;
        public Matrix4 ToMatrix() => Matrix4.CreateFromQuaternion(Orientation)*Matrix4.CreateScale(Scale)*Matrix4.CreateTranslation(Translation);
    }
}