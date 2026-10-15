"""Isolate actual saved head/neck/collar for an unobstructed neck-depth review."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-first-bloom-refinement.py').read_text(encoding='utf-8').replace('first-bloom-refinement','rift-titan-lined-volume-r14').replace('creature-models/2026-10-04','creature-models/2026-10-14').replace("row.get('study_height_metres',4.65)","row.get('study_height_metres',7.0)")
start=s.index('for view,direction in [');end=s.index(' target=center;span=size',start)
s=s[:start]+"for view,direction in [('neck-front',(0,2,.15)),('neck-side',(-2,.65,.15))]:\n"+s[end:]
s=s.replace('deformation={}', '''deformation={}
source_before=hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest()
parts=json.loads(scene['silhouette_components']);keep={v for k,v in parts.items() if k=='UnifiedCrystalHead' or k=='StoneCollarbone' or k.startswith('NeckMetalRib')}
labels=o.data.attributes['attachment_component'];bm=bmesh.new();bm.from_mesh(o.data);bm.faces.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[f for f in bm.faces if labels.data[f.index].value not in keep],context='FACES');bm.to_mesh(o.data);bm.free()
labels=o.data.attributes['attachment_component'];neck_parts={v for k,v in parts.items() if k.startswith('NeckMetalRib')};indices={i for f in o.data.polygons if labels.data[f.index].value in neck_parts for i in f.vertices}
p=[o.matrix_world@o.data.vertices[i].co for i in indices];neck_lo=Vector([min(v[k] for v in p) for k in range(3)]);neck_hi=Vector([max(v[k] for v in p) for k in range(3)])
focus=(neck_lo+neck_hi)/2;neck_span=max(neck_hi-neck_lo)*1.55
''')
s=s.replace(' target=center;span=size',' target=focus;span=neck_span').replace("view=='clay'","True")
s=s.replace(" scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))", " if view=='neck-side':assert cam.rotation_euler.to_matrix().col[0].dot(Vector((0,1,0)))<0\n scene.render.filepath=str(OUT/(ID+'--'+view+'.png'))")
s=s[:s.index('data=(OUT/(ID+')]
s+="\nassert source_before==hashlib.sha256((ROOT/row['source']).read_bytes()).hexdigest()\n"
exec(compile(s,str(__file__),'exec'))
