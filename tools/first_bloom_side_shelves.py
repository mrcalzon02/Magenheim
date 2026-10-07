"""Keep pelvic mushrooms lateral and expose the central bark torso strip."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/first_bloom_pelvic_shelves.py').read_text().replace('first-bloom-pelvic-r17-raw','first-bloom-sides-r18-raw')
source=source.replace('center=Vector((s*(.235+.045*k),.115+.045*k,1.30-.16*k))',"center=Vector((s*(.245+.045*k),.005+.025*k,1.30-.16*k+(.025 if side=='L' else 0)))")
source=source.replace('((.37+.045*k)*math.cos(a)*r*wave,.25*math.sin(a)*r,z)', '(s*(.22+.015*k)*(1+math.cos(a))*r*wave,.25*math.sin(a)*r,z)')
source=source.replace('if inside(p,hosttree):return p','if abs(p.x)>=.20 and inside(p,hosttree):return p')
source=source.replace("o.data.update()\n   records.append", "o.data.update()\n   assert all(s*(o.matrix_world@v.co).x>=.20 for v in o.data.vertices),'cap crosses the central bark strip'\n   records.append")
source=source.replace('overlapping lower-torso mushroom shelves flaring over hip joints','lateral lower-torso shelves with a continuous exposed central bark strip at least 0.40 authored units wide')
exec(compile(source,str(__file__),'exec'))
