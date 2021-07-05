using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using OpenTK.Mathematics;

namespace DGL.Model
{
    public sealed class WavefrontOBJ
    {
        private record MultiIndex(int Position, int? UV, int? Normal)
        {
            public static MultiIndex Parse(string description)
            {
                var parts = description.Split('/');
                // We're subtracting 1 because Wavefront OBJ indices start at 1
                return new(
                    Position : int.Parse(parts[0])-1,
                    UV       : (parts[1].Length > 0) ? int.Parse(parts[1])-1 : null,
                    Normal   : (parts[2].Length > 0) ? int.Parse(parts[2])-1 : null
                );
            }
        }
        private List<List<MultiIndex>> faces = new();
        private List<Vector3> positions = new(), normals = new();
        private List<Vector2> uvs = new();

        public Model ToModel()
        {
            List<Vector3> pos = new(), norm = new();
            List<Vector2> uv = new();
            List<int> indices = new();

            int nextIndex = 0;
            Dictionary<MultiIndex,int> indexMap = new();

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
                        uv.Add(uvs[uvIndex]);
                    }
                    else
                        uv.Add(Vector2.Zero);
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
                CollectionsMarshal.AsSpan(uv)
            );
        }

        private static IEnumerable<string> ByLine(Stream stream)
        {
            var reader = new StreamReader(stream);
            string? line;
            while((line = reader.ReadLine()) is string ln)
                yield return ln;
        }

        public static WavefrontOBJ Parse(Stream stream)
        {
            var model = new WavefrontOBJ();
            foreach(var line in ByLine(stream).Select(ln => ln.TrimStart())) 
            {
                var tokens = line.Split(' ',StringSplitOptions.RemoveEmptyEntries).Skip(1);
                switch(line.First())
                {
                    case 'f':
                    model.faces.Add(tokens.Select(MultiIndex.Parse).ToList());
                    break;

                    case 'v':
                    var vector = tokens.Select(float.Parse).ToArray();
                    switch(line[1]) {
                        case ' ': model.positions.Add(new(vector[0], vector[1], vector[2])); break;
                        case 'n': model.normals.Add((new Vector3(vector[0], vector[1], vector[2])).Normalized()); break;
                        case 't': model.uvs.Add(new (vector[0], vector[1])); break;
                        default: break;
                    }
                    break;
                    case '#': break; //skip comments
                    default: break; //skip not-yet-implemented OBJ stuff
                }
            }
            return model;
        }
    }
}