using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ZhangMoxuan.Controls;

/// <summary>
/// UEG 雷达监控：距离环、方位刻度、扫描扇区与目标接触点。
/// 对应影片作战室屏幕的雷达监控界面，不使用世界地图点阵或斜向装饰线。
/// </summary>
public partial class NetworkTopology : UserControl
{
    private sealed record Contact(string Id, double Bearing, double Range, string Type, string Status);

    private static readonly Contact[] Contacts =
    {
        new("BEIJING-ROOT", 12, 0.58, "ROOT", "OK"),
        new("TOKYO-ROOT", 38, 0.72, "ROOT", "OK"),
        new("WASHINGTON-ROOT", 312, 0.81, "ROOT", "OK"),
        new("LONDON", 296, 0.36, "UEG", "OK"),
        new("PARIS", 301, 0.39, "UEG", "OK"),
        new("MOSCOW", 328, 0.25, "UEG", "OK"),
        new("DUBAI", 88, 0.44, "UEG", "OK"),
        new("MUMBAI", 102, 0.39, "UEG", "OK"),
        new("SINGAPORE", 132, 0.63, "UEG", "OK"),
        new("SYDNEY", 158, 0.78, "UEG", "WARN"),
        new("SAO-PAULO", 224, 0.71, "UEG", "OK"),
        new("CAIRO", 76, 0.30, "UEG", "OK"),
        new("JOHANNESBURG", 196, 0.53, "UEG", "OK"),
        new("NEW-YORK", 306, 0.66, "UEG", "OK"),
        new("LOS-ANGELES", 282, 0.83, "UEG", "OK"),
        new("BERLIN", 304, 0.33, "UEG", "OK"),
        new("SEOUL", 34, 0.61, "UEG", "OK"),
        new("UNKNOWN-01", 118, 0.88, "UNKNOWN", "TRACK"),
        new("UNKNOWN-02", 252, 0.91, "UNKNOWN", "TRACK"),
    };

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly DispatcherTimer _statTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Random _rng = new(2075);
    private RotateTransform _sweepRotate = null!;
    private double _sweepAngle;
    private int _tick;

