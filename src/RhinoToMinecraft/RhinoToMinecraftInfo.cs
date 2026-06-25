using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace RhinoToMinecraft;

public class RhinoToMinecraftInfo : GH_AssemblyInfo
{
    public override string Name => "RhinoToMinecraft";

    public override Bitmap Icon => null!;

    public override string Description =>
        "Voxelizes Rhino geometry, maps it to Minecraft blocks, and exports it for import via Minecraft tooling.";

    public override Guid Id => new Guid("3b2a6f54-9c1d-4e7a-8b3f-1d9a7c5e2f60");

    public override string AuthorName => "";

    public override string AuthorContact => "";

    public override string Version => "0.1.0";
}
