# SF3D

SF3D is a simple flight simulator.

### Features
- Open world
- Deferred shading (supports omni & directional lights)
- Shadows
- Depth fog
- Bloom

### Controls
- Left mouse button shoots missiles
- Acceleration/deceleration: Space/Shift
- Pitch: W/S
- Yaw: Q/E
- Roll: A/D
- Restart: R
- Go fullscreen: F11
- Exit: ESC
- Show texture atlas: T

### Building
```
dotnet build
```

### Requirements
- Windows 8/10 or Linux
- OpenGL 3.3
- .NET runtime 5.0
- On Linux you also need to install libgdiplus (not sure about Windows)

### Used assets
- Skyboxes were generated using the tool https://github.com/wwwtyro/space-3d
- All other models & textures were made by myself