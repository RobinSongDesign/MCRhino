using System;
using Grasshopper.Kernel.Special;
using RhinoToMinecraft.Core;

namespace RhinoToMinecraft.Components;

/// <summary>
/// Slice 8: a pre-populated GH Value List exposing the curated building-block
/// subset of Minecraft Block IDs (see BlockPalette) as a dropdown, so users pick
/// a Block ID instead of typing a raw string like 'minecraft:red_concrete' by
/// hand. This is a transitional list - Slice 5's RGB-distance-matched candidate
/// list is a separate, larger feature.
/// </summary>
public class PickBlockComponent : GH_ValueList
{
    public PickBlockComponent()
    {
        Name = "Pick Block";
        NickName = "PickBlock";
        Description = "Pick a Minecraft Block ID from a curated list of building-friendly blocks.";
        Category = "RhinoToMinecraft";
        SubCategory = "Pipeline";

        ListItems.Clear();
        foreach (var blockId in BlockPalette.CuratedBlockIds)
            ListItems.Add(new GH_ValueListItem(blockId, $"\"{blockId}\""));
    }

    public override Guid ComponentGuid => new Guid("4e9b2d6a-7c1f-4a8e-9b3d-6f2a8c1e5d94");
}
