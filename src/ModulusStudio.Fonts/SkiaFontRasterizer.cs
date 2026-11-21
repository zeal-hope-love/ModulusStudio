using ModulusStudio.Core.Fonts;
using ModulusStudio.Core.Models;
using SkiaSharp;

namespace ModulusStudio.Fonts;

/// <summary>
/// 基于 Skia（FreeType 光栅化）的字体后端：字形由字体轮廓重新生成，
/// 可精确控制微调与闭合，代价是与原软件的 GDI 点阵存在 1px 级差异。
/// </summary>
public sealed class SkiaFontRasterizer : IFontRasterizer
{
    public string Name => "Skia / FreeType（轮廓渲染）";

    public MonoBitmap Rasterize(string text, FontSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var width = Math.Clamp(spec.Width, 1, 1024);
        var height = Math.Clamp(spec.Height, 1, 1024);
        var result = new MonoBitmap(width, height);
        if (string.IsNullOrEmpty(text)) return result;

        var style = new SKFontStyle(
            spec.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            spec.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);

        using var typeface = SKTypeface.FromFamilyName(spec.Family, style) ?? SKTypeface.Default;
        using var font = new SKFont(typeface, Math.Max(1, spec.Height))
        {
            Edging = SKFontEdging.Alias,
            Hinting = SKFontHinting.None,
            Subpixel = false
        };

        using var bitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Opaque);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = false,
            Style = SKPaintStyle.Fill
        };

        var metrics = font.Metrics;
        var baseline = -metrics.Ascent + spec.OffsetY;
        var x = (float)spec.OffsetX;

        foreach (var rune in text.EnumerateRunes())
        {
            var glyphText = rune.ToString();
            var advance = font.MeasureText(glyphText);

            canvas.Save();
            canvas.Translate(x, 0);
            if (spec.FixedWidth && advance > 0)
                canvas.Scale(width / advance, 1);
            canvas.DrawText(glyphText, 0, baseline, SKTextAlign.Left, font, paint);
            canvas.Restore();

            x += spec.FixedWidth ? width : advance;
        }

        var pixels = bitmap.GetPixelSpan();
        var stride = bitmap.RowBytes;
        for (var y = 0; y < height; y++)
        for (var px = 0; px < width; px++)
        {
            if (pixels[y * stride + px] > 127) result.Set(px, y, true);
        }

        return result;
    }

    public void Dispose()
    {
    }
}
