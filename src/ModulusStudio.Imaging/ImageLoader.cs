using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ModulusStudio.Core.Imaging;

namespace ModulusStudio.Imaging;

/// <summary>使用 WIC 解码常见图片格式（BMP/JPG/GIF/ICO/PNG/TIFF/WMF/EMF）。</summary>
public static class ImageLoader
{
    public static readonly string[] SupportedExtensions =
    {
        ".bmp", ".dib", ".jpg", ".jpeg", ".gif", ".ico", ".png", ".tif", ".tiff", ".wmf", ".emf"
    };

    public static ImageFrame Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static ImageFrame Load(Stream stream)
    {
        var decoder = BitmapDecoder.Create(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        if (decoder.Frames.Count == 0)
            throw new InvalidOperationException("图片不包含任何帧");

        var source = decoder.Frames[0];
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        var width = converted.PixelWidth;
        var height = converted.PixelHeight;
        var pixels = new byte[width * height * 4];
        converted.CopyPixels(pixels, width * 4, 0);
        return new ImageFrame(width, height, pixels);
    }
}
