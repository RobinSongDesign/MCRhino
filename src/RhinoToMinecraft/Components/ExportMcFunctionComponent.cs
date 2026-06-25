using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoToMinecraft.Core;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Slice 4 (MVP milestone): writes the final resolved voxel set to a .mcfunction
/// file of "/setblock" commands, converting Rhino's Z-up coordinates to
/// Minecraft's Y-up coordinates and applying a configurable origin offset.
/// </summary>
public class ExportMcFunctionComponent : GH_Component
{
    public ExportMcFunctionComponent()
        : base("Export McFunction", "ExportMC",
            "Writes resolved voxels to a .mcfunction file of /setblock commands (Rhino Z-up -> Minecraft Y-up).",
            "RhinoToMinecraft", "Pipeline")
    {
    }

    public override Guid ComponentGuid => new Guid("4f9c2d6e-8a1b-4f3c-9e7d-2b5a8c1f6d93");

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Positions", "P", "Final resolved voxel positions, in Minecraft block space.", GH_ParamAccess.list);
        pManager.AddTextParameter("BlockId", "B", "Block ID per position, parallel to Positions.", GH_ParamAccess.list);
        pManager.AddPointParameter("OriginOffset", "O", "World-origin offset applied in Minecraft coordinate space (X, Y-up, Z), after the Rhino Z-up -> Minecraft Y-up axis swap.", GH_ParamAccess.item, new Point3d(0, 0, 0));
        pManager.AddTextParameter("FilePath", "F", "Output .mcfunction file path.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddIntegerParameter("CommandCount", "N", "Number of /setblock commands written.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var positions = new List<Point3d>();
        var blockIds = new List<string>();
        Point3d originOffset = new Point3d(0, 0, 0);
        string filePath = null!;

        if (!DA.GetDataList(0, positions)) return;
        if (!DA.GetDataList(1, blockIds)) return;
        if (!DA.GetData(2, ref originOffset)) return;
        if (!DA.GetData(3, ref filePath)) return;

        if (positions.Count != blockIds.Count)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"Positions has {positions.Count} item(s) but BlockId has {blockIds.Count} - these must be parallel lists of equal length.");
            return;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "FilePath must not be empty.");
            return;
        }

        var voxels = positions.Zip(blockIds, (p, b) => (Position: p, BlockId: b)).ToList();

        int count;
        try
        {
            count = McFunctionExporter.WriteFile(filePath, voxels, originOffset);
        }
        catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not write file: {ex.Message}");
            return;
        }

        DA.SetData(0, count);
    }
}
