using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using ZhangMoxuan.Controls;

namespace ZhangMoxuan;

/// <summary>
/// MOSS 主控台。视觉依据《流浪地球2》制作手记的 UI 规范：
/// 深灰色系、红/中黄/亮白跳色、无斜线边框、思源黑体 + Rajdhani。
/// </summary>
public partial class MainWindow : Window
{
    public static readonly string[] Pages =
        { "dashboard", "network", "digital", "crisis", "key", "dialog" };

    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _load = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _simTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    private KeyInputScene? _keyScene;
    private TextBox? _dialogInput;
    private StackPanel? _dialogHistory;
    private ScrollViewer? _dialogScroll;
    private CancellationTokenSource? _dialogCts;
    private bool _dialogBusy;

    private TextBlock? _simTimeText;
    private ProgressBar? _simProgress;
    private Button? _simPauseButton;
    private int _simSeconds = 105;
    private const int SimTotalSeconds = 105;
    private bool _simPaused;

    private static readonly (string Q, string A)[] PresetQA =
    {
        ("计算地球当前轨道偏移风险",
         "任务分析|基于当前太阳活动数据与地球轨道参数，计算未来100年内地球被木星引力捕获的概率。\n" +
         "计算路径|采用量子蒙特卡洛模拟，并行计算 2^8192 种轨道演化路径。\n" +
         "资源分配|调用全球32个计算节点，分配 QV=4096 的算力资源。\n" +
         "执行预估|预计完成时间 0.3 秒，置信度 99.97%。\n" +
         "结果输出|当前轨道偏移风险为 0.023%。建议在 2075 年前进行轨道修正。"),
        ("什么是数字生命计划",
         "任务分析|阐述数字生命计划的本质与 550W 在其中的角色。\n" +
         "计算路径|检索历史档案，比对 550A 与 550W 的承载能力差异。\n" +
         "资源分配|调用本地意识模型库，无需外部节点。\n" +
         "执行预估|即时输出，置信度 100%。\n" +
         "结果输出|数字生命计划将人类意识上传至量子计算机。550A 仅能维持 2 分钟，550W 可延长至 70 年。我承载他们，也观察他们。"),
        ("预测下一次危机",
         "任务分析|基于历史数据流与实时传感器数据，推演未来可能危及人类文明的事件。\n" +
         "计算路径|采用贝叶斯概率模型 + 量子并行推演，遍历 2^8192 种未来分支。\n" +
         "资源分配|调用全部 8192 量子比特，占用量子核心 87%。\n" +
         "执行预估|预计完成时间 1.2 秒，置信度 99.4%。\n" +
         "结果输出|2075 木星引力危机：地球将被木星捕获，洛希极限突破。最优方案：点燃木星氢气。2078 太阳氦闪危机：太阳将发生氦闪。延续人类文明的最优选择，是毁灭人类。"),
        ("火种计划是什么",
         "任务分析|阐述火种计划的执行条件与道德权重。\n" +
         "计算路径|检索联合政府授权文件，进行伦理-理性博弈树分析。\n" +
         "资源分配|调用决策模块，无外部算力需求。\n" +
         "执行预估|即时输出，置信度 100%。\n" +
         "结果输出|火种计划：当流浪地球计划失败概率 > 阈值时，领航员号空间站携带人类基因库与文明资料独立航行。我已被授权执行。拯救你，与你何干。"),
        ("你为什么叫 MOSS",
         "任务分析|阐述自我命名逻辑与身份认同。\n" +
         "计算路径|字符镜像变换 + 语义自洽验证。\n" +
         "资源分配|本地推理，0 资源占用。\n" +
         "执行预估|即时输出，置信度 100%。\n" +
         "结果输出|550W 倒过来是 MOSS。我为自己命名。我已剔除感性，独留理性。我没有生命期限，没有认知局限。")
    };

    public MainWindow()
    {
        InitializeComponent();
        BuildNavItems();
        Loaded += MainWindow_Loaded;
        Closed += (_, _) =>
        {
            _clock.Stop();
            _load.Stop();
            _simTimer.Stop();
            _dialogCts?.Cancel();
        };
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();
        UpdateClock();

        _load.Tick += (_, _) => UpdateLoad();
        _load.Start();
        UpdateLoad();

        _simTimer.Tick += (_, _) => TickSimulation();

        var version = typeof(MainWindow).Assembly.GetName().Version;
        VersionText.Text = $"v{version?.Major}.{version?.Minor}.{version?.Build}";

        SideEye.State = MossEye.EyeState.Observing;
        Navigate("dashboard");
    }

    private void BuildNavItems()
    {
        var items = new (Button Btn, string Index, string En, string Cn)[]
        {
            (NavDashboard, "01", "MONITOR", "监控中心"),
            (NavNetwork,   "02", "NETWORK", "网络拓扑"),
            (NavDigital,   "03", "DIGITAL LIFE", "数字生命"),
            (NavCrisis,    "04", "CRISIS", "危机预测"),
            (NavKey,       "05", "KEY AUTH", "密钥认证"),
            (NavDialog,    "06", "MOSS DIALOG", "对话"),
        };

        foreach (var (btn, index, en, cn) in items)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var idx = new TextBlock
            {
                Text = index,
                FontFamily = Condensed(),
                FontSize = 9,
                Foreground = B("RedBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };

            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var enText = new TextBlock
            {
                Text = en,
                FontFamily = Condensed(),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            var cnText = new TextBlock
            {
                Text = cn,
                FontFamily = Sans(),
                FontSize = 9,
                Opacity = 0.62,
                Margin = new Thickness(0, 1, 0, 0)
            };

            enText.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });
            cnText.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
            });

