using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ZhangMoxuan.Controls;

/// <summary>
/// 数字生命卡控件：模仿《流浪地球2》中图丫丫 / 图恒宇的数字生命卡
/// 插入 550W 后的状态面板。红色边框 + 黑色面板，左侧脉冲头像，
/// 右侧生命体征实时跳动，底部为意识上传进度。
/// 丫丫→红，恒宇→蓝。
/// </summary>
public partial class DigitalLifeCard : UserControl
{
    public static readonly DependencyProperty SubjectNameProperty =
        DependencyProperty.Register(nameof(SubjectName), typeof(string), typeof(DigitalLifeCard),
            new PropertyMetadata("图丫丫", OnSubjectNameChanged));

    public static readonly DependencyProperty SubjectIdProperty =
        DependencyProperty.Register(nameof(SubjectId), typeof(string), typeof(DigitalLifeCard),
            new PropertyMetadata("DLID-550W-0001", OnSubjectIdChanged));

    /// <summary>受试者姓名（图丫丫 / 图恒宇），setter 触发 UI 更新（头像脉冲配色）。</summary>
    public string SubjectName
    {
        get => (string)GetValue(SubjectNameProperty);
        set => SetValue(SubjectNameProperty, value);
    }

    /// <summary>数字生命 ID。</summary>
    public string SubjectId
    {
        get => (string)GetValue(SubjectIdProperty);
        set => SetValue(SubjectIdProperty, value);
    }

    private readonly DispatcherTimer _timer;
    private Storyboard? _pulseStoryboard;
    private Storyboard? _blinkStoryboard;
    private bool _uploading;
    private int _progress;

    // 生命体征基线数值，由 DispatcherTimer 每秒微动
    private int _iter;
    private double _stability = 92.5;
    private double _memory = 87.3;
    private int _lifespanSeconds = 72 * 3600 + 14 * 60 + 33;

    public DigitalLifeCard()
    {
        InitializeComponent();
        Loaded += DigitalLifeCard_Loaded;
        Unloaded += DigitalLifeCard_Unloaded;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateVitals();
    }

    private void DigitalLifeCard_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySubject(SubjectName);
        IdText.Text = string.IsNullOrEmpty(SubjectId) ? "----" : SubjectId;
        ApplyProgress(_progress);
        ApplyUploadingState(_uploading);
        UpdateVitals();

        _pulseStoryboard = (Storyboard)FindResource("AvatarPulse");
        _blinkStoryboard = (Storyboard)FindResource("AccessBlink");
        _pulseStoryboard.Begin();

        _timer.Start();
    }

    private void DigitalLifeCard_Unloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _pulseStoryboard?.Stop();
        _blinkStoryboard?.Stop();
    }

    private static void OnSubjectNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DigitalLifeCard card)
            card.ApplySubject((string)e.NewValue);
    }

    private static void OnSubjectIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DigitalLifeCard card)
            card.IdText.Text = (string)e.NewValue ?? "----";
    }

    /// <summary>根据受试者姓名切换头像配色：图丫丫→红，图恒宇→蓝。</summary>
    private void ApplySubject(string? name)
    {
        NameText.Text = string.IsNullOrEmpty(name) ? "----" : name;
        bool isYaya = name == "图丫丫";

        Color core, mid, outer, glow, ring2;
        if (isYaya)
        {
            core = Color.FromRgb(0xFF, 0x38, 0x38);
            mid = Color.FromRgb(0xFF, 0x1A, 0x1A);
            outer = Color.FromRgb(0x0A, 0x00, 0x00);
            glow = Color.FromRgb(0xFF, 0x1A, 0x1A);
            ring2 = Color.FromRgb(0xFF, 0x1A, 0x1A);
        }
        else
        {
            // 图恒宇：人类蓝
            core = Color.FromRgb(0x6E, 0xAA, 0xFF);
            mid = Color.FromRgb(0x1E, 0x6F, 0xFF);
            outer = Color.FromRgb(0x00, 0x08, 0x14);
            glow = Color.FromRgb(0x1E, 0x6F, 0xFF);
            ring2 = Color.FromRgb(0x1E, 0x6F, 0xFF);
        }

        AvatarStop0.Color = core;
        AvatarStop1.Color = Color.FromArgb(0x8A, mid.R, mid.G, mid.B);
        AvatarStop2.Color = Color.FromArgb(0x40, outer.R, outer.G, outer.B);
        AvatarGlow.Color = glow;
        AvatarRing2.Stroke = new SolidColorBrush(ring2);
    }

    /// <summary>控制上传状态：true=UPLOADING（琥珀色 + 指示灯闪烁），false=ACTIVE。</summary>
    public void SetUploading(bool uploading)
    {
        _uploading = uploading;
        if (IsLoaded) ApplyUploadingState(uploading);
    }

    private void ApplyUploadingState(bool uploading)
    {
        if (uploading)
        {
            StatusText.Text = "UPLOADING";
            StatusText.Foreground = (Brush)FindResource("AmberBrush");
            AccessLight.Fill = (Brush)FindResource("AmberBrush");
            _blinkStoryboard?.Begin();
        }
        else
        {
            StatusText.Text = "ACTIVE";
            StatusText.Foreground = (Brush)FindResource("RedBrightBrush");
            AccessLight.Fill = (Brush)FindResource("RedBrush");
            AccessLight.Opacity = 1.0;
            _blinkStoryboard?.Stop();
        }
    }

    /// <summary>设置意识上传进度（0-100）。</summary>
    public void SetProgress(int percent)
    {
        _progress = Math.Clamp(percent, 0, 100);
        if (IsLoaded) ApplyProgress(_progress);
    }

    private void ApplyProgress(int percent)
    {
        ProgressScale.ScaleX = percent / 100.0;
    }

    /// <summary>每秒微动生命体征数据。</summary>
    private void UpdateVitals()
    {
        // 迭代次数缓慢增长
        _iter += Random.Shared.Next(1, 4);
        IterText.Text = _iter.ToString("N0");

        // 意识稳定度 88.0 - 96.5% 之间微动
        _stability += (Random.Shared.NextDouble() - 0.5) * 0.8;
        _stability = Math.Clamp(_stability, 88.0, 96.5);
        StabilityText.Text = _stability.ToString("F2") + "%";

        // 记忆完整度 84.0 - 92.5% 之间微动
        _memory += (Random.Shared.NextDouble() - 0.5) * 0.6;
        _memory = Math.Clamp(_memory, 84.0, 92.5);
        MemoryText.Text = _memory.ToString("F2") + "%";

        // 寿命剩余倒计时（小时:分:秒）
        if (_lifespanSeconds > 0) _lifespanSeconds--;
        var ts = TimeSpan.FromSeconds(_lifespanSeconds);
        LifespanText.Text = $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
    }
}
