using System;
using OpenTK.Mathematics;

namespace DGL
{
    public static class MathExtensions
    {
        public static float Clamp(this float value, float min, float max) => MathF.Max(min, MathF.Min(max, value));
        public static Quaternion RotateTowards(this Vector3 current, Vector3 target, float maxRotation = MathF.PI)
        {
            Vector3 axis = Vector3.Cross(current, target);
            float sine = axis.Length;
            float cosine = Vector3.Dot(current, target);
            if(sine < 0.01f) return Quaternion.Identity;
            else return Quaternion.FromAxisAngle(axis/sine, MathF.Atan2(sine,cosine).Clamp(-maxRotation,maxRotation));
        }
        public static Vector3 Transform(this Quaternion p, Vector3 q) => (p*(new Quaternion(q))*Quaternion.Conjugate(p)*(1/p.LengthSquared)).Xyz;
        public static bool Intersects(this Box3 a, Box3 b) => (new Box3(a.Min-b.HalfSize, a.Max+b.HalfSize)).Contains(b.Center);
    }
}