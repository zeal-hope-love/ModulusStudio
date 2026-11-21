using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ModulusStudio.App;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log(e.ExceptionObject as Exception, "AppDomain");
        TaskScheduler.UnobservedTaskException += (_, e) => Log(e.Exception, "TaskScheduler");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log(e.Exception, "Dispatcher");
    }

    private static void Log(Exception? exception, string source)
    {
        if (exception is null) return;
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ModulusStudio");
            Directory.CreateDirectory(dir);
            var content = new StringBuilder()
                .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}")
                .AppendLine(exception.ToString())
                .AppendLine()
                .ToString();
            File.AppendAllText(Path.Combine(dir, "crash.log"), content);
        }
        catch
        {
            // 日志失败不应影响程序退出流程
        }
    }
}
