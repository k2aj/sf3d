using System;
using System.Collections.Generic;
using DGL;
using DGL.Model;
using OpenTK.Mathematics;

namespace SF3D
{
    public sealed class World
    {
        private Dictionary<Vector2i, Chunk> loadedChunks = new();
        private List<Entity> entities = new(), newEntities = new();

        internal static Vector2i ToChunkCoords(Vector3 position) =>  new((int)Math.Round(position.X/128), (int)Math.Round(position.Z/128));
        internal static Vector3 ToWorldCoords(Vector2i chunkCoords) => new Vector3(chunkCoords.X*128, 0, chunkCoords.Y*128);

        public void Spawn(Entity entity)
        {
            newEntities.Add(entity);
        }

        public void Update(Scene scene, Vector3 cameraPosition, float dt)
        {
            const int renderDistance = 4;
            // Load chunks in render distance
            Vector2i cameraChunkCoords = ToChunkCoords(cameraPosition);
            for(int dx = -renderDistance; dx <= renderDistance; ++dx)
                for(int dz = -renderDistance; dz <= renderDistance; ++dz)
                {
                    var chunkCoords = cameraChunkCoords + new Vector2i(dx,dz);
                    if(!loadedChunks.ContainsKey(chunkCoords))
                    {
                        //Console.WriteLine($"Loading chunk {chunkCoords}");
                        var chunk = new Chunk(chunkCoords);
                        chunk.OnLoaded(this,scene);
                        loadedChunks.Add(chunkCoords, chunk);
                    }
                }

            // Unload chunks outside of render distance
            List<Vector2i> unloadedChunkCoords = new();
            foreach(var (chunkCoords, chunk) in loadedChunks)
            {
                chunk.Update(this, scene, dt);
                var displacement = cameraChunkCoords - chunkCoords;
                if((Math.Abs(displacement.X) > renderDistance) || (Math.Abs(displacement.Y) > renderDistance)) 
                {
                    //Console.WriteLine($"Unloading chunk {chunkCoords}");
                    unloadedChunkCoords.Add(chunkCoords);
                    chunk.OnUnloaded(this,scene);
                }
            }
            foreach(var chunkCoords in unloadedChunkCoords)
                loadedChunks.Remove(chunkCoords);

            for(int i=0; i<newEntities.Count; ++i)
                newEntities[i].OnSpawned(this, scene);
            entities.AddRange(newEntities);
            newEntities.Clear();

            //Console.WriteLine($"{loadedChunks.Count} chunks loaded.");
            foreach(var entity in entities)
            {
                entity.Update(this, scene, dt);
                if(!entity.IsAlive)
                    entity.OnDespawned(this, scene);
            }
            entities.RemoveAll(e => !e.IsAlive);
        }

        private IEnumerable<Chunk> GetNearbyChunks(Vector3 position)
        {
            var chunkCoords = ToChunkCoords(position);
            var chunkCenter = ToWorldCoords(chunkCoords);
            for(int dx = -1; dx <= 1; ++dx)
                for(int dz = -1; dz <= 1; ++dz)
                {
                    var coords = chunkCoords + new Vector2i(dx,dz);
                    if(loadedChunks.TryGetValue(coords, out Chunk? chunk))
                        yield return chunk ?? throw new Exception("This should never happen");
                }
        }

        public IEnumerable<Box3> GetNearbyHitboxes(Vector3 position)
        {
            foreach(var chunk in GetNearbyChunks(position))
                foreach(var hitbox in chunk.Hitboxes)
                    yield return hitbox;
        }

        public IEnumerable<Box3> GetNearbyLandingAreas(Vector3 position)
        {
            foreach(var chunk in GetNearbyChunks(position))
                foreach(var area in chunk.LandingAreas)
                    yield return area;
        }
    }

