from pathlib import Path
import json
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/refine-first-bloom-radiating.py').read_text().replace('first-bloom-radiating-r7','first-bloom-seated-r9').replace('first_bloom_radiating_frills','first_bloom_cavity_attachment')
exec(compile(source,str(__file__),'exec'))
path=ROOT/'docs/art/creature-models/2026-10-04/first-bloom-seated-r9/refinement-metrics.json'
metrics=json.loads(path.read_text());baseline=json.loads((ROOT/'docs/art/creature-models/2026-10-04/first-bloom-fan-r8/refinement-metrics.json').read_text())
metrics.update(previous_triangles=baseline['triangles'],previous_id=baseline['id'],reduction_percent=(1-metrics['triangles']/baseline['triangles'])*100)
path.write_text(json.dumps(metrics,indent=2)+'\n')
