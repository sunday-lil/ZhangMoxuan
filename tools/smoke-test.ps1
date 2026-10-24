# 本地 UI 冒烟测试：构建 Release、以 --capture 离屏渲染全部页面，并校验产物。
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File tools/smoke-test.ps1

param(
    [string]$Configuration = "Release",
    [int]$TimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$expectedPages = @("boot", "dashboard", "network", "digital", "crisis", "key", "dialog")

Push-Location $root
$output = $null
try {
    Write-Host "==> dotnet build ($Configuration)" -ForegroundColor Cyan
    dotnet build ZhangMoxuan.sln -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet build 失败（exit $LASTEXITCODE）" }

    $exe = Join-Path $root "ZhangMoxuan\bin\$Configuration\net10.0-windows\ZhangMoxuan.exe"
    if (-not (Test-Path $exe)) { throw "找不到构建产物：$exe" }

    $output = Join-Path ([IO.Path]::GetTempPath()) ("zmx-smoke-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $output | Out-Null

    Write-Host "==> 离屏渲染页面到 $output" -ForegroundColor Cyan
    $process = Start-Process -FilePath $exe -ArgumentList "--capture", $output -WorkingDirectory $root -PassThru
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill()
        throw "截图进程超过 $TimeoutSeconds 秒未退出"
    }
    if ($process.ExitCode -ne 0) { throw "截图进程退出码 $($process.ExitCode)" }

    $missing = @()
    $tooSmall = @()
    foreach ($page in $expectedPages) {
        $file = Join-Path $output "$page.png"
        if (-not (Test-Path $file)) {
            $missing += "$page.png"
        } elseif ((Get-Item $file).Length -lt 10KB) {
            $tooSmall += "$page.png"
        }
    }
    if ($missing.Count -gt 0) { throw "缺少截图：$($missing -join ', ')" }
    if ($tooSmall.Count -gt 0) { throw "截图疑似空白：$($tooSmall -join ', ')" }

    Write-Host "==> 通过：$($expectedPages.Count) 张页面截图均正常生成" -ForegroundColor Green
}
finally {
    if ($output -and (Test-Path $output)) {
        Remove-Item $output -Recurse -Force -ErrorAction SilentlyContinue
    }
    Pop-Location
}
