"""Authored thorax and abdomen envelopes for the established Sulfurous Wastes Ashmite.

Retains the original 0.36 m hexapod proportions, centerlines, armature bones and
heat-chitin PBR material. A low dorsal profile keeps five independent scutes
legible; flank buttresses, heat fissures and ventral plates are real geometry.
The Blender-independent data function permits topology/silhouette regression.
"""
from math import cos, exp, pi, sin

RINGS = 36
SIDES = 80
CONTRACT = 'ashmite-sculpted-body-envelope-36x80-v1'
BODIES = {
    'thorax': ((0., 0., .085), (.095, .105, .052), 'Thorax'),
    'abdomen': ((0., -.105, .080), (.105, .125, .060), 'Abdomen'),
}


def specification(kind):
    if kind not in BODIES:
        raise ValueError('Unknown Ashmite body segment: '+str(kind))
    return BODIES[kind]


def body_mesh_data(kind):
    """Return local vertices, outward closed faces and per-loop seam-safe UVs."""
    _, (half_width, half_length, half_height), _ = specification(kind)
    vertices = [(0., -half_length, 0.)]
    for i in range(RINGS):
        t = (i + .5) / RINGS
        y = half_length * (2*t - 1)
        envelope = sin(pi*t)**.63
        # Thorax is front-shouldered; the abdomen remains broad to the rear.
        fullness = (1 + .075*(t-.5)) if kind == 'thorax' else (1 - .13*(t-.5))
        for j in range(SIDES):
            phi = 2*pi*j/SIDES
            c, s = cos(phi), sin(phi)
            flank = abs(c)**6
            top = max(0., s)**8
            under = max(0., -s)**6
            # Load-bearing hip shields are integrated into the shell, not added
            # as disconnected spikes. Their leg sockets remain unobstructed.
            if kind == 'thorax':
                buttress = .075 * exp(-((t-.70)/.13)**2) * flank
                rib_positions = (.27, .52, .77)
                scute_lobes = (.64, .88)
            else:
                buttress = .065 * (exp(-((t-.28)/.11)**2) +
                                   exp(-((t-.63)/.13)**2)) * flank
                rib_positions = (.22, .41, .60, .79)
                scute_lobes = (.30, .54, .78)
            # Heat fissures curve around the flanks; no random micro-spikes.
            crack1 = exp(-((t-(.33+.035*cos(2*phi)))/.028)**2)
            crack2 = exp(-((t-(.69-.025*cos(2*phi)))/.030)**2)
            fissure = -.045*(crack1+crack2)*abs(c)**2
            # Three/four physical sternite divisions below the armored back.
            sternite = -.058 * sum(exp(-((t-p)/.022)**2) for p in rib_positions) * under
            # Lateral margins of the existing dorsal scutes remain visible
            # in silhouette as overlapping, scalloped armor shoulders.
            plate_flare = .055 * sum(exp(-((t-p)/.065)**2) for p in scute_lobes) * flank
            width = half_width*envelope*fullness*(1+buttress+plate_flare+fissure)
            height = half_height*envelope*(1-.12*top+sternite+.4*fissure)
            vertices.append((width*c, y, height*s))
    vertices.append((0., half_length, 0.))
    rear, front = 0, len(vertices)-1
    faces, uvs = [], []
    for j in range(SIDES):
        k=(j+1)%SIDES
        a,b=2*pi*j/SIDES,2*pi*k/SIDES
        faces.append((rear,1+j,1+k))
        uvs.append(((.5,.5),(.5+.46*cos(a),.5+.46*sin(a)),
                    (.5+.46*cos(b),.5+.46*sin(b))))
    for i in range(RINGS-1):
        for j in range(SIDES):
            k=(j+1)%SIDES
            faces.append((1+i*SIDES+j,1+(i+1)*SIDES+j,
                          1+(i+1)*SIDES+k,1+i*SIDES+k))
            uvs.append(((j/SIDES,(i+.5)/RINGS),
                        (j/SIDES,(i+1.5)/RINGS),
                        ((j+1)/SIDES,(i+1.5)/RINGS),
                        ((j+1)/SIDES,(i+.5)/RINGS)))
    last=1+(RINGS-1)*SIDES
    for j in range(SIDES):
        k=(j+1)%SIDES
        a,b=2*pi*j/SIDES,2*pi*k/SIDES
        faces.append((front,last+k,last+j))
        uvs.append(((.5,.5),(.5+.46*cos(b),.5+.46*sin(b)),
                    (.5+.46*cos(a),.5+.46*sin(a))))
    return vertices, faces, uvs


def sculpted_body(name, kind, material):
    """Blender adapter; caller binds existing Thorax/Abdomen armature bone."""
    import bpy
    verts, faces, uvs = body_mesh_data(kind)
    mesh=bpy.data.meshes.new(name+'_Mesh')
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    origin,_,_=specification(kind)
    obj.location=origin
    mesh.materials.append(material)
    obj['magenheim_ashmite_body_contract']=CONTRACT
    obj['magenheim_ashmite_body_kind']=kind
    layer=mesh.uv_layers.new(name='AshmiteUV')
    for poly,loop_uv in zip(mesh.polygons,uvs):
        poly.use_smooth=len(poly.vertices)==4
        for li,pair in zip(poly.loop_indices,loop_uv):
            layer.data[li].uv=pair
    return obj
