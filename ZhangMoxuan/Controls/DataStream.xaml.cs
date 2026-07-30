using System.Windows.Controls;
using System.Windows.Threading;

namespace ZhangMoxuan.Controls;

/// <summary>
/// 滚动数据流：影片中屏幕持续滚动的科技感文字。
/// </summary>
public partial class DataStream : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(120) };
    private readonly List<string> _lines = new();
    private readonly string[] _vocab =
    {
        "0x4A8F", "QBIT", "OBS", "TOF", "0xFF1A", "ENT", "YAYA", "550W", "MOSS", "UEG",
        "PLAN-A", "ROOT", "SYNC", "0x00AE", "NEURAL", "LOOP", "EG", "BIO", "OPTIC",
        "8192", "0xC3D2", "OBSERVE", "COLLAPSE", "VARIABLE", "HUMAN", "0x1F4A", "DIGITAL"
    };
    private readonly Random _rng = new();

    public DataStream()
    {
        InitializeComponent();
        Loaded += (_, _) => _timer.Start();
        Unloaded += (_, _) => _timer.Stop();
        _timer.Tick += Tick;
    }

    private void Tick(object? sender, EventArgs e)
    {
        var line = GenerateLine();
        _lines.Add(line);
        if (_lines.Count > 18) _lines.RemoveAt(0);
        Stream.Text = string.Join("\n", _lines);
    }

    private string GenerateLine()
    {
        var parts = new List<string>();
        var count = _rng.Next(4, 8);
        for (int i = 0; i < count; i++)
            parts.Add(_vocab[_rng.Next(_vocab.Length)]);
        return string.Join(" ", parts);
    }
}
