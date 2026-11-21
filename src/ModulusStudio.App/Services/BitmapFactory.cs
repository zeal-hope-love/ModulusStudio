using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ModulusStudio.Core.Imaging;
using ModulusStudio.Core.Models;

namespace ModulusStudio.App.Services;

public static class BitmapFactory
{
    public static BitmapSource FromFrame(ImageFrame frame)
    {
        var source = BitmapSource.Create(
            frame.Width, frame.Height, 96, 96,
            PixelFormats.Bgra32, null, frame.Bgra, frame.Width * 4);
        source.Freeze();
        return source;
    }

    /// <summary>把点阵放大绘制为预览图，可选网格线。</summary>
    public static BitmapSource FromMono(
        MonoBitmap bitmap,
        int scale,
        Color onColor,
        Color offColor,
        bool drawGrid,
        Color gridColor)
    {
        scale = Math.Clamp(scale, 1, 40);
        var w = bitmap.Width * scale;
        var h = bitmap.Height * scale;
        var stride = w * 4;
        var pixels = new byte[stride * h];

        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var on = bitmap.IsOn(x / scale, y / scale);
            var color = on ? onColor : offColor;
            if (drawGrid && ((x % scale == 0) || (y % scale == 0)))
                color = gridColor;

            var i = y * stride + x * 4;
            pixels[i] = color.B;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.R;
            pixels[i + 3] = 255;
        }

        var source = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        source.Freeze();
        return source;
    }

    /// <summary>把多个字符点阵横向拼接为一张预览图。</summary>
    public static BitmapSource FromMonoStrip(
        IReadOnlyList<MonoBitmap> glyphs,
        int scale,
        Color onColor,
        Color offColor,
        bool drawGrid,
        Color gridColor)
    {
        if (glyphs.Count == 0)
        {
            return FromMono(new MonoBitmap(8, 8), scale, onColor, offColor, drawGrid, gridColor);
        }

        var glyphWidth = glyphs.Max(g => g.Width);
        var glyphHeight = glyphs.Max(g => g.Height);
        var gap = 1;
        var totalWidth = glyphs.Count * glyphWidth + (glyphs.Count - 1) * gap;
        var combined = new MonoBitmap(totalWidth, glyphHeight);
        for (var i = 0; i < glyphs.Count; i++)
        {
            var g = glyphs[i];
            var x0 = i * (glyphWidth + gap);
            for (var y = 0; y < g.Height; y++)
            for (var x = 0; x < g.Width; x++)
                combined.Set(x0 + x, y, g.IsOn(x, y));
        }
        return FromMono(combined, scale, onColor, offColor, drawGrid, gridColor);
    }

    public static BitmapSource ColorSwatch(Color color, int size = 16)
    {
        var pixels = new byte[size * size * 4];
        for (var i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = color.B;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.R;
            pixels[i + 3] = 255;
        }
        var source = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4);
        source.Freeze();
        return source;
    }

    public static Color ParseColor(string hex, Color fallback)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return fallback;
        }
    }
}
