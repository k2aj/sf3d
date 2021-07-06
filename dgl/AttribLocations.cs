namespace DGL
{
    public sealed class AttribLocations
    {
        public static readonly int 
            Position = 0,
            Normal = 1,
            // Diffuse and Specular can either be colors or texture coordinates
            Diffuse = 2,
            Specular = 3;
    }
}