"""Export the final atlas material without unused construction paint streams."""
import bpy,json,hashlib,struct,sys,argparse,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('--slug',required=True);args=p.parse_args(sys.argv[sys.argv.index('--')+1:])
ID='underworld-boss-'+args.slug;out=ROOT/'docs/art/creature-models/2026-10-16'/args.slug
source=ROOT/'assets/models/studies'/(ID+'.blend');original=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source));arm=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');o=next(o for o in bpy.context.scene.objects if o.type=='MESH')
assert len([o for o in bpy.context.scene.objects if o.type=='MESH'])==len(o.data.materials)==1
arm.animation_data.action=None;arm.data.pose_position='REST'
images=[n.image for n in o.data.materials[0].node_tree.nodes if n.type=='TEX_IMAGE'];assert len(images)==4 and all(tuple(im.size)==(512,512) and im.packed_file for im in images)
path=out/(ID+'.glb');bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',export_animations=False,export_yup=True,export_vertex_color='NONE')
raw=path.read_bytes();size,kind=struct.unpack_from('<II',raw,12);doc=json.loads(raw[20:20+size])
assert len(doc['meshes'])==len(doc['materials'])==1 and len(doc['images'])==4 and len(doc['skins'])==1
primitives=doc['meshes'][0]['primitives'];assert len(primitives)==1
attr=primitives[0]['attributes'];assert set(attr)=={'POSITION','NORMAL','TEXCOORD_0','JOINTS_0','WEIGHTS_0'}
offset=20+size;bin_size,bin_kind=struct.unpack_from('<II',raw,offset);binary=raw[offset+8:offset+8+bin_size]
def values(index):
    a=doc['accessors'][index];v=doc['bufferViews'][a['bufferView']];start=v.get('byteOffset',0)+a.get('byteOffset',0)
    width={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']];fmt={5126:'f',5125:'I',5123:'H',5121:'B'}[a['componentType']];length=struct.calcsize('<'+fmt*width);stride=v.get('byteStride',length)
    return [struct.unpack_from('<'+fmt*width,binary,start+i*stride) for i in range(a['count'])]
positions=values(attr['POSITION']);weights=values(attr['WEIGHTS_0']);indices=values(primitives[0]['indices']);joints=values(attr['JOINTS_0'])
assert all(math.isfinite(c) for p in positions for c in p)
assert all(abs(sum(w)-1)<1e-5 and sum(v>1e-7 for v in w)<=2 for w in weights)
assert all(0<=i[0]<len(positions) for i in indices)
assert all(max(row)<len(doc['skins'][0]['joints']) for row in joints)
metrics=json.loads((out/'refinement-metrics.json').read_text());assert len(indices)//3==metrics['triangles']
assert hashlib.sha256(source.read_bytes()).hexdigest()==original
report=dict(result='PASS',source_sha256=original,glb_sha256=hashlib.sha256(raw).hexdigest(),triangles=len(indices)//3,vertices=len(positions),attributes=list(attr),atlas_maps=4,atlas_pixels=512,skin_joints=len(doc['skins'][0]['joints']),unused_paint_streams_exported=False,source_unchanged=True,scope='actual exported accessors, compact normalized skin weights, indices, embedded maps and no unused paint channels; native acceptance separate')
(out/'portable-export-verification.json').write_text(json.dumps(report,indent=2)+'\n')
print('VERIFIED compact atlas-only portable skin export; saved source unchanged')
