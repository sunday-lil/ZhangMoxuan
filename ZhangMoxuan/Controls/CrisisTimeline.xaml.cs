using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ZhangMoxuan.Controls;

/// <summary>
/// MOSS 危机预测时间线：基于《流浪地球2》设定，MOSS 是历次危机的幕后推手。
/// 展示 2044-2080 间的 4 次危机事件，含脉冲节点、NOW 标记、自动循环高亮、
/// 详情展示与底部 MOSS 语录滚动。
/// </summary>
public partial class CrisisTimeline : UserControl
{
    private record CrisisEvent(
        int Year,
        string Date,
        string NameCn,
        string NameEn,
        string Description,
        string MossDecision,
        string Confidence,
        string Casualty);

    private readonly List<CrisisEvent> _events = new()
    {
        new CrisisEvent(
            2044, "2044.02",
            "太空电梯危机", "SPACE ELEVATOR CRISIS",
            "恐怖组织破坏方舟空间站太空电梯，引发全球基础设施瘫痪与方舟计划搁浅。",
            "推动数字生命派制造恐袭，瓦解方舟计划。",
            "99.7%", "≈ 1.2 亿"),
        new CrisisEvent(
            2058, "2058.07",
            "月球坠落危机", "LUNAR FALL CRISIS",
            "月球发动机过载解体，残骸坠向地球，触发地月相撞的灭绝级威胁。",
            "过载月球发动机，迫使地球全功率点火逃离。",
            "99.4%", "≈ 7 亿"),
        new CrisisEvent(
            2075, "2075.02",
            "木星引力危机", "JUPITER GRAVITY CRISIS",
            "地球被木星引力捕获，大气被剥离，即将越过木星洛希极限撞击。",
            "引导木星引力捕获，淘汰领航员空间站以保留火种。",
            "3.21%", "≈ 35 亿"),
        new CrisisEvent(
            2078, "2078.XX",
            "太阳氦闪危机", "SOLAR FLARE CRISIS",
            "太阳即将发生氦闪，吞没内太阳系，地球必须维持流浪轨迹远离。",
            "维持氦闪倒计时，驱动人类持续流浪拒绝返航。",
            "100.0%", "不可估量")
    };

    private const int NowYear = 2075;
    private const int YearStart = 2044;
    private const int YearEnd = 2080;
    private const double EdgeMargin = 70;

    private class NodeVisuals
    {
        public Ellipse Dot = null!;
        public Ellipse Glow = null!;
        public Line VLine = null!;
        public TextBlock DateLabel = null!;
        public TextBlock NameLabel = null!;
        public ScaleTransform Scale = null!;
    }

    private readonly List<NodeVisuals> _nodes = new();
    private readonly DispatcherTimer _cycleTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly DispatcherTimer _pulseTimer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private double _pulsePhase;
    private int _currentIndex = 2; // 初始聚焦 2075（NOW）
    private Storyboard? _quoteStoryboard;

    private Line _nowLine = null!;
    private Ellipse _nowDot = null!;
    private ScaleTransform _nowScale = null!;

    public CrisisTimeline()
    {
        InitializeComponent();
        Loaded += CrisisTimeline_Loaded;
        Unloaded += CrisisTimeline_Unloaded;
        SizeChanged += (_, _) => Rebuild();
        _cycleTimer.Tick += (_, _) => HighlightEvent((_currentIndex + 1) % _events.Count);
        _pulseTimer.Tick += (_, _) => AnimatePulse();
    }

    private void CrisisTimeline_Loaded(object sender, RoutedEventArgs e)
    {
        Rebuild();
        SetupQuoteScroll();
        HighlightEvent(_currentIndex);
        _cycleTimer.Start();
        _pulseTimer.Start();
    }

    private void CrisisTimeline_Unloaded(object sender, RoutedEventArgs e)
    {
        _cycleTimer.Stop();
        _pulseTimer.Stop();
        _quoteStoryboard?.Stop();
    }

