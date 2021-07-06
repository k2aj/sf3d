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

        public void Update(Scene scene, Vector3 cameraPosition)
        {
            const int renderDistance = 2;
            // Load chunks in render distance
            Vector2i cameraChunkCoords = new((int)Math.Round(cameraPosition.X/128), (int)Math.Round(cameraPosition.Z/128));
            for(int dx = -renderDistance; dx <= renderDistance; ++dx)
                for(int dz = -renderDistance; dz <= renderDistance; ++dz)
                {
                    var chunkCoords = cameraChunkCoords + new Vector2i(dx,dz);
                    if(!loadedChunks.ContainsKey(chunkCoords))
                    {
                        Console.WriteLine($"Loading chunk {chunkCoords}");
                        var chunk = new Chunk(chunkCoords);
                        chunk.OnLoaded(scene);
                        loadedChunks.Add(chunkCoords, chunk);
                    }
                }

            // Unload chunks outside of render distance
            List<Vector2i> unloadedChunkCoords = new();
            foreach(var (chunkCoords, chunk) in loadedChunks)
            {
                var displacement = cameraChunkCoords - chunkCoords;
                if((Math.Abs(displacement.X) > renderDistance) || (Math.Abs(displacement.Y) > renderDistance)) 
                {
                    Console.WriteLine($"Unloading chunk {chunkCoords}");
                    unloadedChunkCoords.Add(chunkCoords);
                    chunk.OnUnloaded(scene);
                }
            }
            foreach(var chunkCoords in unloadedChunkCoords)
                loadedChunks.Remove(chunkCoords);

            //Console.WriteLine($"{loadedChunks.Count} chunks loaded.");
        }
    }

    public sealed class Chunk
    {
        private Model terrain;
        private Scene.ObjectID terrainID;
        private int orientation;
        public readonly Vector2i ChunkCoords;

        public Chunk(Vector2i chunkCoords)
        {
            ChunkCoords = chunkCoords;
            var rng = new Random(chunkCoords.X*13 + chunkCoords.Y*31);

            if(chunkCoords == Vector2i.Zero)
            {
                terrain = Models.Airport;
                orientation = 0;
            }
            else
            {
                terrain = rng.Choice(
                    new Model[]{Models.Hills, Models.Mountain, Models.Volcano, Models.Plains},
                    new double[]{2, 1, 0.5, 4}
                );
                orientation = rng.Next() % 4;
            }
        }

        public void OnLoaded(Scene scene)
        {
            terrainID = scene.Add(terrain, Matrix4.CreateRotationY(MathF.PI/2*orientation)*Matrix4.CreateTranslation(ChunkCoords.X*128, 0, ChunkCoords.Y*128));
        }

        public void OnUnloaded(Scene scene)
        {
            scene.Remove(terrainID);
        }

    }
}