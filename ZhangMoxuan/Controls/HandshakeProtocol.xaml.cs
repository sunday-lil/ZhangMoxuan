using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ZhangMoxuan.Controls;

/// <summary>
/// 握手协议可视化控件：模仿影片中 550W 联网时的协议握手过程。
/// 垂直展示各握手步骤，状态依次推进 PENDING → PROCESSING（琥珀闪烁）→ OK（红色常亮），
/// 最后一步以 READY 收尾。提供 RunAsync() 依次执行并触发 HandshakeCompleted 事件。
/// </summary>
public partial class HandshakeProtocol : UserControl
{
    private enum StepState { Pending, Processing, Ok, Ready }

    private class StepRow
    {
        public Ellipse Light = null!;
        public TextBlock Elapsed = null!;
        public TextBlock StatusText = null!;
        public Rectangle ProgressFill = null!;
        public double ProgressMax = 100;
        public StepState State = StepState.Pending;
    }

    private readonly List<StepRow> _rows = new();
    private readonly DispatcherTimer _blinkTimer = new() { Interval = TimeSpan.FromMilliseconds(160) };
    private readonly Random _rng = new();
    private Rectangle _overallFill = null!;
    private bool _blinkOn;

    private static readonly string[] StepNames =
    {
        "TCP 三次握手",
        "TLS 1.3 量子加密协商",
        "UEG BACKBONE 认证",
        "根服务器同步 (北京/东京/华盛顿)",
        "量子纠缠通道建立",
        "MOSS 身份验证",
        "数字生命协议加载",
    };

    /// <summary>所有步骤执行完成后触发。</summary>
    public event EventHandler? HandshakeCompleted;

