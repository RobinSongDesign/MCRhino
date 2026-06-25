using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Slice 8: looks up a Layer by name/full path in the active document and returns
/// every Brep geometry object on it, plus that Layer's own Color as the single
/// representative color for the whole layer - replaces having to manually wire
/// each object on a layer into ExtractColorComponent one at a time.
/// </summary>
public class GetLayerObjectsComponent : GH_Component
{
    public GetLayerObjectsComponent()
        : base("Get Layer Objects", "LayerObjs",
            "Looks up a Layer by name and returns all of its Brep geometry plus the Layer's representative color.",
            "RhinoToMinecraft", "Pipeline")
    {
    }

    public override Guid ComponentGuid => new Guid("8b6e1a4c-3f9d-4e2b-a7c5-9d1f6e3b8a02");

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Layer", "L", "Layer name or full path (e.g. 'Parent::Child') to look up in the active document.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddBrepParameter("Geometry", "G", "Brep geometry of every object found on the layer.", GH_ParamAccess.list);
        pManager.AddColourParameter("Color", "C", "The layer's own display color, used as its single representative color.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        string layerName = null!;
        if (!DA.GetData(0, ref layerName)) return;

        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No active Rhino document.");
            return;
        }

        Layer? layer = doc.Layers.FirstOrDefault(l =>
            string.Equals(l.FullPath, layerName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(l.Name, layerName, StringComparison.OrdinalIgnoreCase));

        if (layer == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"No layer named '{layerName}' was found in the active document.");
            return;
        }

        var breps = new List<Brep>();
        foreach (var rhinoObject in doc.Objects.FindByLayer(layer))
        {
            if (rhinoObject.Geometry is Brep brep)
                breps.Add(brep);
        }

        if (breps.Count == 0)
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Layer '{layerName}' contains no Brep geometry.");

        DA.SetDataList(0, breps);
        DA.SetData(1, layer.Color);
    }
}
