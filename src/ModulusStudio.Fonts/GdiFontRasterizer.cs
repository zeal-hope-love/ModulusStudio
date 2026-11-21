using System.Runtime.InteropServices;
using ModulusStudio.Core.Fonts;
using ModulusStudio.Core.Models;

namespace ModulusStudio.Fonts;

/// <summary>
/// 基于 GDI 的字体点阵后端：与原 PCtoLCD2002（Delphi + GDI TextOut）走同一条字形光栅化路径，
/// 因此对系统字体可做到接近像素一致。
/// </summary>
public sealed unsafe class GdiFontRasterizer : IFontRasterizer
{
    private readonly Dictionary<string, IntPtr> _fontCache = new();

    public string Name => "GDI（系统字体）";

    public MonoBitmap Rasterize(string text, FontSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        text ??= string.Empty;

        var width = Math.Clamp(spec.Width, 1, 1024);
        var height = Math.Clamp(spec.Height, 1, 1024);
        var result = new MonoBitmap(width, height);

        var hdc = Gdi32.CreateCompatibleDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero)
            throw new InvalidOperationException("CreateCompatibleDC 失败");

        IntPtr dib = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;
        IntPtr font = IntPtr.Zero;

        try
        {
            var bmi = new Gdi32.BITMAPINFO
            {
                bmiHeader = new Gdi32.BITMAPINFOHEADER
                {
                    biSize = (uint)Marshal.SizeOf<Gdi32.BITMAPINFOHEADER>(),
                    biWidth = width,
                    biHeight = -height, // 自上而下
                    biPlanes = 1,
                    biBitCount = 1,
                    biCompression = 0,
                    biSizeImage = 0,
                    biXPelsPerMeter = 0,
                    biYPelsPerMeter = 0,
                    biClrUsed = 2,
                    biClrImportant = 2
                },
                bmiColors0 = new Gdi32.RGBQUAD { rgbBlue = 0, rgbGreen = 0, rgbRed = 0 },
                bmiColors1 = new Gdi32.RGBQUAD { rgbBlue = 255, rgbGreen = 255, rgbRed = 255 }
            };

            dib = Gdi32.CreateDIBSection(hdc, ref bmi, Gdi32.DIB_RGB_COLORS, out var bits, IntPtr.Zero, 0);
            if (dib == IntPtr.Zero)
                throw new InvalidOperationException("CreateDIBSection 失败");

            oldBitmap = Gdi32.SelectObject(hdc, dib);
            Gdi32.PatBlt(hdc, 0, 0, width, height, Gdi32.BLACKNESS);

            font = GetFont(spec);
            if (font == IntPtr.Zero)
                return result;

            Gdi32.SelectObject(hdc, font);
            Gdi32.SetBkMode(hdc, Gdi32.TRANSPARENT);
            Gdi32.SetTextColor(hdc, 0x00FFFFFF);
            Gdi32.SetBkColor(hdc, 0x00000000);

            if (text.Length > 0)
                Gdi32.TextOutW(hdc, spec.OffsetX, spec.OffsetY, text, text.Length);

            // 读取 1bpp DIB：每行按 4 字节对齐
            var stride = ((width + 31) / 32) * 4;
            var span = new Span<byte>((void*)bits, stride * height);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var bit = (span[y * stride + (x >> 3)] >> (7 - (x & 7))) & 1;
                if (bit != 0) result.Set(x, y, true);
            }
        }
        finally
        {
            if (oldBitmap != IntPtr.Zero) Gdi32.SelectObject(hdc, oldBitmap);
            if (dib != IntPtr.Zero) Gdi32.DeleteObject(dib);
            Gdi32.DeleteDC(hdc);
        }

        return result;
    }

    private IntPtr GetFont(FontSpec spec)
    {
        var key = string.Join('|',
            spec.Family, spec.Width, spec.Height, spec.Bold, spec.Italic,
            spec.Underline, spec.Strikeout, spec.Charset, spec.FixedWidth, spec.ExplicitWidth);

        if (_fontCache.TryGetValue(key, out var cached) && cached != IntPtr.Zero)
            return cached;

        var lf = new Gdi32.LOGFONTW
        {
            lfHeight = -Math.Abs(spec.Height),
            // lfWidth=0 使用字体自身的自然宽度：宋体等 CJK 字体在 16px 时本身就是方形，
            // 若强行指定 lfWidth，GDI 会按“平均字宽”把全角字放大到约 2 倍宽。
            // 半角字符则显式指定半宽，才能与 PCtoLCD2002 的压窄字形一致。
            lfWidth = spec.ExplicitWidth,
            lfEscapement = 0,
            lfOrientation = 0,
            lfWeight = spec.Bold ? 700 : 400,
            lfItalic = (byte)(spec.Italic ? 1 : 0),
            lfUnderline = (byte)(spec.Underline ? 1 : 0),
            lfStrikeOut = (byte)(spec.Strikeout ? 1 : 0),
            lfCharSet = (byte)spec.Charset,
            lfOutPrecision = Gdi32.OUT_DEFAULT_PRECIS,
            lfClipPrecision = Gdi32.CLIP_DEFAULT_PRECIS,
            lfQuality = Gdi32.DEFAULT_QUALITY,
            lfPitchAndFamily = Gdi32.DEFAULT_PITCH | Gdi32.FF_DONTCARE,
            lfFaceName = spec.Family
        };

        var font = Gdi32.CreateFontW(
            lf.lfHeight, lf.lfWidth, 0, 0, lf.lfWeight,
            lf.lfItalic, lf.lfUnderline, lf.lfStrikeOut, lf.lfCharSet,
            Gdi32.OUT_DEFAULT_PRECIS, Gdi32.CLIP_DEFAULT_PRECIS, Gdi32.DEFAULT_QUALITY,
            (uint)(Gdi32.DEFAULT_PITCH | Gdi32.FF_DONTCARE), lf.lfFaceName);

        if (font != IntPtr.Zero) _fontCache[key] = font;
        return font;
    }

    public void Dispose()
    {
        foreach (var font in _fontCache.Values)
            if (font != IntPtr.Zero)
                Gdi32.DeleteObject(font);
        _fontCache.Clear();
    }
}
