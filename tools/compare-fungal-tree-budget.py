"""Measured installed vanilla meshes versus actual exported authored canopy payloads."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
before = {'glowcap':2776, 'glowcap-bent':7442, 'glowcap-elder':9422,
          'glowcap-twin':11618, 'spirestalk':4720, 'spirestalk-young':3440,
          'spirestalk-towered':7280, 'tanglecap':5008, 'puffcap-clustered':11674}
lines = ['# Mushroom / installed vanilla tree geometry comparison', '',
         'Native measurements come from inspect-native-tree-cost.py against the installed game assets.',
         'Counts are individual mesh triangles, not frame-rate measurements.', '',
         '| Installed tree | Near mesh | Further meshes |', '|---|---:|---|']
for raw in (ROOT/'dist/native-tree-cost-comparison.jsonl').read_text().splitlines():
    row=json.loads(raw)
    if 'native' not in row:
        continue
    near=row['meshes'][0]
    further=', '.join(f'{m[0]}: {m[1]}' for m in row['meshes'][1:]) or 'native billboard / culling'
    lines.append(f"| {row['native']} | {near[1]} | {further} |")
lines += ['', 'Pine is an expensive native outlier; Beech and Fir are the density budget references.', '',
          '| Authored mushroom | Previous triangles | Rebuilt triangles | Reduction |', '|---|---:|---:|---:|']
for model, old in before.items():
    data=json.loads((ROOT/f'assets/models/runtime/underworld-flora-fungal-{model}.model.json').read_text())
    count=sum(len(part['triangles'])//3 for part in data['parts'])
    assert count <= 596, (model,count)
    lines.append(f'| {model} | {old} | {count} | {1-count/old:.1%} |')
for path in (ROOT/'assets/models/runtime').glob('underworld-flora-fungal-*.model.json'):
    data=json.loads(path.read_text())
    count=sum(len(part['triangles'])//3 for part in data['parts'])
    assert count <= 596, (path.stem,count)
lines += ['', 'The 24 Fungal Forest payloads retain their owned textures and have native LODGroup distance culling.',
          'They retain multiple material parts and do not yet reproduce Beech’s intermediate/billboard LODs.',
          'Equivalent live forest frame rate is pending measurement; triangle reduction alone does not establish it.', '']
output=ROOT/'docs/validation/2026-10-03-fungal-tree-comparison.md'
output.write_text('\n'.join(lines),encoding='utf-8')
print('\n'.join(lines))
