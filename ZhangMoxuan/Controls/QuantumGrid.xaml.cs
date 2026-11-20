using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ZhangMoxuan.Controls;

/// <summary>
/// 量子核心矩阵：正交排列的 8192 量子比特单元、行/列扫描线与状态热区。
/// 不使用斜向连线或点阵装饰，符合影片 UI 的“避免斜线边框”规范。
/// </summary>
public partial class QuantumGrid : UserControl
{
    private sealed class Cell
    {
        public Rectangle Rect = null!;
        public int State;
    }

    private readonly List<Cell> _cells = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Random _rng = new(8192);
    private int _cols = 96;
    private int _rows = 24;
    private double _cell = 6;
    private double _startX;
    private double _startY;
    private int _scanRow;

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
        _cells.Clear();

        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var marginX = 10.0;
        var marginY = 8.0;
        var availW = w - marginX * 2;
        var availH = h - marginY * 2;
        _cell = Math.Min(availW / _cols, availH / _rows);
        _startX = (w - _cell * _cols) / 2;
        _startY = (h - _cell * _rows) / 2;

        var dim = (Brush)FindResource("RedDimBrush");
        var red = (Brush)FindResource("RedBrush");
        var bright = (Brush)FindResource("RedBrightBrush");
        var amber = (Brush)FindResource("AmberBrush");
        var grid = (Brush)FindResource("GridBrush");

        // 正交分区线
        for (int x = 0; x <= _cols; x += 12)
        {
            var line = new Line
            {
                X1 = _startX + x * _cell, Y1 = _startY,
                X2 = _startX + x * _cell, Y2 = _startY + _rows * _cell,
                Stroke = grid, StrokeThickness = 0.5, Opacity = 0.55
            };
            Layer1.Children.Add(line);
        }
        for (int y = 0; y <= _rows; y += 8)
        {
            var line = new Line
            {
                X1 = _startX, Y1 = _startY + y * _cell,
                X2 = _startX + _cols * _cell, Y2 = _startY + y * _cell,
                Stroke = grid, StrokeThickness = 0.5, Opacity = 0.55
            };
            Layer1.Children.Add(line);
        }

        var pad = Math.Max(1, _cell * 0.18);
        for (int y = 0; y < _rows; y++)
        for (int x = 0; x < _cols; x++)
        {
            var rect = new Rectangle
            {
                Width = Math.Max(1.4, _cell - pad),
                Height = Math.Max(1.4, _cell - pad),
                Fill = dim,
                Opacity = 0.45
            };
            Canvas.SetLeft(rect, _startX + x * _cell + pad / 2);
            Canvas.SetTop(rect, _startY + y * _cell + pad / 2);
            Layer0.Children.Add(rect);
            _cells.Add(new Cell { Rect = rect, State = 0 });
        }

        // 列标题
        for (int x = 0; x < _cols; x += 12)
        {
            var label = new TextBlock
            {
                Text = $"{x / 12 + 1:00}",
                FontFamily = (FontFamily)FindResource("CondensedFont"),
                FontSize = 7,
                Foreground = (Brush)FindResource("TextTertiaryBrush")
            };
            Canvas.SetLeft(label, _startX + x * _cell + 1);
            Canvas.SetTop(label, _startY - 8);
            Layer2.Children.Add(label);
        }

        // 扫描行
        _scanRow = 0;
        _timer.Start();
    }

    private void Tick(object? sender, EventArgs e)
    {
        if (_cells.Count == 0) return;
        var dim = (Brush)FindResource("RedDimBrush");
        var red = (Brush)FindResource("RedBrush");
        var bright = (Brush)FindResource("RedBrightBrush");
        var amber = (Brush)FindResource("AmberBrush");
        // 少量比特随机翻转
        for (int i = 0; i < 42; i++)
        {
            var cell = _cells[_rng.Next(_cells.Count)];
            cell.State = (cell.State + 1) % 4;
            cell.Rect.Fill = cell.State switch
            {
                0 => dim,
                1 => red,
                2 => bright,
                _ => amber
            };
            cell.Rect.Opacity = cell.State == 0 ? 0.35 + _rng.NextDouble() * 0.2 : 0.65 + _rng.NextDouble() * 0.35;
        }

        // 正交扫描线：每帧下移一行
        if (_scanLine != null) Layer1.Children.Remove(_scanLine);
        _scanRow = (_scanRow + 1) % _rows;
        _scanLine = new Line
        {
            X1 = _startX,
            Y1 = _startY + _scanRow * _cell,
            X2 = _startX + _cols * _cell,
            Y2 = _startY + _scanRow * _cell,
            Stroke = bright,
            StrokeThickness = 1,
            Opacity = 0.65
        };
        Layer1.Children.Add(_scanLine);
    }

    private Line? _scanLine;

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (IsLoaded) Build();
    }
}
