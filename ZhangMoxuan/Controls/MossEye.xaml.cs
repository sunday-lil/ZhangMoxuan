using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ZhangMoxuan.Controls;

/// <summary>
/// 550W 光学传感器：白色/灰色金属外壳、黑色镜筒、红色虹膜。
/// 状态只改变虹膜亮度、呼吸频率与光晕，不再使用悬浮的红色圆环。
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
        Loaded += (_, _) => ApplyState(State);
        Unloaded += (_, _) => _stateStoryboard?.Stop();
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
        var breath = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromSeconds(2),
            RepeatBehavior = RepeatBehavior.Forever
        };

        Color color;
        double blur;
        double low;
        double high;
        double period;

        switch (state)
        {
            case EyeState.Idle:
                low = 0.82; high = 0.96; period = 2.0;
                color = Color.FromRgb(0x6B, 0x2B, 0x33); blur = 12;
                break;
            case EyeState.Observing:
                low = 0.92; high = 1.04; period = 1.8;
                color = Color.FromRgb(0xF0, 0x4A, 0x4A); blur = 22;
                break;
            case EyeState.Analyzing:
                low = 0.92; high = 1.12; period = 0.7;
                color = Color.FromRgb(0xFF, 0x6A, 0x6A); blur = 30;
                break;
            case EyeState.Speaking:
                low = 1.0; high = 1.05; period = 0.9;
                color = Color.FromRgb(0xF0, 0x4A, 0x4A); blur = 24;
                break;
            case EyeState.Alert:
                low = 0.86; high = 1.18; period = 0.36;
                color = Color.FromRgb(0xFF, 0x3A, 0x3A); blur = 36;
                break;
            default:
                low = 0.9; high = 1.0; period = 2.0;
                color = Color.FromRgb(0xF0, 0x4A, 0x4A); blur = 20;
                break;
        }

        breath.Duration = TimeSpan.FromSeconds(period);
        breath.KeyFrames.Add(new LinearDoubleKeyFrame(low, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        breath.KeyFrames.Add(new LinearDoubleKeyFrame(high, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(period / 2))));
        breath.KeyFrames.Add(new LinearDoubleKeyFrame(low, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(period))));

        Storyboard.SetTarget(breath, PupilScale);
        Storyboard.SetTargetProperty(breath, new PropertyPath(ScaleTransform.ScaleXProperty));
        sb.Children.Add(breath);

        var breathY = breath.Clone();
        Storyboard.SetTarget(breathY, PupilScale);
        Storyboard.SetTargetProperty(breathY, new PropertyPath(ScaleTransform.ScaleYProperty));
        sb.Children.Add(breathY);

        var glow = new DoubleAnimation(0.55, 1.0, TimeSpan.FromSeconds(period / 2))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        Storyboard.SetTarget(glow, PupilGlow);
        Storyboard.SetTargetProperty(glow, new PropertyPath(DropShadowEffect.OpacityProperty));
        sb.Children.Add(glow);

        Pupil.Fill = new SolidColorBrush(color);
        PupilGlow.Color = color;
        PupilGlow.BlurRadius = blur;

        sb.Begin(this);
        _stateStoryboard = sb;
    }
}
