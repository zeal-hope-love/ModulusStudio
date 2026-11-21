using System.IO;
using System.Text;
using Microsoft.UI.Xaml.Media;
using ModulusStudio.App.WinUI.Mvvm;
using ModulusStudio.App.WinUI.Services;
using ModulusStudio.Core.Formatting;
using ModulusStudio.Core.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace ModulusStudio.App.WinUI.ViewModels;

public sealed class ImageModulusViewModel : ObservableObject
{
    private ImageFrame? _frame;
    private byte[] _packed = Array.Empty<byte>();
    private string _fileName = string.Empty;

    public ImageModulusViewModel()
    {
        OpenCommand = new RelayCommand(() => _ = RunAsync(OpenImageAsync));
        RegenerateCommand = new RelayCommand(Regenerate);
        CopyCommand = new RelayCommand(CopyCode);
        SaveBinaryCommand = new RelayCommand(() => _ = RunAsync(SaveBinaryAsync));
        ClearCommand = new RelayCommand(Clear);
    }

    public IReadOnlyList<string> DepthNames { get; } = new[]
    {
        "单色（1 位）", "4 级灰度", "16 级灰度", "256 级灰度",
        "4096 色（RGB444）", "16 位彩色（RGB565）", "18 位彩色（RGB666）",
        "24 位彩色（RGB888）", "32 位彩色（ARGB8888）"
    };

    public IReadOnlyList<string> ScanNames { get; } = new[] { "水平扫描", "垂直扫描" };

    public IReadOnlyList<string> FormatNames { get; } = new[] { "C51", "A51", "十进制" };

    public RelayCommand OpenCommand { get; }
    public RelayCommand RegenerateCommand { get; }
    public RelayCommand CopyCommand { get; }
    public RelayCommand SaveBinaryCommand { get; }
    public RelayCommand ClearCommand { get; }

    private ImageSource? _preview;
    public ImageSource? Preview
    {
        get => _preview;
        private set => SetProperty(ref _preview, value);
    }

    private string _outputCode = string.Empty;
    public string OutputCode
    {
        get => _outputCode;
        private set => SetProperty(ref _outputCode, value);
    }

    private string _statusText = "拖入或打开一张图片开始";
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private string _imageInfo = "未加载图片";
    public string ImageInfo
    {
        get => _imageInfo;
        private set => SetProperty(ref _imageInfo, value);
    }

    private int _depthIndex;
    public int DepthIndex
    {
        get => _depthIndex;
        set { if (SetProperty(ref _depthIndex, value)) Regenerate(); }
    }

    private int _scanIndex;
    public int ScanIndex
    {
        get => _scanIndex;
        set { if (SetProperty(ref _scanIndex, value)) Regenerate(); }
    }

    private int _formatIndex;
    public int FormatIndex
    {
        get => _formatIndex;
        set { if (SetProperty(ref _formatIndex, value)) Regenerate(); }
    }

    private int _bytesPerLine = 16;
    public int BytesPerLine
    {
        get => _bytesPerLine;
        set { if (SetProperty(ref _bytesPerLine, Math.Clamp(value, 1, 80))) Regenerate(); }
    }

    private bool _msbFirst = true;
    public bool MsbFirst
    {
        get => _msbFirst;
        set { if (SetProperty(ref _msbFirst, value)) Regenerate(); }
    }

    private bool _rightToLeft;
    public bool RightToLeft
    {
        get => _rightToLeft;
        set { if (SetProperty(ref _rightToLeft, value)) Regenerate(); }
    }

    private bool _bottomToTop;
    public bool BottomToTop
    {
        get => _bottomToTop;
        set { if (SetProperty(ref _bottomToTop, value)) Regenerate(); }
    }

    private bool _reverseBits;
    public bool ReverseBits
    {
        get => _reverseBits;
        set { if (SetProperty(ref _reverseBits, value)) Regenerate(); }
    }

    private bool _lowByteFirst = true;
    public bool LowByteFirst
    {
        get => _lowByteFirst;
        set { if (SetProperty(ref _lowByteFirst, value)) Regenerate(); }
    }

    private bool _includeHeader;
    public bool IncludeHeader
    {
        get => _includeHeader;
        set { if (SetProperty(ref _includeHeader, value)) Regenerate(); }
    }

    private bool _invertColors;
    public bool InvertColors
    {
        get => _invertColors;
        set { if (SetProperty(ref _invertColors, value)) Regenerate(); }
    }

    private int _threshold = 128;
    public int Threshold
    {
        get => _threshold;
        set { if (SetProperty(ref _threshold, Math.Clamp(value, 0, 255))) Regenerate(); }
    }

    private int _brightness;
    public int Brightness
    {
        get => _brightness;
        set { if (SetProperty(ref _brightness, Math.Clamp(value, -100, 100))) Regenerate(); }
    }

