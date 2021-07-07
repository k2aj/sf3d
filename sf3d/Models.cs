#nullable disable

using DGL;
using DGL.Model;

namespace SF3D
{
    public static class Models
    {
        public static Model Plane, Missile, Tree, Bush, Rock, DirtClump, OmniLight, TestCube, Airport, Hills, Mountain, Volcano, Plains, SkyscraperBase, SkyscraperFloor, Smoke;
        public static Atlas Atlas;

        public static void Init()
        {
            Atlas = new(new(512));
            Plane = WavefrontOBJ.Parse("models/jetfighter/jetfighter.obj").ToModel(Atlas);
            Missile = WavefrontOBJ.Parse("models/missile/missile.obj").ToModel(Atlas);
            Tree = WavefrontOBJ.Parse("models/tree/tree.obj").ToModel(Atlas);
            Bush = WavefrontOBJ.Parse("models/bush/bush.obj").ToModel(Atlas);
            Rock = WavefrontOBJ.Parse("models/rock/rock.obj").ToModel(Atlas);
            DirtClump = WavefrontOBJ.Parse("models/dirtclump/dirtclump.obj").ToModel(Atlas);
            OmniLight = WavefrontOBJ.Parse("models/omnilight/omnilight.obj").ToModel(Atlas);
            TestCube = WavefrontOBJ.Parse("models/testcube/testcube.obj").ToModel(Atlas);
            Airport = WavefrontOBJ.Parse("models/terrain/terrain.obj").ToModel(Atlas);
            Hills = WavefrontOBJ.Parse("models/hills/hills.obj").ToModel(Atlas);
            Mountain = WavefrontOBJ.Parse("models/mountain/mountain.obj").ToModel(Atlas);
            Volcano = WavefrontOBJ.Parse("models/volcano/volcano.obj").ToModel(Atlas);
            Plains = WavefrontOBJ.Parse("models/plains/plains.obj").ToModel(Atlas);
            SkyscraperBase = WavefrontOBJ.Parse("models/skyscraper/base.obj").ToModel(Atlas);
            SkyscraperFloor = WavefrontOBJ.Parse("models/skyscraper/floor.obj").ToModel(Atlas);
            Smoke = WavefrontOBJ.Parse("models/smoke/smoke.obj").ToModel(Atlas);
        }

        public static void Dispose()
        {
            Atlas.Dispose();
            Plane.Dispose();
            Missile.Dispose();
            Tree.Dispose();
            Bush.Dispose();
            Rock.Dispose();
            DirtClump.Dispose();
            OmniLight.Dispose();
            TestCube.Dispose();
            Airport.Dispose();
            Hills.Dispose();
            Mountain.Dispose();
            Volcano.Dispose();
            Plains.Dispose();
            SkyscraperBase.Dispose();
            SkyscraperFloor.Dispose();
            Smoke.Dispose();
        }
    }
}