"""Finish saved model surfaces, packed texture fidelity and cloth silhouettes."""
import bpy,bmesh,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for file in sorted((ROOT/'assets/models/source').glob('*.blend')):
    bpy.ops.wm.open_mainfile(filepath=str(file))
    if bpy.context.scene.get('surface_finish')=='2':continue
    for im in bpy.data.images:
        if im.type!='IMAGE':continue
        dimensions=tuple(im.size)  # Force lazy packed image decoding.
        if not im.has_data:continue
        if min(im.size)<256:
            im.scale(max(256,im.size[0]),max(256,im.size[1]))
            values=list(im.pixels[:]);w,h=im.size
            for y in range(h):
                for x in range(w):
                    # Fine cross-grain modulation, stored in the actual source.
                    grain=1+.026*math.sin(x*1.71+y*.41)*math.sin(y*2.13-x*.31)
                    i=4*(y*w+x)
                    for c in range(3):values[i+c]=max(0,min(1,values[i+c]*grain))
            im.pixels.foreach_set(values)
            im.pack()
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        # Earth models contain thin atlas-mapped inlays: broad mesh-wide bevels
        # create needle triangles there. Retain the original authored edges.
        if file.stem.startswith('earth-'):
            for mod in list(obj.modifiers):
                if mod.name=='Crafted edge chamfer':obj.modifiers.remove(mod)
        if bpy.context.scene.get('surface_finish') is None and file.stem.startswith('crystal-banner') and 'cloth' in obj.name.lower():
            bm=bmesh.new();bm.from_mesh(obj.data)
            bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=5,use_grid_fill=True)
            bm.to_mesh(obj.data);bm.free()
            inv=obj.matrix_world.inverted()
            points=[obj.matrix_world@v.co for v in obj.data.vertices]
            zmin=min(v.z for v in points);zmax=max(v.z for v in points)
            for v,p in zip(obj.data.vertices,points):
                fall=(zmax-p.z)/max(.001,zmax-zmin)
                p.y+=.035*fall*math.sin(p.x*14)
                v.co=inv@p
            obj.data.update()
    bpy.context.scene['surface_finish']='2'
    bpy.context.preferences.filepaths.save_version=0
    bpy.ops.wm.save_as_mainfile(filepath=str(file),compress=True)
    print('FINISHED',file.stem,flush=True)
