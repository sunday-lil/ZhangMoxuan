"""生成 MOSS / 550W 应用图标（多尺寸 .ico）。

设计对应影片中 550W 的光学传感器：白色/灰色金属外壳、黑色镜筒、红色虹膜。
用法：
    python tools/generate-icon.py
输出：
    ZhangMoxuan/Assets/moss.ico
    docs/icon.png
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUT_ICO = ROOT / "ZhangMoxuan" / "Assets" / "moss.ico"
OUT_PNG = ROOT / "docs" / "icon.png"

SIZE = 1024
C = SIZE / 2


def radial_disc(draw: ImageDraw.ImageDraw, cx: float, cy: float, radius: float,
                inner: tuple[int, int, int], outer: tuple[int, int, int], steps: int = 80) -> None:
    for i in range(steps, 0, -1):
        t = i / steps
        color = tuple(round(inner[c] + (outer[c] - inner[c]) * t) for c in range(3))
        r = radius * t
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=color + (255,))


def build() -> Image.Image:
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    # 金属外壳（圆角方形）
    d.rounded_rectangle([8, 8, SIZE - 8, SIZE - 8], radius=int(SIZE * 0.16), fill=(176, 176, 176, 255))
    d.rounded_rectangle([8, 8, SIZE - 8, SIZE - 8], radius=int(SIZE * 0.16), outline=(70, 70, 70, 255), width=10)
    # 高光/阴影
    d.rounded_rectangle([18, 18, SIZE - 18, SIZE - 18], radius=int(SIZE * 0.15), outline=(226, 226, 226, 180), width=6)
    d.rounded_rectangle([10, 10, SIZE - 10, SIZE - 10], radius=int(SIZE * 0.16), outline=(42, 42, 42, 120), width=4)

    # 黑色内嵌面板
    inset = int(SIZE * 0.18)
    d.rectangle([inset, inset, SIZE - inset, SIZE - inset], fill=(10, 10, 10, 255), outline=(52, 52, 52, 255), width=6)

    # 镜筒
    outer = int(SIZE * 0.29)
    d.ellipse([C - outer, C - outer, C + outer, C + outer], fill=(5, 5, 5, 255), outline=(120, 120, 120, 255), width=10)
    mid = int(SIZE * 0.25)
    d.ellipse([C - mid, C - mid, C + mid, C + mid], fill=(12, 12, 12, 255), outline=(58, 58, 58, 255), width=6)
    inner = int(SIZE * 0.215)
    d.ellipse([C - inner, C - inner, C + inner, C + inner], fill=(3, 3, 3, 255), outline=(26, 26, 26, 255), width=4)

    # 红色虹膜 + 光晕
    glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gr = int(SIZE * 0.145)
    gd.ellipse([C - gr, C - gr, C + gr, C + gr], fill=(240, 74, 74, 230))
    glow = glow.filter(ImageFilter.GaussianBlur(SIZE * 0.055))
    img = Image.alpha_composite(img, glow)
    d = ImageDraw.Draw(img)

    iris = int(SIZE * 0.135)
    radial_disc(d, C, C, iris, (255, 170, 170), (200, 40, 40), steps=60)
    d.ellipse([C - iris, C - iris, C + iris, C + iris], outline=(255, 130, 130, 150), width=4)

    # 瞳孔与高光
    pupil = int(SIZE * 0.048)
    d.ellipse([C - pupil, C - pupil, C + pupil, C + pupil], fill=(40, 2, 2, 255))
    glint = int(SIZE * 0.018)
    gx, gy = C - iris * 0.38, C - iris * 0.40
    d.ellipse([gx - glint, gy - glint, gx + glint, gy + glint], fill=(255, 235, 235, 220))

    # 镜筒刻度
    for i in range(12):
        a = math.radians(i * 30 - 90)
        r1, r2 = inner + 6, mid + 2
        d.line([(C + r1 * math.cos(a), C + r1 * math.sin(a)),
                (C + r2 * math.cos(a), C + r2 * math.sin(a))],
               fill=(150, 150, 150, 160), width=3)

    # 状态灯
    d.rectangle([inset + 14, inset + 14, inset + 30, inset + 30], fill=(240, 74, 74, 255))
    return img


def main() -> None:
    image = build()
    OUT_PNG.parent.mkdir(parents=True, exist_ok=True)
    OUT_ICO.parent.mkdir(parents=True, exist_ok=True)

    icon_256 = image.resize((256, 256), Image.LANCZOS)
    icon_256.save(OUT_PNG, format="PNG")
    icon_256.save(OUT_ICO, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    print(f"wrote {OUT_ICO.relative_to(ROOT)}")
    print(f"wrote {OUT_PNG.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
