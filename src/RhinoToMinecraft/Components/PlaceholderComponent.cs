using System;
using Grasshopper.Kernel;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Slice 0 placeholder: proves the plugin assembly loads and registers a component
/// in its own Grasshopper ribbon category. No real behavior yet.
/// </summary>
public class PlaceholderComponent : GH_Component
{
    public PlaceholderComponent()
        : base("Placeholder", "Placeholder",
            "Slice 0 scaffold placeholder - confirms the RhinoToMinecraft plugin loads.",
            "RhinoToMinecraft", "Pipeline")
    {
    }

    public override Guid ComponentGuid => new Guid("5d3e8a21-7f4b-4c9e-a1d2-3e6b9f8c4a17");

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Status", "S", "Confirms the plugin is loaded.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        DA.SetData(0, "RhinoToMinecraft plugin loaded.");
    }
}
