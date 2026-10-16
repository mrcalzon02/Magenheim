from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/verify-rift-titan-concept-body-r17.py').read_text().replace('rift-titan-concept-body-r17','rift-titan-broken-strata-r18').replace('underworld-boss-rift-titan-fracture-planes-r16.blend','underworld-boss-rift-titan-concept-body-r17.blend').replace("['UnifiedCrystalHead']+['NeckMetalRib'+str(j) for j in range(6)]","['UnifiedCrystalHead','StoneCollarbone','WrappedStoneBand']+['NeckMetalRib'+str(j) for j in range(6)]")
exec(compile(s,str(__file__),'exec'))
# Compare the saved rest skeleton independently, including bone roll matrices.
with bpy.data.libraries.load(str(ROOT/'assets/models/studies/underworld-boss-rift-titan-concept-body-r17.blend'),link=False) as (data,loaded):loaded.objects=[arm.name]
old_arm=loaded.objects[0];assert set(arm.data.bones.keys())==set(old_arm.data.bones.keys())
rig_errors={}
for bone in arm.data.bones:
    before=old_arm.data.bones[bone.name]
    assert (bone.parent.name if bone.parent else None)==(before.parent.name if before.parent else None)
    error=max(abs(bone.matrix_local[i][j]-before.matrix_local[i][j]) for i in range(4) for j in range(4));assert error<2e-6,(bone.name,error);rig_errors[bone.name]=error
report['r17_rest_bone_matrix_max_difference']=rig_errors
(OUT/'body-verification.json').write_text(json.dumps(report,indent=2)+'\n')
print('VERIFIED exact saved r17 rest bone matrices and hierarchy')
