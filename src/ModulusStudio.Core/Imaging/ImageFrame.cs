namespace ModulusStudio.Core.Imaging;

/// <summary>图像像素缓冲（BGRA8888，行优先）。</summary>
public sealed class ImageFrame
{
    public ImageFrame(int width, int height, byte[] bgra)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (bgra.Length != width * height * 4)
            throw new ArgumentException("像素缓冲长度与尺寸不匹配", nameof(bgra));
        Width = width;
        Height = height;
        Bgra = bgra;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Bgra { get; }

    public (byte B, byte G, byte R, byte A) GetPixel(int x, int y)
    {
        var i = (y * Width + x) * 4;
        return (Bgra[i], Bgra[i + 1], Bgra[i + 2], Bgra[i + 3]);
    }

    public void SetPixel(int x, int y, byte b, byte g, byte r, byte a = 255)
    {
        var i = (y * Width + x) * 4;
        Bgra[i] = b;
        Bgra[i + 1] = g;
        Bgra[i + 2] = r;
        Bgra[i + 3] = a;
    }

    /// <summary>最近邻缩放到目标尺寸（超出目标尺寸的缩放由调用方决定是否执行）。</summary>
    public ImageFrame Resize(int width, int height)
    {
        if (width == Width && height == Height) return this;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var sy = (int)((long)y * Height / height);
            for (var x = 0; x < width; x++)
            {
                var sx = (int)((long)x * Width / width);
                var src = (sy * Width + sx) * 4;
                var dst = (y * width + x) * 4;
                pixels[dst] = Bgra[src];
                pixels[dst + 1] = Bgra[src + 1];
                pixels[dst + 2] = Bgra[src + 2];
                pixels[dst + 3] = Bgra[src + 3];
            }
        }
        return new ImageFrame(width, height, pixels);
    }

    public ImageFrame Clone() => new(Width, Height, (byte[])Bgra.Clone());
}