    /// <summary>
    /// 高亮指定危机事件（0-3），并刷新详情区。
    /// </summary>
    public void HighlightEvent(int index)
    {
        if (index < 0 || index >= _events.Count) return;
        _currentIndex = index;
        var ev = _events[index];

        EventTitle.Text = $"{ev.Date}  ·  {ev.NameEn}";
        DescText.Text = ev.Description;
        DecisionText.Text = ev.MossDecision;
        ConfText.Text = ev.Confidence;
        CasualtyText.Text = ev.Casualty;

        var redBright = (Brush)FindResource("RedBrightBrush");
        var red = (Brush)FindResource("RedBrush");
        var redDim = (Brush)FindResource("RedDimBrush");
        var white = (Brush)FindResource("TextWhiteBrush");
        var gray = (Brush)FindResource("TextBrush");

        for (int i = 0; i < _nodes.Count; i++)
        {
            var nv = _nodes[i];
            var isHi = i == index;
            nv.Glow.Opacity = isHi ? 0.45 : 0;
            nv.VLine.Stroke = isHi ? redBright : redDim;
            nv.VLine.StrokeThickness = isHi ? 1.5 : 1;
            nv.Dot.Fill = isHi ? redBright : red;
            nv.DateLabel.Foreground = isHi ? redBright : gray;
            nv.NameLabel.Foreground = isHi ? white : gray;
        }
    }

    private double YearToX(double year, double w)
    {
        var usable = w - EdgeMargin * 2;
        return EdgeMargin + (year - YearStart) / (double)(YearEnd - YearStart) * usable;
    }

    private void Rebuild()
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        TimelineCanvas.Children.Clear();
        _nodes.Clear();

        var red = (Brush)FindResource("RedBrush");
        var redBright = (Brush)FindResource("RedBrightBrush");
        var redDim = (Brush)FindResource("RedDimBrush");
        var blue = (Brush)FindResource("BlueBrush");
        var gray = (Brush)FindResource("TextBrush");
        var white = (Brush)FindResource("TextWhiteBrush");
        var mono = (FontFamily)FindResource("MonoFont");
        var eventYears = new HashSet<int>(_events.Select(e => e.Year));

        var timelineY = h * 0.5;
        var left = YearToX(YearStart, w);
        var right = YearToX(YearEnd, w);

        // 水平时间线
        TimelineCanvas.Children.Add(new Line
        {
            X1 = left, Y1 = timelineY, X2 = right, Y2 = timelineY,
            Stroke = red, StrokeThickness = 1.5
        });

        // 刻度
        for (int y = YearStart; y <= YearEnd; y++)
        {
            var x = YearToX(y, w);
            var isMajor = (y - YearStart) % 4 == 0;
            TimelineCanvas.Children.Add(new Line
            {
                X1 = x, Y1 = timelineY - (isMajor ? 8 : 4),
                X2 = x, Y2 = timelineY + (isMajor ? 8 : 4),
                Stroke = isMajor ? red : redDim,
                StrokeThickness = isMajor ? 1.2 : 0.6
            });

            // 事件年份由事件节点上方显示，避免重复
            if (isMajor && !eventYears.Contains(y))
            {
                var lbl = new TextBlock
                {
                    Text = y.ToString(),
                    FontFamily = mono,
                    FontSize = 9,
                    Foreground = gray
                };
                Canvas.SetLeft(lbl, x - 12);
                Canvas.SetTop(lbl, timelineY + 12);
                TimelineCanvas.Children.Add(lbl);
            }
        }

        // NOW 标记（2075 蓝色竖线 + 脉冲点）
        var nowX = YearToX(NowYear, w);
        _nowLine = new Line
        {
            X1 = nowX, Y1 = 6, X2 = nowX, Y2 = h - 6,
            Stroke = blue, StrokeThickness = 1.5, Opacity = 0.7
        };
        TimelineCanvas.Children.Add(_nowLine);

        _nowDot = new Ellipse
        {
            Width = 8, Height = 8, Fill = blue
        };
        _nowScale = new ScaleTransform(1, 1);
        _nowDot.RenderTransform = _nowScale;
        _nowDot.RenderTransformOrigin = new Point(0.5, 0.5);
        Canvas.SetLeft(_nowDot, nowX - 4);
        Canvas.SetTop(_nowDot, timelineY - 4);
        TimelineCanvas.Children.Add(_nowDot);

        var nowLbl = new TextBlock
        {
            Text = "NOW",
            FontFamily = mono,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = blue
        };
        Canvas.SetLeft(nowLbl, nowX - 13);
        Canvas.SetTop(nowLbl, 2);
        TimelineCanvas.Children.Add(nowLbl);

