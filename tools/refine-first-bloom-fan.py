from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-radiating.py').read_text().replace('first-bloom-radiating-r7','first-bloom-fan-r8').replace('first_bloom_radiating_frills','first_bloom_fan_guide')
exec(compile(source,str(__file__),'exec'))
import json
path=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-fan-r8/refinement-metrics.json'
metrics=json.loads(path.read_text())
baseline=json.loads((ROOT/'docs/art/creature-models/2026-10-04/first-bloom-radiating-r7/refinement-metrics.json').read_text())
metrics['previous_triangles']=baseline['triangles'];metrics['previous_id']=baseline['id']
metrics['reduction_percent']=(1-metrics['triangles']/baseline['triangles'])*100
path.write_text(json.dumps(metrics,indent=2)+'\n')
