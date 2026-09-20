#!/usr/bin/env python3
"""Generate and validate deterministic PBR foundations for the six Underworld terrain biomes.

These are Magenheim-authored textures, not extracted Valheim assets. They are intended to replace
32px runtime placeholders once the runtime material loader is switched to packaged assets.
Each 256px tile represents the existing four-metre terrain UV tile and emits albedo, normal and
roughness maps with biome-specific surface language. Generation fails if a map collapses below
basic production-readability thresholds so flat or malformed terrain cannot be accepted silently.
"""
from pathlib import Path
import math
from PIL import Image, ImageStat

SIZE = 256
OUT = Path(__file__).resolve().parents[1] / "assets" / "textures" / "underworld" / "terrain"
BIOMES = {
    "fungal_forest": ((48, 82, 69), 0.78, 0.75),
    "blackwater_deep": ((18, 32, 48), 0.42, 0.38),
    "sulfurous_wastes": ((105, 78, 38), 0.76, 1.00),
    "frozen_caverns": ((91, 124, 137), 0.48, 0.72),
    "fracture_zones": ((82, 57, 67), 0.82, 1.12),
    "great_decay": ((65, 72, 45), 0.88, 0.90),
}


def hash01(x, y, seed):
    n = (x * 374761393 + y * 668265263 + seed * 1442695041) & 0xffffffff
    n = ((n ^ (n >> 13)) * 1274126177) & 0xffffffff
    return ((n ^ (n >> 16)) & 0x7fffffff) / 2147483647.0


def structure(name, x, y):
    # Coordinates are normalized from the old 32px prototype so motifs retain physical scale.
    qx, qy = x / 8.0, y / 8.0
    if name == "fungal_forest":
        return 0.13 * max(0.0, math.sin(qx * .72 + qy * 1.38))
    if name == "blackwater_deep":
        # Preserve the subdued drowned-sediment identity while retaining enough broad-band
        # separation to survive terrain mip reduction at ordinary gameplay distance.
        return -0.12 if int(qy) % 8 < 2 else 0.03
    if name == "sulfurous_wastes":
        return 0.15 if int(qx * 3 + qy) % 13 < 2 else -0.02
    if name == "frozen_caverns":
        return 0.17 if int(abs(qx - qy)) % 9 == 0 else 0.0
    if name == "fracture_zones":
        return -0.19 if int(abs(qx * 2 - qy)) % 13 < 2 else 0.015
    if name == "great_decay":
        pits = math.sin(qx * .9 + math.sin(qy * .7))
        return -0.12 if pits > .72 else 0.01
    return 0.0


def height(name, x, y, seed):
    coarse = hash01(x // 32, y // 32, seed)
    medium = hash01(x // 8, y // 8, seed * 3)
    fine = hash01(x, y, seed * 7)
    return max(.42, min(1.20, .69 + coarse*.18 + medium*.10 + fine*.055 + structure(name, x, y)))


def wrap(v):
    return v % SIZE


def channel_range(image, channel=0):
    extrema = image.getextrema()
    if isinstance(extrema[0], tuple):
        lo, hi = extrema[channel]
    else:
        lo, hi = extrema
    return hi - lo


def validate_maps(name, albedo, normal, rough):
    """Reject technically valid PNGs that have lost useful gameplay-distance material information."""
    if albedo.size != (SIZE, SIZE) or normal.size != (SIZE, SIZE) or rough.size != (SIZE, SIZE):
        raise RuntimeError(f"{name}: generated map dimensions are not {SIZE}x{SIZE}")

    # Evaluate a 64px mip-like view: this is much closer to terrain seen in play than raw texels.
    reduced_albedo = albedo.resize((64, 64), Image.Resampling.LANCZOS).convert("L")
    reduced_rough = rough.resize((64, 64), Image.Resampling.LANCZOS)
    if channel_range(reduced_albedo) < 12 or ImageStat.Stat(reduced_albedo).stddev[0] < 2.0:
        raise RuntimeError(f"{name}: albedo structure collapses at gameplay distance")
    if channel_range(reduced_rough) < 10 or ImageStat.Stat(reduced_rough).stddev[0] < 1.5:
        raise RuntimeError(f"{name}: roughness response collapses at gameplay distance")

    reduced_normal = normal.resize((64, 64), Image.Resampling.LANCZOS)
    normal_stat = ImageStat.Stat(reduced_normal)
    if max(normal_stat.stddev[0], normal_stat.stddev[1]) < 2.0:
        raise RuntimeError(f"{name}: normal map is effectively flat at gameplay distance")
    if normal_stat.mean[2] < 180:
        raise RuntimeError(f"{name}: normal map has lost its +Z tangent-space orientation")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for index, (name, (base, rough_base, normal_gain)) in enumerate(BIOMES.items(), 1):
        seed = 101 + index * 97
        field = [[height(name, x, y, seed) for x in range(SIZE)] for y in range(SIZE)]
        albedo = Image.new("RGB", (SIZE, SIZE))
        normal = Image.new("RGB", (SIZE, SIZE))
        rough = Image.new("L", (SIZE, SIZE))
        ap, np, rp = albedo.load(), normal.load(), rough.load()
        for y in range(SIZE):
            for x in range(SIZE):
                h = field[y][x]
                shade = max(.55, min(1.15, h))
                ap[x, y] = tuple(max(0, min(255, round(c*shade))) for c in base)
                dx = field[y][wrap(x-1)] - field[y][wrap(x+1)]
                dy = field[wrap(y-1)][x] - field[wrap(y+1)][x]
                nx, ny, nz = dx*normal_gain*4.0, dy*normal_gain*4.0, 1.0
                inv = 1.0 / math.sqrt(nx*nx + ny*ny + nz*nz)
                np[x, y] = (round((nx*inv*.5+.5)*255), round((ny*inv*.5+.5)*255), round((nz*inv*.5+.5)*255))
                micro = (hash01(x, y, seed+811)-.5)*.14
                rv = max(.12, min(.96, rough_base + micro + (1.0-h)*.16))
                rp[x, y] = round(rv*255)

        validate_maps(name, albedo, normal, rough)
        albedo.save(OUT / f"{name}_albedo.png", optimize=True)
        normal.save(OUT / f"{name}_normal.png", optimize=True)
        rough.save(OUT / f"{name}_roughness.png", optimize=True)
        print(f"generated+validated {name}: 256px albedo/normal/roughness")


if __name__ == "__main__":
    main()
