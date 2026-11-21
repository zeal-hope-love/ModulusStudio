namespace ModulusStudio.Core.Packing;

/// <summary>取模扫描方式（与 PCtoLCD2002 的四种方式对应）。</summary>
public enum ScanMode
{
    /// <summary>逐行式：字节内为 8 个水平像素，按行输出。</summary>
    RowMajor,

    /// <summary>逐列式：字节内为 8 个垂直像素，按列输出。</summary>
    ColumnMajor,

    /// <summary>列行式：字节内为 8 个垂直像素，按 8 行带输出（等同 HZK 字库布局）。</summary>
    ColumnRow,

    /// <summary>行列式：字节内为 8 个水平像素，按 8 列带输出。</summary>
    RowColumn
}

/// <summary>字节内位的排列顺序。</summary>
public enum BitOrder
{
    /// <summary>顺向：高位在前（bit7 对应第一个像素）。</summary>
    MsbFirst,

    /// <summary>逆向：低位在前（bit0 对应第一个像素）。</summary>
    LsbFirst
}

/// <summary>取模参数。</summary>
public sealed class ScanOptions
{
    public ScanMode ScanMode { get; set; } = ScanMode.ColumnRow;

    public BitOrder BitOrder { get; set; } = BitOrder.MsbFirst;

    /// <summary>阳码：按位取反（1 表示背景、0 表示前景）。</summary>
    public bool Invert { get; set; }
}
