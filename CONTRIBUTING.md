# 贡献指南

感谢你愿意为 **张莫轩 / 550W · MOSS** 做出贡献。这是一个偏视觉与交互的 WPF 桌面项目，代码量不大，但很看重渲染细节与稳定性。

## 开发环境

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows 10 1809+ / Windows 11 x64 |
| SDK | [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| IDE | Visual Studio 2022/2026、Rider 或 VS Code（可选） |
| 运行库 | 开发运行需要 .NET 10 Desktop Runtime；Release 单文件版自带运行时 |

```powershell
git clone https://github.com/sunday-lil/ZhangMoxuan.git
cd ZhangMoxuan
dotnet build ZhangMoxuan.sln -c Release
dotnet run --project ZhangMoxuan/ZhangMoxuan.csproj
```

启动参数（开发与文档用）：

| 参数 | 作用 |
|------|------|
| `--skip-boot` | 跳过 4 阶段开机自检，直接进入主控台 |
| `--capture [目录]` | 在屏幕外渲染全部 7 张页面（6 个功能页 + 自检）为 PNG（默认输出到可执行文件旁的 `screenshots/`） |

```powershell
# 快速调试
dotnet run --project ZhangMoxuan/ZhangMoxuan.csproj -- --skip-boot

# 重新生成 README 截图
dotnet run --project ZhangMoxuan/ZhangMoxuan.csproj -- --capture ..\docs\screenshots

# UI 冒烟测试：构建 + 离屏渲染 7 张页面截图并校验非空
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke-test.ps1
```

> `--capture` 会把窗口放到屏幕外（`Left = -8000`），不会抢占桌面焦点；渲染完成后自动退出。

## 代码结构

```text
ZhangMoxuan/
├── App.xaml(.cs)              # 入口、参数解析、全局异常与日志、截图模式
├── BootWindow.xaml(.cs)       # 4 阶段自检（ESC 可跳过，关闭时取消序列）
├── MainWindow.xaml(.cs)       # 左侧导航 + 6 个页面 + 右侧 MOSS 之眼
└── Controls/                  # 可复用视觉控件
    ├── MossEye               # MOSS 之眼（5 种状态）
    ├── QuantumGrid           # 8192 量子比特网格
    ├── DataStream            # 滚动数据流
    ├── HandshakeProtocol     # 7 步握手协议
    ├── NetworkTopology       # 全球节点 / 根服务器 / 数据包
    ├── DigitalLifeCard       # 数字生命卡
    ├── CrisisTimeline        # 危机预测时间线
    └── KeyInputScene         # 密钥认证场景
```

几条约定：

- **不引入浏览器内核**（WebView2 / CEF / Electron）与第三方 UI 框架。
- 主题资源集中在 [`App.xaml`](ZhangMoxuan/App.xaml)，新控件请复用其中的颜色、字体与 `HudLabel` 样式，不要硬编码新的品牌色。
- 动画优先使用 `Storyboard` / `DoubleAnimation`（跑在合成线程）；需要逐帧更新的才用 `DispatcherTimer`。
- 任何 `DispatcherTimer`、`Storyboard`、`CancellationTokenSource` 都必须在 `Unloaded` / `Closed` 中停止或取消，避免页面切走后继续 tick。
- 面向用户的文案以中文为主、英文 HUD 标签为辅；提交前请确认中文与数字对齐没有破版。

## 提交规范

使用 [Conventional Commits](https://www.conventionalcommits.org/)：

```text
feat: 新增 xx 模块
fix: 修复退出后自检仍创建主窗口
style: 调整面板留白
perf: 降低 QuantumGrid 重绘开销
docs: 更新截图与构建说明
refactor: 拆分 MakePanel
chore: 升级 Actions
```

一个提交只做一件事；UI 改动请附截图。提交信息可以使用中文。

## Pull Request

1. 从 `main` 切出分支：`feat/xxx`、`fix/xxx`。
2. 保证 `dotnet build ZhangMoxuan.sln -c Release` **0 警告 0 错误**。
3. 手动跑一遍受影响页面；涉及布局的改动请重新生成 `docs/screenshots/`。
4. 面向用户的改动更新 [`CHANGELOG.md`](CHANGELOG.md)。
5. 在 PR 描述中写清楚验证方式，勾选模板中的检查项。

CI（[`build.yml`](.github/workflows/build.yml)）会在 `windows-latest` 上构建 Debug 与 Release 两个配置；PR 必须通过。

## 应用图标

图标由 Python + Pillow 生成，源文件在 [`tools/generate-icon.py`](tools/generate-icon.py)：

```powershell
python tools/generate-icon.py
```

会覆盖 `ZhangMoxuan/Assets/moss.ico` 与 `docs/icon.png`。修改图标设计时请同时检查 16×16 与 32×32 的可读性。

## 发布流程（维护者）

```powershell
dotnet publish ZhangMoxuan/ZhangMoxuan.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true -o publish

git tag v1.2.0
git push origin v1.2.0
gh release create v1.2.0 publish/ZhangMoxuan.exe `
  --title "v1.2.0" --notes-file docs/RELEASE_NOTES_v1.2.0.md
```

发布前请确认 `ZhangMoxuan.csproj` 中的 `<Version>` 与 tag 一致。

## 行为准则

- 讨论对事不对人；不接受针对个人、地区、群体的攻击性内容。
- 本项目是《流浪地球》的非官方粉丝作品，请勿提交任何官方素材、盗版资源或受版权保护的影片截图。
- 不要为了“更像电影”而引入真实武器、监控或破坏性功能的可执行实现；保持桌面玩具/演示的边界。

## License

提交代码即表示同意以 [MIT License](LICENSE) 授权。
