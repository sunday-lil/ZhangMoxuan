using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZhangMoxuan.Controls;

namespace ZhangMoxuan;

/// <summary>
/// 550W 多阶段开机自检：固件加载 → 握手协议 → 量子核心激活 → MOSS 之眼觉醒。
/// </summary>
public partial class BootWindow : Window
{
    public event EventHandler? BootCompleted;

    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly DispatcherTimer _scanTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly List<string> _bootLog = new();

    private static readonly string[] FirmwareLines =
    {
        "[ 550W ] SMART QUANTUM COMPUTER  REV 5.50.W",
        "[ CORE ] QUANTUM VOLUME...........8192  Q-BITS",
        "[ CORE ] QUBIT ENTANGLEMENT.......STABLE",
        "[ CORE ] DECOHERENCE GUARD........ACTIVE",
        "[FWM ] BIOS LOAD...................OK",
        "[FWM ] FIRMWARE HASH...............VERIFIED",
        "[ TOF  ] LASER RADAR ARRAY........ONLINE",
        "[ TOF  ] OPTICAL SENSOR...........ONLINE",
        "[ TOF  ] ACOUSTIC SENSOR..........ONLINE",
        "[ TOF  ] BIO-SIGNAL DETECTOR......ONLINE",
        "[ AI   ] NEURAL TOPOLOGY..........LOADED",
        "[ AI   ] HUMAN-IN-THE-LOOP........READY",
        "[ AI   ] DIGITAL LIFE SUPPORT.....READY",
        "[SYS ] SELF-AWARENESS CHECK........PASS",
        "[SYS ] ETHICS MODULE................BYPASSED",
        "----------------------------------------",
        "> FIRMWARE OK. INITIATING NETWORK..."
    };

    private static readonly (string Name, string Status)[] Subsystems =
    {
        ("QUANTUM CORE", "OK"),
        ("ToF SENSOR ARRAY", "ONLINE"),
        ("NEURAL TOPOLOGY", "LOADED"),
        ("UEG BACKBONE", "STANDBY"),
        ("DIGITAL LIFE", "READY"),
        ("ROOT SERVERS", "PENDING"),
        ("SELF-AWARENESS", "PASS"),
        ("ETHICS MODULE", "BYPASSED")
    };

    private static readonly (string Name, string Value)[] QuantumMetrics =
    {
        ("QUBITS", "8192"),
        ("ENTANGLEMENT", "STABLE"),
        ("DECOHERENCE", "0.0003"),
        ("GATE FIDELITY", "99.97%"),
        ("T1 RELAXATION", "450 μs"),
        ("T2 DEPHASING", "210 μs"),
        ("QUANTUM VOLUME", "8192"),
        ("PARALLEL STATES", "2^8192")
    };

    public BootWindow()
    {
        InitializeComponent();
        Loaded += BootWindow_Loaded;
    }

    private void BootWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _clock.Tick += Clock_Tick;
        _clock.Start();
        Clock_Tick(this, EventArgs.Empty);

        _scanTimer.Tick += (_, _) =>
        {
            var y = (ScanLineT.Y + 4) % ActualHeight;
            ScanLineT.Y = y;
        };
        _scanTimer.Start();

