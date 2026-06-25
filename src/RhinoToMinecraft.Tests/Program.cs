using Rhino.Geometry;
using RhinoToMinecraft.Core;

int failures = 0;

void Check(string name, bool condition)
{
    if (condition)
    {
        Console.WriteLine($"PASS: {name}");
    }
    else
    {
        Console.WriteLine($"FAIL: {name}");
        failures++;
    }
}

// Test 1: a closed 2x2x2 cube should voxelize without throwing and produce a
// non-empty, bounded set of voxel centers, in the cube's own model-space
// coordinates (issue 11) - centers sit at a half-cell offset from grid lines
// (issue 07), e.g. 0.5, 1.5 for scale 1.0, not on integer grid lines.
var cube = new Box(Plane.WorldXY, new Interval(0, 2), new Interval(0, 2), new Interval(0, 2)).ToBrep();

List<Point3d> voxelsScale1;
try
{
    voxelsScale1 = VoxelizationEngine.Voxelize(cube, scaleFactor: 1.0);
    Check("Scale 1.0 voxelization does not throw", true);
}
catch (Exception ex)
{
    Console.WriteLine($"  Exception: {ex.Message}");
    Check("Scale 1.0 voxelization does not throw", false);
    voxelsScale1 = new List<Point3d>();
}

Check("Scale 1.0 produces at least one voxel", voxelsScale1.Count > 0);
Check("Scale 1.0 voxels stay within [0,2]^3 (+/- tolerance)",
    voxelsScale1.All(p => p.X >= -0.01 && p.X <= 2.01 && p.Y >= -0.01 && p.Y <= 2.01 && p.Z >= -0.01 && p.Z <= 2.01));
Check("Scale 1.0 voxel centers sit at the half-cell offset (e.g. x=0.5), not on integer grid lines",
    voxelsScale1.All(p => Math.Abs(p.X - (Math.Floor(p.X) + 0.5)) < 1e-6));

// Test 2: doubling the scale factor should noticeably increase voxel density,
// while the model-space extent stays anchored to the original [0,2]^3 cube
// (issue 11) - higher scale must not stretch the point cloud away from the
// input geometry.
var voxelsScale2 = VoxelizationEngine.Voxelize(cube, scaleFactor: 2.0);
Check("Scale 2.0 produces more voxels than scale 1.0", voxelsScale2.Count > voxelsScale1.Count);
Console.WriteLine($"  Scale 1.0 voxel count: {voxelsScale1.Count}, Scale 2.0 voxel count: {voxelsScale2.Count}");
Check("Scale 2.0 voxels stay within the original [0,2]^3 model-space bounds (not stretched by scale)",
    voxelsScale2.Max(p => p.X) <= 2.01);

// Test 3: ToBlockSpace converts model-space centers into Minecraft block space
// by applying the scale factor - the cube's [0,2]^3 model-space extent becomes
// ~[0,4]^3 at scale 2.0, and each resulting coordinate is a half-integer (e.g.
// 2.5) whose containing block is found via Floor, not Round (issue 07).
var voxelsScale2BlockSpace = voxelsScale2.Select(p => VoxelizationEngine.ToBlockSpace(p, 2.0)).ToList();
Check("ToBlockSpace maps scale 2.0 voxels into block space (~[0,4]^3)",
    voxelsScale2BlockSpace.Max(p => p.X) > 3.0 && voxelsScale2BlockSpace.Max(p => p.X) <= 4.01);
Check("ToBlockSpace output sits at half-integer coordinates (e.g. x=2.5)",
    voxelsScale2BlockSpace.All(p => Math.Abs(p.X - (Math.Floor(p.X) + 0.5)) < 1e-6));

// Test 4: an open (non-solid) surface must be rejected, not silently repaired -
// this matches the PRD decision that closed-solid input is a hard precondition.
var openSurface = Brep.CreateFromCornerPoints(
    new Point3d(0, 0, 0), new Point3d(1, 0, 0), new Point3d(1, 1, 0), new Point3d(0, 1, 0), 0.01);

bool threw = false;
try
{
    VoxelizationEngine.Voxelize(openSurface, scaleFactor: 1.0);
}
catch (InvalidOperationException)
{
    threw = true;
}
Check("Open/non-solid Brep is rejected with InvalidOperationException", threw);

Console.WriteLine();
if (failures == 0)
{
    Console.WriteLine("All checks passed.");
    Environment.Exit(0);
}
else
{
    Console.WriteLine($"{failures} check(s) failed.");
    Environment.Exit(1);
}
