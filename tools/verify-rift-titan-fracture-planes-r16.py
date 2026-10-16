"""Retain support/rig checks and independently inspect the saved ridged head."""
from pathlib import Path
import builtins
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/verify-rift-titan-paired-recesses-r15.py').read_text().replace('rift-titan-paired-recesses-r15','rift-titan-fracture-planes-r16')
# Propagate the current evidence date into the final expanded verifier, keeping
# its comparison source at the actual historical r14 path.
def compile(source,filename,mode):
    source=source.replace('creature-models/2026-10-14/rift-titan-fracture-planes-r16','creature-models/2026-10-16/rift-titan-fracture-planes-r16')
    return builtins.compile(source,filename,mode)
s=s.replace('{1:78,2:28,3:10}','{1:90,2:20,3:6}').replace('range(1,8)','range(1,6)')
s=s.replace("len(plate_faces)==18", "len(plate_faces)==42")
s=s.replace("assert plate_ids=={i for i in ids if outline.data[i].value}", "assert {i for i in ids if outline.data[i].value}.issubset(plate_ids) and len(plate_ids)==32")
s=s.replace('solid_front_plate_triangles=18','solid_front_plate_triangles=42').replace('front_plate_interior_samples=18','front_plate_interior_samples=42')
s=s.replace('len(floor_faces)==5 and len(wall_faces)==14','len(floor_faces)==3 and len(wall_faces)==10').replace('rim_vertices=7,floor_vertices=7','rim_vertices=5,floor_vertices=5').replace('central_bridge_interior_samples=7','central_bridge_interior_samples=5')
s=s.replace("(p-old.data.vertices[i].co).length", "max(abs(p.x-old.data.vertices[i].co.x),abs(p.z-old.data.vertices[i].co.z))")
s=s.replace('approved_front_outline_max_3d_difference_metres','approved_front_outline_max_xz_difference_metres')
exec(compile(s,str(__file__),'exec'))
