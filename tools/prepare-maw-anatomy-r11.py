from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];T=ROOT/'tools'
for source,target in [('refine-blackwater-maw-left-facing','refine-blackwater-maw-anatomy-r11'),('verify-blackwater-maw-left-facing-raw','verify-blackwater-maw-anatomy-r11-raw'),('process-blackwater-maw-left-facing','process-blackwater-maw-anatomy-r11'),('review-blackwater-maw-left-facing','review-blackwater-maw-anatomy-r11')]:
 s=(T/(source+'.py')).read_text().replace('blackwater-maw-left-facing-r10','blackwater-maw-anatomy-r11').replace('blackwater_maw_left_facing','blackwater_maw_anatomy_r11')
 if source.startswith('process'):
  s=s.replace("for filename in", "import json\nraw_triangles=str(json.loads((ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-anatomy-r11-raw/refinement-metrics.json').read_text())['triangles'])\nfor filename in")
  s=s.replace("replace('9252','7600')","replace('9252',raw_triangles)")
 (T/(target+'.py')).write_text(s)
print('Prepared anatomy r11 pipeline')
