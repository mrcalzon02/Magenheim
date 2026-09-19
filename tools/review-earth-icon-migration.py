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
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat

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
EDGE_CLEARANCE = max(0.0, min(0.10, float(os.environ.get("MAGENHEIM_ICON_MIN_EDGE_CLEARANCE", "0.01"))))


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


def flat_data(image: Image.Image):
    """Return pixels without Pillow 12's deprecated Image.getdata() path."""
    getter = getattr(image, "get_flattened_data", None)
    return getter() if getter is not None else image.getdata()


def metrics(image: Image.Image):
    """Measure exactly the same gameplay-visible pixels as verify-earth-assets.py."""
    proxy = image.convert("RGBA").resize((GAMEPLAY, GAMEPLAY), Image.Resampling.LANCZOS)
    alpha = proxy.getchannel("A")
    mask = alpha.point(lambda value: 255 if value >= 96 else 0)
    bbox = mask.getbbox()
    if not bbox:
        return proxy, 0.0, 0.0, 0.0, 0.0
    visible = [p for p, a in zip(flat_data(proxy.convert("RGB")), flat_data(alpha)) if a >= 96]
    luminance = [.2126*r + .7152*g + .0722*b for r, g, b in visible]
    opaque = len(visible)
    coverage = opaque / (GAMEPLAY * GAMEPLAY)
    left, top, right, bottom = bbox
    clearance = min(left, top, GAMEPLAY - right, GAMEPLAY - bottom) / GAMEPLAY
    # Use the thresholded gameplay-visibility mask, not fractional antialias alpha. Otherwise
    # translucent edge pixels influence sigma here while the authoritative verifier ignores them.
    deviation = ImageStat.Stat(proxy.convert("L"), mask=mask).stddev[0]
    contrast = max(luminance) - min(luminance) if luminance else 0.0
    return proxy, coverage, clearance, contrast, deviation


def legacy_delta(committed: Image.Image, staged: Image.Image) -> float:
    """Measure whether the staged result contains visible change beyond a nearest-neighbor upscale."""
    legacy = committed.convert("RGBA").resize(staged.size, Image.Resampling.NEAREST)
    diff = ImageChops.difference(legacy, staged.convert("RGBA")).convert("RGB")
    stat = ImageStat.Stat(diff)
    return sum(stat.mean) / 3.0


def main() -> int:
    output_arg = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else ROOT / "artifacts" / "earth-icon-migration-review.png"
    output_arg.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="magenheim-earth-review-") as temp:
        stage = Path(temp)
        env = os.environ.copy()
        env["MAGENHEIM_EARTH_OUTPUT_ROOT"] = str(stage)
        subprocess.run([sys.executable, str(GENERATOR)], cwd=ROOT, env=env, check=True)

        width, row_h = 1180, 210
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
            proxy, coverage, clearance, contrast, deviation = metrics(staged)
            delta = legacy_delta(committed, staged)
            draw.text((30, y + 10), name.replace("-", " ").title(), font=font(20, True), fill=(229, 216, 189))
            draw.text((30, y + 42), f"new {staged.width}x{staged.height}  coverage {coverage:.1%}  edge {clearance:.1%}", font=font(15), fill=(166, 174, 164))
            draw.text((30, y + 66), f"luma range {contrast:.1f}  sigma {deviation:.1f}  legacy delta {delta:.1f}", font=font(15), fill=(166, 174, 164))
            old = committed.resize((150, 150), Image.Resampling.NEAREST)
            new = staged.resize((150, 150), Image.Resampling.LANCZOS)
            game = proxy.resize((150, 150), Image.Resampling.NEAREST)
            sheet.paste(old, (565, y + 10), old)
            sheet.paste(new, (750, y + 10), new)
            sheet.paste(game, (935, y + 10), game)
            if staged.size[0] < 256 or staged.size[1] < 256:
                failures.append(f"{name}: regenerated icon is still below 256 px")
            if not (0.08 <= coverage <= 0.78):
                failures.append(f"{name}: gameplay coverage {coverage:.1%} outside 8-78%")
            if clearance < EDGE_CLEARANCE:
                failures.append(f"{name}: gameplay silhouette crowds the frame ({clearance:.1%} < {EDGE_CLEARANCE:.1%})")
            if contrast < 28 or deviation < 7:
                failures.append(f"{name}: gameplay material/facet contrast collapses (range={contrast:.1f}, stddev={deviation:.1f})")

        sheet.save(output_arg)
        print(f"Earth icon migration review: {output_arg}")
        if failures:
            for failure in failures:
                print(f"FAIL: {failure}", file=sys.stderr)
            return 1
        print("PASS: all six Earth migration targets match the authoritative 64 px gameplay readability gate")
        return 0


if __name__ == "__main__":
    raise SystemExit(main())
