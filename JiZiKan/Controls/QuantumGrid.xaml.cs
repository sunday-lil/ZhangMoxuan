using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace JiZiKan.Controls;

/// <summary>
/// 量子比特可视化：影片中"屏幕上流动的量子位状态可视化"。
/// 显示 8192 量子比特的网格，状态在 |0⟩、|1⟩、叠加态之间流动，并展示量子纠缠连接线。
/// </summary>
public partial class QuantumGrid : UserControl
{
    private class Qubit
    {
        public int X { get; init; }
        public int Y { get; init; }
        public Ellipse Dot { get; init; } = null!;
        public int State { get; set; }
    }

    private readonly List<Qubit> _qubits = new();
    private readonly List<Line> _links = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private int _cols = 32;
    private int _rows = 16;

    public QuantumGrid()
    {
        InitializeComponent();
        Loaded += (_, _) => Build();
        Unloaded += (_, _) => _timer.Stop();
        _timer.Tick += Tick;
    }

    private void Build()
    {
        Layer0.Children.Clear();
        Layer1.Children.Clear();
        Layer2.Children.Clear();
        _qubits.Clear();
        _links.Clear();

        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var margin = 8;
        var availW = w - margin * 2;
        var availH = h - margin * 2;
        var cell = Math.Min(availW / _cols, availH / _rows);
        var startX = (w - cell * _cols) / 2 + cell / 2;
        var startY = (h - cell * _rows) / 2 + cell / 2;

        var redDim = (Brush)FindResource("RedDimBrush");
        var red = (Brush)FindResource("RedBrush");

        // 量子比特点阵
        for (int y = 0; y < _rows; y++)
        for (int x = 0; x < _cols; x++)
        {
            var dot = new Ellipse
            {
                Width = 3, Height = 3,
                Fill = redDim,
                Opacity = 0.4
            };
            Canvas.SetLeft(dot, startX + x * cell - 1.5);
            Canvas.SetTop(dot, startY + y * cell - 1.5);
            Layer0.Children.Add(dot);
            _qubits.Add(new Qubit { X = x, Y = y, Dot = dot, State = 0 });
        }

        // 预生成纠缠连接线（稀疏）
        var rng = new Random(42);
        for (int i = 0; i < 40; i++)
        {
            var a = _qubits[rng.Next(_qubits.Count)];
            var b = _qubits[rng.Next(_qubits.Count)];
            if (a == b) continue;
            var line = new Line
            {
                X1 = startX + a.X * cell,
                Y1 = startY + a.Y * cell,
                X2 = startX + b.X * cell,
                Y2 = startY + b.Y * cell,
                Stroke = red,
                StrokeThickness = 0.5,
                Opacity = 0
            };
            Layer1.Children.Add(line);
            _links.Add(line);
        }

        _timer.Start();
    }

    private void Tick(object? sender, EventArgs e)
    {
        // 流动：随机一些比特改变状态，形成"波"
        var rng = Random.Shared;
        var redDim = (Brush)FindResource("RedDimBrush");
        var red = (Brush)FindResource("RedBrush");
        var redBright = (Brush)FindResource("RedBrightBrush");

        for (int i = 0; i < 60; i++)
        {
            var q = _qubits[rng.Next(_qubits.Count)];
            q.State = (q.State + 1) % 3;
            switch (q.State)
            {
                case 0:
                    q.Dot.Fill = redDim;
                    q.Dot.Opacity = 0.3 + rng.NextDouble() * 0.2;
                    break;
                case 1:
                    q.Dot.Fill = red;
                    q.Dot.Opacity = 0.5 + rng.NextDouble() * 0.3;
                    break;
                case 2:
                    q.Dot.Fill = redBright;
                    q.Dot.Opacity = 0.8 + rng.NextDouble() * 0.2;
                    break;
            }
        }

        // 连接线随机闪烁
        for (int i = 0; i < _links.Count; i++)
        {
            if (rng.NextDouble() < 0.1)
                _links[i].Opacity = rng.NextDouble() * 0.5;
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (IsLoaded) Build();
    }
}
