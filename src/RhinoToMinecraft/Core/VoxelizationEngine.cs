using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace RhinoToMinecraft.Core;

/// <summary>
/// Pure geometry logic for Slice 1, deliberately kept free of any GH_Component/UI
/// dependency so it can be exercised from a standalone console harness without
/// needing a running Rhino/Grasshopper session.
/// </summary>
public static class VoxelizationEngine
{
    /// <summary>
    /// Builds a single closed, welded mesh from a closed Brep solid. Throws if the
    /// input does not produce a usable mesh - this engine assumes closed solids
    /// per the PRD (no automatic repair of open/non-manifold geometry).
    /// </summary>
    public static Mesh MeshFromBrep(Brep solid)
    {
        var pieces = Mesh.CreateFromBrep(solid, MeshingParameters.QualityRenderMesh);
        if (pieces == null || pieces.Length == 0)
            throw new InvalidOperationException("Brep could not be meshed - check that it is a closed solid.");

        var mesh = new Mesh();
        foreach (var piece in pieces)
            mesh.Append(piece);

        mesh.Weld(Math.PI);

        if (!mesh.IsClosed)
            throw new InvalidOperationException("Resulting mesh is not closed - input Brep must be a closed solid.");

        return mesh;
    }

    /// <summary>
    /// Generates voxel center points for a closed solid at the given scale factor,
    /// in the solid's own Rhino model-space coordinates (not Minecraft block
    /// space) - see issue 11. The sampling grid is anchored to the world origin
    /// (cell boundaries are multiples of 1/scaleFactor from x=0/y=0/z=0), not to
    /// this solid's own bounding box, so that multiple objects voxelized
    /// independently still land on the exact same grid lines - this is required
    /// for overlap resolution (Slice 3) to correctly detect when two objects claim
    /// the same voxel position. Each candidate point sits at the center of its
    /// 1/scaleFactor-sized cell (offset by half a cell from the grid lines) rather
    /// than on a grid line intersection - see issue 07 - because Rhino models
    /// conventionally align geometry to whole grid lines, so a voxel's true
    /// interior center is half a cell away from those lines. Use
    /// <see cref="ToBlockSpace"/> to convert the returned points into Minecraft
    /// block coordinates before export/overlap resolution.
    /// </summary>
    public static List<Point3d> Voxelize(Brep solid, double scaleFactor, double tolerance = 0.01)
    {
        if (scaleFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(scaleFactor), "Scale factor must be positive.");

        var mesh = MeshFromBrep(solid);
        return Voxelize(mesh, solid.GetBoundingBox(true), scaleFactor, tolerance);
    }

    public static List<Point3d> Voxelize(Mesh mesh, BoundingBox bbox, double scaleFactor, double tolerance = 0.01)
    {
        if (scaleFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(scaleFactor), "Scale factor must be positive.");

        double step = 1.0 / scaleFactor;
        double halfStep = step / 2.0;
        var centers = new List<Point3d>();

        int iMin = (int)Math.Floor(bbox.Min.X / step);
        int iMax = (int)Math.Ceiling(bbox.Max.X / step);
        int jMin = (int)Math.Floor(bbox.Min.Y / step);
        int jMax = (int)Math.Ceiling(bbox.Max.Y / step);
        int kMin = (int)Math.Floor(bbox.Min.Z / step);
        int kMax = (int)Math.Ceiling(bbox.Max.Z / step);

        for (int i = iMin; i <= iMax; i++)
        {
            double x = i * step + halfStep;
            for (int j = jMin; j <= jMax; j++)
            {
                double y = j * step + halfStep;
                for (int k = kMin; k <= kMax; k++)
                {
                    double z = k * step + halfStep;
                    var point = new Point3d(x, y, z);
                    if (mesh.IsPointInside(point, tolerance, false))
                        centers.Add(point);
                }
            }
        }

        return centers;
    }

    /// <summary>
    /// Converts a model-space voxel center (as returned by <see cref="Voxelize"/>)
    /// into Minecraft block space, by applying the same scale factor used to
    /// generate it. Because model-space centers sit at a half-cell offset, the
    /// result lands on a half-integer point (e.g. 2.5) whose containing block is
    /// found by flooring each coordinate - see issue 07.
    /// </summary>
    public static Point3d ToBlockSpace(Point3d modelSpacePoint, double scaleFactor) =>
        new Point3d(modelSpacePoint.X * scaleFactor, modelSpacePoint.Y * scaleFactor, modelSpacePoint.Z * scaleFactor);
}
