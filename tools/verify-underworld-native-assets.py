"""Verify native donor IDs against the installed game's actual soft-asset manifest."""
from pathlib import Path
import re
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

# Bind the configured flora donor to the installed manifest rather than trusting a plausible name.
flora_source = (ROOT / 'src/Magenheim.Runtime/UnderworldFloraWorldgenRegistrar.cs').read_text(encoding='utf-8')
donor = re.search(r'NativeTreeDonor\s*=\s*"([^"]+)"', flora_source).group(1)
prefab_names = {Path(path.strip()).stem for path in re.findall(r'path in bundle: (.+\.prefab)', manifest)}
assert donor in prefab_names, f'Native flora donor is absent from the installed game: {donor}'
catalog = (ROOT / 'src/Magenheim.Core/Underworld/UnderworldResourceCatalog.cs').read_text(encoding='utf-8')
pickup_donors = set(re.findall(r'"(Pickable_[^"]+)"', catalog))
assert pickup_donors, 'No resource pickup donors inspected'
assert pickup_donors <= prefab_names, f'Missing native pickup donors: {pickup_donors - prefab_names}'
print(f'PASS: flora donor {donor} and all {len(pickup_donors)} resource pickup donor names exist in the installed manifest.')
