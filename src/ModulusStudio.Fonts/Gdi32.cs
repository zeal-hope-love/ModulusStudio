using System.Runtime.InteropServices;

namespace ModulusStudio.Fonts;

public static class Gdi32
{
    public const int TRANSPARENT = 1;
    public const uint DIB_RGB_COLORS = 0;
    public const uint BLACKNESS = 0x00000042;
    public const int DEFAULT_CHARSET = 1;
    public const int DEFAULT_QUALITY = 0;
    public const int CLIP_DEFAULT_PRECIS = 0;
    public const int OUT_DEFAULT_PRECIS = 0;
    public const int DEFAULT_PITCH = 0;
    public const int FF_DONTCARE = 0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LOGFONTW
    {
        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string lfFaceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public RGBQUAD bmiColors0;
        public RGBQUAD bmiColors1;
    }

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern IntPtr CreateDIBSection(
        IntPtr hdc, ref BITMAPINFO pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateFontW(
        int nHeight, int nWidth, int nEscapement, int nOrientation, int fnWeight,
        uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet,
        uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality,
        uint fdwPitchAndFamily, string lpszFace);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern int SetTextColor(IntPtr hdc, int color);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern int SetBkColor(IntPtr hdc, int color);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern int SetBkMode(IntPtr hdc, int mode);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern bool PatBlt(IntPtr hdc, int x, int y, int w, int h, uint rop);

    [DllImport("gdi32.dll", SetLastError = true)]
    public static extern int GetDeviceCaps(IntPtr hdc, int index);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool TextOutW(IntPtr hdc, int x, int y, string lpString, int c);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint GetGlyphOutlineW(
        IntPtr hdc, uint uChar, uint fuFormat, ref GLYPHMETRICS lpgm,
        uint cjBuffer, IntPtr pvBuffer, ref MAT2 lpmat2);

    [StructLayout(LayoutKind.Sequential)]
    public struct GLYPHMETRICS
    {
        public uint gmBlackBoxX;
        public uint gmBlackBoxY;
        public int gmptGlyphOriginX;
        public int gmptGlyphOriginY;
        public short gmCellIncX;
        public short gmCellIncY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MAT2
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] eM11eM12eM21eM22;
    }
}
