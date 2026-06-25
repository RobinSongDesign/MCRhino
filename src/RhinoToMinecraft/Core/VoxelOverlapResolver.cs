using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace RhinoToMinecraft.Core;

/// <summary>
/// Pure logic for Slice 3: resolves voxel positions claimed by more than one
/// object. Objects are processed in the order given, and an object later in that
/// order overwrites the Block ID of any position already claimed by an earlier
/// object - i.e. "last in the list wins", per the PRD's deliberately simple
/// overlap rule (no separate priority management UI).
/// </summary>
public static class VoxelOverlapResolver
{
    public readonly record struct ResolvedVoxel(Point3d Position, string BlockId);

    public static List<ResolvedVoxel> Resolve(IReadOnlyList<IReadOnlyList<Point3d>> voxelsPerObject, IReadOnlyList<string> blockIdsPerObject)
    {
        if (voxelsPerObject.Count != blockIdsPerObject.Count)
            throw new ArgumentException("voxelsPerObject and blockIdsPerObject must contain the same number of entries (one per object).");

        var map = new Dictionary<(long X, long Y, long Z), ResolvedVoxel>();

        for (int objectIndex = 0; objectIndex < voxelsPerObject.Count; objectIndex++)
        {
            string blockId = blockIdsPerObject[objectIndex];
            foreach (var position in voxelsPerObject[objectIndex])
            {
                var key = ToGridKey(position);
                // Later objects in the input order overwrite earlier ones at the same position.
                map[key] = new ResolvedVoxel(position, blockId);
            }
        }

        return new List<ResolvedVoxel>(map.Values);
    }

    /// <summary>
    /// Block-space voxel positions sit at a half-integer offset (e.g. 2.5) per
    /// issue 07, so the containing block is found by flooring each coordinate -
    /// Math.Round would apply banker's rounding to the .5 value and is not
    /// reliably injective (e.g. both 1.5 and 2.5 round to 2).
    /// </summary>
    private static (long X, long Y, long Z) ToGridKey(Point3d position) =>
        ((long)Math.Floor(position.X), (long)Math.Floor(position.Y), (long)Math.Floor(position.Z));
}
