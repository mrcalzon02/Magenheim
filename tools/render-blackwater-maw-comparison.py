"""Render the prior study and native donor with the established review framing."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-boss-quality-comparison.py').read_text()
source=source.replace("OUT=ROOT/'docs/art/creature-models/2026-10-04/detail-r2'", "OUT=ROOT/'docs/art/creature-models/2026-10-04/blackwater-maw-refinement-r4'")
start=source.index('args=sys.argv');end=source.index('for row in records:',start)
source=source[:start]+"records=[dict(id='donor-Serpent',source='dist/boss-donor-quality/native-source/Serpent.blend',native=True),dict(id='before-blackwater-maw',source='assets/models/source/underworld-boss-blackwater-maw-detail-r2.blend')]\n"+source[end:]
source=source.replace("span=size*(.31 if 'blackwater-maw' in row['id'] else .43)","span=size*.43")
exec(compile(source,str(__file__),'exec'))
