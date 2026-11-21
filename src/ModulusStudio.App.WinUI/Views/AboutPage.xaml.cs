using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ModulusStudio.App.WinUI.Services;

namespace ModulusStudio.App.WinUI.Views;

public sealed partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();

        VersionText.Text = $"版本 {AppUpdateService.CurrentVersionText}";
        VersionRowText.Text = AppUpdateService.CurrentVersionText;
        RepoLink.Visibility = AppUpdateService.IsConfigured ? Visibility.Visible : Visibility.Collapsed;
        SourceText.Text = AppUpdateService.IsConfigured
            ? AppUpdateService.RepositoryUrl
            : "更新源未配置：发布到 GitHub 后填写 AppUpdateService 中的 GitHubOwner。";
        UpdateStatusText.Text = AppUpdateService.IsConfigured
            ? "点击“检查更新”获取最新版本。"
            : "暂未连接到更新源。";
        CheckUpdateButton.IsEnabled = AppUpdateService.IsConfigured;
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateStatusText.Text = "正在检查更新…";

        try
        {
            var info = await AppUpdateService.CheckAsync();
            if (info is null)
            {
                UpdateStatusText.Text = $"当前已是最新版本（v{AppUpdateService.CurrentVersionText}）。";
                return;
            }

            var confirm = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "发现新版本",
                Content = $"新版本 v{info.DisplayVersion} 可用，是否现在下载？",
                PrimaryButtonText = "下载",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await confirm.ShowAsync() != ContentDialogResult.Primary)
            {
                UpdateStatusText.Text = "已取消更新。";
                return;
            }

            var progress = new Progress<double>(p => UpdateStatusText.Text = $"正在下载… {p:P0}");
            var path = await AppUpdateService.DownloadAsync(info, progress);
            UpdateStatusText.Text = "下载完成。";

            var restart = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "更新已就绪",
                Content = $"新版本 v{info.DisplayVersion} 已下载完成，是否立即重启并更新？",
                PrimaryButtonText = "立即重启更新",
                CloseButtonText = "稍后",
                DefaultButton = ContentDialogButton.Primary
            };

            if (await restart.ShowAsync() == ContentDialogResult.Primary)
                AppUpdateService.ApplyAndRestart(path);
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"检查更新失败：{ex.Message}";
        }
        finally
        {
            CheckUpdateButton.IsEnabled = AppUpdateService.IsConfigured;
        }
    }

    private async void RepoLink_Click(object sender, RoutedEventArgs e)
    {
        if (!AppUpdateService.IsConfigured) return;
        await Windows.System.Launcher.LaunchUriAsync(new Uri(AppUpdateService.RepositoryUrl));
    }
}
