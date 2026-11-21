namespace ModulusStudio.Core.Imaging;

/// <summary>图像取模的颜色深度。</summary>
public enum ImageColorDepth
{
    Monochrome,
    Gray4,
    Gray16,
    Gray256,
    Color4096,
    Color16Bit,
    Color18Bit,
    Color24Bit,
    Color32Bit
}

/// <summary>图像取模的扫描方向。</summary>
public enum ImageScanDirection
{
    Horizontal,
    Vertical
}

/// <summary>图像取模参数（对应 Img2Lcd 的设置项）。</summary>
public sealed class ImagePackOptions
{
    public ImageColorDepth Depth { get; set; } = ImageColorDepth.Monochrome;

    public ImageScanDirection ScanDirection { get; set; } = ImageScanDirection.Horizontal;

    /// <summary>字节内高位在前。</summary>
    public bool MsbFirst { get; set; } = true;

    /// <summary>自右至左。</summary>
    public bool RightToLeft { get; set; }

    /// <summary>自底至顶。</summary>
    public bool BottomToTop { get; set; }

    /// <summary>字节内位反序（例如 0b00000001 → 0b10000000）。</summary>
    public bool ReverseBits { get; set; }

    /// <summary>多字节像素低字节在前（RGB565 常见）。</summary>
    public bool LowByteFirst { get; set; } = true;

    /// <summary>单色阈值（0-255）。</summary>
    public int Threshold { get; set; } = 128;

    /// <summary>单色取反。</summary>
    public bool InvertMono { get; set; }

    /// <summary>包含 BMP 文件头。</summary>
    public bool IncludeHeader { get; set; }

    /// <summary>包含调色板（单色 / 灰度）。</summary>
    public bool IncludePalette { get; set; } = true;

    /// <summary>亮度调整（-100 ~ 100）。</summary>
    public int Brightness { get; set; }

    /// <summary>对比度调整（-100 ~ 100）。</summary>
    public int Contrast { get; set; }

    /// <summary>反色。</summary>
    public bool InvertColors { get; set; }

    /// <summary>最大宽度限制（0 表示不限制）。</summary>
    public int MaxWidth { get; set; }

    /// <summary>最大高度限制（0 表示不限制）。</summary>
    public int MaxHeight { get; set; }
}
