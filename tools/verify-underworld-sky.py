"""Offline quality gate for the authored cavern panorama, not live skybox acceptance."""
from pathlib import Path
from PIL import Image, ImageStat

root = Path(__file__).resolve().parents[1]
path = root / "assets/textures/underworld/sky/lava-fungal-roof-v2.png"
with Image.open(path) as source:
    assert source.format == "PNG", "Sky asset must be a PNG"
    image = source.convert("RGB")
w, h = image.size
assert w == h * 2 and w >= 1024, "Sky asset must be a detailed 2:1 equirectangular panorama"
pixels = list(image.get_flattened_data() if hasattr(image, "get_flattened_data") else image.getdata())
assert max(ImageStat.Stat(image).stddev) > 8, "Roof cannot be a flat dark swatch"
lava = sum(r > 45 and r > g * 1.5 and r > b * 1.5 for r, g, b in pixels)
fungus = sum(g > 40 and b > 35 and g > r * 1.3 and b > r * 1.2 for r, g, b in pixels)
assert lava > w * h * .0003, "Molten fissures must remain visible"
assert fungus > w * h * .0003, "Fungal star colonies must remain visible"
bright = sum(max(p) > 180 for p in pixels) / (w * h)
assert .0001 < bright < .04, "Emission must be localized rather than a bright daytime sky"
edge = sum(sum(abs(image.getpixel((0, y))[c] - image.getpixel((w-1, y))[c]) for c in range(3)) / 3
           for y in range(h)) / h
assert edge < 8, "Panorama wrap seam has a conspicuous colour discontinuity"
assert max(ImageStat.Stat(image.crop((0, h * 3 // 4, w, h))).mean) < 35, "Lower hemisphere must fade to cavern darkness"
print(f"PASS: cavern roof {w}x{h}; lava={lava} pixels; fungus={fungus} pixels; bright={bright:.3%}; wrap difference={edge:.2f}/255.")

with Image.open(path.with_name("lava-fungal-roof-emission.png")) as source:
    assert source.size == image.size, "Emission mask must align with the panorama"
    mask = source.convert("L")
values = list(mask.get_flattened_data() if hasattr(mask, "get_flattened_data") else mask.getdata())
coverage = sum(value > 16 for value in values) / len(values)
assert .001 < coverage < .10, "Emission must isolate luminous colonies and lava, not the whole roof"
assert max(values) > 200 and min(values) == 0, "Emission needs bright cores and unlit rock"
assert max(ImageStat.Stat(mask.crop((0, h * 3 // 4, w, h))).mean) < 1, "Lower haze must not emit light"
print(f"PASS: pixel-aligned emission mask; luminous coverage={coverage:.3%}; dark lower hemisphere.")
