using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using RhinoToMinecraft.Core;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Slice 3: deduplicates voxel positions claimed by more than one object. Voxels
/// come in as a tree with one branch per object; BlockId comes in as a parallel
/// list with one entry per branch. Branches/list entries are processed in order,
/// and a later object overwrites an earlier one at any shared position.
/// </summary>
public class ResolveOverlapsComponent : GH_Component
{
    public ResolveOverlapsComponent()
        : base("Resolve Overlaps", "Overlaps",
            "Deduplicates voxel positions shared by multiple objects - later objects (by branch order) override earlier ones.",
            "RhinoToMinecraft", "Pipeline")
    {
    }

    public override Guid ComponentGuid => new Guid("7e1a4c9d-6b3f-4e8a-9d2c-5f7b1e3a8c40");

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Voxels", "V", "Voxel center points per object, as a tree with one branch per object in override-priority order (later branches win).", GH_ParamAccess.tree);
        pManager.AddTextParameter("BlockId", "B", "Block ID per object, one item per branch, in the same order as the Voxels tree's branches.", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Positions", "P", "Deduplicated final voxel positions.", GH_ParamAccess.list);
        pManager.AddTextParameter("BlockId", "B", "Final Block ID for each output position, parallel to Positions.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var tree = new GH_Structure<GH_Point>();
        if (!DA.GetDataTree(0, out tree)) return;

        var blockIds = new List<string>();
        if (!DA.GetDataList(1, blockIds)) return;

        if (tree.Branches.Count != blockIds.Count)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"Voxel tree has {tree.Branches.Count} branch(es) but {blockIds.Count} BlockId value(s) were given - these must match 1:1, one per object.");
            return;
        }

        var voxelsPerObject = tree.Branches
            .Select(branch => (IReadOnlyList<Point3d>)branch.Select(ghPoint => ghPoint.Value).ToList())
            .ToList();

        List<VoxelOverlapResolver.ResolvedVoxel> resolved;
        try
        {
            resolved = VoxelOverlapResolver.Resolve(voxelsPerObject, blockIds);
        }
        catch (ArgumentException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        DA.SetDataList(0, resolved.Select(v => v.Position));
        DA.SetDataList(1, resolved.Select(v => v.BlockId));
    }
}
