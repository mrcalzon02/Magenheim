"""Verify native donor IDs against the installed game's actual soft-asset manifest."""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
manifest = (ROOT.parent / 'valheim_Data/StreamingAssets/SoftRef/manifest_extended').read_text(encoding='utf-8')
for asset_id, asset_path, source in [
    ('22a0a0b6f09802a0f861b9789a1bde32', 'Assets/world/Props/DeepNorth/LastBossGate/LastBossGate.prefab', 'UnderworldDeepGateRegistrar.cs'),
    ('e1e858596580e684788c10ca60865118', 'Assets/Shaders/ParticleUnlit.shader', 'UnderworldSkyboxPresentation.cs'),
]:
    block = manifest.split('- asset ID: ' + asset_id + '\n', 1)
    assert len(block) == 2 and 'path in bundle: ' + asset_path in block[1].split('- asset ID:', 1)[0], asset_path
    assert asset_id in (ROOT / 'src/Magenheim.Runtime' / source).read_text(encoding='utf-8'), source
print('PASS: Aesir boss gate and cavern background shader IDs resolve to the actual installed native assets.')
