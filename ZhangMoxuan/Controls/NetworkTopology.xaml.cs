using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ZhangMoxuan.Controls;

/// <summary>
/// 全球网络拓扑可视化控件：UEG 主干网络全球分布。
/// 绘制点阵世界地图、3 个根服务器（红色脉冲）、10+ UEG 计算节点、
/// 拓扑连接线与流动数据包动画，并提供右下角节点/带宽/延迟统计。
/// </summary>
public partial class NetworkTopology : UserControl
{
    private record Node(string Name, double Nx, double Ny, bool IsRoot);
    private record Link(int From, int To);

    private class Packet
    {
        public int LinkIndex;
        public double Progress;
        public double Speed;
        public Ellipse Dot = null!;
    }

    private readonly List<Node> _nodes = new();
    private readonly List<Link> _links = new();
    private readonly List<Packet> _packets = new();
    private readonly List<Line> _linkLines = new();
    private readonly List<Ellipse> _nodeDots = new();
    private readonly List<Ellipse> _rootRings = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly DispatcherTimer _statTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Random _rng = new(1337);
    private int _tick;

    // 简化大陆轮廓（归一化坐标），用于点阵世界地图背景
    private static readonly (double x, double y, double w, double h)[] Continents =
    {
        // 北美
        (0.10, 0.12, 0.06, 0.08),
        (0.16, 0.14, 0.12, 0.10),
        (0.22, 0.22, 0.10, 0.10),
        (0.18, 0.30, 0.05, 0.06),
        // 南美
        (0.25, 0.42, 0.06, 0.08),
        (0.27, 0.48, 0.05, 0.10),
        (0.28, 0.56, 0.03, 0.10),
        // 欧洲
        (0.46, 0.12, 0.05, 0.06),
        (0.48, 0.18, 0.07, 0.07),
        // 非洲
        (0.48, 0.28, 0.09, 0.08),
        (0.50, 0.34, 0.07, 0.10),
        (0.51, 0.44, 0.05, 0.07),
        // 亚洲
        (0.55, 0.10, 0.12, 0.08),
        (0.60, 0.18, 0.10, 0.08),
        (0.70, 0.20, 0.13, 0.12),
        (0.87, 0.27, 0.04, 0.06),
        (0.66, 0.32, 0.05, 0.07),
        (0.72, 0.36, 0.06, 0.06),
        // 澳洲
        (0.80, 0.52, 0.08, 0.06),
    };

