using ModulusStudio.Core.Fonts;
using ModulusStudio.Core.Packing;
using ModulusStudio.Fonts;

namespace ModulusStudio.Core.Tests;

public class RasterizerTests
{
    [Fact]
    public void Gdi_RendersChineseGlyphWithReasonableInk()
    {
        using var rasterizer = new GdiFontRasterizer();
        var spec = new FontSpec { Family = "宋体", Width = 16, Height = 16 };

        var bitmap = rasterizer.Rasterize("中", spec);

        Assert.Equal(16, bitmap.Width);
        Assert.Equal(16, bitmap.Height);
        Assert.InRange(bitmap.CountOn(), 30, 160);

        var bytes = BitmapPacker.Pack(bitmap, new ScanOptions
        {
            ScanMode = ScanMode.ColumnRow,
            BitOrder = BitOrder.MsbFirst
        });
        Assert.Equal(32, bytes.Length);
        Assert.Contains(bytes, b => b != 0);
    }

    [Fact]
    public void Gdi_RendersAscii()
    {
        using var rasterizer = new GdiFontRasterizer();
        var spec = new FontSpec { Family = "宋体", Width = 8, Height = 16 };

        var bitmap = rasterizer.Rasterize("A", spec);

        Assert.InRange(bitmap.CountOn(), 5, 120);
    }

    [Fact]
    public void Skia_RendersChineseGlyph()
    {
        using var rasterizer = new SkiaFontRasterizer();
        var spec = new FontSpec { Family = "宋体", Width = 16, Height = 16 };

        var bitmap = rasterizer.Rasterize("中", spec);

        Assert.InRange(bitmap.CountOn(), 20, 200);
    }

    [Fact]
    public void Gdi_And_Skia_ProduceSimilarDensity()
    {
        using var gdi = new GdiFontRasterizer();
        using var skia = new SkiaFontRasterizer();
        var spec = new FontSpec { Family = "宋体", Width = 16, Height = 16 };

        var g = gdi.Rasterize("永", spec).CountOn();
        var s = skia.Rasterize("永", spec).CountOn();

        Assert.InRange(Math.Abs(g - s), 0, Math.Max(30, g / 2));
    }
}
