using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;

namespace DGL
{
    public static class RandomExtensions
    {
        public static float NextFloat(this Random random) => (float) random.NextDouble();
        public static float NextFloat(this Random random, float min, float max) => random.NextFloat() * (max-min) + min;
        public static Vector3 NextVector3(this Random random, float max) => new(random.NextFloat(-max,max), random.NextFloat(-max,max), random.NextFloat(-max,max));
        public static T Choice<T>(this Random random, IEnumerable<T> values, IEnumerable<double> weights)
        {
            double r = random.NextDouble() * weights.Sum();
            foreach(var (value, weight) in values.Zip(weights))
            {
                if(r < weight) return value;
                r -= weight;
            }
            return values.First();
        }
    }
}