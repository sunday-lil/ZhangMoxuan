"""
张莫轩 / 550W MOSS 系统 - 启动器
================================
优先使用本机已安装的 .NET 10 桌面运行时启动单文件 exe；
若未检测到运行时，自动下载微软官方 dotnet-install 脚本静默安装兜底；
安装失败则给出手动下载链接，绝不卡死。
"""
import os
import sys
import subprocess
import urllib.request
import tempfile

ROOT = os.path.dirname(os.path.abspath(__file__))
EXE = os.path.join(ROOT, "publish", "ZhangMoxuan.exe")
DOTNET_INSTALL_URL = "https://dot.net/v1/dotnet-install.ps1"
DOTNET_DIR = os.path.join(os.environ.get("LOCALAPPDATA", r"%LOCALAPPDATA%\Microsoft\dotnet"), "Microsoft", "dotnet")
DOTNET_EXE = os.path.join(DOTNET_DIR, "dotnet.exe")
RUNTIME_NAME = "Microsoft.WindowsDesktop.App"
RUNTIME_MAJOR = "10."


def find_dotnet():
    """定位 dotnet 可执行文件：先查 PATH，再查默认安装位置。"""
    try:
        subprocess.check_output(["dotnet", "--version"], stderr=subprocess.STDOUT, timeout=5)
        return "dotnet"
    except Exception:
        pass
    if os.path.exists(DOTNET_EXE):
        return DOTNET_EXE
    return None


def has_desktop_runtime(dotnet):
    """检查是否已安装 .NET 10 桌面运行时（WPF 依赖）。"""
    try:
        out = subprocess.check_output([dotnet, "--list-runtimes"], text=True, timeout=5)
        for line in out.splitlines():
            if line.startswith(f"{RUNTIME_NAME} {RUNTIME_MAJOR}"):
                return True
    except Exception:
        pass
    return False


def install_runtime():
    """下载并执行微软官方 dotnet-install.ps1，安装 windowsdesktop 运行时。"""
    print("[MOSS] 未检测到 .NET 10 桌面运行时，正在自动下载安装...")
    ps1 = os.path.join(tempfile.gettempdir(), "moss_dotnet_install.ps1")
    urllib.request.urlretrieve(DOTNET_INSTALL_URL, ps1)
    subprocess.run(
        ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ps1,
         "-Runtime", "windowsdesktop", "-Channel", "10.0"],
        check=True,
    )
    if DOTNET_DIR not in os.environ.get("PATH", ""):
        os.environ["PATH"] = DOTNET_DIR + os.pathsep + os.environ.get("PATH", "")


def main():
    if not os.path.exists(EXE):
        print("[MOSS] 错误：未找到 publish/ZhangMoxuan.exe，请先执行发布。", file=sys.stderr)
        print("       dotnet publish ZhangMoxuan/ZhangMoxuan.csproj -c Release -r win-x64 "
              "--self-contained false -p:PublishSingleFile=true -o publish", file=sys.stderr)
        sys.exit(1)

    dotnet = find_dotnet()
    if not dotnet or not has_desktop_runtime(dotnet):
        try:
            install_runtime()
        except Exception as e:
            print(f"[MOSS] 运行时自动安装失败：{e}", file=sys.stderr)
            print("[MOSS] 请手动安装 .NET 10 桌面运行时：", file=sys.stderr)
            print("       https://dotnet.microsoft.com/download/dotnet/10.0", file=sys.stderr)
            sys.exit(1)

    print("[MOSS] 启动 550W / MOSS 系统...")
    os.startfile(EXE)


if __name__ == "__main__":
    main()