    public NetworkTopology()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _timer.Tick += OnTick;
        _statTimer.Tick += OnStatTick;
        InitNodes();
        BuildLinks();
        MapCanvas.SizeChanged += (_, _) => { if (IsLoaded) Build(); };
    }

    private void InitNodes()
    {
        // 3 个根服务器（红色脉冲）
        _nodes.Add(new Node("BEIJING-ROOT", 0.823, 0.278, true));
        _nodes.Add(new Node("TOKYO-ROOT", 0.888, 0.302, true));
        _nodes.Add(new Node("WASHINGTON-ROOT", 0.286, 0.284, true));
        // 14 个 UEG 计算节点
        _nodes.Add(new Node("LONDON", 0.500, 0.214, false));
        _nodes.Add(new Node("PARIS", 0.507, 0.228, false));
        _nodes.Add(new Node("MOSCOW", 0.604, 0.190, false));
        _nodes.Add(new Node("DUBAI", 0.654, 0.360, false));
        _nodes.Add(new Node("MUMBAI", 0.703, 0.394, false));
        _nodes.Add(new Node("SINGAPORE", 0.788, 0.493, false));
        _nodes.Add(new Node("SYDNEY", 0.920, 0.688, false));
        _nodes.Add(new Node("SAO-PAULO", 0.371, 0.631, false));
        _nodes.Add(new Node("CAIRO", 0.587, 0.333, false));
        _nodes.Add(new Node("JOHANNESBURG", 0.578, 0.646, false));
        _nodes.Add(new Node("NEW-YORK", 0.294, 0.274, false));
        _nodes.Add(new Node("LOS-ANGELES", 0.172, 0.311, false));
        _nodes.Add(new Node("BERLIN", 0.537, 0.208, false));
        _nodes.Add(new Node("SEOUL", 0.853, 0.291, false));
    }

    private void BuildLinks()
    {
        _links.Clear();
        // 根服务器互联（三角主干）
        _links.Add(new Link(0, 1)); // 北京—东京
        _links.Add(new Link(1, 2)); // 东京—华盛顿
        _links.Add(new Link(2, 0)); // 华盛顿—北京
        // 各 UEG 节点连接到最近的根服务器
        for (int i = 3; i < _nodes.Count; i++)
            _links.Add(new Link(i, NearestRoot(i)));
        // 额外区域主干链路，构成更密集的拓扑
        _links.Add(new Link(3, 4));   // London—Paris
        _links.Add(new Link(3, 15));  // London—Berlin
        _links.Add(new Link(5, 11));  // Moscow—Cairo
        _links.Add(new Link(7, 8));   // Mumbai—Singapore
        _links.Add(new Link(8, 9));   // Singapore—Sydney
        _links.Add(new Link(16, 1));  // Seoul—Tokyo
        _links.Add(new Link(14, 13)); // LA—NewYork
        _links.Add(new Link(10, 13)); // SaoPaulo—NewYork
    }

    private int NearestRoot(int i)
    {
        var n = _nodes[i];
        double best = double.MaxValue;
        int bestIdx = 0;
        for (int r = 0; r < 3; r++)
        {
            var root = _nodes[r];
            var d = (n.Nx - root.Nx) * (n.Nx - root.Nx) + (n.Ny - root.Ny) * (n.Ny - root.Ny);
            if (d < best) { best = d; bestIdx = r; }
        }
        return bestIdx;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Build();
        _timer.Start();
        _statTimer.Start();
        OnStatTick(null, EventArgs.Empty);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _statTimer.Stop();
    }

    private void Build()
    {
        MapCanvas.Children.Clear();
        _linkLines.Clear();
        _nodeDots.Clear();
        _rootRings.Clear();
        _packets.Clear();

        var w = MapCanvas.ActualWidth;
        var h = MapCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var redDim = (Brush)FindResource("RedDimBrush");
        var red = (Brush)FindResource("RedBrush");
        var redBright = (Brush)FindResource("RedBrightBrush");
        var gridBrush = (Brush)FindResource("GridBrush");
        var textBrush = (Brush)FindResource("TextBrush");
        var mono = (FontFamily)FindResource("MonoFont");

        // 背景网格线
        double gridStep = 40;
        for (double x = 0; x < w; x += gridStep)
        {
            MapCanvas.Children.Add(new Line
            {
                X1 = x, Y1 = 0, X2 = x, Y2 = h,
                Stroke = gridBrush, StrokeThickness = 0.5, Opacity = 0.5
            });
        }
        for (double y = 0; y < h; y += gridStep)
        {
            MapCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = y, X2 = w, Y2 = y,
                Stroke = gridBrush, StrokeThickness = 0.5, Opacity = 0.5
            });
        }

        // 大陆点阵（仅陆地绘制点，构成简化世界地图轮廓）
        double dotStep = 10;
        for (double gy = dotStep / 2; gy < h; gy += dotStep)
        {
            for (double gx = dotStep / 2; gx < w; gx += dotStep)
            {
                var nx = gx / w;
                var ny = gy / h;
                bool onLand = false;
                foreach (var c in Continents)
                {
                    if (nx >= c.x && nx <= c.x + c.w && ny >= c.y && ny <= c.y + c.h)
                    {
                        onLand = true;
                        break;
                    }
                }
                if (!onLand) continue;
                var dot = new Ellipse
                {
                    Width = 1.6, Height = 1.6,
                    Fill = redDim, Opacity = 0.5
                };
                Canvas.SetLeft(dot, gx - 0.8);
                Canvas.SetTop(dot, gy - 0.8);
                MapCanvas.Children.Add(dot);
            }
        }

        // 拓扑连接线
        foreach (var link in _links)
        {
            var a = _nodes[link.From];
            var b = _nodes[link.To];
            var line = new Line
            {
                X1 = a.Nx * w, Y1 = a.Ny * h,
                X2 = b.Nx * w, Y2 = b.Ny * h,
                Stroke = red, StrokeThickness = 0.7, Opacity = 0.3
            };
            MapCanvas.Children.Add(line);
            _linkLines.Add(line);
        }

        // 节点
        for (int i = 0; i < _nodes.Count; i++)
        {
            var n = _nodes[i];
            var px = n.Nx * w;
            var py = n.Ny * h;

            if (n.IsRoot)
            {
                // 脉冲外环（动画时扩展淡出）
                var ring = new Ellipse
                {
                    Width = 12, Height = 12,
                    Stroke = red, StrokeThickness = 1, Opacity = 0.6
                };
                Canvas.SetLeft(ring, px - 6);
                Canvas.SetTop(ring, py - 6);
                MapCanvas.Children.Add(ring);
                _rootRings.Add(ring);

                // 根节点实心点
                var dot = new Ellipse
                {
                    Width = 7, Height = 7,
                    Fill = red, Opacity = 1
                };
                Canvas.SetLeft(dot, px - 3.5);
                Canvas.SetTop(dot, py - 3.5);
                MapCanvas.Children.Add(dot);
                _nodeDots.Add(dot);

                // 根服务器标签
                var label = new TextBlock
                {
                    Text = n.Name,
                    FontFamily = mono, FontSize = 9,
                    Foreground = red
                };
                Canvas.SetLeft(label, px + 8);
                Canvas.SetTop(label, py - 15);
                MapCanvas.Children.Add(label);
            }
            else
            {
                var dot = new Ellipse
                {
                    Width = 4, Height = 4,
                    Fill = redDim, Opacity = 0.85
                };
                Canvas.SetLeft(dot, px - 2);
                Canvas.SetTop(dot, py - 2);
                MapCanvas.Children.Add(dot);
                _nodeDots.Add(dot);

                var label = new TextBlock
                {
                    Text = n.Name,
                    FontFamily = mono, FontSize = 8,
                    Foreground = textBrush, Opacity = 0.65
                };
                Canvas.SetLeft(label, px + 5);
                Canvas.SetTop(label, py + 4);
                MapCanvas.Children.Add(label);
            }
        }

        // 数据包（沿连接线流动的小红点）
        for (int i = 0; i < _links.Count; i++)
        {
            var pkt = new Packet
            {
                LinkIndex = i,
                Progress = _rng.NextDouble(),
                Speed = 0.008 + _rng.NextDouble() * 0.014,
                Dot = new Ellipse
                {
                    Width = 3, Height = 3,
                    Fill = redBright, Opacity = 0.95
                }
            };
            MapCanvas.Children.Add(pkt.Dot);
            _packets.Add(pkt);
            UpdatePacketPosition(pkt, w, h);
        }
    }

    private void UpdatePacketPosition(Packet pkt, double w, double h)
    {
        var link = _links[pkt.LinkIndex];
        var a = _nodes[link.From];
        var b = _nodes[link.To];
        var x = (a.Nx + (b.Nx - a.Nx) * pkt.Progress) * w;
        var y = (a.Ny + (b.Ny - a.Ny) * pkt.Progress) * h;
        Canvas.SetLeft(pkt.Dot, x - 1.5);
        Canvas.SetTop(pkt.Dot, y - 1.5);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var w = MapCanvas.ActualWidth;
        var h = MapCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;
        _tick++;

        // 数据包流动
        foreach (var pkt in _packets)
        {
            pkt.Progress += pkt.Speed;
            if (pkt.Progress >= 1.0)
            {
                pkt.Progress = 0;
                pkt.LinkIndex = _rng.Next(_links.Count);
                pkt.Speed = 0.008 + _rng.NextDouble() * 0.014;
            }
            UpdatePacketPosition(pkt, w, h);
        }

        // 根服务器脉冲（外环扩展淡出 + 实心点呼吸）
        for (int r = 0; r < _rootRings.Count && r < 3; r++)
        {
            var t = ((_tick * 0.025 + r * 0.4) % 1.0);
            var ring = _rootRings[r];
            var size = 8 + t * 24;
            ring.Width = size;
            ring.Height = size;
            ring.Opacity = (1 - t) * 0.7;
            var n = _nodes[r];
            var px = n.Nx * w;
            var py = n.Ny * h;
            Canvas.SetLeft(ring, px - size / 2);
            Canvas.SetTop(ring, py - size / 2);

            if (r < _nodeDots.Count)
                _nodeDots[r].Opacity = 0.7 + 0.3 * (0.5 + 0.5 * Math.Sin(_tick * 0.12 + r));
        }

        // 连接线随机闪烁
        if (_tick % 3 == 0)
        {
            for (int i = 0; i < _linkLines.Count; i++)
                _linkLines[i].Opacity = 0.18 + _rng.NextDouble() * 0.35;
        }

        // UEG 节点随机闪烁
        for (int i = 3; i < _nodeDots.Count; i++)
        {
            if (_rng.NextDouble() < 0.04)
                _nodeDots[i].Opacity = 0.4 + _rng.NextDouble() * 0.5;
        }
    }

    private void OnStatTick(object? sender, EventArgs e)
    {
        var total = _nodes.Count;
        var offline = _rng.Next(0, 3);
        var online = total - offline;
        NodesStat.Text = $"{online}/{total}";
        BandwidthStat.Text = $"{480 + _rng.Next(0, 80)} Tbps";
        LatencyStat.Text = $"{8 + _rng.Next(0, 14)} ms";
        ClockText.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
    }

    /// <summary>
    /// 刷新拓扑统计状态，供外部调用。
    /// </summary>
    public void RefreshStatus()
    {
        OnStatTick(null, EventArgs.Empty);
        for (int i = 0; i < _linkLines.Count; i++)
            _linkLines[i].Opacity = 0.2 + _rng.NextDouble() * 0.5;
    }
}
