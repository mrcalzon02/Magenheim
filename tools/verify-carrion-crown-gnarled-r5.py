from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/verify-carrion-crown-anatomy-r3.py').read_text().replace('carrion-crown-anatomy-r3','carrion-crown-gnarled-r5').replace('2026-10-16','2026-10-25')
exec(compile(s,str(__file__),'exec'))
assert len(records)==120 and len(parts)==len(set(parts.values()))
me=obj.data;labels=me.attributes['attachment_component'];uv=me.uv_layers.active.data
flowers={v for k,v in parts.items() if k.startswith(('CentralCrownCup','GraftedFlower','TimberTooth'))};amber={v for k,v in parts.items() if k.startswith(('CentralAmberSeed','HangingAmberPod'))}
for f in me.polygons:
    component=labels.data[f.index].value;low,high=(.52,.82) if component in flowers else (.84,1.0) if component in amber else (0,.50)
    assert all(low-1e-4<=uv[li].uv.y<=high+1e-4 for li in f.loop_indices)
print('VERIFIED explicit wood/flower/amber atlas allocation and 120 named supports')