    private int _contrast;
    public int Contrast
    {
        get => _contrast;
        set { if (SetProperty(ref _contrast, Math.Clamp(value, -100, 100))) Regenerate(); }
    }

    private int _maxWidth;
    public int MaxWidth
    {
        get => _maxWidth;
        set { if (SetProperty(ref _maxWidth, Math.Max(0, value))) Regenerate(); }
    }

    private int _maxHeight;
    public int MaxHeight
    {
        get => _maxHeight;
        set { if (SetProperty(ref _maxHeight, Math.Max(0, value))) Regenerate(); }
    }

    private async Task OpenImageAsync()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary
        };
        PickerHelper.Initialize(picker);
        foreach (var ext in ImageLoader.SupportedExtensions)
            picker.FileTypeFilter.Add(ext);

        var file = await picker.PickSingleFileAsync();
        if (file is null) return;
        await LoadFileAsync(file);
    }

    public async Task LoadFileAsync(StorageFile file)
    {
        try
        {
            _frame = await ImageLoader.LoadAsync(file);
            _fileName = file.Name;
            Preview = BitmapFactory.FromFrame(_frame);
            ImageInfo = $"{_fileName}  ·  {_frame.Width} × {_frame.Height}";
            Regenerate();
        }
        catch (Exception ex)
        {
            StatusText = $"打开失败：{ex.Message}";
        }
    }

    private void Clear()
    {
        _frame = null;
        _packed = Array.Empty<byte>();
        Preview = null;
        OutputCode = string.Empty;
        ImageInfo = "未加载图片";
        StatusText = "已清空";
    }

    private ImagePackOptions BuildOptions() => new()
    {
        Depth = (ImageColorDepth)Math.Clamp(DepthIndex, 0, 8),
        ScanDirection = ScanIndex == 1 ? ImageScanDirection.Vertical : ImageScanDirection.Horizontal,
        MsbFirst = MsbFirst,
        RightToLeft = RightToLeft,
        BottomToTop = BottomToTop,
        ReverseBits = ReverseBits,
        LowByteFirst = LowByteFirst,
        Threshold = Threshold,
        IncludeHeader = IncludeHeader,
        IncludePalette = true,
        Brightness = Brightness,
        Contrast = Contrast,
        InvertColors = InvertColors,
        MaxWidth = MaxWidth,
        MaxHeight = MaxHeight
    };

    private void Regenerate()
    {
        if (_frame is null) return;
        try
        {
            _packed = ImagePacker.Pack(_frame, BuildOptions());

            var format = FormatIndex switch
            {
                1 => CodeFormatOptions.CreateA51(),
                2 => CodeFormatOptions.CreateDecimal(),
                _ => CodeFormatOptions.CreateC51()
            };
            format.BytesPerLine = BytesPerLine;

            OutputCode = CodeFormatter.Format(_packed, format);

            var depth = (ImageColorDepth)Math.Clamp(DepthIndex, 0, 8);
            var bits = depth switch
            {
                ImageColorDepth.Monochrome => 1,
                ImageColorDepth.Gray4 => 2,
                ImageColorDepth.Gray16 => 4,
                ImageColorDepth.Gray256 => 8,
                ImageColorDepth.Color4096 => 12,
                ImageColorDepth.Color16Bit => 16,
                ImageColorDepth.Color18Bit => 18,
                ImageColorDepth.Color24Bit => 24,
                _ => 32
            };
            StatusText = $"{bits} 位 · 共 {_packed.Length} 字节";
        }
        catch (Exception ex)
        {
            StatusText = $"生成失败：{ex.Message}";
        }
    }

    private void CopyCode()
    {
        if (string.IsNullOrEmpty(OutputCode)) return;
        var package = new DataPackage();
        package.SetText(OutputCode);
        Clipboard.SetContent(package);
        Clipboard.Flush();
        StatusText = "已复制到剪贴板";
    }

    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusText = $"操作失败：{ex.Message}";
        }
    }

    private async Task SaveBinaryAsync()
    {
        if (_packed.Length == 0) return;

        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "image"
        };
        PickerHelper.Initialize(picker);
        picker.FileTypeChoices.Add("二进制数据", new List<string> { ".bin" });
        picker.FileTypeChoices.Add("BMP 图片", new List<string> { ".bmp" });
        picker.FileTypeChoices.Add("C 头文件", new List<string> { ".h" });

        var file = await picker.PickSaveFileAsync();
        if (file is null) return;

        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (ext == ".h")
        {
            var header = $"#ifndef MODULUS_IMAGE_DATA_H{Environment.NewLine}#define MODULUS_IMAGE_DATA_H{Environment.NewLine}{Environment.NewLine}{OutputCode}{Environment.NewLine}{Environment.NewLine}#endif{Environment.NewLine}";
            File.WriteAllText(file.Path, header, new UTF8Encoding(false));
        }
        else
        {
            File.WriteAllBytes(file.Path, _packed);
        }
        StatusText = $"已保存 {_packed.Length} 字节";
    }
}
