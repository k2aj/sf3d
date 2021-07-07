using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace SF3D
{
    public record Input(
        KeyboardState Keyboard,
        MouseState Mouse,
        Vector3 LookDir
    ){}
}