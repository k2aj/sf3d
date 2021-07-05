using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Drawing;
using OpenTK.Mathematics;

namespace DGL.Model
{
    public sealed class WavefrontOBJ
    {
        private record MultiIndex(int Position, int? UV, int? Normal, int Material)
        {
            public static MultiIndex Parse(string description, int material)
            {
                var parts = description.Split('/');
                // We're subtracting 1 because Wavefront OBJ indices start at 1
                return new(
                    Position : int.Parse(parts[0])-1,
                    UV       : (parts[1].Length > 0) ? int.Parse(parts[1])-1 : null,
                    Normal   : (parts[2].Length > 0) ? int.Parse(parts[2])-1 : null,
                    Material : material
                );
            }
        }
        private List<List<MultiIndex>> faces = new();
        private List<Vector3> positions = new(), normals = new();
        private List<Vector2> uvs = new();
        private List<Material> materials = new();
        public Model ToModel(Atlas atlas)
        {
            List<Vector3> pos = new(), norm = new();
            List<Vector2> diffuseUv = new();
            List<Vector2> specularUv = new();
            List<int> indices = new();

            int nextIndex = 0;
            Dictionary<MultiIndex,int> indexMap = new();

            List<Box2i> diffuseSlices = new();
            List<Box2i> specularSlices = new();
            foreach(var mtl in materials)
            {
                if(mtl.DiffuseMap is string diffusePath) diffuseSlices.Add(atlas.Allocate(new Bitmap(diffusePath)).Area);
                else diffuseSlices.Add(new Box2i(0,0,0,0));
                if(mtl.SpecularMap is string specularPath) specularSlices.Add(atlas.Allocate(new Bitmap(specularPath)).Area);
                else specularSlices.Add(new Box2i(0,0,0,0));
            }

            int MergeIndices(MultiIndex multiIndex)
            {
                int index;
                if(!indexMap.TryGetValue(multiIndex, out index))
                {
                    index = nextIndex++;
                    indexMap[multiIndex] = index;

                    pos.Add(positions[multiIndex.Position]);
                    if(multiIndex.Normal is int normalIndex)
                        norm.Add(normals[normalIndex]);
                    else
                    {
                        //can't generate normals here
                        norm.Add(Vector3.UnitY);
                        //Vector3 d1 = pos[prevIndex] - pos[prevPrevIndex];
                        //Vector3 d2 = pos[index] - pos[prevPrevIndex];
                        //norm.Add(-Vector3.Cross(d1,d2).Normalized());
                    }
                    if(multiIndex.UV is int uvIndex)
                    {
                        //Console.WriteLine(uvIndex);
                        Vector2 MapUVs(Vector2 uv, Box2i slice) => slice.Min + slice.Size*uv;
                        diffuseUv.Add(MapUVs(uvs[uvIndex], diffuseSlices[multiIndex.Material]));
                        specularUv.Add(MapUVs(uvs[uvIndex], specularSlices[multiIndex.Material]));
                    }
                    else
                    {
                        diffuseUv.Add(Vector2.Zero);
                        specularUv.Add(Vector2.Zero);
                    }
                }
                return index;
            }

            foreach(var face in faces)
            {
                // Convert each face into a triangle fan
                int firstIndex = MergeIndices(face[0]), prevIndex = MergeIndices(face[1]);
                for(int i=2; i<face.Count; ++i) 
                {
                    int curIndex = MergeIndices(face[i]);
                    indices.Add(firstIndex);
                    indices.Add(prevIndex);
                    indices.Add(curIndex);

                    prevIndex = curIndex;
                }
            }

            return new Model(
                CollectionsMarshal.AsSpan(indices), 
                CollectionsMarshal.AsSpan(pos), 
                CollectionsMarshal.AsSpan(norm), 
                CollectionsMarshal.AsSpan(diffuseUv),
                CollectionsMarshal.AsSpan(specularUv)
            );
        }

        private static IEnumerable<string> ByLine(Stream stream)
        {
            var reader = new StreamReader(stream);
            string? line;
            while((line = reader.ReadLine()) is string ln)
                yield return ln;
        }

        public static WavefrontOBJ Parse(string path)
        {
            using(var stream = File.OpenRead(path))
            {
                var model = new WavefrontOBJ();
                int currentMaterial = 0;
                foreach(var line in ByLine(stream).Select(ln => ln.TrimStart())) 
                {
                    var tokens = line.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                    if(tokens.Any())
                        switch(line.First())
                        {
                            // Face
                            case 'f':
                            model.faces.Add(tokens.Skip(1).Select(tok => MultiIndex.Parse(tok, currentMaterial)).ToList());
                            break;

                            // Vertex parameter
                            case 'v':
                            var vector = tokens.Skip(1).Select(float.Parse).ToArray();
                            switch(line[1]) {
                                case ' ': model.positions.Add(new(vector[0], vector[1], vector[2])); break;
                                case 'n': model.normals.Add((new Vector3(vector[0], vector[1], vector[2])).Normalized()); break;
                                case 't': model.uvs.Add(new (vector[0], 1-vector[1])); break; //remap second texcoord becasue in Blender it goes from bottom to top
                                default: break;
                            }
                            break;

                            // mtllib declaration
                            case 'm':
                            var mtllibPath = Path.Combine(Path.GetDirectoryName(path) ?? throw new Exception("This should never happen"), tokens[1]);
                            model.materials = Material.Parse(mtllibPath);
                            break;

                            // usemtl declaration
                            case 'u':
                            currentMaterial = model.materials.FindIndex(mtl => mtl.Name == tokens[1]);
                            break;

                            case '#': break; //skip comments
                            default: break; //skip not-yet-implemented OBJ stuff
                        }
                }
                return model;
            }
        }

        public class Material
        {
            //store path to map to avoid loading every material map at the same time
            public string Name {get; private set;}
            public string? DiffuseMap {get; private set;}
            public string? SpecularMap {get; private set;}

            public Material(string name) => Name = name;

            // TODO:
            // This will break horribly if material names & map filenames contain spaces
            // and should probably be fixed
            public static List<Material> Parse(string path)
            {
                string directory = Path.GetDirectoryName(path) ?? throw new Exception("This should not happen");
                List<Material> result = new();
                using(var stream = File.OpenRead(path))
                {
                    foreach(var line in ByLine(stream).Select(ln => ln.TrimStart()))
                    {
                        var tokens = line.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                        if(tokens.Any())
                            switch(tokens.First())
                            {
                                case "newmtl": result.Add(new(tokens[1])); break;
                                case "map_Kd": result[^1].DiffuseMap = Path.Combine(directory, tokens[1]); break;
                                case "map_Ks": result[^1].SpecularMap = Path.Combine(directory, tokens[1]); break;
                            }
                    }
                }
                return result;
            }
        }
    }
}