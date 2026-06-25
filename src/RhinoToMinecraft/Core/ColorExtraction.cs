using System;
using System.Drawing;
using System.IO;
using Rhino.DocObjects;

namespace RhinoToMinecraft.Core;

/// <summary>
/// Pure logic for Slice 2: extracts a single representative color for a Rhino
/// object, sourced either from its flat display color (ObjectColor / Layer Color)
/// or from the average color of its assigned render material's texture map.
/// Deliberately does not support per-UV/per-point sampling - per the PRD, only a
/// single representative color per object/layer is in scope.
/// </summary>
public static class ColorExtraction
{
    public enum ColorSource
    {
        DisplayColor = 0,
        MaterialTextureAverage = 1,
    }

    public static Color GetRepresentativeColor(RhinoObject obj, ColorSource source)
    {
        return source switch
        {
            ColorSource.DisplayColor => GetDisplayColor(obj),
            ColorSource.MaterialTextureAverage => GetMaterialTextureAverageColor(obj),
            _ => throw new ArgumentOutOfRangeException(nameof(source)),
        };
    }

    public static Color GetDisplayColor(RhinoObject obj)
    {
        var attributes = obj.Attributes;
        if (attributes.ColorSource == ObjectColorSource.ColorFromLayer)
        {
            var layerIndex = attributes.LayerIndex;
            var layer = obj.Document.Layers[layerIndex];
            return layer.Color;
        }

        return attributes.ObjectColor;
    }

    /// <summary>
    /// Reads the object's assigned material; if it has a bitmap texture, returns the
    /// average color across the whole image. Falls back to the material's flat
    /// diffuse color if no texture is assigned, so this always yields a single
    /// usable representative color.
    /// </summary>
    public static Color GetMaterialTextureAverageColor(RhinoObject obj)
    {
        var material = obj.GetMaterial(true);
        if (material == null)
            throw new InvalidOperationException("Object has no material assigned - cannot extract a texture color.");

        var texture = material.GetBitmapTexture();
        if (texture == null)
            return material.DiffuseColor;

        string path = texture.FileReference.FullPath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new InvalidOperationException($"Texture file could not be located on disk: '{path}'");

        using var bitmap = new Bitmap(path);
        return AverageColor(bitmap);
    }

    /// <summary>
    /// Averages pixel colors across the bitmap. Steps through the image rather than
    /// reading every pixel, since this only needs to produce one representative
    /// color, not a precise histogram - exact precision is not the goal here.
    /// </summary>
    private static Color AverageColor(Bitmap bitmap, int maxSamplesPerAxis = 64)
    {
        long r = 0, g = 0, b = 0, count = 0;
        int stepX = Math.Max(1, bitmap.Width / maxSamplesPerAxis);
        int stepY = Math.Max(1, bitmap.Height / maxSamplesPerAxis);

        for (int x = 0; x < bitmap.Width; x += stepX)
        {
            for (int y = 0; y < bitmap.Height; y += stepY)
            {
                var pixel = bitmap.GetPixel(x, y);
                r += pixel.R;
                g += pixel.G;
                b += pixel.B;
                count++;
            }
        }

        if (count == 0)
            throw new InvalidOperationException("Texture bitmap contained no readable pixels.");

        return Color.FromArgb((int)(r / count), (int)(g / count), (int)(b / count));
    }
}