    public sealed class Chunk
    {
        private Model terrain;
        private Scene.ObjectID terrainID;
        private int orientation;
        public readonly Vector2i ChunkCoords;
        public List<Box3> Hitboxes = new();
        public List<Box3> LandingAreas = new();
        private List<Entity> localEntities = new();
        public Chunk(Vector2i chunkCoords)
        {
            ChunkCoords = chunkCoords;
            Vector3 chunkWorldCoords = World.ToWorldCoords(ChunkCoords);
            var rng = new Random(chunkCoords.X*13 + chunkCoords.Y*31);

            if(chunkCoords == Vector2i.Zero)
            {
                terrain = Models.Airport;
                orientation = 0;
            }
            else
            {
                terrain = rng.Choice(
                    new Model[]{Models.Hills, Models.Mountain, Models.Volcano, Models.Plains, Models.Airport},
                    new double[]{2, 1, 0.5, 4, 0.1}
                );
                orientation = rng.Next() % 4;
            }
            void Decorate(Random rng, Model model, int count, float scaleMin, float scaleMax)
            {
                for(int i=0; i<count; ++i)
                {
                    var decoration = new Entity(model);
                    decoration.Transform.Scale = rng.NextFloat(scaleMin, scaleMax);
                    decoration.Transform.Orientation *= Quaternion.FromAxisAngle(Vector3.UnitY, rng.NextFloat(0, 2*MathF.PI));
                    decoration.Transform.Orientation *= Quaternion.FromAxisAngle(Vector3.UnitZ, rng.NextFloat(-0.25f, 0.25f));
                    decoration.Transform.Orientation *= Quaternion.FromAxisAngle(Vector3.UnitX, rng.NextFloat(-0.25f, 0.25f));
                    decoration.Transform.Translation = chunkWorldCoords+new Vector3(rng.NextFloat(-64,64),0,rng.NextFloat(-64,64));
                    localEntities.Add(decoration);
                }
            }
            if(terrain == Models.Airport)
            {
                Vector4 low = new(-3.5f, 0, -16f, 1);
                Vector4 high = new(11.5f, 0, 52.5f, 1);
                var m = Matrix4.CreateRotationY(MathF.PI/2*orientation)*Matrix4.CreateTranslation(ChunkCoords.X*128, 0, ChunkCoords.Y*128);
                var p1 = low*m;
                var p2 = high*m;

                LandingAreas.Add(new(
                    Math.Min(p1.X,p2.X),
                    -10,
                    Math.Min(p1.Z,p2.Z),
                    Math.Max(p1.X,p2.X),
                    0.5f,
                    Math.Max(p1.Z,p2.Z)
                ));
            }
            if(terrain == Models.Plains)
            {
                bool forest = rng.NextFloat() < 0.25f;
                bool rockyPlain = !forest && rng.NextFloat() < 0.15f;
                bool city = !forest && !rockyPlain && rng.NextFloat() < 0.2f;
                if(city)
                {
                    for(int gx=0; gx<8; ++gx)
                        for(int gz=0; gz<8; ++gz)
                        {
                            float x = chunkWorldCoords.X-56+16*gx, z = chunkWorldCoords.Z-56+16*gz;
                            int distFromCenter = (int)(Math.Max(MathF.Abs(gx-3.5f), MathF.Abs(gz-3.5f)));
                            var height = rng.Next(8-2*distFromCenter, 20-5*distFromCenter);

                            if(rng.Next()%5 >= distFromCenter+1)
                            {
                                for(int gy=0; gy<height; ++gy)
                                {
                                    float y = gy*2;
                                    var floor = new Entity(gy==0 ? Models.SkyscraperBase : Models.SkyscraperFloor);
                                    floor.Transform.Translation = new Vector3(x,y,z);
                                    floor.Transform.Orientation *= Quaternion.FromAxisAngle(Vector3.UnitY, rng.Next()%4*MathF.PI/2);
                                    localEntities.Add(floor);
                                }
                                Hitboxes.Add(new Box3(x-4,0,z-4,x+4,2*height,z+4));
                            }
                        }
                }
                else
                {
                    Decorate(rng, Models.Tree, forest ? rng.Next()%50+50 : rng.Next()%20, 0.5f, forest ? 6f : 3f);
                    Decorate(rng, Models.Bush, rng.Next()%35+10, 0.75f, 1.5f);
                    Decorate(rng, Models.Rock, rng.Next()%50, 0.25f, rockyPlain ? 3f : 1f);
                }
            }
            
        }

        public void OnLoaded(World world, Scene scene)
        {
            terrainID = scene.Add(terrain, Matrix4.CreateRotationY(MathF.PI/2*orientation)*Matrix4.CreateTranslation(ChunkCoords.X*128, 0, ChunkCoords.Y*128));
            foreach(var e in localEntities)
                e.OnSpawned(world, scene);
        }

        public void OnUnloaded(World world, Scene scene)
        {
            scene.Remove(terrainID);
            foreach(var e in localEntities)
                e.OnDespawned(world, scene);
        }

        public void Update(World world, Scene scene, float dt)
        {
            foreach(var e in localEntities)
                e.Update(world, scene, dt);
        }

    }
}