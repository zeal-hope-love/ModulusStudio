using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using ModulusStudio.App.WinUI.Services;
using ModulusStudio.App.WinUI.Views;
using Windows.UI;

namespace ModulusStudio.App.WinUI;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    public MainWindow()
    {
        InitializeComponent();

        // 使用 Windows 11 原生 Mica 材质透出桌面背景，旧系统回退到亚克力
        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
        else if (DesktopAcrylicController.IsSupported())
            SystemBackdrop = new DesktopAcrylicBackdrop();

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        var scale = GetDpiForWindow(hwnd) / 96.0;
        appWindow.Resize(new Windows.Graphics.SizeInt32((int)(1280 * scale), (int)(820 * scale)));

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            // 自绘标题栏：更大的图标与标题文字，背景透出 Mica
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            var titleBar = appWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x18, 0, 0, 0);
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(0x28, 0, 0, 0);
            titleBar.ButtonForegroundColor = Color.FromArgb(255, 0x1A, 0x1A, 0x1A);
            titleBar.ButtonInactiveForegroundColor = Color.FromArgb(255, 0x8A, 0x8A, 0x8A);
        }
        else
        {
            // 旧系统不支持自定义标题栏时，隐藏自绘栏，使用系统标题栏
            AppTitleBar.Visibility = Visibility.Collapsed;
            TitleBarRow.Height = new GridLength(0);
        }

        RootGrid.Loaded += MainWindow_Loaded;
        ContentFrame.Navigate(typeof(TextModulusPage), null, new EntranceNavigationTransitionInfo());
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        var page = (item.Tag as string) switch
        {
            "image" => typeof(ImageModulusPage),
            "about" => typeof(AboutPage),
            _ => typeof(TextModulusPage)
        };

        if (ContentFrame.CurrentSourcePageType != page)
            ContentFrame.Navigate(page, null, new EntranceNavigationTransitionInfo());
    }

    /// <summary>启动后静默检查更新，下载完成后提示重启。</summary>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (AppUpdateService.StartupCheckDone) return;
        AppUpdateService.StartupCheckDone = true;

        try
        {
            await Task.Delay(2000);
            var info = await AppUpdateService.CheckAsync();
            if (info is null) return;

            var path = await AppUpdateService.DownloadAsync(info);

            var dialog = new ContentDialog
            {
                XamlRoot = RootGrid.XamlRoot,
                Title = "发现新版本",
                Content = $"新版本 v{info.DisplayVersion} 已下载完成，是否立即重启并更新？",
                PrimaryButtonText = "立即重启更新",
                CloseButtonText = "稍后",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                AppUpdateService.ApplyAndRestart(path);
        }
        catch
        {
            // 自动检查失败时保持静默，不打扰使用
        }
    }
}
