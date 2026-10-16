"""Inspect actual production GLB/JSON skins and immutable study provenance."""
from pathlib import Path
import hashlib,json,struct
import numpy as np
ROOT=Path(__file__).resolve().parents[1];ASSETS=ROOT/'assets/models';OUT=ROOT/'docs/validation/2026-10-16-refined-boss-runtime'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
reports=[]
for boss in ('first-bloom','rift-titan','carrion-crown'):
    model='underworld-boss-'+boss+'-refined';runtime=ASSETS/'runtime'/(model+'.model.json');glb=ASSETS/'glb'/(model+'.glb');doc=json.loads(runtime.read_text());part=doc['parts'][0];proof=json.loads((OUT/(boss+'-export.json')).read_text())
    assert sha(runtime)==proof['runtime_sha256'] and sha(glb)==proof['glb_sha256'] and sha(ROOT/proof['study'])==proof['source_sha256']==doc['source_sha256']
    assert proof['source_unchanged'] and proof['max_skin_error_metres']<.00015 and proof['compared_posed_vertices']>0
    raw=glb.read_bytes();size,kind=struct.unpack_from('<II',raw,12);gltf=json.loads(raw[20:20+size]);offset=20+size;length,kind=struct.unpack_from('<II',raw,offset);binary=raw[offset+8:offset+8+length]
    def accessor(index):
        a=gltf['accessors'][index];view=gltf['bufferViews'][a['bufferView']];start=view.get('byteOffset',0)+a.get('byteOffset',0);width={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']];fmt={5126:'f',5125:'I',5123:'H',5121:'B'}[a['componentType']];length=struct.calcsize('<'+fmt*width);stride=view.get('byteStride',length)
        return np.array([struct.unpack_from('<'+fmt*width,binary,start+i*stride) for i in range(a['count'])])
    assert len(gltf['meshes'])==len(gltf['skins'])==len(gltf['materials'])==1
    primitive=gltf['meshes'][0]['primitives'][0];attrs=primitive['attributes'];assert set(attrs)=={'POSITION','NORMAL','TEXCOORD_0','JOINTS_0','WEIGHTS_0'}
    order=accessor(primitive['indices']).ravel().astype(int);positions=accessor(attrs['POSITION'])[order];expected=np.array(part['vertices']);assert positions.shape==expected.shape
    position_error=float(np.max(np.linalg.norm(positions-expected,axis=1)));assert position_error<.00003,(model,'GLB/JSON face position drift',position_error)
    names=[gltf['nodes'][i]['name'] for i in gltf['skins'][0]['joints']];native_names=[b['name'] for b in doc['creatureRig']['bones']];assert set(names)==set(native_names)
    joints=accessor(attrs['JOINTS_0'])[order].astype(int);weights=accessor(attrs['WEIGHTS_0'])[order];assert np.max(np.abs(weights.sum(axis=1)-1))<1e-5
    maximum_weight_error=0.
    for i,row in enumerate(part['skinWeights']):
        actual={names[j]:float(w) for j,w in zip(joints[i],weights[i]) if w>1e-7};wanted={native_names[b]:w for b,w in row}
        assert set(actual)==set(wanted),(model,'GLB skin joint mismatch',i)
        maximum_weight_error=max(maximum_weight_error,max(abs(actual[n]-wanted[n]) for n in wanted))
    assert maximum_weight_error<1e-5
    textures={part['material'][k]:sha(ASSETS/'textures'/part['material'][k]) for k in ('texture','normalTexture','metallicGlossTexture','emissionTexture')};assert len(textures)==4
    assert all(name==digest[:16]+'.png' for name,digest in textures.items()),(model,'Content-addressed atlas changed')
    assert abs(min(v[1] for v in part['vertices']))<1e-6
    reports.append(dict(model=model,result='PASS',triangles=len(order)//3,bones=len(native_names),actions=len(doc['creatureRig']['actions']),glb_json_max_position_error_metres=position_error,glb_json_max_weight_error=maximum_weight_error,textures=textures,source_sha256=doc['source_sha256'],runtime_sha256=sha(runtime),glb_sha256=sha(glb)))
(OUT/'production-verification.json').write_text(json.dumps(reports,indent=2)+'\n')
print('PASS: all three production skins preserve GLB/JSON geometry, joint identities/weights, atlas hashes, grounding and unchanged reviewed sources.')
