# v2.0.1 — 分辨率适配与运行时修复

## 修复

- **1366×768 等低分辨率裁切**：主界面固定 1600×1000 设计稿，外层用 `Viewbox` 等比缩放；1024 高度的布局在 768 高度屏幕上不再截断底部状态栏。
- **MOSS 状态被每 2 秒覆盖**：负载定时器现在只更新 Monitor / Digital Life / Dialog；Crisis 与 Key Auth 的 ALERT 状态保持。
- **节点清单末行被裁切**：节点表行高收紧，SEOUL 等最后一行完整显示。
- **密钥场景重入日志残留**：`Start()` 清空上一轮的 System Log。
- **截图模式关闭竞态**：仅关闭仍可见的窗口，避免重复 `Close()` 抛异常。
- **无边框最大化边缘伪影**：`ResizeMode=NoResize`。

## 新增

- `--capture-size WxH`：离屏截图尺寸可指定，默认 `1600x1000`，用于 1366×768 等低分辨率回归。

## 验证

- `dotnet build`：0 warning / 0 error
- 1600×1000 与 1366×768：各 7 张页面截图通过
- 单文件大小：`144,565,272` 字节
- SHA-256：`2930829f316df72f7e57890b130c4fa3cb76df1e991f9b46f00a1e45016b4c69`
- 在仅含 exe 的空目录、`DOTNET_ROOT` 指向不存在路径时运行通过（`--capture-size 1366x768`），确认无需本机 .NET 运行时

## 安装

下载 `ZhangMoxuan.exe`（self-contained 单文件，内置 .NET 10 运行时），双击运行。

系统要求：Windows 10 1809+ / Windows 11 x64。

**Full Changelog**: https://github.com/sunday-lil/ZhangMoxuan/compare/v2.0.0...v2.0.1
