# 张莫轩 / 550W · MOSS 更新日志（CHANGELOG）

> 按时间倒序排列。格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)，
> 版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

## v1.2.0 — 2026-10

面向开源发布与稳定性的整备版本。

### 新增

- **`--skip-boot` 启动参数**：跳过 4 阶段自检，便于开发与录屏。
- **`--capture [目录]` 截图模式**：离屏渲染 6 个功能页与自检画面（共 7 张 PNG），README 截图可一键复现，同时作为 UI 冒烟测试。
- **`Ctrl+1…6` 键盘导航**：与左侧 6 个模块一一对应。
- **MOSS 应用图标**：由 `tools/generate-icon.py` 生成，接入窗口与单文件 exe。
- **网络页自动握手**：进入页面即执行 7 步握手，按钮可重跑。
- **对话演示接口**：截图/演示时可自动发起一次预设问答。

### 修复

- 自检窗口关闭或按 `ESC` 后，异步序列不再继续执行、也不会在应用退出后创建主窗口。
- MOSS 之眼的眼体、内环与瞳孔偏移，瞳孔不再跑出眼体右下角。
- MOSS 对话五段式输出标签错位：不再出现「任务分析 / 任务分析」重复，也不会把下一段标题混入正文。
- 主控台 `DispatcherTimer` 在窗口关闭后未停止的问题。
- 「Reboot 550W」现在会真正重启进程，而不是直接退出。
- 地球状态面板底部数据被裁切、握手/网络面板标题重叠、危机时间线相邻事件标签重叠。
- 数字生命卡的 `Storyboard` 未指定 Namescope 导致动画可能不启动。
- 密钥场景重入时倒计时颜色、卡片状态未复位。

### 优化

- 量子核心网格提升到 48×24，并改为以邻近比特为主的纠缠连线。
- 握手协议列宽与文案调整，窄面板下不再大面积截断。
- 网络拓扑精简非枢纽城市标签，缓解东亚/欧洲区域标签重叠。
- 日志从程序目录迁移至 `%LOCALAPPDATA%\ZhangMoxuan\moss.log`，超过 512 KB 自动轮转。

### 文档与仓库

- 重写 README：截图、功能模块、启动参数、快捷键、架构、构建发布与故障排查。
- 新增 `CONTRIBUTING.md`、`CHANGELOG.md` 规范、Issue 表单、PR 模板、`.gitattributes`。
- 新增 GitHub Actions 构建矩阵（Debug / Release），支持手动发布单文件产物。
- **chore: GitHub Actions 升级至 `checkout@v7` / `setup-dotnet@v6` / `upload-artifact@v7`**，Dependabot 更新改为分组，减少 PR 噪音。
- 新增 `docs/screenshots/` 全页面截图与 `docs/icon.png`。

## 2026-09

- **chore: gitignore 添加 .trae/ 编辑器配置目录** — IDE/AI 工作区配置不再入库。
- **fix: 补回 TextSecondaryBrush 资源** — 修复运行时因缺失资源导致的崩溃。
- **chore: 移除 start.py 启动器** — 改为 Release 单文件直接运行。
- **style: 视觉风格全面重做** — 去除廉价霓虹感，回归影片级高对比单色 HUD。
- **perf: 大幅降低 UI 卡顿** — 渲染与动画性能优化。
- **fix: 量子核心 QuantumGrid 渲染修复**。
- **docs: 更新 README** — 适配 self-contained 单文件发布说明。
- **rename: JiZiKan → ZhangMoxuan（张莫轩）** — 项目更名。

## 2026-08

- **feat: 550W / MOSS 系统机子看** — 流浪地球风格 WPF 原生应用初版：开机自检、MOSS 之眼、握手协议、网络拓扑、数字生命、危机预测与密钥认证。
