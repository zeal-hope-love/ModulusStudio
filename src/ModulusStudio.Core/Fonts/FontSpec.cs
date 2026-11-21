namespace ModulusStudio.Core.Fonts;

/// <summary>渲染字符所用的字体与点阵尺寸描述。</summary>
public sealed class FontSpec
{
    public string Family { get; set; } = "宋体";
    public int Width { get; set; } = 16;
    public int Height { get; set; } = 16;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool Strikeout { get; set; }

    /// <summary>字符集（对应 GDI 的 CHARSET，中文用 134=GB2312）。</summary>
    public int Charset { get; set; } = 134;

    /// <summary>固定字形宽度（关闭时按字体自然宽度渲染）。</summary>
    public bool FixedWidth { get; set; } = true;

    /// <summary>
    /// 显式字符宽度（对应 GDI 的 lfWidth）。0 表示使用字体自然宽度；
    /// 半角字符按半宽单元格渲染时需显式指定，才能与 PCtoLCD2002 一致。
    /// </summary>
    public int ExplicitWidth { get; set; }

    /// <summary>像素微调。</summary>
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }

    public FontSpec Clone() => (FontSpec)MemberwiseClone();
}
