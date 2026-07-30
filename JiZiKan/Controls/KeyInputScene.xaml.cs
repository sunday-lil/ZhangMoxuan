using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace JiZiKan.Controls;

/// <summary>
/// 图恒宇水下北京根服务器密钥输入场景。
/// 还原《流浪地球2》550W/MOSS 经典桥段：插入数字生命卡，输入 8 位密钥启动根服务器。
/// 包含倒计时、数字键盘、密钥显示、状态机、滚动日志与 KeyAccepted/Timeout 事件。
/// </summary>
public partial class KeyInputScene : UserControl
{
    private const string CorrectKey = "31415926";   // π 前 8 位，致敬电影中的数字
    private const int TotalSeconds = 180;            // 03:00
    private const double TickIntervalMs = 100;       // 1 实际秒 = 10 场景秒

    private readonly DispatcherTimer _timer;
    private readonly StringBuilder _input = new();
    private readonly List<TextBlock> _slots = new();
    private readonly Storyboard _countdownPulse;
    private int _remainingSeconds = TotalSeconds;
    private bool _finished;

    /// <summary>密钥输入成功（根服务器 ONLINE）时触发。</summary>
    public event EventHandler? KeyAccepted;

    /// <summary>倒计时归零仍未完成时触发。</summary>
    public event EventHandler? Timeout;

