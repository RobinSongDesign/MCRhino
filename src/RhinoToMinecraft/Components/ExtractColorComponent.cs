using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using RhinoToMinecraft.Core;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Slice 2: extracts a single representative color from a referenced Rhino object,
/// either its flat display color (ObjectColor / Layer Color) or the average color
/// of its assigned material's texture map. Requires a referenced document object
/// (not bare/baked geometry) since color and material live on the RhinoObject, not
/// on the geometry itself.
/// </summary>
public class ExtractColorComponent : GH_Component
{
    public ExtractColorComponent()
        : base("Extract Color", "ExtColor",
            "Extracts a single representative color from a referenced object's display color or material texture average.",
            "RhinoToMinecraft", "Pipeline")
    {
    }

    public override Guid ComponentGuid => new Guid("9d4f2b8a-3c6e-4a1d-8f5b-7e2c9a4d1b68");

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddGeometryParameter("Object", "O", "Referenced Rhino object to extract a representative color from.", GH_ParamAccess.item);
        pManager.AddIntegerParameter("Source", "S", "0 = Display Color (ObjectColor/Layer Color), 1 = Material texture average color.", GH_ParamAccess.item, 0);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddColourParameter("Color", "C", "Extracted representative color.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        IGH_GeometricGoo goo = null!;
        int sourceValue = 0;

        if (!DA.GetData(0, ref goo)) return;
        if (!DA.GetData(1, ref sourceValue)) return;

        if (goo == null || goo.ReferenceID == Guid.Empty)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input must reference a Rhino document object (not raw/baked geometry) - color and material live on the object, not the geometry.");
            return;
        }

        var doc = RhinoDoc.ActiveDoc;
        var rhinoObject = doc?.Objects.FindId(goo.ReferenceID);
        if (rhinoObject == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Referenced object could not be found in the active document.");
            return;
        }

        if (sourceValue != 0 && sourceValue != 1)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Source must be 0 (Display Color) or 1 (Material texture average).");
            return;
        }

        var source = (ColorExtraction.ColorSource)sourceValue;

        Color color;
        try
        {
            color = ColorExtraction.GetRepresentativeColor(rhinoObject, source);
        }
        catch (InvalidOperationException ex)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            return;
        }

        DA.SetData(0, color);
    }
}
