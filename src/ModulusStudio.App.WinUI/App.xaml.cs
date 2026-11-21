using Microsoft.UI.Xaml;

namespace ModulusStudio.App.WinUI;

public partial class App : Application
{
    public static Window? MainWindowInstance { get; private set; }

    public App()
    {
        RequestedTheme = ApplicationTheme.Light;
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindowInstance = new MainWindow();
        MainWindowInstance.Activate();
    }
}
