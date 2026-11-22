# v2.0.0 — 依据影片 UI 规范重建系统界面

## 为什么重做

v1.2.0 的界面是常见的“深蓝黑 + 霓虹红”科幻 HUD，与《流浪地球 2》的实际系统界面差距很大。v2.0.0 按制作手记和影片屏幕重新拆解：

- 中文字体：思源黑体（Windows 以微软雅黑替代）；英文字体：Rajdhani
- 整体深灰色系、红/中黄/亮白跳色、避免斜线边框
- 高信息密度：表格式数据、分组框、状态栏、雷达/摄像头/倒计时等真实界面结构

完整检索与拆解见 [UI_REFERENCE.md](https://github.com/sunday-lil/ZhangMoxuan/blob/main/docs/UI_REFERENCE.md)。

## 主要变化

- **UEG 雷达监控**：距离环、方位刻度、扫描扇区、19 个接触点与节点清单，替代原来的世界地图点阵。
- **550A 数字生命模拟**：左侧数字行为/语音/情绪/场景参数，中间 550A 场景，右侧摄像头识别，底部 `00:01:45` 倒计时 + 暂停键 + 进度条。
- **550W 光学传感器**：白色/灰色金属外壳、黑色镜筒、红色虹膜，替代悬浮红色圆环。
- **正交 HUD**：0 圆角、细线分组框、矩阵扫描、顶部状态条、左侧模块栏，整体密度接近影片作战室。
- **字体与图标**：内置 Rajdhani（SIL OFL），重新生成 550W 硬件风格应用图标。
- **清理**：移除已不再使用的 `DigitalLifeCard` 旧控件。

## 验证

- 单文件大小：`144,565,272` 字节
- SHA-256：`ca727d7ccb7f4c0931fdd3d7e7a64059680887d73182104e6c47fe7a3ebe3ae4`
- 在仅含 exe 的空目录、`DOTNET_ROOT` 指向不存在路径时运行通过，7 张页面截图正常
- GitHub Actions：Debug / Release 构建、单文件发布、artifact 上传、UI smoke 全部通过

## 安装

下载 `ZhangMoxuan.exe`（self-contained 单文件，内置 .NET 10 运行时），双击运行。

系统要求：Windows 10 1809+ / Windows 11 x64。

## 已知说明

- 数字生命场景为程序生成的近似画面，不含电影原片素材。
- 首次运行可能触发 SmartScreen：选择「更多信息 → 仍要运行」。
- 日志位于 `%LOCALAPPDATA%\ZhangMoxuan\moss.log`。

**Full Changelog**: https://github.com/sunday-lil/ZhangMoxuan/compare/v1.2.0...v2.0.0
