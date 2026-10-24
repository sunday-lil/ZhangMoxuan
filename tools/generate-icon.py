"""生成 MOSS 之眼应用图标（多尺寸 .ico）。

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
CENTER = SIZE / 2

BG = (10, 14, 20, 255)
RED = (216, 67, 76, 255)
RED_BRIGHT = (242, 85, 94, 255)
RED_DIM = (107, 43, 51, 255)


def radial_disc(draw: ImageDraw.ImageDraw, cx: float, cy: float, radius: float,
                inner: tuple[int, int, int], outer: tuple[int, int, int], steps: int = 96) -> None:
    """用同心圆模拟径向渐变。"""
    for i in range(steps, 0, -1):
        t = i / steps
        color = tuple(round(inner[c] + (outer[c] - inner[c]) * t) for c in range(3))
        r = radius * t
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=color + (255,))


def build() -> Image.Image:
    image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    # 圆角深色底板
    draw.rounded_rectangle([0, 0, SIZE - 1, SIZE - 1], radius=int(SIZE * 0.17), fill=BG)

    # 眼体：深色球面
    ball_r = SIZE * 0.355
    radial_disc(draw, CENTER, CENTER, ball_r, (26, 10, 15), (4, 5, 8))
    draw.ellipse(
        [CENTER - ball_r, CENTER - ball_r, CENTER + ball_r, CENTER + ball_r],
        outline=(42, 16, 21, 255),
        width=max(2, int(SIZE * 0.006)),
    )

    # 瞳孔外发光（在瞳孔之下）
    glow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    glow_draw = ImageDraw.Draw(glow)
    glow_r = SIZE * 0.175
    glow_draw.ellipse(
        [CENTER - glow_r, CENTER - glow_r, CENTER + glow_r, CENTER + glow_r],
        fill=RED_BRIGHT[:3] + (210,),
    )
    glow = glow.filter(ImageFilter.GaussianBlur(SIZE * 0.07))
    image = Image.alpha_composite(image, glow)
    draw = ImageDraw.Draw(image)

    # 外圈刻度环
    ring_r = SIZE * 0.435
    for i in range(72):
        angle = math.radians(i * 5 - 90)
        major = i % 6 == 0
        inner = ring_r - (SIZE * 0.035 if major else SIZE * 0.018)
        outer = ring_r
        color = (RED if major else RED_DIM)
        width = max(1, int(SIZE * (0.0045 if major else 0.0022)))
        draw.line(
            [
                CENTER + inner * math.cos(angle),
                CENTER + inner * math.sin(angle),
                CENTER + outer * math.cos(angle),
                CENTER + outer * math.sin(angle),
            ],
            fill=color,
            width=width,
        )

    # 扫描弧
    arc_r = SIZE * 0.405
    draw.arc(
        [CENTER - arc_r, CENTER - arc_r, CENTER + arc_r, CENTER + arc_r],
        start=-70,
        end=35,
        fill=RED_BRIGHT,
        width=max(2, int(SIZE * 0.005)),
    )

    # 内环
    inner_r = SIZE * 0.30
    draw.ellipse(
        [CENTER - inner_r, CENTER - inner_r, CENTER + inner_r, CENTER + inner_r],
        outline=(107, 43, 51, 170),
        width=max(1, int(SIZE * 0.003)),
    )

    # 红色瞳孔
    pupil_r = SIZE * 0.15
    radial_disc(draw, CENTER, CENTER, pupil_r, (255, 178, 184), (216, 67, 76), steps=64)
    draw.ellipse(
        [CENTER - pupil_r, CENTER - pupil_r, CENTER + pupil_r, CENTER + pupil_r],
        outline=(255, 120, 128, 120),
        width=max(1, int(SIZE * 0.003)),
    )

    # 高光
    glint_r = pupil_r * 0.16
    gx, gy = CENTER - pupil_r * 0.42, CENTER - pupil_r * 0.42
    draw.ellipse([gx - glint_r, gy - glint_r, gx + glint_r, gy + glint_r], fill=(255, 232, 232, 190))

    return image


def main() -> None:
    image = build()
    OUT_PNG.parent.mkdir(parents=True, exist_ok=True)
    OUT_ICO.parent.mkdir(parents=True, exist_ok=True)

    icon_256 = image.resize((256, 256), Image.LANCZOS)
    icon_256.save(OUT_PNG, format="PNG")

    sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    icon_256.save(OUT_ICO, format="ICO", sizes=sizes)
    print(f"wrote {OUT_ICO.relative_to(ROOT)}")
    print(f"wrote {OUT_PNG.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
