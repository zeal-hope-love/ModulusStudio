using ModulusStudio.Core.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ModulusStudio.App.WinUI.Services;

/// <summary>使用 WIC（WinRT BitmapDecoder）解码常见图片格式。</summary>
public static class ImageLoader
{
    public static readonly string[] SupportedExtensions =
    {
        ".bmp", ".dib", ".jpg", ".jpeg", ".gif", ".ico", ".png", ".tif", ".tiff", ".wmf", ".emf"
    };

    public static async Task<ImageFrame> LoadAsync(StorageFile file)
    {
        using IRandomAccessStream stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);

        var provider = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Straight,
            new BitmapTransform(),
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);

        var pixels = provider.DetachPixelData();
        return new ImageFrame((int)decoder.PixelWidth, (int)decoder.PixelHeight, pixels);
    }
}
