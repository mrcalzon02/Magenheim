#!/usr/bin/env python3
"""Build paged Rootwarren context/top-down contact sheets."""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT / "dist/underworld-production-review"
RENDERS = BASE / "rootwarren-renders"
SHEETS = BASE / "sheets"
SHEETS.mkdir(parents=True, exist_ok=True)

PREFIX = "underworld-dungeon-fungal-rootwarren-"
SUFFIXES = [
    "fracture-mouth","mycelial-gallery","glowcap-vault","spore-basin","root-bridge",
    "sunken-nursery","tangle-junction","shelf-drop","amber-grotto","worldroot-hollow",
    "crawler-nest","stalker-den","puffback-graze","buried-archway","root-squeeze",
    "heartcap-sanctum","passage",
]
font = ImageFont.load_default()
thumb_w, thumb_h = 320, 240
label_h = 34
models_per_page = 6

for page, start in enumerate(range(0, len(SUFFIXES), models_per_page), 1):
    batch = SUFFIXES[start:start+models_per_page]
    rows = len(batch)
    sheet = Image.new("RGB", (thumb_w*2, rows*(thumb_h+label_h)), (18, 24, 22))
    draw = ImageDraw.Draw(sheet)
    for row, suffix in enumerate(batch):
        model_id = PREFIX + suffix
        y = row * (thumb_h + label_h)
        for column, mode in enumerate(("context", "top")):
            path = RENDERS / f"{model_id}--{mode}.png"
            if not path.is_file():
                raise RuntimeError("Missing Rootwarren review render " + str(path))
            image = Image.open(path).convert("RGB")
            image.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
            x = column * thumb_w + (thumb_w-image.width)//2
            sheet.paste(image, (x, y + (thumb_h-image.height)//2))
        draw.text((8, y+thumb_h+8), suffix, fill=(225,238,230), font=font)
        draw.text((thumb_w+8, y+thumb_h+8), "top-down", fill=(170,200,190), font=font)

    target = SHEETS / f"rootwarren-{page:02d}.png"
    sheet.save(target)
    print("BUILT", target.name, flush=True)

print("BUILT Rootwarren review sheets", math.ceil(len(SUFFIXES)/models_per_page), flush=True)
