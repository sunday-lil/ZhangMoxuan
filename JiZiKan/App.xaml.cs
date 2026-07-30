using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace JiZiKan;

/// <summary>
/// 应用入口。启动时先进入 550W 自检序列（BootWindow），自检完成后进入主控界面（MainWindow）。
/// </summary>
public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "moss.log");

    public static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); }
        catch { /* ignore */ }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("=== MOSS 启动 ===");
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        base.OnStartup(e);
        try
        {
            var boot = new BootWindow();
            boot.BootCompleted += (_, _) =>
            {
                Log("BootCompleted → 创建 MainWindow");
                Dispatcher.Invoke(() =>
                {
                    var main = new MainWindow();
                    main.Show();
                    Log("MainWindow.Show 完成");
                });
            };
            boot.Show();
            Log("BootWindow.Show 完成");
        }
        catch (Exception ex)
        {
            Log("OnStartup 异常: " + ex);
            throw;
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log("Dispatcher 异常: " + e.Exception);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log("AppDomain 异常: " + (e.ExceptionObject as Exception));
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log("Task 未观察异常: " + e.Exception);
        e.SetObserved();
    }
}
