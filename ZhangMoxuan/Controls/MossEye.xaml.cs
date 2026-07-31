using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ZhangMoxuan.Controls;

/// <summary>
/// MOSS 之眼控件：影片中 550W 最核心视觉符号（致敬《2001太空漫游》HAL9000）。
/// 包含：外环刻度、旋转扫描弧、瞳孔呼吸、注视跟踪、情绪状态切换（红/蓝）。
/// </summary>
public partial class MossEye : UserControl
{
    public enum EyeState { Idle, Observing, Analyzing, Speaking, Alert }

    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(nameof(State), typeof(EyeState), typeof(MossEye),
            new PropertyMetadata(EyeState.Idle, OnStateChanged));

    public EyeState State
    {
        get => (EyeState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    private Storyboard? _stateStoryboard;

    public MossEye()
    {
        InitializeComponent();
        Loaded += MossEye_Loaded;
        SizeChanged += (_, _) => Rebuild();
    }

    private void MossEye_Loaded(object sender, RoutedEventArgs e)
    {
        Rebuild();
        // 扫描弧旋转：DoubleAnimation 跑在合成线程，避免 60fps DispatcherTimer 占用 UI 线程
        var rotate = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(4)) { RepeatBehavior = RepeatBehavior.Forever };
        ArcRotate.BeginAnimation(RotateTransform.AngleProperty, rotate);
        ApplyState(State);
    }

    private void Rebuild()
    {
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        var cx = w / 2;
        var cy = h / 2;
        var radius = Math.Min(w, h) / 2 - 4;
        var ballR = radius * 0.52;
        var pupilR = ballR * 0.32;

        // 外环刻度
        RingCanvas.Children.Clear();
        RingCanvas.Width = w;
        RingCanvas.Height = h;
        for (int i = 0; i < 72; i++)
        {
            var angle = i * 5 * Math.PI / 180;
            var isMajor = i % 6 == 0;
            var r1 = radius - (isMajor ? 10 : 5);
            var r2 = radius;
            var line = new Line
            {
                X1 = cx + r1 * Math.Cos(angle),
                Y1 = cy + r1 * Math.Sin(angle),
                X2 = cx + r2 * Math.Cos(angle),
                Y2 = cy + r2 * Math.Sin(angle),
                Stroke = (Brush)FindResource("RedDimBrush"),
                StrokeThickness = isMajor ? 1.2 : 0.6,
                Opacity = isMajor ? 0.9 : 0.5
            };
            RingCanvas.Children.Add(line);
        }

        // 旋转扫描弧
        Arc.Width = w;
        Arc.Height = h;
        var pg = new PathGeometry();
        var fig = new PathFigure { StartPoint = new Point(cx + radius * 0.9, cy) };
        fig.Segments.Add(new ArcSegment(
            new Point(cx + radius * 0.5, cy + radius * 0.75),
            new Size(radius * 0.9, radius * 0.9), 0, false, SweepDirection.Clockwise, true));
        pg.Figures.Add(fig);
        Arc.Data = pg;
        ArcRotate.CenterX = cx;
        ArcRotate.CenterY = cy;

        // 眼球
        Ball.Width = ballR * 2;
        Ball.Height = ballR * 2;
        Ball.RenderTransform = new TranslateTransform(cx - ballR, cy - ballR);

        InnerRing.Width = ballR * 2.4;
        InnerRing.Height = ballR * 2.4;
        InnerRing.RenderTransform = new TranslateTransform(cx - ballR * 1.2, cy - ballR * 1.2);

        // 瞳孔
        Pupil.Width = pupilR * 2;
        Pupil.Height = pupilR * 2;
        Pupil.RenderTransform = new TranslateTransform(cx - pupilR, cy - pupilR);
        PupilScale.CenterX = pupilR;
        PupilScale.CenterY = pupilR;

        // 高光
        Glint.Width = pupilR * 0.4;
        Glint.Height = pupilR * 0.4;
        Glint.RenderTransform = new TranslateTransform(cx - pupilR * 0.7, cy - pupilR * 0.7);
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MossEye eye && eye.IsLoaded)
            eye.ApplyState((EyeState)e.NewValue);
    }

    private void ApplyState(EyeState state)
    {
        _stateStoryboard?.Stop();

        var sb = new Storyboard();
        var breath = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(2), RepeatBehavior = RepeatBehavior.Forever };
        Color color;
        double blur;

        switch (state)
        {
            case EyeState.Idle:
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.85, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.85, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2))));
                color = Color.FromRgb(0xFF, 0x1A, 0x1A);
                blur = 25;
                break;
            case EyeState.Observing:
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(1.05, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.6))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.9, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.2))));
                color = Color.FromRgb(0xFF, 0x2A, 0x2A);
                blur = 30;
                break;
            case EyeState.Analyzing:
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.7, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(1.1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.25))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.7, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
                color = Color.FromRgb(0xFF, 0x38, 0x38);
                blur = 38;
                break;
            case EyeState.Speaking:
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.4))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8))));
                color = Color.FromRgb(0xFF, 0x44, 0x44);
                blur = 32;
                break;
            case EyeState.Alert:
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.6, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.18))));
                breath.KeyFrames.Add(new LinearDoubleKeyFrame(0.6, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.36))));
                color = Color.FromRgb(0xFF, 0x00, 0x00);
                blur = 45;
                break;
            default:
                color = Color.FromRgb(0xFF, 0x1A, 0x1A);
                blur = 25;
                break;
        }

        Storyboard.SetTarget(breath, PupilScale);
        Storyboard.SetTargetProperty(breath, new PropertyPath(ScaleTransform.ScaleXProperty));
        sb.Children.Add(breath);

        var breathY = breath.Clone();
        Storyboard.SetTarget(breathY, PupilScale);
        Storyboard.SetTargetProperty(breathY, new PropertyPath(ScaleTransform.ScaleYProperty));
        sb.Children.Add(breathY);

        Pupil.Fill = new SolidColorBrush(color);
        PupilGlow.Color = color;
        PupilGlow.BlurRadius = blur;

        sb.Begin();
        _stateStoryboard = sb;
    }
}
