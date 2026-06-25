using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using RhinoToMinecraft.Core;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Slice 9: writes resolved voxels directly into a Minecraft world save's
/// datapack folder (creating pack.mcmeta + the data/&lt;namespace&gt;/functions
/// structure as needed), instead of requiring the user to manually place a
/// .mcfunction file in the right location. Does not trigger /reload or
/// /function in-game - see issue 09 for scope; complements (does not replace)
/// ExportMcFunctionComponent's plain-FilePath export.
/// </summary>
public class ExportMcFunctionToWorldComponent : GH_Component
{
    public ExportMcFunctionToWorldComponent()
        : base("Export McFunction To World", "ExportToWorld",
            "Writes resolved voxels into a world save's datapacks folder, auto-creating pack.mcmeta and the functions folder structure.",
            "RhinoToMinecraft", "Pipeline")
    {
    }

    public override Guid ComponentGuid => new Guid("3a7d5f9c-1e8b-4c6a-9f2d-7b4e8c1a6d35");

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Positions", "P", "Final resolved voxel positions, in Minecraft block space.", GH_ParamAccess.list);
        pManager.AddTextParameter("BlockId", "B", "Block ID per position, parallel to Positions.", GH_ParamAccess.list);
        pManager.AddPointParameter("OriginOffset", "O", "World-origin offset applied in Minecraft coordinate space (X, Y-up, Z), after the Rhino Z-up -> Minecraft Y-up axis swap.", GH_ParamAccess.item, new Point3d(0, 0, 0));
        pManager.AddTextParameter("WorldSaveFolder", "W", "Path to the world save folder, e.g. '...\\saves\\test'.", GH_ParamAccess.item);
        pManager.AddTextParameter("PackName", "N", "Datapack folder name to create under '<WorldSaveFolder>\\datapacks\\'.", GH_ParamAccess.item, "rhinotominecraft");
        pManager.AddTextParameter("Namespace", "NS", "Namespace under which the function is registered, e.g. 'rtm'.", GH_ParamAccess.item, "rtm");
        pManager.AddTextParameter("FunctionName", "F", "Function name, e.g. 'build'. Run in-game via '/function <namespace>:<name>' after '/reload'.", GH_ParamAccess.item, "build");
        pManager.AddIntegerParameter("PackFormat", "PF", "Datapack pack_format written into pack.mcmeta if it doesn't already exist (41 = Minecraft 1.20.5/1.20.6).", GH_ParamAccess.item, 41);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("FilePath", "F", "Full path of the written .mcfunction file.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("CommandCount", "N", "Number of /setblock commands written.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var positions = new List<Point3d>();
        var blockIds = new List<string>();
        Point3d originOffset = new Point3d(0, 0, 0);
        string worldSaveFolder = null!;
        string packName = "rhinotominecraft";
        string @namespace = "rtm";
        string functionName = "build";
        int packFormat = 41;

        if (!DA.GetDataList(0, positions)) return;
        if (!DA.GetDataList(1, blockIds)) return;
        if (!DA.GetData(2, ref originOffset)) return;
        if (!DA.GetData(3, ref worldSaveFolder)) return;
        if (!DA.GetData(4, ref packName)) return;
        if (!DA.GetData(5, ref @namespace)) return;
        if (!DA.GetData(6, ref functionName)) return;
        if (!DA.GetData(7, ref packFormat)) return;

        if (positions.Count != blockIds.Count)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"Positions has {positions.Count} item(s) but BlockId has {blockIds.Count} - these must be parallel lists of equal length.");
            return;
        }

        if (string.IsNullOrWhiteSpace(worldSaveFolder))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "WorldSaveFolder must not be empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(packName) || string.IsNullOrWhiteSpace(@namespace) || string.IsNullOrWhiteSpace(functionName))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "PackName, Namespace and FunctionName must not be empty.");
            return;
        }

        var voxels = positions.Zip(blockIds, (p, b) => (Position: p, BlockId: b)).ToList();

        string filePath;
        int count;
        try
        {
            (filePath, count) = McFunctionExporter.WriteToWorld(worldSaveFolder, packName, @namespace, functionName, voxels, originOffset, packFormat);
        }
        catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not write file: {ex.Message}");
            return;
        }

        DA.SetData(0, filePath);
        DA.SetData(1, count);
    }
}
