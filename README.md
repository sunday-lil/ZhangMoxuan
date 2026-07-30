# 张莫轩 / 550W · MOSS

基于《流浪地球》系列的 **550W 智能量子计算机 / MOSS** 桌面系统复刻。

Windows 原生 WPF 应用，**纯 C# + DirectX 渲染，无浏览器内核**，完整还原影片中的高对比度单色 GUI、MOSS 之眼、开机自检、握手协议、网络拓扑、数字生命、危机预测与密钥认证等场景。

> GitHub：https://github.com/sunday-lil/ZhangMoxuan
> Release 下载（单文件免安装）：https://github.com/sunday-lil/ZhangMoxuan/releases

## 特征

- 纯黑底 + MOSS 红（`#FF1A1A`）+ 人类蓝对比，等宽字体，四角 HUD 标记
- 全屏沉浸式控制台，左侧导航多模块切换
- 多阶段开机自检弹窗序列
- Windows 原生 WPF（.NET 10），无任何 Webview / CEF / Electron

## 运行

### 方式一：下载 Release 单文件（推荐，零依赖）

从 [Release v1.0](https://github.com/sunday-lil/ZhangMoxuan/releases/tag/v1.0) 下载 `ZhangMoxuan.exe`（约 133 MB），**self-contained 单文件，内置 .NET 运行时，无需任何安装，双击即跑**。

### 方式二：一键启动（需源码）

```bash
python start.py
```

`start.py` 会：检测 `publish/ZhangMoxuan.exe` 是否存在 → 不存在则自动 `dotnet publish` 生成 → 启动；发布失败则引导从 Release 下载。

### 方式三：开发调试

```bash
dotnet run --project ZhangMoxuan/ZhangMoxuan.csproj
```

## 发布单文件 exe

```bash
dotnet publish ZhangMoxuan/ZhangMoxuan.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

生成 `publish/ZhangMoxuan.exe`（self-contained 单文件，约 133 MB，内置 .NET 运行时，目标机无需安装运行库）。

## 功能模块

### 开机自检（BootWindow，4 阶段弹窗）

1. **固件加载** — POST 日志流 + 子系统检查清单 + 进度条
2. **握手协议** — 7 步握手（TCP→TLS 1.3 量子加密→UEG 认证→根服务器同步→量子纠缠→MOSS 身份→数字生命协议）+ 全球网络拓扑实时显示
3. **量子核心激活** — 8192 量子比特 + 指标面板（T1/T2/保真度/量子体积）
4. **MOSS 觉醒** — 之眼激活 + 550W → MOSS 翻转

### 主控台（6 个功能模块，左侧导航切换）

| 模块 | 内容 |
|------|------|
| MONITOR 监控中心 | 地球状态（轨道速度/距木星/人口）+ 行星发动机网络（10000 台/区域状态）+ 量子核心 |
| NETWORK 网络拓扑 | 全球节点地图 + 数据包流动 + 可重跑握手协议 |
| DIGITAL LIFE 数字生命 | 图丫丫（红）+ 图恒宇（蓝）数字生命卡 + 意识上传进度 + 迭代日志 |
| CRISIS 危机预测 | 2044 太空电梯 / 2058 月球坠落 / 2075 木星引力 / 2078 太阳氦闪 时间线 |
| KEY AUTH 密钥认证 | 图恒宇水下北京根服务器场景，倒计时 + 键盘 + 密钥 `31415926` |
| MOSS DIALOG 对话 | 5 段式响应（任务分析/计算路径/资源分配/执行预估/结果输出），含预设问答 |

### 右侧固定面板

MOSS 之眼（5 种状态切换）+ 数据流 + 快捷命令

## 项目结构

```
.
├── ZhangMoxuan.sln              # 解决方案（传统 .sln 格式）
├── start.py                     # 启动器（exe 不存在则自动发布）
├── README.md
├── .editorconfig                # 关闭代码风格 Info 提示
├── cspell.json                  # 拼写词典
├── publish/                     # 单文件 exe 发布输出（gitignore）
└── ZhangMoxuan/
    ├── ZhangMoxuan.csproj
    ├── App.xaml / App.xaml.cs
    ├── BootWindow.xaml(.cs)         # 开机自检 4 阶段
    ├── MainWindow.xaml(.cs)         # 主控台 + 导航
    └── Controls/
        ├── MossEye.xaml(.cs)            # MOSS 之眼
        ├── QuantumGrid.xaml(.cs)        # 量子比特网格
        ├── DataStream.xaml(.cs)         # 数据流
        ├── HandshakeProtocol.xaml(.cs)  # 握手协议
        ├── NetworkTopology.xaml(.cs)    # 全球网络拓扑
        ├── DigitalLifeCard.xaml(.cs)    # 数字生命卡
        ├── CrisisTimeline.xaml(.cs)     # 危机预测时间线
        └── KeyInputScene.xaml(.cs)      # 密钥输入场景
```

## 技术栈

- **.NET 10** + **WPF**（Windows Presentation Foundation）
- 纯 C#，DirectX 硬件加速渲染
- 无浏览器内核、无 WebView2、无 CEF、无 Electron

## 系统要求

- Windows 10/11 x64
- Release 单文件版：**无需 .NET 运行时**（self-contained）
- 开发调试：需 .NET 10 SDK