    public KeyInputScene()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TickIntervalMs) };
        _timer.Tick += OnTick;

        _countdownPulse = BuildPulse();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildKeySlots();
        UpdateCountdown();
        UpdateSlots();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _countdownPulse.Stop();
    }

    // ---------------- 公共 API ----------------

    /// <summary>启动场景：开始倒计时并进入认证状态。</summary>
    public void Start()
    {
        if (_timer.IsEnabled) return;
        _finished = false;
        _remainingSeconds = TotalSeconds;
        _input.Clear();
        UpdateCountdown();
        UpdateSlots();
        SetServerStatus("AUTHENTICATING", (Brush)FindResource("AmberBrush"));
        StatusDot.Fill = (Brush)FindResource("AmberBrush");
        _timer.Start();
        _countdownPulse.Begin();
        Log("ROOT SERVER BOOT SEQUENCE INITIATED");
        Log("DIGITAL LIFE CARD // TU_HENGYU + TU_YAYA INSERTED");
        Log("550W QUANTUM INTERFACE ACTIVE");
        Log("AWAITING AUTHORIZATION KEY [ 8 DIGITS ]...");
    }

    /// <summary>自动演示：依次输入正确密钥序列。</summary>
    public async void AutoSolve()
    {
        Start();
        foreach (var ch in CorrectKey)
        {
            if (_finished) break;
            if (FindKeyButton(ch.ToString()) is { } btn)
            {
                FlashButton(btn);
                EnterDigit(ch);
            }
            await Task.Delay(360);
        }
    }

    // ---------------- 倒计时 ----------------

    private void OnTick(object? sender, EventArgs e)
    {
        if (_finished) { _timer.Stop(); return; }
        _remainingSeconds--;
        if (_remainingSeconds <= 0)
        {
            _remainingSeconds = 0;
            UpdateCountdown();
            _timer.Stop();
            OnTimeout();
            return;
        }
        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        CountdownText.Text = $"{_remainingSeconds / 60:D2}:{_remainingSeconds % 60:D2}";
        if (_remainingSeconds <= 30 && !_finished)
        {
            CountdownText.Foreground = (Brush)FindResource("RedBrightBrush");
            CountdownGlow.Color = Color.FromRgb(0xFF, 0x38, 0x38);
            CountdownGlow.BlurRadius = 36;
        }
    }

    private void OnTimeout()
    {
        _finished = true;
        _countdownPulse.Stop();
        SetServerStatus("OFFLINE", (Brush)FindResource("RedDimBrush"));
        StatusDot.Fill = (Brush)FindResource("RedDimBrush");
        Log("!! TIMEOUT // ROOT SERVER OFFLINE");
        Log("!! MISSION FAILED");
        Timeout?.Invoke(this, EventArgs.Empty);
    }

    private Storyboard BuildPulse()
    {
        var sb = new Storyboard();
        var anim = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(1),
            RepeatBehavior = RepeatBehavior.Forever
        };
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(0.35, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
        anim.KeyFrames.Add(new LinearDoubleKeyFrame(0.35, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1))));
        Storyboard.SetTarget(anim, CountdownGlow);
        Storyboard.SetTargetProperty(anim, new PropertyPath(DropShadowEffect.OpacityProperty));
        sb.Children.Add(anim);
        return sb;
    }

    // ---------------- 密钥显示槽 ----------------

    private void BuildKeySlots()
    {
        var dim = (Brush)FindResource("RedDimBrush");
        var mono = (FontFamily)FindResource("MonoFont");
        for (int i = 0; i < 8; i++)
        {
            var tb = new TextBlock
            {
                Text = "·",
                FontFamily = mono,
                FontSize = 30,
                FontWeight = FontWeights.Bold,
                Foreground = dim,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var border = new Border
            {
                Width = 46,
                Height = 58,
                Background = new SolidColorBrush(Color.FromRgb(0x05, 0x06, 0x08)),
                BorderBrush = dim,
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(3),
                Child = tb
            };
            KeySlotsPanel.Children.Add(border);
            _slots.Add(tb);
        }
    }

    private void UpdateSlots()
    {
        var dim = (Brush)FindResource("RedDimBrush");
        var bright = (Brush)FindResource("RedBrightBrush");
        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < _input.Length)
            {
                _slots[i].Text = _input[i].ToString();
                _slots[i].Foreground = bright;
            }
            else
            {
                _slots[i].Text = "·";
                _slots[i].Foreground = dim;
            }
        }
    }

    // ---------------- 键盘输入 ----------------

    private void KeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_finished) return;
        if (sender is Button btn && btn.Tag is string tag)
        {
            FlashButton(btn);
            switch (tag)
            {
                case "CLR":
                    _input.Clear();
                    UpdateSlots();
                    Log("INPUT CLEARED");
                    break;
                case "ENT":
                    TryAuthenticate();
                    break;
                default:
                    if (tag.Length == 1 && char.IsDigit(tag[0]))
                        EnterDigit(tag[0]);
                    break;
            }
        }
    }

    private void EnterDigit(char digit)
    {
        if (_input.Length >= 8) return;
        int pos = _input.Length;
        char expected = CorrectKey[pos];
        if (digit == expected)
        {
            _input.Append(digit);
            UpdateSlots();
            Log($"KEY ENTRY [{pos + 1}/8] ... OK");
            if (_input.Length == 8)
                TryAuthenticate();
        }
        else
        {
            Log($"KEY ENTRY [{pos + 1}/8] !! MISMATCH");
            TriggerFail();
            _input.Clear();
            UpdateSlots();
        }
    }

    private void TryAuthenticate()
    {
        if (_input.Length < 8)
        {
            Log($"AUTH ABORTED // KEY INCOMPLETE ({_input.Length}/8)");
            return;
        }
        if (_input.ToString() == CorrectKey)
            OnSuccess();
        else
        {
            TriggerFail();
            _input.Clear();
            UpdateSlots();
        }
    }

    private void OnSuccess()
    {
        _finished = true;
        _timer.Stop();
        _countdownPulse.Stop();
        CountdownText.Foreground = (Brush)FindResource("BlueBrush");
        CountdownGlow.Color = Color.FromRgb(0x1E, 0x6F, 0xFF);
        SetServerStatus("ONLINE", (Brush)FindResource("BlueBrush"));
        StatusDot.Fill = (Brush)FindResource("BlueBrush");
        CardStateText.Text = "NEURAL UPLINK";
        CardStateText.Foreground = (Brush)FindResource("BlueBrush");
        CardDot.Fill = (Brush)FindResource("BlueBrush");
        Log(">> KEY ACCEPTED");
        Log(">> HANDSHAKE INITIATED // 550W <-> ROOT-01");
        Log(">> CRYPTO VERIFY ....... PASS");
        Log(">> QUANTUM CHANNEL ...... STABLE");
        Log(">> ROOT SERVER 01 // ONLINE");
        Log(">> PLAN-B COORDINATES UNLOCKED");
        KeyAccepted?.Invoke(this, EventArgs.Empty);
    }

    private void TriggerFail()
    {
        Log("!! AUTH FAILED // RETRY");
        FailText.Visibility = Visibility.Visible;
        var sb = new Storyboard();
        var anim = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(1500) };
        for (int i = 0; i <= 5; i++)
            anim.KeyFrames.Add(new DiscreteDoubleKeyFrame(
                i % 2 == 0 ? 1.0 : 0.12,
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 300))));
        Storyboard.SetTarget(anim, FailText);
        Storyboard.SetTargetProperty(anim, new PropertyPath(UIElement.OpacityProperty));
        sb.Children.Add(anim);
        sb.Completed += (_, _) => FailText.Visibility = Visibility.Collapsed;
        sb.Begin();
    }

    private void FlashButton(Button btn)
    {
        btn.RenderTransform = new ScaleTransform();
        btn.RenderTransformOrigin = new Point(0.5, 0.5);
        var sb = new Storyboard();
        var sx = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(220) };
        sx.KeyFrames.Add(new LinearDoubleKeyFrame(0.82, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(45))));
        sx.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))));
        var sy = sx.Clone();
        Storyboard.SetTarget(sx, btn);
        Storyboard.SetTargetProperty(sx, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
        Storyboard.SetTarget(sy, btn);
        Storyboard.SetTargetProperty(sy, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
        sb.Children.Add(sx);
        sb.Children.Add(sy);
        sb.Begin();
    }

    private Button? FindKeyButton(string tag)
    {
        foreach (var child in KeypadGrid.Children.OfType<Button>())
            if (child.Tag is string t && t == tag) return child;
        return null;
    }

    // ---------------- 辅助 ----------------

    private void SetServerStatus(string text, Brush color)
    {
        ServerStatusText.Text = text;
        ServerStatusText.Foreground = color;
    }

    private void Log(string message)
    {
        LogBox.AppendText($"[T-{_remainingSeconds:D3}] {message}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }
}