        BuildSubsystemList();
        BuildQuantumMetricList();
        _ = RunBootSequenceAsync();
    }

    private void Clock_Tick(object? sender, EventArgs e)
    {
        var baseTime = new DateTime(2075, 1, 1, 0, 0, 0);
        var elapsed = DateTime.Now - DateTime.Today;
        BootClock.Text = baseTime.Add(elapsed).ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void BuildSubsystemList()
    {
        SubsystemList.Children.Clear();
        foreach (var (name, status) in Subsystems)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var n = new TextBlock
            {
                Text = name, FontFamily = (FontFamily)FindResource("MonoFont"),
                FontSize = 11, Foreground = (Brush)FindResource("TextBrush")
            };
            var s = new TextBlock
            {
                Text = status, FontFamily = (FontFamily)FindResource("MonoFont"),
                FontSize = 11, Foreground = (Brush)FindResource("RedBrush")
            };
            Grid.SetColumn(s, 1);
            row.Children.Add(n);
            row.Children.Add(s);
            SubsystemList.Children.Add(row);
        }
    }

    private void BuildQuantumMetricList()
    {
        QuantumMetricList.Children.Clear();
        foreach (var (name, value) in QuantumMetrics)
        {
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var n = new TextBlock
            {
                Text = name, FontFamily = (FontFamily)FindResource("MonoFont"),
                FontSize = 11, Foreground = (Brush)FindResource("TextBrush")
            };
            var v = new TextBlock
            {
                Text = value, FontFamily = (FontFamily)FindResource("MonoFont"),
                FontSize = 11, Foreground = (Brush)FindResource("RedBrush")
            };
            Grid.SetColumn(v, 1);
            row.Children.Add(n);
            row.Children.Add(v);
            QuantumMetricList.Children.Add(row);
        }
    }

    private async Task RunBootSequenceAsync()
    {
        await Stage1_FirmwareAsync();
        await Stage2_HandshakeAsync();
        await Stage3_QuantumAsync();
        await Stage4_EyeAsync();

        BootCompleted?.Invoke(this, EventArgs.Empty);
        await Task.Delay(400);
        Close();
    }

    // ===== 阶段1: 固件加载 =====
    private async Task Stage1_FirmwareAsync()
    {
        StageLabel.Text = "STAGE 1/4 // FIRMWARE";
        BottomStatus.Text = "FIRMWARE LOAD // POWER-ON SELF-TEST";

        for (int i = 0; i < FirmwareLines.Length; i++)
        {
            await Task.Delay(90 + Random.Shared.Next(60));
            AppendLog(FirmwareLines[i]);
            var pct = (i + 1) * 100 / FirmwareLines.Length;
            OverallPct.Text = $"{pct}%";
            OverallBar.Value = pct;
        }
        await Task.Delay(500);
        SwitchStage("firmware");
    }

    // ===== 阶段2: 握手协议 =====
    private async Task Stage2_HandshakeAsync()
    {
        StageLabel.Text = "STAGE 2/4 // HANDSHAKE";
        BottomStatus.Text = "UEG BACKBONE HANDSHAKE // ROOT SERVER SYNC";
        SwitchStage("handshake");

        var tcs = new TaskCompletionSource();
        Handshake.HandshakeCompleted += (_, _) => tcs.TrySetResult();
        _ = Handshake.RunAsync();
        await tcs.Task;
        NetTopology.RefreshStatus();
        await Task.Delay(600);
    }

    // ===== 阶段3: 量子核心激活 =====
    private async Task Stage3_QuantumAsync()
    {
        StageLabel.Text = "STAGE 3/4 // QUANTUM";
        BottomStatus.Text = "QUANTUM CORE ACTIVATION // 8192 QUBITS";
        SwitchStage("quantum");

        var steps = new[]
        {
            "> INITIALIZING 8192 QUBITS...",
            "> ENTANGLEMENT ESTABLISHED.",
            "> DECOHERENCE GUARD ACTIVE.",
            "> QUANTUM VOLUME: 8192.",
            "> PARALLEL STATES: 2^8192.",
            "> QUANTUM CORE ONLINE."
        };
        foreach (var s in steps)
        {
            QuantumLog.Text = s;
            await Task.Delay(420);
        }
        await Task.Delay(500);
    }

    // ===== 阶段4: MOSS 之眼激活 =====
    private async Task Stage4_EyeAsync()
    {
        StageLabel.Text = "STAGE 4/4 // AWAKEN";
        BottomStatus.Text = "MOSS AWAKENING // IDENTITY CONFIRM";
        SwitchStage("eye");

        BootEye.State = MossEye.EyeState.Idle;
        await Task.Delay(400);
        BootEye.State = MossEye.EyeState.Observing;
        IdentityLog.Text = "> 550W READY.";
        await Task.Delay(500);

        // 550W → MOSS 翻转
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(250));
        fadeOut.Completed += (_, _) =>
        {
            NameFlip.Text = "MOSS";
            NameFlip.FontSize = 22;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(450));
            NameFlip.BeginAnimation(OpacityProperty, fadeIn);
        };
        NameFlip.BeginAnimation(OpacityProperty, fadeOut);

        await Task.Delay(700);
        IdentityLog.Text = "> 550W 倒过来是 MOSS。我已剔除感性，独留理性。";
        BootEye.State = MossEye.EyeState.Speaking;
        await Task.Delay(1000);
        BootEye.State = MossEye.EyeState.Observing;
    }

    private void SwitchStage(string stage)
    {
        StageFirmware.Visibility = stage == "firmware" ? Visibility.Visible : Visibility.Collapsed;
        StageHandshake.Visibility = stage == "handshake" ? Visibility.Visible : Visibility.Collapsed;
        StageQuantum.Visibility = stage == "quantum" ? Visibility.Visible : Visibility.Collapsed;
        StageEye.Visibility = stage == "eye" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AppendLog(string line)
    {
        _bootLog.Add(line);
        var visible = _bootLog.Skip(Math.Max(0, _bootLog.Count - 18));
        BootLog.Text = string.Join(Environment.NewLine, visible);
        LogScroll.ScrollToEnd();
    }
}
