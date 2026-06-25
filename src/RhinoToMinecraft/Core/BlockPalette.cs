using System.Collections.Generic;

namespace RhinoToMinecraft.Core;

/// <summary>
/// Slice 8 transitional block list: a hand-picked subset of full-block, flat-color
/// Minecraft building blocks (wool, concrete, planks, common stone types), used by
/// the "Pick Block" value list so users can choose a Block ID from a dropdown
/// instead of typing the string by hand. This is deliberately a small, manually
/// curated set, not the full RGB-matched palette dataset described in Slice 5 -
/// it only needs valid Block ID strings, not color data, so there is no licensing
/// concern in shipping it.
/// </summary>
public static class BlockPalette
{
    public static readonly IReadOnlyList<string> CuratedBlockIds = new List<string>
    {
        // Wool
        "minecraft:white_wool", "minecraft:orange_wool", "minecraft:magenta_wool", "minecraft:light_blue_wool",
        "minecraft:yellow_wool", "minecraft:lime_wool", "minecraft:pink_wool", "minecraft:gray_wool",
        "minecraft:light_gray_wool", "minecraft:cyan_wool", "minecraft:purple_wool", "minecraft:blue_wool",
        "minecraft:brown_wool", "minecraft:green_wool", "minecraft:red_wool", "minecraft:black_wool",
        // Concrete
        "minecraft:white_concrete", "minecraft:orange_concrete", "minecraft:magenta_concrete", "minecraft:light_blue_concrete",
        "minecraft:yellow_concrete", "minecraft:lime_concrete", "minecraft:pink_concrete", "minecraft:gray_concrete",
        "minecraft:light_gray_concrete", "minecraft:cyan_concrete", "minecraft:purple_concrete", "minecraft:blue_concrete",
        "minecraft:brown_concrete", "minecraft:green_concrete", "minecraft:red_concrete", "minecraft:black_concrete",
        // Wood planks
        "minecraft:oak_planks", "minecraft:spruce_planks", "minecraft:birch_planks", "minecraft:jungle_planks",
        "minecraft:acacia_planks", "minecraft:dark_oak_planks", "minecraft:crimson_planks", "minecraft:warped_planks",
        // Common stone/building blocks
        "minecraft:stone", "minecraft:cobblestone", "minecraft:stone_bricks", "minecraft:smooth_stone",
        "minecraft:bricks", "minecraft:quartz_block", "minecraft:sandstone", "minecraft:andesite",
    };
}
