#nullable disable

using DGL;
using DGL.Model;

namespace SF3D
{
    public static class Models
    {
        public static Model Plane, Tree, OmniLight, TestCube, Terrain;
        public static Atlas Atlas;

        public static void Init()
        {
            Atlas = new(new(512));
            Plane = WavefrontOBJ.Parse("models/jetfighter/jetfighter.obj").ToModel(Atlas);
            Tree = WavefrontOBJ.Parse("models/tree/tree.obj").ToModel(Atlas);
            OmniLight = WavefrontOBJ.Parse("models/omnilight/omnilight.obj").ToModel(Atlas);
            TestCube = WavefrontOBJ.Parse("models/testcube/testcube.obj").ToModel(Atlas);
            Terrain = WavefrontOBJ.Parse("models/terrain/terrain.obj").ToModel(Atlas);
        }

        public static void Dispose()
        {
            Atlas.Dispose();
            Plane.Dispose();
            Tree.Dispose();
            OmniLight.Dispose();
            TestCube.Dispose();
            Terrain.Dispose();
        }
    }
}