    public NetworkTopology()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Build();
            _timer.Start();
            _statTimer.Start();
            UpdateStats();
        };
        Unloaded += (_, _) =>
        {
            _timer.Stop();
            _statTimer.Stop();
        };
        _timer.Tick += (_, _) => Tick();
        _statTimer.Tick += (_, _) => UpdateStats();
        RadarCanvas.SizeChanged += (_, _) => { if (IsLoaded) Build(); };
    }

    private void Build()
    {
        RadarCanvas.Children.Clear();
        var w = RadarCanvas.ActualWidth;
        var h = RadarCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;

        var cx = w / 2;
        var cy = h / 2;
        var radius = Math.Min(w, h) * 0.42;
        var grid = (Brush)FindResource("GridBrush");
        var bright = (Brush)FindResource("GridBrightBrush");
        var dim = (Brush)FindResource("TextTertiaryBrush");
        var red = (Brush)FindResource("RedBrush");
        var amber = (Brush)FindResource("AmberBrush");
        var green = (Brush)FindResource("GreenBrush");
        var white = (Brush)FindResource("TextPrimaryBrush");
        var mono = (FontFamily)FindResource("CondensedFont");

        // 背景细网格（正交）
        for (double x = cx % 40; x < w; x += 40)
            RadarCanvas.Children.Add(new Line { X1 = x, Y1 = 0, X2 = x, Y2 = h, Stroke = grid, StrokeThickness = 0.4, Opacity = 0.35 });
        for (double y = cy % 40; y < h; y += 40)
            RadarCanvas.Children.Add(new Line { X1 = 0, Y1 = y, X2 = w, Y2 = y, Stroke = grid, StrokeThickness = 0.4, Opacity = 0.35 });

        // 距离环
        for (int i = 1; i <= 4; i++)
        {
            var r = radius * i / 4;
            RadarCanvas.Children.Add(new Ellipse
            {
                Width = r * 2, Height = r * 2,
                Stroke = i == 4 ? bright : grid,
                StrokeThickness = i == 4 ? 1.1 : 0.7,
                Opacity = i == 4 ? 0.85 : 0.5
            });
            Canvas.SetLeft(RadarCanvas.Children[^1] as UIElement, cx - r);
            Canvas.SetTop(RadarCanvas.Children[^1] as UIElement, cy - r);
            var lbl = new TextBlock { Text = $"{i * 20:00} KM", FontFamily = mono, FontSize = 7, Foreground = dim };
            Canvas.SetLeft(lbl, cx + 4);
            Canvas.SetTop(lbl, cy - r + 2);
            RadarCanvas.Children.Add(lbl);
        }

        // 十字准线
        RadarCanvas.Children.Add(new Line { X1 = cx, Y1 = cy - radius, X2 = cx, Y2 = cy + radius, Stroke = grid, StrokeThickness = 0.8, Opacity = 0.7 });
        RadarCanvas.Children.Add(new Line { X1 = cx - radius, Y1 = cy, X2 = cx + radius, Y2 = cy, Stroke = grid, StrokeThickness = 0.8, Opacity = 0.7 });

        // 方位刻度
        for (int deg = 0; deg < 360; deg += 10)
        {
            var rad = (deg - 90) * Math.PI / 180;
            var major = deg % 30 == 0;
            var r1 = radius - (major ? 9 : 4);
            var r2 = radius;
            RadarCanvas.Children.Add(new Line
            {
                X1 = cx + r1 * Math.Cos(rad), Y1 = cy + r1 * Math.Sin(rad),
                X2 = cx + r2 * Math.Cos(rad), Y2 = cy + r2 * Math.Sin(rad),
                Stroke = major ? bright : grid,
                StrokeThickness = major ? 1 : 0.5,
                Opacity = major ? 0.9 : 0.55
            });
            if (major)
            {
                var label = new TextBlock
                {
                    Text = $"{deg:000}",
                    FontFamily = mono,
                    FontSize = 7,
                    Foreground = dim
                };
                var lr = radius + 14;
                Canvas.SetLeft(label, cx + lr * Math.Cos(rad) - 8);
                Canvas.SetTop(label, cy + lr * Math.Sin(rad) - 6);
                RadarCanvas.Children.Add(label);
            }
        }

        // 扫描扇区
        var sector = BuildSweepSector(cx, cy, radius);
        sector.Fill = new SolidColorBrush(Color.FromArgb(28, 240, 74, 74));
        _sweepRotate = new RotateTransform(_sweepAngle, cx, cy);
        sector.RenderTransform = _sweepRotate;
        RadarCanvas.Children.Add(sector);
        var sweepLine = new Line
        {
            X1 = cx, Y1 = cy, X2 = cx, Y2 = cy - radius,
            Stroke = red, StrokeThickness = 1.2, Opacity = 0.9,
            RenderTransform = _sweepRotate
        };
        RadarCanvas.Children.Add(sweepLine);

        // 接触点
        var usedLabels = new List<Rect>();
        foreach (var c in Contacts)
        {
            var rad = (c.Bearing - 90) * Math.PI / 180;
            var x = cx + radius * c.Range * Math.Cos(rad);
            var y = cy + radius * c.Range * Math.Sin(rad);
            var color = c.Type switch
            {
                "ROOT" => red,
                "UNKNOWN" => amber,
                _ => c.Status == "WARN" ? amber : white
            };
            var size = c.Type == "ROOT" ? 7.0 : 4.5;
            var dot = new Rectangle
            {
                Width = size, Height = size,
                Fill = color,
                Opacity = c.Type == "UNKNOWN" ? 0.8 : 0.95
            };
            Canvas.SetLeft(dot, x - size / 2);
            Canvas.SetTop(dot, y - size / 2);
            RadarCanvas.Children.Add(dot);

            if (c.Type == "ROOT")
            {
                RadarCanvas.Children.Add(new Ellipse
                {
                    Width = 16, Height = 16,
                    Stroke = red, StrokeThickness = 0.8, Opacity = 0.55
                });
                Canvas.SetLeft(RadarCanvas.Children[^1] as UIElement, x - 8);
                Canvas.SetTop(RadarCanvas.Children[^1] as UIElement, y - 8);
            }

            var showLabel = c.Type == "ROOT" || c.Id is "LONDON" or "MOSCOW" or "SINGAPORE"
                or "SYDNEY" or "SAO-PAULO" or "LOS-ANGELES" or "NEW-YORK" or "JOHANNESBURG";
            if (!showLabel) continue;

            var label = new TextBlock
            {
                Text = c.Id,
                FontFamily = mono,
                FontSize = c.Type == "ROOT" ? 8 : 7,
                Foreground = color,
                Opacity = c.Type == "ROOT" ? 1 : 0.8
            };
            var lw = c.Id.Length * (c.Type == "ROOT" ? 4.6 : 4.0) + 4;
            var lh = 10.0;
            if (c.Type == "ROOT")
            {
                Canvas.SetLeft(label, x + 8);
                Canvas.SetTop(label, y - 13);
                RadarCanvas.Children.Add(label);
                usedLabels.Add(new Rect(x + 8, y - 13, lw, lh));
                continue;
            }

            var candidates = new[]
            {
                new Point(x + 7, y - 11),
                new Point(x - lw - 7, y - 11),
                new Point(x + 7, y + 4),
                new Point(x - lw - 7, y + 4),
            };
            foreach (var pt in candidates)
            {
                var rect = new Rect(pt.X, pt.Y, lw, lh);
                if (rect.Left < 2 || rect.Top < 2 || rect.Right > w - 2 || rect.Bottom > h - 2) continue;
                if (usedLabels.Any(r => r.IntersectsWith(rect))) continue;
                usedLabels.Add(rect);
                Canvas.SetLeft(label, pt.X);
                Canvas.SetTop(label, pt.Y);
                RadarCanvas.Children.Add(label);
                break;
            }
        }

        // 中心
        RadarCanvas.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = red, Opacity = 0.9 });
        Canvas.SetLeft(RadarCanvas.Children[^1] as UIElement, cx - 4);
        Canvas.SetTop(RadarCanvas.Children[^1] as UIElement, cy - 4);

        // 左上角状态
        var status = new StackPanel();
        status.Children.Add(new TextBlock { Text = "RADAR // SECTOR 07", FontFamily = mono, FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = white });
        status.Children.Add(new TextBlock { Text = "ACTIVE TRACKING · 19 CONTACTS", FontFamily = mono, FontSize = 7, Foreground = dim, Margin = new Thickness(0, 2, 0, 0) });
        Canvas.SetLeft(status, 12);
        Canvas.SetTop(status, 10);
        RadarCanvas.Children.Add(status);

        // 图例
        var legend = new StackPanel { Orientation = Orientation.Horizontal };
        legend.Children.Add(LegendItem(red, "ROOT SERVER"));
        legend.Children.Add(LegendItem(white, "UEG NODE"));
        legend.Children.Add(LegendItem(amber, "UNKNOWN"));
        Canvas.SetLeft(legend, 12);
        Canvas.SetTop(legend, h - 26);
        RadarCanvas.Children.Add(legend);
    }

    private static Polygon BuildSweepSector(double cx, double cy, double radius)
    {
        var points = new PointCollection { new Point(cx, cy) };
        for (int a = -22; a <= 0; a += 2)
        {
            var rad = (a - 90) * Math.PI / 180;
            points.Add(new Point(cx + radius * Math.Cos(rad), cy + radius * Math.Sin(rad)));
        }
        return new Polygon { Points = points, IsHitTestVisible = false };
    }

    private FrameworkElement LegendItem(Brush brush, string text)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 12, 0) };
        sp.Children.Add(new Rectangle { Width = 6, Height = 6, Fill = brush, Margin = new Thickness(0, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center });
        sp.Children.Add(new TextBlock { Text = text, FontFamily = (FontFamily)FindResource("CondensedFont"), FontSize = 7, Foreground = (Brush)FindResource("TextTertiaryBrush"), VerticalAlignment = VerticalAlignment.Center });
        return sp;
    }

    private void Tick()
    {
        _sweepAngle = (_sweepAngle + 1.6) % 360;
        if (_sweepRotate != null) _sweepRotate.Angle = _sweepAngle;
        _tick++;
    }

    private void UpdateStats()
    {
        var total = Contacts.Length;
        var offline = _rng.Next(0, 3);
        NodesStat.Text = $"{total - offline}/{total}";
        BandwidthStat.Text = $"{480 + _rng.Next(0, 80)} Tbps";
        LatencyStat.Text = $"{8 + _rng.Next(0, 14)} ms";
    }

    public void RefreshStatus() => UpdateStats();
}
