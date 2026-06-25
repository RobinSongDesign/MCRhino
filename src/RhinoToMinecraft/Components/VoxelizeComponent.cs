using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoToMinecraft.Core;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Slice 1: voxelizes a single closed solid via a Bounding Box point grid + Point in
/// Mesh containment test. Outputs voxel center points in two coordinate spaces -
/// see issue 11: "ModelVoxels" stays in the solid's own Rhino model-space
/// coordinates, so feeding it into native GH "Center Box" + "Custom Preview"
/// visually overlays the voxels on the original input geometry regardless of
/// scale; "Voxels" is the same centers converted into Minecraft block space
/// (1 unit = 1 block) for the downstream Resolve Overlaps / Export pipeline.
/// This component does not draw anything itself, by design (Slice 1 explicitly
/// reuses existing GH components for display).
/// </summary>
public class VoxelizeComponent : GH_Component
{
    public VoxelizeComponent()
        : base("Voxelize", "Voxelize",
            "Voxelizes a closed solid into voxel center points using a Bounding Box point grid and Point in Mesh containment test.",
            "RhinoToMinecraft", "Pipeline")
    {
    }

    public override Guid ComponentGuid => new Guid("6a1c9e3f-8b2d-4a5e-9c7f-2d4b8e1a6c93");

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddBrepParameter("Solid", "S", "Closed solid Brep to voxelize. Must be a closed solid - open/non-manifold geometry is not auto-repaired.", GH_ParamAccess.item);
        pManager.AddNumberParameter("Scale", "X", "Scale factor. Higher values produce a finer voxel grid at the cost of more voxels.", GH_ParamAccess.item, 1.0);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Voxels", "V", "Voxel center points, in Minecraft block space (1 unit = 1 block) - feed into Resolve Overlaps / Export.", GH_ParamAccess.list);
        pManager.AddPointParameter("ModelVoxels", "M", "Voxel center points, in the input solid's own Rhino model-space coordinates - feed into Custom Preview to overlay voxels on the original geometry.", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        Brep solid = null!;
        double scale = 1.0;

        if (!DA.GetData(0, ref solid)) return;
        if (!DA.GetData(1, ref scale)) return;

        if (solid == null || !solid.IsSolid)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input geometry is not a closed solid Brep.");
            return;
        }

        if (scale <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scale must be greater than zero.");
            return;
        }

        List<Point3d> modelVoxels;
        try
        {
            modelVoxels = VoxelizationEngine.Voxelize(solid, scale);
        }
        catch (InvalidOperationException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        var blockVoxels = modelVoxels.Select(p => VoxelizationEngine.ToBlockSpace(p, scale)).ToList();

        DA.SetDataList(0, blockVoxels);
        DA.SetDataList(1, modelVoxels);
    }
}
