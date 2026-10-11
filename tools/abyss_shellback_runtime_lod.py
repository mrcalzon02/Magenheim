"""Source-preserving export-time LOD0 for the established Abyss Shellback elite.

Only evaluated runtime meshes are reduced; the saved .blend keeps its original
high-detail geometry, armature, UVs, materials and anatomical object identities.
"""
CAP=24000
MODIFIER="MagenheimShellbackRuntimeLOD"

def triangles(obj, depsgraph=None):
    if depsgraph is None:
        return sum(max(0,len(p.vertices)-2) for p in obj.data.polygons)
    evaluated=obj.evaluated_get(depsgraph)
    mesh=evaluated.to_mesh()
    try:
        mesh.calc_loop_triangles()
        return len(mesh.loop_triangles)
    finally:
        evaluated.to_mesh_clear()

def protected(name,count):
    return count<=256 or name.startswith("Eye_") or name.endswith(("_Upper","_Lower")) or "_Finger_" in name

def weight(name):
    if name.startswith("ShellPlate_"):return 1.5
    if name in ("BodyMass","VulnerableUnderside"):return 1.1
    if name.startswith(("Claw_","DorsalKeel_","PlateRim_")):return .95
    return .75

def allocate(counts,cap=CAP):
    fixed=sum(n for name,n in counts.items() if protected(name,n))
    candidates={name:n for name,n in counts.items() if not protected(name,n)}
    if not candidates or fixed>=cap-512:raise ValueError("Insufficient reducible Shellback geometry")
    available=cap-fixed-512
    if sum(candidates.values())<=available:return {}
    low,high=0.0,1.0
    for _ in range(48):
        mid=(low+high)/2
        if sum(n*min(1.0,mid*weight(name)) for name,n in candidates.items())>available:high=mid
        else:low=mid
    ratios={name:min(1.0,low*weight(name)) for name in candidates}
    if any(r<.045 for r in ratios.values()):raise ValueError("Shellback decimation would erase anatomical form")
    return ratios

def bounds(mesh):
    return [(min(v.co[axis] for v in mesh.vertices),max(v.co[axis] for v in mesh.vertices)) for axis in range(3)]

def install(scene,depsgraph):
    meshes=[o for o in scene.objects if o.type=="MESH"]
    if any(o.modifiers.get(MODIFIER) for o in meshes):raise RuntimeError("Shellback LOD modifier leaked into editable source")
    counts={o.name:triangles(o) for o in meshes}
    ratios=allocate(counts)
    if not ratios:return {}
    before={o.name:bounds(o.data) for o in meshes if o.name in ratios}
    for o in meshes:
        if o.name in ratios:
            mod=o.modifiers.new(MODIFIER,"DECIMATE")
            mod.decimate_type="COLLAPSE"
            mod.ratio=ratios[o.name]
    for _ in range(12):
        depsgraph.update()
        total=sum(triangles(o,depsgraph) for o in meshes)
        if total<=CAP:break
        factor=min(.94,.965*CAP/total)
        for o in meshes:
            mod=o.modifiers.get(MODIFIER)
            if mod:
                mod.ratio*=factor
                if mod.ratio<.045:raise RuntimeError("Shellback LOD0 cannot preserve anatomy inside budget")
    else:raise RuntimeError("Shellback runtime LOD0 exceeds elite ceiling")
    for o in meshes:
        if o.name not in ratios:continue
        evaluated=o.evaluated_get(depsgraph)
        mesh=evaluated.to_mesh()
        try:
            if not mesh.vertices or not mesh.uv_layers:raise RuntimeError(o.name+": decimation lost geometry/UV")
            after=bounds(mesh)
            if any(abs(before[o.name][a][edge]-after[a][edge])>.035 for a in range(3) for edge in range(2)):
                raise RuntimeError(o.name+": silhouette envelope shifted more than 3.5 cm")
        finally:evaluated.to_mesh_clear()
    return {o.name:o.modifiers[MODIFIER].ratio for o in meshes if o.modifiers.get(MODIFIER)}

def remove(scene):
    for o in scene.objects:
        if o.type=="MESH" and o.modifiers.get(MODIFIER):o.modifiers.remove(o.modifiers[MODIFIER])

def restore(scene,ratios):
    for name,ratio in ratios.items():
        o=scene.objects.get(name)
        if o is None or o.type!="MESH" or o.modifiers.get(MODIFIER):
            raise RuntimeError("Shellback GLB export lost LOD object: "+name)
        mod=o.modifiers.new(MODIFIER,"DECIMATE")
        mod.decimate_type="COLLAPSE"
        mod.ratio=ratio