    public HandshakeProtocol()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _blinkTimer.Tick += OnBlinkTick;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildSteps();
        BuildOverall();
        _blinkTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _blinkTimer.Stop();
    }

    private void BuildSteps()
    {
        StepsPanel.Children.Clear();
        _rows.Clear();

        var mono = (FontFamily)FindResource("MonoFont");
        var textBrush = (Brush)FindResource("TextBrush");
        var redBrush = (Brush)FindResource("RedBrush");
        var gridBrush = (Brush)FindResource("GridBrush");

        for (int i = 0; i < StepNames.Length; i++)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });     // 序号
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 协议名
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });     // 状态灯
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });     // 状态文字
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) });     // 耗时
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });    // 进度条

            var num = new TextBlock
            {
                Text = $"{i + 1:00}",
                FontFamily = mono, FontSize = 12, Foreground = redBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(num, 0);
            row.Children.Add(num);

            var name = new TextBlock
            {
                Text = StepNames[i],
                FontFamily = mono, FontSize = 12, Foreground = textBrush,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(name, 1);
            row.Children.Add(name);

            var light = new Ellipse
            {
                Width = 8, Height = 8,
                Fill = gridBrush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(light, 2);
            row.Children.Add(light);

            var statusText = new TextBlock
            {
                Text = "PENDING",
                FontFamily = mono, FontSize = 10, Foreground = textBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(statusText, 3);
            row.Children.Add(statusText);

            var elapsed = new TextBlock
            {
                Text = "--ms",
                FontFamily = mono, FontSize = 10, Foreground = textBrush,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(elapsed, 4);
            row.Children.Add(elapsed);

            // 每步进度条
            var barHost = new Grid { Width = 100, Height = 6, VerticalAlignment = VerticalAlignment.Center, ClipToBounds = true };
            var barBg = new Rectangle { Fill = gridBrush, Width = 100, Height = 6 };
            var barFill = new Rectangle { Fill = redBrush, Width = 0, Height = 6, HorizontalAlignment = HorizontalAlignment.Left };
            barHost.Children.Add(barBg);
            barHost.Children.Add(barFill);
            Grid.SetColumn(barHost, 5);
            row.Children.Add(barHost);

            StepsPanel.Children.Add(row);
            _rows.Add(new StepRow
            {
                Light = light,
                Elapsed = elapsed,
                StatusText = statusText,
                ProgressFill = barFill,
                ProgressMax = 100,
                State = StepState.Pending
            });
        }
    }

    private void BuildOverall()
    {
        OverallHost.Children.Clear();
        var gridBrush = (Brush)FindResource("GridBrush");
        var redBrush = (Brush)FindResource("RedBrush");
        var bg = new Rectangle { Fill = gridBrush, Width = 180, Height = 6 };
        var fill = new Rectangle { Fill = redBrush, Width = 0, Height = 6, HorizontalAlignment = HorizontalAlignment.Left };
        OverallHost.Children.Add(bg);
        OverallHost.Children.Add(fill);
        _overallFill = fill;
        UpdateOverall(0);
    }

    private void UpdateOverall(double ratio)
    {
        var r = Math.Clamp(ratio, 0, 1);
        if (_overallFill != null)
            _overallFill.Width = 180 * r;
        OverallText.Text = $" {(int)(r * 100)}%";
    }

    private void OnBlinkTick(object? sender, EventArgs e)
    {
        _blinkOn = !_blinkOn;
        var amber = (Brush)FindResource("AmberBrush");
        foreach (var r in _rows)
        {
            if (r.State == StepState.Processing)
            {
                r.Light.Fill = amber;
                r.Light.Opacity = _blinkOn ? 1.0 : 0.25;
            }
        }
    }

    private void SetState(int index, StepState state, double elapsedMs = 0)
    {
        var row = _rows[index];
        row.State = state;
        var red = (Brush)FindResource("RedBrush");
        var amber = (Brush)FindResource("AmberBrush");
        var gridBrush = (Brush)FindResource("GridBrush");
        var textBrush = (Brush)FindResource("TextBrush");

        switch (state)
        {
            case StepState.Pending:
                row.Light.Fill = gridBrush;
                row.Light.Opacity = 1;
                row.StatusText.Text = "PENDING";
                row.StatusText.Foreground = textBrush;
                row.Elapsed.Text = "--ms";
                row.ProgressFill.BeginAnimation(WidthProperty, null);
                row.ProgressFill.Width = 0;
                break;
            case StepState.Processing:
                row.Light.Fill = amber;
                row.Light.Opacity = 1;
                row.StatusText.Text = "PROC...";
                row.StatusText.Foreground = amber;
                row.Elapsed.Text = "--ms";
                break;
            case StepState.Ok:
                row.Light.Fill = red;
                row.Light.Opacity = 1;
                row.StatusText.Text = "OK";
                row.StatusText.Foreground = red;
                row.Elapsed.Text = $"{(int)elapsedMs}ms";
                row.ProgressFill.BeginAnimation(WidthProperty, null);
                row.ProgressFill.Width = row.ProgressMax;
                break;
            case StepState.Ready:
                row.Light.Fill = amber;
                row.Light.Opacity = 1;
                row.StatusText.Text = "READY";
                row.StatusText.Foreground = amber;
                row.Elapsed.Text = $"{(int)elapsedMs}ms";
                row.ProgressFill.BeginAnimation(WidthProperty, null);
                row.ProgressFill.Width = row.ProgressMax;
                break;
        }
    }

    /// <summary>
    /// 依次执行各握手步骤，每步耗时随机 200~600ms。
    /// 完成后触发 <see cref="HandshakeCompleted"/>。
    /// </summary>
    public async Task RunAsync()
    {
        // 重置所有步骤
        for (int i = 0; i < _rows.Count; i++)
            SetState(i, StepState.Pending);
        UpdateOverall(0);
        await Task.Delay(150);

        for (int i = 0; i < _rows.Count; i++)
        {
            SetState(i, StepState.Processing);
            var delay = _rng.Next(200, 601);

            // 进度条随处理时长填充
            var anim = new DoubleAnimation(0, _rows[i].ProgressMax, TimeSpan.FromMilliseconds(delay));
            _rows[i].ProgressFill.BeginAnimation(WidthProperty, anim);

            await Task.Delay(delay);

            var final = (i == _rows.Count - 1) ? StepState.Ready : StepState.Ok;
            SetState(i, final, delay);
            UpdateOverall((i + 1.0) / _rows.Count);
            await Task.Delay(80);
        }

        HandshakeCompleted?.Invoke(this, EventArgs.Empty);
    }
}
