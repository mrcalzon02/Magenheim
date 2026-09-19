#!/usr/bin/env python3
"""Stage and review the remaining Earth icon migration without touching committed assets.

Runs the authoritative generator into a temporary directory, then emits a contact sheet that
shows each migration target at source resolution and at Valheim-like 64 px inventory scale.
The review intentionally includes the four model-backed process identities plus geode and
Crystal Shaping so a 256 px dimension increase cannot masquerade as improved readability.
"""
from __future__ import annotations

import os
import subprocess
import sys
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageStat

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets" / "earth"
GENERATOR = ROOT / "tools" / "generate-earth-assets.py"
TARGETS = (
    "workstation",
    "fracturing-block",
    "faceting-wheel",
    "resonance-frame",
    "geode",
    "crystal-shaping",
)
GAMEPLAY = 64


def font(size: int, bold: bool = False):
    names = [
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf" if bold else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
        "C:/Windows/Fonts/segoeuib.ttf" if bold else "C:/Windows/Fonts/segoeui.ttf",
    ]
    for name in names:
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def metrics(image: Image.Image):
    proxy = image.convert("RGBA").resize((GAMEPLAY, GAMEPLAY), Image.Resampling.LANCZOS)
    alpha = proxy.getchannel("A")
    mask = alpha.point(lambda value: 255 if value >= 96 else 0)
    bbox = mask.getbbox()
    if not bbox:
        return proxy, 0.0, 0.0, 0.0
    opaque = sum(1 for value in alpha.getdata() if value >= 96)
    coverage = opaque / (GAMEPLAY * GAMEPLAY)
    left, top, right, bottom = bbox
    clearance = min(left, top, GAMEPLAY - right, GAMEPLAY - bottom) / GAMEPLAY
    deviation = ImageStat.Stat(proxy.convert("L"), mask=mask).stddev[0]
    return proxy, coverage, clearance, deviation


def main() -> int:
    output_arg = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else ROOT / "artifacts" / "earth-icon-migration-review.png"
    output_arg.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="magenheim-earth-review-") as temp:
        stage = Path(temp)
        env = os.environ.copy()
        env["MAGENHEIM_EARTH_OUTPUT_ROOT"] = str(stage)
        subprocess.run([sys.executable, str(GENERATOR)], cwd=ROOT, env=env, check=True)

        width, row_h = 1080, 210
        sheet = Image.new("RGB", (width, 110 + row_h * len(TARGETS)), (27, 30, 29))
        draw = ImageDraw.Draw(sheet)
        draw.text((30, 20), "MAGENHEIM / EARTH ICON MIGRATION REVIEW", font=font(30, True), fill=(235, 219, 181))
        draw.text((30, 62), "Committed legacy  |  regenerated source  |  64 px gameplay", font=font(18), fill=(166, 174, 164))
        failures = []

        for row, name in enumerate(TARGETS):
            y = 105 + row * row_h
            staged_path = stage / f"{name}.icon.png"
            committed_path = SOURCE / f"{name}.icon.png"
            if not staged_path.exists():
                failures.append(f"{name}: generator did not emit {staged_path.name}")
                continue
            staged = Image.open(staged_path).convert("RGBA")
            committed = Image.open(committed_path).convert("RGBA") if committed_path.exists() else Image.new("RGBA", (128, 128))
            proxy, coverage, clearance, deviation = metrics(staged)
            draw.text((30, y + 10), name.replace("-", " ").title(), font=font(20, True), fill=(229, 216, 189))
            draw.text((30, y + 42), f"new {staged.width}x{staged.height}  coverage {coverage:.1%}  edge {clearance:.1%}  contrast σ {deviation:.1f}", font=font(15), fill=(166, 174, 164))
            old = committed.resize((150, 150), Image.Resampling.NEAREST)
            new = staged.resize((150, 150), Image.Resampling.LANCZOS)
            game = proxy.resize((150, 150), Image.Resampling.NEAREST)
            sheet.paste(old, (465, y + 10), old)
            sheet.paste(new, (650, y + 10), new)
            sheet.paste(game, (835, y + 10), game)
            if staged.size[0] < 256 or staged.size[1] < 256:
                failures.append(f"{name}: regenerated icon is still below 256 px")
            if not (0.08 <= coverage <= 0.78):
                failures.append(f"{name}: gameplay coverage {coverage:.1%} outside 8-78%")
            if clearance < 0.01:
                failures.append(f"{name}: gameplay silhouette crowds the frame ({clearance:.1%})")

        sheet.save(output_arg)
        print(f"Earth icon migration review: {output_arg}")
        if failures:
            for failure in failures:
                print(f"FAIL: {failure}", file=sys.stderr)
            return 1
        print("PASS: all six Earth migration targets regenerate at 256 px+ and survive the 64 px readability envelope")
        return 0


if __name__ == "__main__":
    raise SystemExit(main())
