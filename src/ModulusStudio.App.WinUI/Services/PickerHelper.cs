using WinRT.Interop;

namespace ModulusStudio.App.WinUI.Services;

/// <summary>桌面应用中为文件选择器绑定当前窗口句柄。</summary>
public static class PickerHelper
{
    public static void Initialize(object picker)
    {
        var window = App.MainWindowInstance;
        if (window is null) return;
        var hwnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, hwnd);
    }
}
