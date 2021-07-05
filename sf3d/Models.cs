#nullable disable

using DGL;
using DGL.Model;

namespace SF3D
{
    public static class Models
    {
        public static Model Plane, Tree, OmniLight;
        public static Atlas Atlas;

        public static void Init()
        {
            Atlas = new(new(512));
            Plane = WavefrontOBJ.Parse("models/jetfighter/jetfighter.obj").ToModel(Atlas);
            Tree = WavefrontOBJ.Parse("models/tree/tree.obj").ToModel(Atlas);
            OmniLight = WavefrontOBJ.Parse("models/omnilight/omnilight.obj").ToModel(Atlas);
        }

        public static void Dispose()
        {
            Atlas.Dispose();
            Plane.Dispose();
            Tree.Dispose();
            OmniLight.Dispose();
        }
    }
}