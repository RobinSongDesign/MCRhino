using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoToMinecraft.Core;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Dev-only smoke test exercising the Slice 1-4 core engine classes end to end with
/// zero inputs required, so it can be verified just by placing it on the canvas
/// (no wiring needed). Builds two overlapping test boxes, voxelizes both, resolves
/// the overlap, and writes a .mcfunction file plus a result log to %TEMP% for
/// inspection. Not part of the PRD-facing pipeline - exists purely to prove the
/// engine classes behave correctly inside a real Rhino/Grasshopper process, since
/// RhinoCommon's native geometry kernel does not initialize in a standalone
/// console harness outside of one.
/// </summary>
public class SelfTestComponent : GH_Component
{
    public SelfTestComponent()
        : base("RTM Self Test", "RTM_SelfTest",
            "Dev-only: exercises Voxelize -> Resolve Overlaps -> Export McFunction on two overlapping test boxes and reports pass/fail.",
            "RhinoToMinecraft", "Dev")
    {
    }

    public override Guid ComponentGuid => new Guid("1a3e7c5b-9d2f-4e6a-8c1b-3f5d9a7e2c64");

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddBooleanParameter("Passed", "P", "True if all checks passed.", GH_ParamAccess.item);
        pManager.AddTextParameter("Summary", "S", "Human-readable result log.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var log = new List<string>();
        bool passed = true;

        try
        {
            var boxA = new Box(Plane.WorldXY, new Interval(0, 2), new Interval(0, 2), new Interval(0, 2)).ToBrep();
            var boxB = new Box(Plane.WorldXY, new Interval(1, 3), new Interval(1, 3), new Interval(1, 3)).ToBrep();

            const double scaleFactor = 1.0;
            var voxelsA = VoxelizationEngine.Voxelize(boxA, scaleFactor)
                .Select(p => VoxelizationEngine.ToBlockSpace(p, scaleFactor)).ToList();
            var voxelsB = VoxelizationEngine.Voxelize(boxB, scaleFactor)
                .Select(p => VoxelizationEngine.ToBlockSpace(p, scaleFactor)).ToList();
            log.Add($"Box A voxel count: {voxelsA.Count}");
            log.Add($"Box B voxel count: {voxelsB.Count}");

            var resolved = VoxelOverlapResolver.Resolve(
                new IReadOnlyList<Point3d>[] { voxelsA, voxelsB },
                new[] { "minecraft:stone", "minecraft:red_concrete" });
            log.Add($"Resolved voxel count (deduplicated): {resolved.Count}");

            int expectedUnion = CountUnion(voxelsA, voxelsB);
            log.Add($"Expected union count (independently computed): {expectedUnion}");
            bool unionCheck = resolved.Count == expectedUnion;
            log.Add(unionCheck ? "UNION COUNT CHECK: PASS" : "UNION COUNT CHECK: FAIL");
            passed &= unionCheck;

            int overlapCount = CountOverlap(voxelsA, voxelsB);
            int redConcreteCount = 0;
            foreach (var v in resolved)
                if (v.BlockId == "minecraft:red_concrete") redConcreteCount++;
            log.Add($"Geometric overlap count (independently computed): {overlapCount}");
            log.Add($"Voxels resolved to minecraft:red_concrete (Box B, later in list): {redConcreteCount}");
            bool overlapCheck = overlapCount > 0 && redConcreteCount >= overlapCount;
            log.Add(overlapCheck ? "OVERLAP-WINS-LATER CHECK: PASS" : "OVERLAP-WINS-LATER CHECK: FAIL");
            passed &= overlapCheck;

            var voxelTuples = new List<(Point3d Position, string BlockId)>();
            foreach (var v in resolved)
                voxelTuples.Add((v.Position, v.BlockId));

            string outDir = Path.GetTempPath();
            string mcfPath = Path.Combine(outDir, "rtm_test_pipeline.mcfunction");
            int written = McFunctionExporter.WriteFile(mcfPath, voxelTuples, new Point3d(100, 64, 100));
            log.Add($"Wrote {written} commands to: {mcfPath}");
            passed &= written == resolved.Count;

            string logPath = Path.Combine(outDir, "rtm_test_pipeline_result.txt");
            File.WriteAllLines(logPath, log);

            DA.SetData(0, passed);
            DA.SetData(1, string.Join("\n", log));
        }
        catch (Exception ex)
        {
            log.Add("EXCEPTION: " + ex);
            File.WriteAllLines(Path.Combine(Path.GetTempPath(), "rtm_test_pipeline_result.txt"), log);
            DA.SetData(0, false);
            DA.SetData(1, string.Join("\n", log));
        }
    }

    // Block-space positions sit at a half-integer offset (e.g. 2.5) per issue 07,
    // so Floor is used rather than Round (which applies banker's rounding to the
    // .5 value and is not reliably injective - e.g. both 1.5 and 2.5 round to 2).
    private static int CountUnion(List<Point3d> a, List<Point3d> b)
    {
        var set = new HashSet<(long, long, long)>();
        foreach (var p in a) set.Add(((long)Math.Floor(p.X), (long)Math.Floor(p.Y), (long)Math.Floor(p.Z)));
        foreach (var p in b) set.Add(((long)Math.Floor(p.X), (long)Math.Floor(p.Y), (long)Math.Floor(p.Z)));
        return set.Count;
    }

    private static int CountOverlap(List<Point3d> a, List<Point3d> b)
    {
        var setA = new HashSet<(long, long, long)>();
        foreach (var p in a) setA.Add(((long)Math.Floor(p.X), (long)Math.Floor(p.Y), (long)Math.Floor(p.Z)));
        int count = 0;
        foreach (var p in b)
            if (setA.Contains(((long)Math.Floor(p.X), (long)Math.Floor(p.Y), (long)Math.Floor(p.Z))))
                count++;
        return count;
    }
}
