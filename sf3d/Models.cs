#nullable disable

using DGL;
using DGL.Model;

namespace SF3D
{
    public static class Models
    {
        public static Model Plane, Tree, OmniLight, TestCube, Airport, Hills;
        public static Atlas Atlas;

        public static void Init()
        {
            Atlas = new(new(512));
            Plane = WavefrontOBJ.Parse("models/jetfighter/jetfighter.obj").ToModel(Atlas);
            Tree = WavefrontOBJ.Parse("models/tree/tree.obj").ToModel(Atlas);
            OmniLight = WavefrontOBJ.Parse("models/omnilight/omnilight.obj").ToModel(Atlas);
            TestCube = WavefrontOBJ.Parse("models/testcube/testcube.obj").ToModel(Atlas);
            Airport = WavefrontOBJ.Parse("models/terrain/terrain.obj").ToModel(Atlas);
            Hills = WavefrontOBJ.Parse("models/hills/hills.obj").ToModel(Atlas);
        }

        public static void Dispose()
        {
            Atlas.Dispose();
            Plane.Dispose();
            Tree.Dispose();
            OmniLight.Dispose();
            TestCube.Dispose();
            Airport.Dispose();
            Hills.Dispose();
        }
    }
}