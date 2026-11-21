using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using ModulusStudio.App.WinUI.Views;

namespace ModulusStudio.App.WinUI;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    public MainWindow()
    {
        InitializeComponent();

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        var scale = GetDpiForWindow(hwnd) / 96.0;
        appWindow.Resize(new Windows.Graphics.SizeInt32((int)(1280 * scale), (int)(820 * scale)));

        ContentFrame.Navigate(typeof(TextModulusPage), null, new EntranceNavigationTransitionInfo());
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item) return;

        var page = (item.Tag as string) switch
        {
            "image" => typeof(ImageModulusPage),
            _ => typeof(TextModulusPage)
        };

        if (ContentFrame.CurrentSourcePageType != page)
            ContentFrame.Navigate(page, null, new EntranceNavigationTransitionInfo());
    }
}
