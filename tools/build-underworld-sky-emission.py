"""Extract a pixel-aligned material data mask; the authored colour panorama is unchanged."""
from pathlib import Path
from PIL import Image
root = Path(__file__).resolve().parents[1]
folder = root / "assets/textures/underworld/sky"
with Image.open(folder / "lava-fungal-roof-v2.png") as source:
    image = source.convert("RGB")
pixels = image.get_flattened_data() if hasattr(image, "get_flattened_data") else image.getdata()
values = []
for r, g, b in pixels:
    peak = max(r, g, b)
    chroma = max(r - max(g, b), min(g, b) - r)
    colour_glow = min(1, max(0, (chroma - 8) / 40)) * min(1, max(0, (peak - 25) / 110))
    white_core = min(1, max(0, (peak - 170) / 85))
    values.append(round(max(colour_glow, white_core) * 255))
mask = Image.new("L", image.size)
mask.putdata(values)
mask.save(folder / "lava-fungal-roof-emission.png", compress_level=9)
print(f"Baked pixel-aligned roof emission mask: {image.width}x{image.height}; colour artwork unchanged.")
