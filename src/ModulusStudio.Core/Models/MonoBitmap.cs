namespace ModulusStudio.Core.Models;

/// <summary>
/// 单通道 1 位点阵：每个元素为 0（背景）或 1（前景）。
/// </summary>
public sealed class MonoBitmap
{
    private readonly byte[] _pixels;

    public MonoBitmap(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        Width = width;
        Height = height;
        _pixels = new byte[width * height];
    }

    public int Width { get; }
    public int Height { get; }

    public byte this[int x, int y]
    {
        get => _pixels[y * Width + x];
        set => _pixels[y * Width + x] = value == 0 ? (byte)0 : (byte)1;
    }

    public bool IsOn(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height && _pixels[y * Width + x] != 0;

    public void Set(int x, int y, bool on)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        _pixels[y * Width + x] = on ? (byte)1 : (byte)0;
    }

    public void Clear()
    {
        Array.Clear(_pixels);
    }

    public int CountOn()
    {
        var n = 0;
        foreach (var p in _pixels) n += p;
        return n;
    }

    public byte[] ToGray8() => _pixels.Select(p => (byte)(p == 0 ? 0 : 255)).ToArray();

    public MonoBitmap Clone()
    {
        var copy = new MonoBitmap(Width, Height);
        Array.Copy(_pixels, copy._pixels, _pixels.Length);
        return copy;
    }

    /// <summary>水平翻转。</summary>
    public void FlipHorizontal()
    {
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width / 2; x++)
        {
            var a = y * Width + x;
            var b = y * Width + (Width - 1 - x);
            (_pixels[a], _pixels[b]) = (_pixels[b], _pixels[a]);
        }
    }

    /// <summary>垂直翻转。</summary>
    public void FlipVertical()
    {
        for (var y = 0; y < Height / 2; y++)
        for (var x = 0; x < Width; x++)
        {
            var a = y * Width + x;
            var b = (Height - 1 - y) * Width + x;
            (_pixels[a], _pixels[b]) = (_pixels[b], _pixels[a]);
        }
    }

    /// <summary>逆时针旋转 90°。</summary>
    public MonoBitmap Rotate90()
    {
        var result = new MonoBitmap(Height, Width);
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            result.Set(y, Width - 1 - x, this[x, y] != 0);
        return result;
    }

    /// <summary>按行绘制字符图样，'#' 或 '1' 为前景，其余为背景。</summary>
    public static MonoBitmap FromArt(params string[] rows)
    {
        var height = rows.Length;
        var width = rows.Max(r => r.Length);
        var bmp = new MonoBitmap(width, height);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < rows[y].Length; x++)
            bmp.Set(x, y, rows[y][x] is '#' or '1' or 'O');
        return bmp;
    }
}
