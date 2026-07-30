"""
张莫轩 / 550W MOSS 系统 - 启动器
================================
启动 self-contained 单文件 exe（内置 .NET 运行时，免安装）。
若 exe 不存在，自动调用 dotnet publish 生成；发布失败则引导从 Release 下载。
"""
import os
import sys
import subprocess

ROOT = os.path.dirname(os.path.abspath(__file__))
EXE = os.path.join(ROOT, "publish", "ZhangMoxuan.exe")
RELEASE_URL = "https://github.com/sunday-lil/ZhangMoxuan/releases"


def ensure_exe():
    if os.path.exists(EXE):
        return True
    print("[MOSS] 未找到 publish/ZhangMoxuan.exe，尝试自动发布（需要 .NET 10 SDK）...")
    try:
        subprocess.run(
            ["dotnet", "publish", os.path.join(ROOT, "ZhangMoxuan", "ZhangMoxuan.csproj"),
             "-c", "Release", "-r", "win-x64", "--self-contained", "true",
             "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true",
             "-o", os.path.join(ROOT, "publish")],
            check=True,
        )
        return os.path.exists(EXE)
    except Exception as e:
        print(f"[MOSS] 自动发布失败：{e}", file=sys.stderr)
        print(f"[MOSS] 请从 Release 下载单文件 exe：{RELEASE_URL}", file=sys.stderr)
        return False


def main():
    if not ensure_exe():
        sys.exit(1)
    print("[MOSS] 启动 550W / MOSS 系统...")
    os.startfile(EXE)


if __name__ == "__main__":
    main()
