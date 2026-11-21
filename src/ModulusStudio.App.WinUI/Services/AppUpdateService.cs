using System.Reflection;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Xaml;

namespace ModulusStudio.App.WinUI.Services;

public sealed record UpdateInfo(Version Version, string DisplayVersion, string AssetName, string DownloadUrl);

/// <summary>
/// 基于 GitHub Releases 的更新服务：检查新版本、下载安装包并重启替换。
/// </summary>
public static class AppUpdateService
{
    // 发布到 GitHub Releases 后，把 Owner 改成你的 GitHub 用户名/组织名
    public const string GitHubOwner = "";
    public const string GitHubRepo = "ModulusStudio";

    public static bool StartupCheckDone { get; set; }

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    public static string CurrentVersionText => CurrentVersion.ToString(3);

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(GitHubOwner);

    public static string RepositoryUrl =>
        IsConfigured ? $"https://github.com/{GitHubOwner}/{GitHubRepo}" : string.Empty;

    public static async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return null;

        using var http = CreateClient();
        var json = await http.GetStringAsync(
            $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest", cancellationToken);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tag = root.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() ?? string.Empty : string.Empty;
        var versionText = tag.TrimStart('v', 'V');
        if (!Version.TryParse(versionText, out var latest) || latest <= CurrentVersion) return null;

        if (!root.TryGetProperty("assets", out var assets)) return null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;

            var url = asset.TryGetProperty("browser_download_url", out var urlElement) ? urlElement.GetString() : null;
            if (string.IsNullOrEmpty(url)) continue;

            return new UpdateInfo(latest, versionText, name, url);
        }

        return null;
    }

    public static async Task<string> DownloadAsync(
        UpdateInfo info, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var dir = Path.Combine(Path.GetTempPath(), "ModulusStudio.Update");
        Directory.CreateDirectory(dir);
        var target = Path.Combine(dir, info.AssetName);

        using var http = CreateClient();
        using var response = await http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(target);

        var buffer = new byte[81920];
        long read = 0;
        int n;
        while ((n = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, n), cancellationToken);
            read += n;
            if (total > 0) progress?.Report((double)read / total);
        }

        return target;
    }

    /// <summary>用后台脚本等待当前进程退出后替换 exe 并重新启动。</summary>
    public static void ApplyAndRestart(string newExePath)
    {
        var exePath = Environment.ProcessPath
                      ?? Process.GetCurrentProcess().MainModule?.FileName
                      ?? throw new InvalidOperationException("无法定位当前程序路径。");

        var dir = Path.Combine(Path.GetTempPath(), "ModulusStudio.Update");
        Directory.CreateDirectory(dir);
        var scriptPath = Path.Combine(dir, "apply-update.ps1");
        var oldPath = exePath + ".old";

        string Q(string value) => value.Replace("'", "''");

        var script = $$"""
            $ErrorActionPreference = 'SilentlyContinue'
            Start-Sleep -Milliseconds 800
            for ($i = 0; $i -lt 60; $i++) {
                try { Move-Item -LiteralPath '{{Q(exePath)}}' -Destination '{{Q(oldPath)}}' -Force; break } catch { Start-Sleep -Milliseconds 250 }
            }
            Copy-Item -LiteralPath '{{Q(newExePath)}}' -Destination '{{Q(exePath)}}' -Force
            Start-Process -FilePath '{{Q(exePath)}}'
            Start-Sleep -Seconds 3
            Remove-Item -LiteralPath '{{Q(oldPath)}}' -Force
            Remove-Item -LiteralPath '{{Q(newExePath)}}' -Force
            """;

        File.WriteAllText(scriptPath, script);
        Process.Start(new ProcessStartInfo("powershell.exe",
            $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Application.Current.Exit();
    }

    private static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ModulusStudio-Updater/1.0");
        return http;
    }
}