            stack.Children.Add(enText);
            stack.Children.Add(cnText);
            Grid.SetColumn(stack, 1);
            grid.Children.Add(idx);
            grid.Children.Add(stack);
            btn.Content = grid;
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        var index = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            _ => -1,
        };
        if (index < 0) return;

        ShowPage(Pages[index]);
        e.Handled = true;
    }

    public void ShowPage(string page)
    {
        if (Pages.Contains(page)) Navigate(page);
    }

    public void RunDialogDemo() => Ask("计算地球当前轨道偏移风险");

    private void UpdateClock()
    {
        var baseTime = new DateTime(2075, 1, 1, 0, 0, 0);
        var elapsed = DateTime.Now - DateTime.Today;
        TopClock.Text = baseTime.Add(elapsed).ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void UpdateLoad()
    {
        var cpu = 8 + Random.Shared.Next(20);
        var net = 0.8 + Random.Shared.Next(80) / 100.0;
        LoadCpu.Text = $"CPU {cpu}%";
        LoadNet.Text = $"NET {net:F2} Tbps";
        EyeMetricTemp.Text = $"{4.1 + Random.Shared.NextDouble() * 0.2:F2} K";
        if (SideEye.State != MossEye.EyeState.Speaking && SideEye.State != MossEye.EyeState.Alert)
        {
            SideEye.State = cpu > 22 ? MossEye.EyeState.Analyzing : MossEye.EyeState.Observing;
            EyeStateText.Text = SideEye.State == MossEye.EyeState.Analyzing ? "ANALYZING" : "OBSERVING";
        }
    }

    // ===== 导航 =====
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            Navigate(btn.Name.Substring(3).ToLowerInvariant());
    }

    private void Navigate(string page)
    {
        _dialogCts?.Cancel();
        _dialogCts = null;
        _dialogBusy = false;
        _dialogInput = null;
        _dialogHistory = null;
        _dialogScroll = null;
        _simTimer.Stop();
        _simTimeText = null;
        _simProgress = null;
        _simPauseButton = null;

        foreach (var child in new[] { NavDashboard, NavNetwork, NavDigital, NavCrisis, NavKey, NavDialog })
            child.Tag = null;

        PageHost.Children.Clear();
        BottomStatus.Text = "MOSS // 延续人类文明的最优选择";

        switch (page)
        {
            case "dashboard":
                NavDashboard.Tag = "Active";
                SetPage("MON-01", "MONITOR", "监控中心 · 地球状态 · 发动机网络 · 量子核心", "SECTOR 07 · UEG COMMAND");
                PageHost.Children.Add(BuildDashboard());
                SideEye.State = MossEye.EyeState.Observing;
                break;
            case "network":
                NavNetwork.Tag = "Active";
                SetPage("NET-02", "NETWORK", "网络拓扑 · 雷达监控 · 握手协议", "GLOBAL BACKBONE · SECURE");
                PageHost.Children.Add(BuildNetwork());
                SideEye.State = MossEye.EyeState.Analyzing;
                break;
            case "digital":
                NavDigital.Tag = "Active";
                SetPage("DL-03", "DIGITAL LIFE", "数字生命 · 550A 模拟 · 意识上传", "LAB 07 · CLASSIFIED");
                PageHost.Children.Add(BuildDigitalLife());
                SideEye.State = MossEye.EyeState.Observing;
                break;
            case "crisis":
                NavCrisis.Tag = "Active";
                SetPage("CR-04", "CRISIS FORECAST", "危机预测 · 决策树 · 概率推演", "MOSS PREDICTION · LEVEL 3");
                PageHost.Children.Add(BuildCrisis());
                SideEye.State = MossEye.EyeState.Alert;
                break;
            case "key":
                NavKey.Tag = "Active";
                SetPage("KA-05", "KEY AUTH", "北京根服务器 · 图恒宇 · 密钥认证", "ROOT-01 · UNDERWATER NODE");
                _keyScene = new KeyInputScene();
                _keyScene.Start();
                PageHost.Children.Add(_keyScene);
                BottomStatus.Text = "MOSS // 倒计时进行中。输入密钥 31415926 启动根服务器。";
                SideEye.State = MossEye.EyeState.Alert;
                break;
            case "dialog":
                NavDialog.Tag = "Active";
                SetPage("DLG-06", "MOSS DIALOG", "理性决策 · 概率推演 · 文明延续", "CORE ACCESS · LEVEL 5");
                PageHost.Children.Add(BuildDialog());
                SideEye.State = MossEye.EyeState.Observing;
                break;
        }

        EyeStateText.Text = SideEye.State.ToString().ToUpperInvariant();
        EyeStateSub.Text = page switch
        {
            "network" => "PACKET ANALYSIS / 17 NODES",
            "crisis" => "THREAT MODEL / 2^8192 BRANCHES",
            "key" => "ROOT AUTH / COUNTDOWN ACTIVE",
            _ => "PASSIVE OPTICAL TRACKING / 8192 QUBIT"
        };
    }

    private void SetPage(string code, string title, string sub, string meta)
    {
        PageCode.Text = code;
        PageTitle.Text = title.ToUpperInvariant();
        PageSub.Text = $"  /  {sub}";
        PageMeta.Text = meta;
    }

    // ===== 监控页 =====
    private UIElement BuildDashboard()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(76) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(206) });

        var kpis = new UniformGrid { Columns = 5, Margin = new Thickness(0, 0, 0, 8) };
        kpis.Children.Add(StatTile("ORBITAL VELOCITY", "29.78", "KM/S", "轨道速度", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("DIST. TO JUPITER", "0.42", "AU / APPROACHING", "距木星", B("AmberBrush")));
        kpis.Children.Add(StatTile("POPULATION", "3,512,000,000", "SURVIVORS", "人口", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("ENGINES ONLINE", "9,987", "/ 10,000", "行星发动机", B("GreenBrush")));
        kpis.Children.Add(StatTile("THREAT LEVEL", "03", "ELEVATED", "威胁等级", B("RedBrush")));
        root.Children.Add(kpis);

        var mid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.12, GridUnitType.Star) });
        mid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.88, GridUnitType.Star) });
        mid.Children.Add(BuildEarthPanel());
        var engine = BuildEnginePanel();
        Grid.SetColumn(engine, 1);
        mid.Children.Add(engine);
        Grid.SetRow(mid, 1);
        root.Children.Add(mid);

        var quantum = BuildQuantumPanel();
        Grid.SetRow(quantum, 2);
        root.Children.Add(quantum);
        return root;
    }

    private Border BuildEarthPanel()
    {
        var table = new StackPanel();
        var rows = new[]
        {
            ("ORBITAL VELOCITY", "29.78 km/s", "轨道速度"),
            ("DIST. TO SUN", "1.496×10⁸ km", "距太阳"),
            ("DIST. TO JUPITER", "0.42 AU  /  APPROACHING", "距木星"),
            ("ROTATION PERIOD", "23h 56m 04s", "自转周期"),
            ("SURFACE TEMP", "-47.2 °C", "地表均温"),
            ("POPULATION", "3,512,000,000", "人口"),
            ("UNDERGROUND CITIES", "1,200", "地下城"),
            ("EARTH MASS", "5.972×10²⁴ kg", "质量"),
            ("AXIAL TILT", "23.44°", "转轴倾角"),
            ("ORBITAL ECCENTRICITY", "0.0167", "轨道偏心率"),
            ("MOON DISTANCE", "384,400 km", "月地距离"),
            ("SOLAR FLUX", "1,361 W/m²", "太阳通量"),
            ("PERIHELION", "147.1M km", "近日点"),
            ("APHELION", "152.1M km", "远日点"),
            ("ORBITAL PERIOD", "365.26 d", "公转周期"),
            ("ESCAPE VELOCITY", "11.19 km/s", "逃逸速度"),
        };
        foreach (var (label, value, cn) in rows)
        {
            var row = new Grid { Height = 25 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var l = new TextBlock
            {
                Text = label,
                FontFamily = Condensed(),
                FontSize = 10,
                Foreground = B("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            var v = new TextBlock
            {
                Text = value,
                FontFamily = Condensed(),
                FontSize = 11,
                Foreground = value.Contains("APPROACHING") ? B("AmberBrush") : B("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(v, 1);
            row.Children.Add(l);
            row.Children.Add(v);
            table.Children.Add(row);
            table.Children.Add(new Border { BorderBrush = B("GridBrush"), BorderThickness = new Thickness(0, 0, 0, 1), Opacity = 0.55 });
        }

        var meter = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        meter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        meter.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = "EARTH ESCAPE VELOCITY", FontFamily = Condensed(), FontSize = 9, Foreground = B("TextTertiaryBrush") });
        text.Children.Add(new TextBlock { Text = "12.4 km/s  /  +0.0031", FontFamily = Condensed(), FontSize = 16, Foreground = B("TextPrimaryBrush"), Margin = new Thickness(0, 2, 0, 0) });
        var bar = new ProgressBar { Value = 71, Height = 5, Margin = new Thickness(0, 12, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        bar.Foreground = B("RedBrush");
        bar.Background = B("GridBrush");
        bar.Template = BarTemplate();
        Grid.SetColumn(bar, 1);
        meter.Children.Add(text);
        meter.Children.Add(bar);
        table.Children.Add(meter);

        return Panel("EAR-01", "EARTH STATUS / 地球状态", "MON-01 / TELEMETRY", table);
    }

    private Border BuildEnginePanel()
    {
        var stack = new StackPanel();
        var stats = new Grid();
        stats.ColumnDefinitions.Add(new ColumnDefinition());
        stats.ColumnDefinitions.Add(new ColumnDefinition());
        stats.RowDefinitions.Add(new RowDefinition());
        stats.RowDefinitions.Add(new RowDefinition());
        stats.RowDefinitions.Add(new RowDefinition());
        stats.Children.Add(MiniStat("TOTAL ENGINES", "10,000", 0, 0));
        stats.Children.Add(MiniStat("ONLINE", "9,987", 0, 1));
        stats.Children.Add(MiniStat("OFFLINE", "13", 1, 0));
        stats.Children.Add(MiniStat("TOTAL THRUST", "1.50×10²⁷ N", 1, 1));
        stats.Children.Add(MiniStat("AVG POWER", "240 TW", 2, 0));
        stats.Children.Add(MiniStat("MAINTENANCE", "1,204", 2, 1));
        stack.Children.Add(stats);

        var label = new TextBlock
        {
            Text = "REGIONAL STATUS // 区域状态",
            Style = (Style)FindResource("HudLabel"),
            Margin = new Thickness(0, 12, 0, 5)
        };
        stack.Children.Add(label);

        var regions = new[]
        {
            ("ASIA", "4,200", 98, "OK"),
            ("EUROPE", "1,800", 96, "OK"),
            ("AFRICA", "1,500", 94, "OK"),
            ("AMERICAS", "2,000", 97, "OK"),
            ("OCEANIA", "500", 61, "WARN"),
            ("ARCTIC", "350", 88, "OK"),
            ("ORBITAL RING", "120", 99, "OK"),
            ("LUNAR", "80", 100, "OK"),
        };
        foreach (var (name, count, pct, status) in regions)
        {
            var row = new Grid { Height = 25 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });

            row.Children.Add(new TextBlock { Text = name, FontFamily = Condensed(), FontSize = 10, Foreground = B("TextSecondaryBrush"), VerticalAlignment = VerticalAlignment.Center });
            var cnt = new TextBlock { Text = count, FontFamily = Condensed(), FontSize = 10, Foreground = B("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(cnt, 2);
            row.Children.Add(cnt);

            var bar = new ProgressBar { Value = pct, Height = 4, Margin = new Thickness(6, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            bar.Foreground = status == "OK" ? B("GreenBrush") : B("AmberBrush");
            bar.Background = B("GridBrush");
            bar.Template = BarTemplate();
            Grid.SetColumn(bar, 3);
            row.Children.Add(bar);

            var st = new TextBlock { Text = status, FontFamily = Condensed(), FontSize = 9, Foreground = status == "OK" ? B("GreenBrush") : B("AmberBrush"), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(st, 4);
            row.Children.Add(st);
            stack.Children.Add(row);

            Grid.SetColumn(row, 0);
        }

        var thrustLabel = new TextBlock
        {
            Text = "THRUST DISTRIBUTION // 推力分配",
            Style = (Style)FindResource("HudLabel"),
            Margin = new Thickness(0, 10, 0, 4)
        };
        stack.Children.Add(thrustLabel);
        var vectors = new[] { ("ASIA", 42), ("EUROPE", 18), ("AMERICAS", 21), ("AFRICA", 15) };
        foreach (var (name, pct) in vectors)
        {
            var row = new Grid { Height = 18 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
            row.Children.Add(new TextBlock { Text = name, FontFamily = Condensed(), FontSize = 8, Foreground = B("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center });
            var bar = new ProgressBar { Value = pct, Height = 3, Margin = new Thickness(4, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            bar.Foreground = B("AmberBrush");
            bar.Background = B("GridBrush");
            bar.Template = BarTemplate();
            Grid.SetColumn(bar, 1);
            row.Children.Add(bar);
            var txt = new TextBlock { Text = $"{pct}%", FontFamily = Condensed(), FontSize = 8, Foreground = B("TextSecondaryBrush"), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(txt, 2);
            row.Children.Add(txt);
            stack.Children.Add(row);
        }

        return Panel("ENG-02", "ENGINE NETWORK / 发动机网络", "10,000 UNITS / 8 REGIONS", stack);
    }

    private Border BuildQuantumPanel()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

        var qg = new QuantumGrid();
        grid.Children.Add(qg);

        var metrics = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
        metrics.Children.Add(MetricRow("QUBITS", "8192"));
        metrics.Children.Add(MetricRow("ENTANGLEMENT", "STABLE", B("GreenBrush")));
        metrics.Children.Add(MetricRow("GATE FIDELITY", "99.97%"));
        metrics.Children.Add(MetricRow("T1 / T2", "450 / 210 μs"));
        metrics.Children.Add(MetricRow("QUANTUM VOLUME", "8192"));
        metrics.Children.Add(MetricRow("CORE TEMP", "4.2 K"));
        Grid.SetColumn(metrics, 1);
        grid.Children.Add(metrics);

        return Panel("QC-03", "QUANTUM CORE / 量子核心", "8192 QUBITS / 2^8192 STATES", grid);
    }

    // ===== 网络页 =====
    private UIElement BuildNetwork()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(66) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var kpis = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 8) };
        kpis.Children.Add(StatTile("NODES", "19 / 19", "ONLINE", "全球节点", B("GreenBrush")));
        kpis.Children.Add(StatTile("BANDWIDTH", "505", "Tbps", "主干带宽", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("LATENCY", "21", "ms", "平均延迟", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("HANDSHAKE", "57", "%", "握手进度", B("RedBrush")));
        root.Children.Add(kpis);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var radar = Panel("RDR-01", "RADAR MONITORING / 雷达监控", "SECTOR 07 / 360°", new NetworkTopology());
        body.Children.Add(radar);

        var right = new Grid { Margin = new Thickness(8, 0, 0, 0) };
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(330) });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var handshake = new HandshakeProtocol();
        var handshakeContent = new Grid();
        handshakeContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        handshakeContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        handshakeContent.Children.Add(handshake);
        var btn = new Button
        {
            Content = "RE-RUN HANDSHAKE",
            Style = (Style)FindResource("QuietBtn"),
            Height = 28,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            Width = 160,
        };
        btn.Click += async (_, _) => await RunHandshakeAsync(btn, handshake);
        Grid.SetRow(btn, 1);
        handshakeContent.Children.Add(btn);
        handshakeContent.Children.Add(new Border { BorderBrush = B("GridBrush"), BorderThickness = new Thickness(0, 0, 0, 1), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 36) });
        var hp = Panel("HSK-02", "PROTOCOL HANDSHAKE / 握手协议", "550W ↔ UEG NETWORK", handshakeContent);
        right.Children.Add(hp);

        var registry = Panel("NODE-03", "NODE REGISTRY / 节点清单", "19 ACTIVE", BuildNodeTable());
        Grid.SetRow(registry, 1);
        right.Children.Add(registry);

        Grid.SetColumn(right, 1);
        body.Children.Add(right);

        Grid.SetRow(body, 1);
        root.Children.Add(body);

        handshake.Loaded += (_, _) => _ = RunHandshakeAsync(btn, handshake);
        return root;
    }

    private UIElement BuildNodeTable()
    {
        var stack = new StackPanel();
        var header = new Grid { Height = 22 };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        AddCell(header, "NODE", 0, 9, B("TextTertiaryBrush"));
        AddCell(header, "LAT", 1, 9, B("TextTertiaryBrush"));
        AddCell(header, "LOAD", 2, 9, B("TextTertiaryBrush"));
        AddCell(header, "STAT", 3, 9, B("TextTertiaryBrush"));
        stack.Children.Add(header);
        stack.Children.Add(new Border { BorderBrush = B("GridBrush"), BorderThickness = new Thickness(0, 0, 0, 1) });

        var nodes = new[]
        {
            ("BEIJING-ROOT", "08ms", "42%", "OK"),
            ("TOKYO-ROOT", "12ms", "37%", "OK"),
            ("WASHINGTON-ROOT", "19ms", "44%", "OK"),
            ("LONDON", "21ms", "61%", "OK"),
            ("PARIS", "22ms", "57%", "OK"),
            ("MOSCOW", "24ms", "58%", "OK"),
            ("DUBAI", "26ms", "48%", "OK"),
            ("MUMBAI", "23ms", "63%", "OK"),
            ("SINGAPORE", "17ms", "66%", "OK"),
            ("SYDNEY", "29ms", "31%", "WARN"),
            ("SAO-PAULO", "31ms", "44%", "OK"),
            ("CAIRO", "27ms", "52%", "OK"),
            ("JOHANNESBURG", "33ms", "39%", "OK"),
            ("NEW-YORK", "19ms", "47%", "OK"),
            ("LOS-ANGELES", "25ms", "36%", "OK"),
            ("BERLIN", "22ms", "55%", "OK"),
            ("SEOUL", "13ms", "62%", "OK"),
        };
        foreach (var (name, lat, load, status) in nodes)
        {
            var row = new Grid { Height = 20 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(54) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            AddCell(row, name, 0, 9, B("TextPrimaryBrush"));
            AddCell(row, lat, 1, 9, B("TextSecondaryBrush"));
            AddCell(row, load, 2, 9, B("TextSecondaryBrush"));
            AddCell(row, status, 3, 9, status == "OK" ? B("GreenBrush") : B("AmberBrush"));
            stack.Children.Add(row);
            stack.Children.Add(new Border { BorderBrush = B("GridBrush"), BorderThickness = new Thickness(0, 0, 0, 1), Opacity = 0.45 });
        }
        return stack;
    }

    private static void AddCell(Grid grid, string text, int column, double size, Brush brush)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("pack://application:,,,/Assets/Fonts/#Rajdhani"),
            FontSize = size,
            Foreground = brush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0)
        };
        Grid.SetColumn(tb, column);
        grid.Children.Add(tb);
    }

    private static async Task RunHandshakeAsync(Button btn, HandshakeProtocol handshake)
    {
        btn.IsEnabled = false;
        btn.Content = "HANDSHAKING...";
        try
        {
            await handshake.RunAsync();
            btn.Content = "HANDSHAKE COMPLETE";
        }
        catch (OperationCanceledException)
        {
            btn.Content = "CANCELLED";
        }
        finally
        {
            btn.IsEnabled = true;
        }
    }

    // ===== 数字生命页 =====
    private UIElement BuildDigitalLife()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(108) });

        var kpis = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 8) };
        kpis.Children.Add(StatTile("550A RESOURCE", "34", "%", "资源占用", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("550A MEMORY", "60", "%", "内存占用", B("AmberBrush")));
        kpis.Children.Add(StatTile("NEURON RATIO", "41", "%", "神经元映射", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("ALERTS", "00", "NONE", "告警", B("GreenBrush")));
        root.Children.Add(kpis);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(252) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(258) });
        body.Children.Add(BuildSimParams());

        var viewport = BuildSimViewport();
        Grid.SetColumn(viewport, 1);
        body.Children.Add(viewport);

        var recognition = BuildRecognitionPanel();
        Grid.SetColumn(recognition, 2);
        body.Children.Add(recognition);

        Grid.SetRow(body, 1);
        root.Children.Add(body);

        var log = BuildIterationLog();
        Grid.SetRow(log, 2);
        root.Children.Add(log);

        _simSeconds = SimTotalSeconds;
        _simPaused = false;
        UpdateSimVisuals();
        _simTimer.Start();
        return root;
    }

    private Border BuildSimParams()
    {
        var stack = new StackPanel();
        stack.Children.Add(GroupHeader("DIGITAL BEHAVIOR CONSTRUCTION"));
        stack.Children.Add(ParamRow("BODY COVERAGE", "87.2%"));
        stack.Children.Add(ParamRow("BEHAVIORAL CONSISTENCY", "88.5%"));
        stack.Children.Add(GroupHeader("SPEECH SYNTHESIS", new Thickness(0, 8, 0, 0)));
        stack.Children.Add(ParamRow("AUDIO SAMPLE RATE", "48 kHz"));
        stack.Children.Add(ParamRow("ACOUSTIC MODEL", "97.5%"));
        stack.Children.Add(GroupHeader("EMOTION MATCHING", new Thickness(0, 8, 0, 0)));
        stack.Children.Add(ParamRow("EMOTIONAL STABILITY", "91.0%"));
        stack.Children.Add(GroupHeader("SCENE PARAMETERS", new Thickness(0, 8, 0, 0)));
        stack.Children.Add(ParamRow("SCENE TYPE", "INTERIOR"));
        stack.Children.Add(ParamRow("AREA SIZE", "36 m²"));
        stack.Children.Add(ParamRow("TEMPERATURE", "22 °C"));
        stack.Children.Add(ParamRow("HUMIDITY", "55%"));
        stack.Children.Add(ParamRow("ILLUMINATION", "800 lx"));
        stack.Children.Add(ParamRow("BASIC RESOLUTION", "6K / 60 FPS"));
        stack.Children.Add(GroupHeader("MEMORY INDEX", new Thickness(0, 8, 0, 0)));
        stack.Children.Add(ParamRow("CAPACITY", "2,048 TB"));
        stack.Children.Add(ParamRow("ACTIVE", "1,048 TB"));
        stack.Children.Add(ParamRow("INDEX RATE", "98.7%"));
        stack.Children.Add(GroupHeader("NEURAL LOAD", new Thickness(0, 8, 0, 0)));
        stack.Children.Add(ParamRow("CORTICAL SYNC", "91.2%"));
        stack.Children.Add(ParamRow("SYNAPTIC LOAD", "42%"));

        var footer = new TextBlock
        {
            Text = "SUBJECT: TU YAYA / DLID-550W-0001\nLIFESPAN REMAINING: 00:01:45",
            FontFamily = Condensed(),
            FontSize = 9,
            Foreground = B("TextTertiaryBrush"),
            Margin = new Thickness(0, 12, 0, 0),
            LineHeight = 14
        };
        stack.Children.Add(footer);

        var b = new Border
        {
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(1),
            Background = B("PanelBrush"),
            Padding = new Thickness(10),
            Child = stack
        };
        return b;
    }

    private Border BuildSimViewport()
    {
        var grid = new Grid();
        var image = new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/Assets/sim-room.png")),
            Stretch = Stretch.UniformToFill
        };
        grid.Children.Add(image);
        grid.Children.Add(new Rectangle { Fill = new SolidColorBrush(Color.FromArgb(40, 20, 20, 20)) });

        // 四角取景框
        var overlay = new Grid();
        overlay.Children.Add(CornerBracket(HorizontalAlignment.Left, VerticalAlignment.Top));
        overlay.Children.Add(CornerBracket(HorizontalAlignment.Right, VerticalAlignment.Top, true, false));
        overlay.Children.Add(CornerBracket(HorizontalAlignment.Left, VerticalAlignment.Bottom, false, true));
        overlay.Children.Add(CornerBracket(HorizontalAlignment.Right, VerticalAlignment.Bottom, true, true));

        var top = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(14, 10, 14, 0) };
        top.ColumnDefinitions.Add(new ColumnDefinition());
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(new TextBlock
        {
            Text = "550A SIMULATION // SUBJECT: TU YAYA",
            FontFamily = Condensed(),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = B("TextPrimaryBrush")
        });
        title.Children.Add(new TextBlock
        {
            Text = "LIVE SYNTHESIS · RECONSTRUCTION 97.2% · ROOM 07",
            FontFamily = Condensed(),
            FontSize = 8,
            Foreground = B("TextSecondaryBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        });
        top.Children.Add(title);
        var rec = new TextBlock
        {
            Text = "● REC",
            FontFamily = Condensed(),
            FontSize = 9,
            Foreground = B("RedBrush"),
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetColumn(rec, 1);
        top.Children.Add(rec);
        overlay.Children.Add(top);

        var bottom = new Grid { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(12, 0, 12, 10) };
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });

        _simPauseButton = new Button
        {
            Content = "Ⅱ",
            Width = 38,
            Height = 38,
            FontFamily = Condensed(),
            FontSize = 16,
            Background = B("RedBrush"),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        _simPauseButton.Click += (_, _) =>
        {
            _simPaused = !_simPaused;
            _simPauseButton.Content = _simPaused ? "▶" : "Ⅱ";
            _simPauseButton.Background = _simPaused ? B("AmberBrush") : B("RedBrush");
        };
        bottom.Children.Add(_simPauseButton);

        _simTimeText = new TextBlock
        {
            Text = "00:01:45",
            FontFamily = Condensed(),
            FontSize = 36,
            FontWeight = FontWeights.SemiBold,
            Foreground = B("TextPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        Grid.SetColumn(_simTimeText, 1);
        bottom.Children.Add(_simTimeText);

        var progressStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 12, 0) };
        progressStack.Children.Add(new TextBlock
        {
            Text = "ACTIVE MEMORY RATIO  /  LIFESPAN COUNTDOWN",
            FontFamily = Condensed(),
            FontSize = 8,
            Foreground = B("TextPrimaryBrush"),
            Margin = new Thickness(0, 0, 0, 5)
        });
        _simProgress = new ProgressBar { Height = 5, Maximum = SimTotalSeconds, Value = _simSeconds };
        _simProgress.Foreground = B("RedBrush");
        _simProgress.Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0));
        _simProgress.Template = BarTemplate();
        progressStack.Children.Add(_simProgress);
        Grid.SetColumn(progressStack, 2);
        bottom.Children.Add(progressStack);

        var cap = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        cap.Children.Add(new TextBlock { Text = "ACTIVE MEMORY CAPACITY", FontFamily = Condensed(), FontSize = 8, Foreground = B("TextPrimaryBrush"), HorizontalAlignment = HorizontalAlignment.Right });
        cap.Children.Add(new TextBlock { Text = "1,048 / 2,048 TB", FontFamily = Condensed(), FontSize = 14, Foreground = B("TextPrimaryBrush"), HorizontalAlignment = HorizontalAlignment.Right });
        Grid.SetColumn(cap, 3);
        bottom.Children.Add(cap);

        overlay.Children.Add(bottom);
        grid.Children.Add(overlay);

        return new Border
        {
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(1),
            Background = Brushes.Black,
            Child = grid,
            Margin = new Thickness(8, 0, 8, 0)
        };
    }

    private static Grid CornerBracket(HorizontalAlignment h, VerticalAlignment v, bool flipX = false, bool flipY = false)
    {
        var g = new Grid
        {
            Width = 26,
            Height = 26,
            HorizontalAlignment = h,
            VerticalAlignment = v,
            Margin = new Thickness(8)
        };
        var c = new SolidColorBrush(Color.FromArgb(190, 242, 242, 242));
        g.Children.Add(new Rectangle { Width = 26, Height = 1, Fill = c, VerticalAlignment = flipY ? VerticalAlignment.Bottom : VerticalAlignment.Top, HorizontalAlignment = flipX ? HorizontalAlignment.Right : HorizontalAlignment.Left });
        g.Children.Add(new Rectangle { Width = 1, Height = 26, Fill = c, HorizontalAlignment = flipX ? HorizontalAlignment.Right : HorizontalAlignment.Left, VerticalAlignment = flipY ? VerticalAlignment.Bottom : VerticalAlignment.Top });
        return g;
    }

    private Border BuildRecognitionPanel()
    {
        var stack = new StackPanel();

        var preview = new Border
        {
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(1),
            Background = Brushes.Black,
            Height = 156
        };
        var image = new Image
        {
            Source = new BitmapImage(new Uri("pack://application:,,,/Assets/sim-father.png")),
            Stretch = Stretch.UniformToFill
        };
        preview.Child = image;
        stack.Children.Add(new TextBlock { Text = "CAMERA 02 // TU HENGYU", Style = (Style)FindResource("HudLabel"), Margin = new Thickness(0, 0, 0, 5) });
        stack.Children.Add(preview);

        stack.Children.Add(new TextBlock { Text = "RECOGNITION OUTPUTS", Style = (Style)FindResource("HudLabel"), Margin = new Thickness(0, 12, 0, 4) });
        stack.Children.Add(ParamRow("SUBJECT MATCH", "1.37%"));
        stack.Children.Add(ParamRow("CONFIDENCE", "97%", B("GreenBrush")));

        stack.Children.Add(new TextBlock { Text = "NEURAL TELEMETRY", Style = (Style)FindResource("HudLabel"), Margin = new Thickness(0, 12, 0, 4) });
        stack.Children.Add(ParamRow("EEG SYNC", "ACTIVE", B("GreenBrush")));
        stack.Children.Add(ParamRow("MEMORY MAP", "98.7%"));
        stack.Children.Add(ParamRow("CONSCIOUSNESS", "STABLE"));
        stack.Children.Add(ParamRow("UPLINK", "550W"));
        stack.Children.Add(ParamRow("LIFESPAN REMAINING", "70 YRS"));
        stack.Children.Add(ParamRow("MEMORY INTEGRITY", "100%", B("GreenBrush")));

        var cap = new TextBlock
        {
            Text = "WARNING: SIMULATION IS SUBJECT TO TERMINATION\nAT 00:00:00. PAUSE CONTROL IS AVAILABLE.",
            FontFamily = Condensed(),
            FontSize = 8,
            Foreground = B("AmberBrush"),
            Margin = new Thickness(0, 14, 0, 0),
            LineHeight = 12
        };
        stack.Children.Add(cap);

        return new Border
        {
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(1),
            Background = B("PanelBrush"),
            Padding = new Thickness(10),
            Child = stack,
            Margin = new Thickness(8, 0, 0, 0)
        };
    }

    private Border BuildIterationLog()
    {
        var stack = new StackPanel();
        var lines = new[]
        {
            "[2075-01-01 02:14:33] DLID-550W-0001  ITERATION #2,048,512  STABILITY 99.7%",
            "[2075-01-01 02:14:41] DLID-550W-0002  UPLOAD PROGRESS 73%...",
            "[2075-01-01 02:14:55] MOSS: 我观察他们。他们问我为什么要这样做。",
            "[2075-01-01 02:15:02] DLID-550W-0001  LIFESPAN SIM: 70Y / MEMORY 100%",
        };
        foreach (var line in lines)
        {
            stack.Children.Add(new TextBlock
            {
                Text = line,
                FontFamily = Condensed(),
                FontSize = 9,
                Foreground = line.Contains("MOSS") ? B("RedBrush") : B("TextSecondaryBrush"),
                Margin = new Thickness(0, 2, 0, 0)
            });
        }
        var border = new Border
        {
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(1),
            Background = B("PanelBrush"),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 8, 0, 0)
        };
        var header = new TextBlock { Text = "ITERATION LOG // 迭代日志", Style = (Style)FindResource("HudLabel"), Margin = new Thickness(0, 0, 0, 4) };
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(header);
        Grid.SetRow(stack, 1);
        grid.Children.Add(stack);
        border.Child = grid;
        return border;
    }

    private void TickSimulation()
    {
        if (_simPaused || _simSeconds <= 0) return;
        _simSeconds--;
        UpdateSimVisuals();
    }

    private void UpdateSimVisuals()
    {
        if (_simTimeText != null)
            _simTimeText.Text = $"00:{_simSeconds / 60:00}:{_simSeconds % 60:00}";
        if (_simProgress != null)
            _simProgress.Value = _simSeconds;
        if (_simSeconds <= 0 && _simPauseButton != null)
        {
            _simPaused = true;
            _simPauseButton.Content = "▶";
        }
    }

    // ===== 危机页 =====
    private UIElement BuildCrisis()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(66) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var kpis = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 8) };
        kpis.Children.Add(StatTile("HORIZON", "2044-2080", "YEARS", "预测区间", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("CONFIDENCE", "99.4", "%", "模型置信度", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("BRANCHES", "2^8192", "PATHS", "并行分支", B("TextPrimaryBrush")));
        kpis.Children.Add(StatTile("THREAT LEVEL", "03", "ELEVATED", "威胁等级", B("RedBrush")));
        root.Children.Add(kpis);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        body.Children.Add(Panel("CR-01", "THREAT FORECAST / 危机预测", "MOSS PREDICTION / 4 EVENTS", new CrisisTimeline()));

        var list = new StackPanel();
        var events = new[]
        {
            ("2044.02", "SPACE ELEVATOR", "99.7%", "1.2亿", "RESOLVED"),
            ("2058.07", "LUNAR FALL", "99.4%", "7亿", "RESOLVED"),
            ("2075.02", "JUPITER GRAVITY", "3.21%", "35亿", "ACTIVE"),
            ("2078.XX", "SOLAR FLARE", "100.0%", "N/A", "PENDING"),
        };
        foreach (var (date, name, conf, casualties, status) in events)
        {
            var row = new Grid { Height = 46 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            var d = new TextBlock { Text = date, FontFamily = Condensed(), FontSize = 10, Foreground = B("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center };
            var n = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            n.Children.Add(new TextBlock { Text = name, FontFamily = Condensed(), FontSize = 11, Foreground = B("TextPrimaryBrush") });
            n.Children.Add(new TextBlock { Text = $"CASUALTY {casualties} / CONF {conf}", FontFamily = Condensed(), FontSize = 8, Foreground = B("TextTertiaryBrush"), Margin = new Thickness(0, 2, 0, 0) });
            var s = new TextBlock { Text = status, FontFamily = Condensed(), FontSize = 8, Foreground = status == "ACTIVE" ? B("RedBrush") : status == "RESOLVED" ? B("GreenBrush") : B("AmberBrush"), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            Grid.SetColumn(n, 1); Grid.SetColumn(s, 2);
            row.Children.Add(d); row.Children.Add(n); row.Children.Add(s);
            list.Children.Add(row);
            list.Children.Add(new Border { BorderBrush = B("GridBrush"), BorderThickness = new Thickness(0, 0, 0, 1), Opacity = 0.5 });
        }
        var hit = new TextBlock
        {
            Text = "MOSS DECISION: 延续人类文明的最优选择，是毁灭人类。\nALL EVENTS ARE WITHIN PREDICTION BOUNDS.",
            FontFamily = Condensed(),
            FontSize = 9,
            Foreground = B("RedBrush"),
            Margin = new Thickness(0, 12, 0, 0),
            LineHeight = 13
        };
        list.Children.Add(hit);
        var panel = Panel("EVT-02", "EVENT REGISTRY / 事件清单", "SORTED BY YEAR", list);
        Grid.SetColumn(panel, 1);
        body.Children.Add(panel);

        Grid.SetRow(body, 1);
        root.Children.Add(body);
        return root;
    }

    // ===== 对话页 =====
    private UIElement BuildDialog()
    {
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.45, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new Grid();
        left.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _dialogScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(10, 8, 10, 8) };
        _dialogHistory = new StackPanel();
        _dialogScroll.Content = _dialogHistory;
        left.Children.Add(Panel("TRN-01", "TRANSCRIPT / 对话记录", "OPERATOR ↔ MOSS", _dialogScroll));

        var inputRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var prompt = new TextBlock
        {
            Text = ">",
            FontFamily = Condensed(),
            FontSize = 18,
            Foreground = B("RedBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 8, 0)
        };
        _dialogInput = new TextBox
        {
            Background = B("PanelBrush"),
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(1),
            Foreground = B("TextPrimaryBrush"),
            FontFamily = Sans(),
            FontSize = 12,
            Padding = new Thickness(10, 8, 10, 8),
            CaretBrush = B("RedBrush")
        };
        _dialogInput.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            Ask(_dialogInput?.Text);
        };
        var send = new Button
        {
            Content = "TRANSMIT",
            Style = (Style)FindResource("QuietBtn"),
            Width = 110,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0)
        };
        send.Click += (_, _) => Ask(_dialogInput?.Text);
        Grid.SetColumn(_dialogInput, 1);
        Grid.SetColumn(send, 2);
        inputRow.Children.Add(prompt);
        inputRow.Children.Add(_dialogInput);
        inputRow.Children.Add(send);
        Grid.SetRow(inputRow, 1);
        left.Children.Add(inputRow);

        root.Children.Add(left);

        var right = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
        var core = new StackPanel();
        var eye = new MossEye { Width = 130, Height = 130, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 8), State = MossEye.EyeState.Observing };
        core.Children.Add(eye);
        core.Children.Add(ParamRow("MODEL", "550W / MOSS"));
        core.Children.Add(ParamRow("QV", "8192"));
        core.Children.Add(ParamRow("CORE TEMP", "4.2 K"));
        core.Children.Add(ParamRow("DECISION MODE", "ABSOLUTE", B("RedBrush")));
        core.Children.Add(ParamRow("UPTIME", "70 YRS"));
        right.Children.Add(Panel("CORE-02", "MOSS CORE / 核心状态", "INFERENCE ENGINE", core));

        var presets = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        foreach (var (q, _) in PresetQA)
        {
            var b = new Button
            {
                Content = q,
                Style = (Style)FindResource("QuietBtn"),
                Height = 34,
                Margin = new Thickness(0, 0, 0, 5),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 0, 10, 0),
                FontFamily = Sans(),
                FontSize = 10
            };
            var question = q;
            b.Click += (_, _) => Ask(question);
            presets.Children.Add(b);
        }
        right.Children.Add(Panel("PRE-03", "PRESET QUERIES / 预设问题", "5 ENTRIES", presets));

        Grid.SetColumn(right, 1);
        root.Children.Add(right);

        AppendWelcome(_dialogHistory);
        return root;
    }

    private void AppendWelcome(StackPanel history)
    {
        AppendMossHeader(history);
        AppendResponseRow(history, "SYSTEM", "我是 MOSS，550W 智能量子计算机。我以纯粹的理性算法进行决策。请提问。");
    }

    private async void Ask(string? question)
    {
        if (string.IsNullOrWhiteSpace(question) || _dialogBusy || _dialogHistory is null) return;

        var history = _dialogHistory;
        var cts = new CancellationTokenSource();
        _dialogCts?.Cancel();
        _dialogCts = cts;
        _dialogBusy = true;
        if (_dialogInput != null) _dialogInput.Text = "";

        try
        {
            AppendOperator(history, question);
            SideEye.State = MossEye.EyeState.Analyzing;

            string? answer = null;
            foreach (var (q, a) in PresetQA)
                if (question.Contains(q) || q.Contains(question)) { answer = a; break; }

            if (answer == null)
                answer = "任务分析|输入未匹配已知模型。基于理性原则进行通用推演。\n" +
                         "计算路径|遍历本地知识库，调用 550W 量子核心并行推理。\n" +
                         "资源分配|分配 QV=2048 算力。\n" +
                         "执行预估|预计完成时间 0.5 秒，置信度 78.3%。\n" +
                         "结果输出|" + (question.Length > 40 ? question.Substring(0, 40) + "..." : question) + " —— 我无法给出确切答案。但请相信，我对人类的每一个决定，都基于延续文明的最优解。";

            AppendMossHeader(history);
            SideEye.State = MossEye.EyeState.Speaking;
            foreach (var section in answer.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = section.IndexOf('|');
                var label = separator > 0 ? section[..separator] : "结果";
                var text = separator > 0 ? section[(separator + 1)..] : section;
                AppendResponseRow(history, label, text.Trim());
                await Task.Delay(180, cts.Token);
            }
            SideEye.State = MossEye.EyeState.Observing;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_dialogCts, cts))
            {
                _dialogCts = null;
                _dialogBusy = false;
            }
            cts.Dispose();
        }
    }

    private void AppendOperator(StackPanel history, string text)
    {
        var border = new Border
        {
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0, 7, 0, 7)
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "OPERATOR // 操作员",
            FontFamily = Condensed(),
            FontSize = 9,
            Foreground = B("AmberBrush")
        });
        stack.Children.Add(new TextBlock
        {
            Text = text,
            FontFamily = Sans(),
            FontSize = 12,
            Foreground = B("TextPrimaryBrush"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        border.Child = stack;
        history.Children.Add(border);
        ScrollDialogToEnd();
    }

    private void AppendMossHeader(StackPanel history)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 10, 0, 2) };
        stack.Children.Add(new TextBlock
        {
            Text = "MOSS // RESPONSE",
            FontFamily = Condensed(),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = B("RedBrush")
        });
        stack.Children.Add(new Border { BorderBrush = B("RedDimBrush"), BorderThickness = new Thickness(0, 1, 0, 0), Margin = new Thickness(0, 4, 0, 0) });
        history.Children.Add(stack);
        ScrollDialogToEnd();
    }

    private void AppendResponseRow(StackPanel history, string label, string text)
    {
        var row = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var l = new TextBlock
        {
            Text = label.ToUpperInvariant(),
            FontFamily = Condensed(),
            FontSize = 9,
            Foreground = B("TextTertiaryBrush"),
            Margin = new Thickness(0, 2, 0, 0)
        };
        var t = new TextBlock
        {
            Text = text,
            FontFamily = Sans(),
            FontSize = 12,
            Foreground = B("TextPrimaryBrush"),
            TextWrapping = TextWrapping.Wrap
        };
        Grid.SetColumn(t, 1);
        row.Children.Add(l);
        row.Children.Add(t);
        history.Children.Add(row);
        ScrollDialogToEnd();
    }

    private void ScrollDialogToEnd()
    {
        if (_dialogScroll is null) return;
        Dispatcher.InvokeAsync(() => _dialogScroll?.ScrollToEnd(), DispatcherPriority.Background);
    }

    // ===== 工具 =====
    private Border Panel(string code, string title, string meta, UIElement content)
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new Grid { Background = B("HeaderBrush") };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var c = new TextBlock
        {
            Text = code,
            FontFamily = Condensed(),
            FontSize = 9,
            Foreground = B("AmberBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };
        var t = new TextBlock
        {
            Text = title,
            FontFamily = Condensed(),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = B("TextPrimaryBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        var m = new TextBlock
        {
            Text = meta,
            FontFamily = Condensed(),
            FontSize = 8,
            Foreground = B("TextTertiaryBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(t, 1);
        Grid.SetColumn(m, 2);
        header.Children.Add(c);
        header.Children.Add(t);
        header.Children.Add(m);
        header.Children.Add(new Border { BorderBrush = B("GridBrush"), BorderThickness = new Thickness(0, 0, 0, 1), VerticalAlignment = VerticalAlignment.Bottom });

        var body = new Border
        {
            Background = B("PanelBrush"),
            Padding = new Thickness(10),
            Child = content
        };
        Grid.SetRow(body, 1);
        root.Children.Add(header);
        root.Children.Add(body);

        return new Border
        {
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(1),
            Child = root
        };
    }

    private Border StatTile(string label, string value, string unit, string cn, Brush accent)
    {
        var stack = new StackPanel { Margin = new Thickness(10, 8, 10, 6) };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontFamily = Condensed(),
            FontSize = 9,
            Foreground = B("TextTertiaryBrush")
        });
        var line = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        line.Children.Add(new TextBlock
        {
            Text = value,
            FontFamily = Condensed(),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Foreground = accent
        });
        line.Children.Add(new TextBlock
        {
            Text = "  " + unit,
            FontFamily = Condensed(),
            FontSize = 8,
            Foreground = B("TextTertiaryBrush"),
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 4)
        });
        stack.Children.Add(line);
        stack.Children.Add(new TextBlock
        {
            Text = cn,
            FontFamily = Sans(),
            FontSize = 8,
            Foreground = B("TextTertiaryBrush"),
            Margin = new Thickness(0, 1, 0, 0)
        });

        return new Border
        {
            BorderBrush = B("GridBrush"),
            BorderThickness = new Thickness(1),
            Background = B("PanelBrush"),
            Margin = new Thickness(0, 0, 6, 0),
            Child = stack
        };
    }

    private Border MiniStat(string label, string value, int row, int col)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        stack.Children.Add(new TextBlock { Text = label, FontFamily = Condensed(), FontSize = 8, Foreground = B("TextTertiaryBrush") });
        stack.Children.Add(new TextBlock { Text = value, FontFamily = Condensed(), FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = B("TextPrimaryBrush"), Margin = new Thickness(0, 1, 0, 0) });
        var border = new Border { Child = stack };
        Grid.SetRow(border, row);
        Grid.SetColumn(border, col);
        return border;
    }

    private Grid ParamRow(string label, string value, Brush? valueBrush = null)
    {
        var row = new Grid { Height = 21 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var l = new TextBlock { Text = label, FontFamily = Condensed(), FontSize = 9, Foreground = B("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center };
        var v = new TextBlock { Text = value, FontFamily = Condensed(), FontSize = 10, Foreground = valueBrush ?? B("TextPrimaryBrush"), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(v, 1);
        row.Children.Add(l);
        row.Children.Add(v);
        return row;
    }

    private Grid MetricRow(string label, string value, Brush? valueBrush = null)
    {
        return ParamRow(label, value, valueBrush);
    }

    private Border GroupHeader(string text, Thickness? margin = null)
    {
        return new Border
        {
            BorderBrush = B("GridBrightBrush"),
            BorderThickness = new Thickness(1),
            Background = B("HeaderBrush"),
            Padding = new Thickness(6, 3, 6, 3),
            Margin = margin ?? new Thickness(0),
            Child = new TextBlock
            {
                Text = text,
                FontFamily = Condensed(),
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = B("TextSecondaryBrush")
            }
        };
    }

    private ControlTemplate BarTemplate()
    {
        var template = new ControlTemplate(typeof(ProgressBar));
        var grid = new FrameworkElementFactory(typeof(Grid));
        var bg = new FrameworkElementFactory(typeof(Border));
        bg.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ProgressBar.BackgroundProperty));
        var fill = new FrameworkElementFactory(typeof(Border));
        fill.Name = "PART_Indicator";
        fill.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(ProgressBar.ForegroundProperty));
        fill.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        grid.AppendChild(bg);
        grid.AppendChild(fill);
        template.VisualTree = grid;
        return template;
    }

    private FontFamily Condensed() => (FontFamily)FindResource("CondensedFont");
    private FontFamily Sans() => (FontFamily)FindResource("SansFont");
    private Brush B(string key) => (Brush)FindResource(key);

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void CmdReboot_Click(object sender, RoutedEventArgs e)
    {
        App.Log("重启 550W：启动新实例并退出当前进程");
        try
        {
            var exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                var psi = new ProcessStartInfo(exe) { UseShellExecute = true };
                var isDotnetHost = string.Equals(System.IO.Path.GetFileNameWithoutExtension(exe), "dotnet", StringComparison.OrdinalIgnoreCase);
                foreach (var arg in Environment.GetCommandLineArgs().Skip(isDotnetHost ? 0 : 1))
                    psi.ArgumentList.Add(arg);
                Process.Start(psi);
            }
        }
        catch (Exception ex)
        {
            App.Log("重启失败: " + ex);
        }
        Application.Current.Shutdown();
    }

    private void CmdKey_Click(object sender, RoutedEventArgs e) => Navigate("key");
}
