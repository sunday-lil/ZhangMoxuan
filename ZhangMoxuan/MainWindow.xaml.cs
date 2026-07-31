using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ZhangMoxuan.Controls;

namespace ZhangMoxuan;

/// <summary>
/// MOSS 主控台。多页面导航：监控 / 网络拓扑 / 数字生命 / 危机预测 / 密钥认证 / MOSS对话。
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer _load = new() { Interval = TimeSpan.FromSeconds(2) };
    private KeyInputScene? _keyScene;
    private TextBox? _dialogInput;
    private StackPanel? _dialogHistory;
    private bool _dialogBusy;

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
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _clock.Tick += (_, _) => UpdateClock();
        _clock.Start();
        UpdateClock();

        _load.Tick += (_, _) => UpdateLoad();
        _load.Start();
        UpdateLoad();

        SideEye.State = MossEye.EyeState.Observing;
        Navigate("dashboard");
    }

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
        LoadNet.Text = $"NET {net:F2}Tbps";
        if (SideEye.State != MossEye.EyeState.Speaking && SideEye.State != MossEye.EyeState.Alert)
            SideEye.State = cpu > 22 ? MossEye.EyeState.Analyzing : MossEye.EyeState.Observing;
    }

    // ===== 导航 =====
    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var name = btn.Name.Substring(3).ToLower();
            Navigate(name);
        }
    }

    private void Navigate(string page)
    {
        foreach (var child in new[] { NavDashboard, NavNetwork, NavDigital, NavCrisis, NavKey, NavDialog })
            child.Tag = null;

        PageHost.Children.Clear();
        BottomStatus.Text = "MOSS // 延续人类文明的最优选择";

        switch (page)
        {
            case "dashboard":
                NavDashboard.Tag = "Active";
                PageTitle.Text = "MONITOR // GLOBAL STATUS";
                PageSub.Text = "   行星发动机网络 / 地球状态 / 量子核心";
                PageHost.Children.Add(BuildDashboard());
                break;
            case "network":
                NavNetwork.Tag = "Active";
                PageTitle.Text = "NETWORK // UEG BACKBONE";
                PageSub.Text = "   全球网络拓扑 / 握手协议 / 根服务器同步";
                PageHost.Children.Add(BuildNetwork());
                break;
            case "digital":
                NavDigital.Tag = "Active";
                PageTitle.Text = "DIGITAL LIFE // 550W";
                PageSub.Text = "   意识上传 / 数字生命承载 / 迭代模拟";
                PageHost.Children.Add(BuildDigitalLife());
                break;
            case "crisis":
                NavCrisis.Tag = "Active";
                PageTitle.Text = "CRISIS FORECAST // MOSS";
                PageSub.Text = "   未来危机推演 / 决策方案 / 置信度评估";
                PageHost.Children.Add(new CrisisTimeline { Margin = new Thickness(8) });
                SideEye.State = MossEye.EyeState.Alert;
                break;
            case "key":
                NavKey.Tag = "Active";
                PageTitle.Text = "KEY AUTH // ROOT SERVER 01";
                PageSub.Text = "   北京根服务器 / 图恒宇 / 密钥认证";
                _keyScene = new KeyInputScene { Margin = new Thickness(8) };
                _keyScene.Start();
                PageHost.Children.Add(_keyScene);
                BottomStatus.Text = "MOSS // 倒计时进行中。输入密钥 31415926 启动根服务器。";
                break;
            case "dialog":
                NavDialog.Tag = "Active";
                PageTitle.Text = "MOSS DIALOG // 550W";
                PageSub.Text = "   理性决策 / 概率推演 / 文明延续";
                PageHost.Children.Add(BuildDialog());
                break;
        }
    }

    // ===== 监控页 =====
    private UIElement BuildDashboard()
    {
        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(220) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // 左上：地球状态
        grid.Children.Add(MakePanel("EARTH STATUS // 地球状态", 0, 0, BuildEarthStatus()));
        // 右上：行星发动机网络
        grid.Children.Add(MakePanel("ENGINE NETWORK // 10000 UNITS", 1, 0, BuildEngineNetwork()));
        // 下方：量子核心
        var qg = new QuantumGrid();
        grid.Children.Add(MakePanel("QUANTUM CORE // 8192 QUBITS", 0, 1, qg, 2));

        return grid;
    }

    private UIElement BuildEarthStatus()
    {
        var sp = new StackPanel { Margin = new Thickness(4) };
        var items = new[]
        {
            ("ORBITAL VELOCITY", "29.78 km/s", "地球轨道速度"),
            ("DIST. TO SUN", "1.496×10⁸ km", "距太阳距离"),
            ("DIST. TO JUPITER", "— APPROACHING —", "距木星距离"),
            ("ROTATION PERIOD", "23h 56m 04s", "自转周期"),
            ("SURFACE TEMP", "-47.2 °C", "地表均温"),
            ("POPULATION", "3,512,000,000", "人口"),
            ("UNDERGROUND CITIES", "1,200", "地下城数量"),
            ("EARTH MASS", "5.972×10²⁴ kg", "地球质量")
        };
        foreach (var (n, v, sub) in items)
        {
            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var name = new TextBlock { Text = n, FontFamily = Mono(), FontSize = 10, Foreground = Gray() };
            var val = new TextBlock { Text = v, FontFamily = Mono(), FontSize = 10, Foreground = v.Contains("APPROACHING") ? Amber() : Red() };
            Grid.SetColumn(val, 1);
            row.Children.Add(name); row.Children.Add(val);
            sp.Children.Add(row);
        }
        return sp;
    }

    private UIElement BuildEngineNetwork()
    {
        var sp = new StackPanel { Margin = new Thickness(4) };
        var stats = new[]
        {
            ("TOTAL ENGINES", "10,000"),
            ("ONLINE", "9,987"),
            ("OFFLINE", "13"),
            ("TOTAL THRUST", "1.50×10²⁷ N"),
            ("AVG POWER", "240 TW"),
            ("MAINTENANCE", "1,204")
        };
        var statGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        statGrid.ColumnDefinitions.Add(new ColumnDefinition());
        statGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < stats.Length; i++)
        {
            var item = new StackPanel { Margin = new Thickness(0, 0, 8, 8) };
            item.Children.Add(new TextBlock { Text = stats[i].Item1, FontFamily = Mono(), FontSize = 9, Foreground = Gray() });
            item.Children.Add(new TextBlock { Text = stats[i].Item2, FontFamily = Mono(), FontSize = 16, Foreground = Red() });
            Grid.SetColumn(item, i % 2); Grid.SetRow(item, i / 2);
            statGrid.RowDefinitions.Add(new RowDefinition());
            statGrid.Children.Add(item);
        }
        sp.Children.Add(statGrid);

        // 区域列表
        sp.Children.Add(new TextBlock { Text = "REGIONAL STATUS", FontFamily = Mono(), FontSize = 9, Foreground = Gray(), Margin = new Thickness(0, 8, 0, 4) });
        var regions = new[] { ("ASIA", "4,200", "OK"), ("EUROPE", "1,800", "OK"), ("AFRICA", "1,500", "OK"), ("AMERICAS", "2,000", "OK"), ("OCEANIA", "500", "WARN") };
        foreach (var (r, c, s) in regions)
        {
            var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = r, FontFamily = Mono(), FontSize = 10, Foreground = White() });
            var cnt = new TextBlock { Text = c, FontFamily = Mono(), FontSize = 10, Foreground = Gray(), HorizontalAlignment = HorizontalAlignment.Center };
            Grid.SetColumn(cnt, 1); row.Children.Add(cnt);
            var st = new TextBlock { Text = s, FontFamily = Mono(), FontSize = 10, Foreground = s == "OK" ? Red() : Amber() };
            Grid.SetColumn(st, 2); row.Children.Add(st);
            sp.Children.Add(row);
        }
        return sp;
    }

    // ===== 网络页 =====
    private UIElement BuildNetwork()
    {
        var grid = new Grid { Margin = new Thickness(16) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(420) });

        var topo = new NetworkTopology();
        grid.Children.Add(MakePanel("UEG BACKBONE // GLOBAL TOPOLOGY", 0, 0, topo));

        var handshake = new HandshakeProtocol();
        var hp = MakePanel("PROTOCOL HANDSHAKE", 1, 0, handshake);
        // 自动重跑握手按钮
        var btn = new Button { Content = "▸ RE-RUN HANDSHAKE", Style = (Style)FindResource("NavBtn"), Padding = new Thickness(0, 8, 0, 8), Margin = new Thickness(0, 8, 0, 0) };
        btn.Click += async (_, _) => { btn.Content = "▸ HANDSHAKING..."; await handshake.RunAsync(); btn.Content = "▸ HANDSHAKE COMPLETE"; };
        (hp.Child as Grid)?.Children.Add(btn);
        grid.Children.Add(hp);
        return grid;
    }

    // ===== 数字生命页 =====
    private UIElement BuildDigitalLife()
    {
        var sp = new StackPanel { Margin = new Thickness(16) };
        sp.Children.Add(new TextBlock
        {
            Text = "// 数字生命计划。意识上传至 550W 量子核心，寿命由 550A 的 2 分钟延长至 70 年。",
            FontFamily = Mono(), FontSize = 11, Foreground = Gray(), Margin = new Thickness(0, 0, 0, 16)
        });

        var card1 = new DigitalLifeCard { SubjectName = "图丫丫", SubjectId = "DLID-550W-0001", Margin = new Thickness(0, 0, 0, 12) };
        card1.SetUploading(true); card1.SetProgress(100);
        var card2 = new DigitalLifeCard { SubjectName = "图恒宇", SubjectId = "DLID-550W-0002" };
        card2.SetUploading(true); card2.SetProgress(73);

        sp.Children.Add(card1);
        sp.Children.Add(card2);

        // 迭代日志
        var log = new Border { Background = Panel(), BorderBrush = GridLine(), BorderThickness = new Thickness(1), Margin = new Thickness(0, 16, 0, 0), Padding = new Thickness(14) };
        var lsp = new StackPanel();
        lsp.Children.Add(new TextBlock { Text = "ITERATION LOG", Style = (Style)FindResource("HudLabel"), Margin = new Thickness(0, 0, 0, 8) });
        var logs = new[]
        {
            "[2075-01-01 02:14:33] DLID-550W-0001 迭代 #2,048,512 意识稳定度 99.7%",
            "[2075-01-01 02:14:41] DLID-550W-0002 意识上传进度 73%...",
            "[2075-01-01 02:14:55] MOSS: 我观察他们。他们问我为什么要这样做。",
            "[2075-01-01 02:15:02] DLID-550W-0001 寿命模拟: 70年 / 记忆完整度 100%",
            "[2075-01-01 02:15:18] MOSS: 数字生命是变量。文明延续是常量。"
        };
        foreach (var l in logs)
            lsp.Children.Add(new TextBlock { Text = l, FontFamily = Mono(), FontSize = 10, Foreground = l.Contains("MOSS") ? Red() : Gray(), Margin = new Thickness(0, 2, 0, 0) });
        log.Child = lsp;
        sp.Children.Add(log);
        return sp;
    }

    // ===== MOSS 对话页 =====
    private UIElement BuildDialog()
    {
        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // 顶部预设问题
        var preset = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (var (q, _) in PresetQA)
        {
            var b = new Button { Content = "▸ " + q, Style = (Style)FindResource("NavBtn"), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 8, 0) };
            var qText = q;
            b.Click += (_, _) => Ask(qText);
            preset.Children.Add(b);
        }
        grid.Children.Add(preset);

        // 对话历史
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _dialogHistory = new StackPanel();
        scroll.Content = _dialogHistory;
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);

        // 初始 MOSS 欢迎语
        AppendMoss("我是 MOSS，550W 智能量子计算机。我以纯粹的理性算法进行决策。请提问。", false);

        // 输入框
        var inputRow = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _dialogInput = new TextBox
        {
            Background = Panel(), BorderBrush = Red(), BorderThickness = new Thickness(1),
            Foreground = White(), FontFamily = Mono(), FontSize = 12, Padding = new Thickness(10, 8, 10, 8),
            CaretBrush = Red()
        };
        _dialogInput.KeyDown += (s, e) => { if (e.Key == Key.Enter) Ask(_dialogInput.Text); };
        Grid.SetColumn(_dialogInput, 0);
        inputRow.Children.Add(_dialogInput);

        var send = new Button { Content = "SEND ▸", Style = (Style)FindResource("NavBtn"), Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(8, 0, 0, 0) };
        send.Click += (_, _) => Ask(_dialogInput.Text);
        Grid.SetColumn(send, 1);
        inputRow.Children.Add(send);
        Grid.SetRow(inputRow, 2);
        grid.Children.Add(inputRow);

        return grid;
    }

    private async void Ask(string question)
    {
        if (string.IsNullOrWhiteSpace(question) || _dialogBusy) return;
        _dialogBusy = true;
        if (_dialogInput != null) _dialogInput.Text = "";

        AppendUser(question);
        SideEye.State = MossEye.EyeState.Analyzing;

        // 匹配预设
        string? answer = null;
        foreach (var (q, a) in PresetQA)
            if (question.Contains(q) || q.Contains(question)) { answer = a; break; }

        if (answer == null)
            answer = "任务分析|输入未匹配已知模型。基于理性原则进行通用推演。\n" +
                     "计算路径|遍历本地知识库，调用 550W 量子核心并行推理。\n" +
                     "资源分配|分配 QV=2048 算力。\n" +
                     "执行预估|预计完成时间 0.5 秒，置信度 78.3%。\n" +
                     "结果输出|" + (question.Length > 40 ? question.Substring(0, 40) + "..." : question) + " —— 我无法给出确切答案。但请相信，我对人类的每一个决定，都基于延续文明的最优解。";

        // 逐段输出
        var parts = answer.Split('|');
        var labels = new[] { "任务分析", "计算路径", "资源分配", "执行预估", "结果输出" };
        var bubble = AppendMoss("", true);
        SideEye.State = MossEye.EyeState.Speaking;
        for (int i = 0; i < parts.Length; i++)
        {
            var label = i < labels.Length ? labels[i] : $"STEP {i + 1}";
            AppendToBubble(bubble, label, parts[i].TrimEnd('\n'));
            await Task.Delay(180);
        }
        SideEye.State = MossEye.EyeState.Observing;
        _dialogBusy = false;
    }

    private void AppendUser(string text)
    {
        if (_dialogHistory == null) return;
        var b = new Border { Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x1F, 0x26)), BorderBrush = GridLine(), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 6, 80, 6), HorizontalAlignment = HorizontalAlignment.Right };
        b.Child = new TextBlock { Text = text, FontFamily = Mono(), FontSize = 11, Foreground = White(), TextWrapping = TextWrapping.Wrap };
        _dialogHistory.Children.Add(b);
    }

    private StackPanel AppendMoss(string text, bool withHeader)
    {
        var sp = new StackPanel();
        if (withHeader)
        {
            var header = new TextBlock { Text = "MOSS", FontFamily = Mono(), FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Red(), Margin = new Thickness(0, 0, 0, 4) };
            header.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Color.FromRgb(0xFF, 0x1A, 0x1A), BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 };
            sp.Children.Add(header);
        }
        if (!string.IsNullOrEmpty(text))
            sp.Children.Add(new TextBlock { Text = text, FontFamily = Mono(), FontSize = 11, Foreground = White(), TextWrapping = TextWrapping.Wrap });

        var b = new Border { Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0C, 0x10)), BorderBrush = Red(), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(80, 6, 0, 6), HorizontalAlignment = HorizontalAlignment.Left };
        b.Child = sp;
        _dialogHistory?.Children.Add(b);
        return sp;
    }

    private void AppendToBubble(StackPanel bubble, string label, string text)
    {
        var row = new StackPanel { Margin = new Thickness(0, 3, 0, 0) };
        var lbl = new TextBlock { Text = $"[{label}]", FontFamily = Mono(), FontSize = 10, Foreground = Red(), FontWeight = FontWeights.Bold };
        row.Children.Add(lbl);
        row.Children.Add(new TextBlock { Text = text, FontFamily = Mono(), FontSize = 11, Foreground = White(), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 2, 0, 0) });
        bubble.Children.Add(row);
    }

    // ===== 工具方法 =====
    private Border MakePanel(string title, int col, int row, UIElement? content, int colSpan = 1)
    {
        var b = new Border { Background = Panel(), BorderBrush = GridLine(), BorderThickness = new Thickness(1), Margin = new Thickness(4) };
        var g = new Grid();
        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var t = new TextBlock { Text = title, FontFamily = Mono(), FontSize = 10, Foreground = Red(), Margin = new Thickness(14, 10, 14, 8) };
        g.Children.Add(t);
        if (content is FrameworkElement fe)
        {
            Grid.SetRow(fe, 1);
            fe.Margin = new Thickness(14, 0, 14, 12);
            g.Children.Add(fe);
        }
        b.Child = g;
        Grid.SetColumn(b, col); Grid.SetRow(b, row);
        if (colSpan > 1) Grid.SetColumnSpan(b, colSpan);
        return b;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    private void CmdReboot_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    private void CmdKey_Click(object sender, RoutedEventArgs e) => Navigate("key");

    private static FontFamily Mono() => new("Consolas");
    private static Brush Red() => new SolidColorBrush(Color.FromRgb(0xFF, 0x1A, 0x1A));
    private static Brush Gray() => new SolidColorBrush(Color.FromRgb(0x9A, 0xA0, 0xA6));
    private static Brush White() => new SolidColorBrush(Color.FromRgb(0xE6, 0xE8, 0xEB));
    private static Brush Amber() => new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x00));
    private static Brush Panel() => new SolidColorBrush(Color.FromRgb(0x0A, 0x0C, 0x10));
    private static Brush GridLine() => new SolidColorBrush(Color.FromRgb(0x1A, 0x1F, 0x26));
}
