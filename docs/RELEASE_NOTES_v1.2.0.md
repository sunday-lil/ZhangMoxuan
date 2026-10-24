# v1.2.0 — 开源发布整备

## 亮点

- **自检可跳过**：启动时按 `ESC` 跳过 4 阶段自检；关闭自检窗口会取消整个序列，不再出现「退出后仍弹主窗口」。
- **`Ctrl+1…6` 键盘导航**：鼠标之外的完整页面切换通路。
- **一键截图模式**：`ZhangMoxuan.exe --capture docs\screenshots` 离屏渲染自检与 6 个功能页（共 7 张 PNG），README 截图与视觉回归都可复现。
- **MOSS 之眼修复**：眼体、内环、瞳孔重新对齐，瞳孔发光不再偏出眼球。
- **对话输出修复**：五段式回答的标签与正文正确分段，不再重复或串段。
- **真正的重启**：Reboot 550W 会拉起新进程后退出当前实例。
- **应用图标与 v1.2.0 版本标识**。

## 安装

下载 `ZhangMoxuan.exe`（self-contained 单文件，约 133 MB，内置 .NET 10 运行时），双击运行。

系统要求：Windows 10 1809+ / Windows 11 x64。

## 已知说明

- 首次运行可能触发 Windows SmartScreen：选择「更多信息 → 仍要运行」。
- 日志位于 `%LOCALAPPDATA%\ZhangMoxuan\moss.log`，反馈问题时请附上末尾 30~50 行。
- 本项目为《流浪地球》非官方粉丝作品，与电影版权方无关联。

**Full Changelog**: https://github.com/sunday-lil/ZhangMoxuan/compare/v1.1...v1.2.0
