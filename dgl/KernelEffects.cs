using System;
using System.Linq;

namespace DGL
{
    public sealed class KernelEffects
    {
        public static float[] Normalized(float[] kernel)
        {
            var sum = kernel.Sum();
            return kernel.Select(x => x/sum).ToArray();
        }
        public static float[] CreateGaussian1D(int length, float sd)
        {
            var kernel = new float[length];
            for(int i=0; i<length; ++i)
            {
                var x = i-(length-1)*0.5f;
                kernel[i] = MathF.Exp(-x*x/(2*sd*sd)) / MathF.Sqrt(2*MathF.PI*sd*sd);
            }
            return Normalized(kernel);
        }
    }
}