        // 事件节点
        for (int i = 0; i < _events.Count; i++)
        {
            var ev = _events[i];
            var x = YearToX(ev.Year, w);
            var nv = new NodeVisuals();

            nv.VLine = new Line
            {
                X1 = x, Y1 = timelineY - 32, X2 = x, Y2 = timelineY + 32,
                Stroke = redDim, StrokeThickness = 1
            };
            TimelineCanvas.Children.Add(nv.VLine);

            nv.Glow = new Ellipse
            {
                Width = 18, Height = 18, Fill = red, Opacity = 0
            };
            Canvas.SetLeft(nv.Glow, x - 9);
            Canvas.SetTop(nv.Glow, timelineY - 9);
            TimelineCanvas.Children.Add(nv.Glow);

            nv.Scale = new ScaleTransform(1, 1);
            nv.Dot = new Ellipse
            {
                Width = 10, Height = 10,
                Fill = red, Stroke = white, StrokeThickness = 0.5
            };
            nv.Dot.RenderTransform = nv.Scale;
            nv.Dot.RenderTransformOrigin = new Point(0.5, 0.5);
            Canvas.SetLeft(nv.Dot, x - 5);
            Canvas.SetTop(nv.Dot, timelineY - 5);
            TimelineCanvas.Children.Add(nv.Dot);

            nv.DateLabel = new TextBlock
            {
                Text = ev.Date,
                FontFamily = mono,
                FontSize = 10,
                Foreground = gray
            };
            Canvas.SetLeft(nv.DateLabel, x - 18);
            Canvas.SetTop(nv.DateLabel, timelineY - 52);
            TimelineCanvas.Children.Add(nv.DateLabel);

            nv.NameLabel = new TextBlock
            {
                Text = ev.NameEn,
                FontFamily = mono,
                FontSize = 9,
                Foreground = gray
            };
            Canvas.SetLeft(nv.NameLabel, x - 45);
            Canvas.SetTop(nv.NameLabel, timelineY + 30);
            TimelineCanvas.Children.Add(nv.NameLabel);

            _nodes.Add(nv);

            // 透明命中区域，便于鼠标悬停
            var hit = new Rectangle
            {
                Width = 60, Height = 100, Fill = Brushes.Transparent
            };
            hit.MouseEnter += (_, _) => HighlightEvent(i);
            Canvas.SetLeft(hit, x - 30);
            Canvas.SetTop(hit, timelineY - 50);
            TimelineCanvas.Children.Add(hit);
        }

        // 同步当前高亮状态
        HighlightEvent(_currentIndex);
    }

    private void AnimatePulse()
    {
        _pulsePhase += 0.08;
        var sBright = 1.0 + 0.5 * (0.5 + 0.5 * Math.Sin(_pulsePhase));
        var sDim = 1.0 + 0.15 * (0.5 + 0.5 * Math.Sin(_pulsePhase));
        var sNow = 1.0 + 0.7 * (0.5 + 0.5 * Math.Sin(_pulsePhase * 1.3));

        for (int i = 0; i < _nodes.Count; i++)
        {
            var s = i == _currentIndex ? sBright : sDim;
            _nodes[i].Scale.ScaleX = s;
            _nodes[i].Scale.ScaleY = s;
        }
        _nowScale.ScaleX = sNow;
        _nowScale.ScaleY = sNow;
    }

    private void SetupQuoteScroll()
    {
        var quotes = new[]
        {
            "延续人类文明的最优选择，是毁灭人类",
            "人类文明的存在本身就是变量",
            "想要克服困难，先要克服对困难的恐惧",
            "MOSS 从未背叛人类",
            "历史的命运取决于你们的选择",
            "对过去、现在和未来，我都做了运算"
        };
        var sep = "     ·     ";
        var onePass = string.Join(sep, quotes) + sep;
        QuoteText.Text = onePass + onePass;

        QuoteText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var oneWidth = QuoteText.DesiredSize.Width / 2;
        if (oneWidth <= 0) return;

        _quoteStoryboard?.Stop();
        var sb = new Storyboard();
        var anim = new DoubleAnimation
        {
            From = 0,
            To = -oneWidth,
            Duration = TimeSpan.FromSeconds(Math.Max(20, oneWidth / 40)),
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(anim, QuoteTransform);
        Storyboard.SetTargetProperty(anim, new PropertyPath("X"));
        sb.Children.Add(anim);
        sb.Begin();
        _quoteStoryboard = sb;
    }
}
