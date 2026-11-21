using System.Runtime.InteropServices;

namespace ModulusStudio.App.WinUI.Services;

/// <summary>通过 GDI 枚举系统字体（返回本地化字体名，与 GDI/Skia 后端共用）。</summary>
public static class FontCatalog
{
    private const byte DefaultCharset = 1;
    private const int GdiTruetype = 4;

    private static IReadOnlyList<string>? _cached;

    public static IReadOnlyList<string> GetFamilies()
    {
        if (_cached is not null) return _cached;

        var names = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        var hdc = Native.CreateCompatibleDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero) return _cached = new List<string>();

        try
        {
            var logfont = new Native.LOGFONT
            {
                lfCharSet = DefaultCharset,
                lfFaceName = string.Empty
            };

            Native.EnumFontFamExProc callback = (ref Native.LOGFONT lf, IntPtr metric, uint type, IntPtr lParam) =>
            {
                var name = lf.lfFaceName;
                if (!string.IsNullOrWhiteSpace(name) && name[0] != '@')
                    names.Add(name);
                return 1;
            };

            Native.EnumFontFamiliesExW(hdc, ref logfont, callback, IntPtr.Zero, 0);
            GC.KeepAlive(callback);
        }
        finally
        {
            Native.DeleteDC(hdc);
        }

        _cached = names.OrderBy(n => n, StringComparer.CurrentCulture).ToList();
        return _cached;
    }

    private static class Native
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LOGFONT
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

        public delegate int EnumFontFamExProc(ref LOGFONT lpelfe, IntPtr lpntme, uint fontType, IntPtr lParam);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        public static extern int EnumFontFamiliesExW(IntPtr hdc, ref LOGFONT lpLogfont, EnumFontFamExProc lpProc, IntPtr lParam, uint dwFlags);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);
    }
}
