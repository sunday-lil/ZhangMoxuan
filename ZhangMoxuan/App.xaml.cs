using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ZhangMoxuan;

/// <summary>
/// 应用入口。启动时先进入 550W 自检序列（BootWindow），自检完成后进入主控界面（MainWindow）。
/// 支持两个开发/文档用参数：
/// <list type="bullet">
/// <item><c>--skip-boot</c>：跳过开机自检，直接进入主控台。</item>
/// <item><c>--capture [目录]</c>：离屏渲染全部页面为 PNG，供 README 与视觉回归使用。</item>
/// </list>
/// </summary>
public partial class App : Application
{
    private const long MaxLogBytes = 512 * 1024;

    private static readonly object LogGate = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZhangMoxuan");
    private static readonly string LogPath = Path.Combine(LogDirectory, "moss.log");

    private static string? _captureDirectory;
    private bool _mainWindowOpened;

    /// <summary>是否通过 <c>--skip-boot</c> 跳过自检。</summary>
    public static bool SkipBoot { get; private set; }

    /// <summary>是否处于离屏截图模式。</summary>
    public static bool CaptureMode { get; private set; }

    /// <summary>写入 <c>%LOCALAPPDATA%\ZhangMoxuan\moss.log</c>，超过 512 KB 自动轮转。</summary>
    public static void Log(string message)
    {
        try
        {
            lock (LogGate)
            {
                Directory.CreateDirectory(LogDirectory);
                RotateLog();
                File.AppendAllText(LogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // 日志失败不应影响主流程。
        }
    }

    private static void RotateLog()
    {
        var file = new FileInfo(LogPath);
        if (!file.Exists || file.Length < MaxLogBytes) return;

        var backup = Path.Combine(LogDirectory, "moss.1.log");
        try
        {
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(LogPath, backup);
        }
        catch
        {
            // 忽略轮转失败，继续向原文件追加。
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log($"=== MOSS 启动 v{typeof(App).Assembly.GetName().Version} ===");
        Log($"{RuntimeInformation.FrameworkDescription} · {RuntimeInformation.OSDescription}");

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        base.OnStartup(e);
        ParseArguments(e.Args);

        if (CaptureMode)
        {
            // 截图模式需要先展示/关闭自检窗口，不能被「最后一个窗口关闭」提前终止。
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _ = RunCaptureAsync();
            return;
        }

        if (SkipBoot)
        {
            Log("--skip-boot：跳过自检序列");
            OpenMainWindow();
            return;
        }

        try
        {
            var boot = new BootWindow();
            boot.BootCompleted += (_, _) => OpenMainWindow();
            boot.Show();
            Log("BootWindow.Show 完成");
        }
        catch (Exception ex)
        {
            Log("OnStartup 异常: " + ex);
            throw;
        }
    }

    private static void ParseArguments(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--skip-boot":
                    SkipBoot = true;
                    break;
                case "--capture":
                    CaptureMode = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                        _captureDirectory = Path.GetFullPath(args[++i]);
                    break;
            }
        }
    }

    private void OpenMainWindow()
    {
        if (_mainWindowOpened) return;
        _mainWindowOpened = true;

        Dispatcher.Invoke(() =>
        {
            var main = new MainWindow();
            MainWindow = main;
            main.Show();
            Log("MainWindow.Show 完成");
        });
    }

    /// <summary>离屏渲染各功能页，输出到指定目录。用于生成 README 截图与冒烟验证。</summary>
    private async Task RunCaptureAsync()
    {
        var directory = _captureDirectory ?? Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(directory);

        MainWindow? window = null;
        BootWindow? boot = null;
        var failed = false;
        try
        {
            // 先抓一张开机自检阶段的画面，再进入主控台。
            boot = new BootWindow
            {
                WindowState = WindowState.Normal,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -8000,
                Top = -8000,
                Width = 1600,
                Height = 1000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };
            boot.Show();
            await Task.Delay(1800);
            var bootFile = Path.Combine(directory, "boot.png");
            Capture(boot, bootFile);
            Log($"截图: boot -> {bootFile}");
            boot.Close();
            boot = null;
            await Task.Delay(200);

            window = new MainWindow
            {
                WindowState = WindowState.Normal,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -8000,
                Top = -8000,
                Width = 1600,
                Height = 1000,
                ShowInTaskbar = false,
                ShowActivated = false,
            };

            MainWindow = window;
            window.Show();
            if (!window.IsLoaded)
            {
                var loaded = new TaskCompletionSource();
                window.Loaded += (_, _) => loaded.TrySetResult();
                await loaded.Task;
            }

            await Task.Delay(800);

            foreach (var page in ZhangMoxuan.MainWindow.Pages)
            {
                window.ShowPage(page);
                if (page == "dialog") window.RunDialogDemo();
                // 等待控件完成布局与首轮动画，网络/危机页需要更久。
                await Task.Delay(page is "network" or "crisis" or "dialog" ? 2200 : 1200);

                var file = Path.Combine(directory, $"{page}.png");
                Capture(window, file);
                Log($"截图: {page} -> {file}");
            }
        }
        catch (Exception ex)
        {
            failed = true;
            Log("截图模式失败: " + ex);
        }
        finally
        {
            boot?.Close();
            window?.Close();
            Shutdown(failed ? 1 : 0);
        }
    }

    private static void Capture(Window window, string path)
    {
        window.UpdateLayout();

        var width = (int)Math.Ceiling(window.ActualWidth);
        var height = (int)Math.Ceiling(window.ActualHeight);
        if (width <= 0 || height <= 0) return;

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log($"=== MOSS 退出 (code {e.ApplicationExitCode}) ===");
        base.OnExit(e);
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
