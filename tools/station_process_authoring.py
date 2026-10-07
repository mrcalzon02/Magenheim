"""Recipe-led working assemblies for the four later Underworld stations.

These are visible representations of existing native crafting recipes, not new
simulation or animation systems. Each fixture has a workpiece and a load path.
"""
import math
import bpy
import bmesh


def build_process_station(kind, api, m):
    cube, cyl, tube, torus = (api[k] for k in ('cube', 'cyl', 'tube', 'torus'))
    drawer_bank, finish = api['drawer_bank'], api['finish']

    def trough(name, center, half, wall=.055, lining=None, open_bottom=False):
        x,y,z=center; w,d,h=half; mat=lining or m['stone']
        if not open_bottom:
            cube(name+'-floor',(x,y,z-h+wall),(w,d,wall),mat,.012)
        for side in (-1,1):
            cube(name+f'-side-{side}',(x+side*w,y,z),(wall,d,h),mat,.012)
            cube(name+f'-end-{side}',(x,y+side*d,z),(w,wall,h),mat,.012)

    def fasten(name, x,y,z, mat=None):
        cyl(name,(x,y,z),.024,.025,mat or m['metal'],8,(math.pi/2,0,0))

    def hand_tool(name, x,y,z, hammer=False, flat=False):
        if flat:
            tube(name+'-handle',(x,y,z),(x-.34,y,z),.025,m['body'],8)
            cube(name+'-blade',(x-.39,y,z),(.09,.05,.014),m['metal'],.006)
            return
        tube(name+'-handle',(x,y,z),(x,y,z+.34),.025,m['body'],8)
        if hammer:
            cube(name+'-head',(x,y,z+.35),(.11,.04,.04),m['metal'],.012)
        else:
            cube(name+'-blade',(x,y,z+.35),(.05,.014,.09),m['metal'],.006)

    def press(name, x,y,z, width, depth, height):
        cube(name+'-bed',(x,y,z),(width,depth,.07),m['metal'],.018)
        for side in (-1,1):
            cube(name+f'-post-{side}',(x+side*(width-.05),y,z+height*.5),(.05,.08,height*.5),m['body'],.015)
            fasten(name+f'-bed-bolt-{side}',x+side*(width-.05),y-depth-.015,z)
        cube(name+'-bridge',(x,y,z+height),(width+.07,.10,.07),m['body'],.018)
        tube(name+'-screw',(x,y,z+.17),(x,y,z+height+.19),.035,m['metal'],10)
        for i in range(7):
            torus(name+f'-screw-thread-{i}',(x,y,z+height-.14+i*.045),.038,.012,m['metal'])
        tube(name+'-turning-bar',(x-.25,y,z+height+.15),(x+.25,y,z+height+.15),.025,m['metal'],8)
        cube(name+'-platen',(x,y,z+.19),(width-.12,depth-.035,.045),m['metal'],.012)

    def stock(name,x,y,z,count=3,mat=None):
        for i in range(count):
            cube(name+f'-layer-{i}',(x+.015*(i%2),y,z+i*.045),(.30,.18,.020),mat or m['body'],.008)

    def inset_hearth():
        # Slagstone courses surround an open fire chamber, including its roof.
        for row in range(4):
            z=.39+row*.25
            for side in (-1,1):
                for j in range(3):
                    cube(f'hearth-body-side-{row}-{side}-{j}',(side*.89,-.62+j*.58,z),(.22,.27,.115),m['stone'],.025)
            for j in range(4):
                cube(f'hearth-body-back-{row}-{j}',(-.80+j*.53,.83,z),(.25,.16,.115),m['stone'],.025)
        cube('hearth-floor',(0,0,.25),(1.10,1.02,.10),m['stone'],.025)
        # Roof courses surround the flue opening instead of capping its airway.
        for side in (-1,1):
            cube(f'hearth-roof-side-{side}',(side*.70,.04,1.33),(.43,.99,.12),m['stone'],.025)
        cube('hearth-roof-front',(0,-.315,1.33),(.27,.635,.12),m['stone'],.025)
        cube('hearth-roof-back',(0,.955,1.33),(.27,.075,.12),m['stone'],.018)
        for i in range(6):
            tube(f'fire-grate-{i}',(-.68+i*.27,-.73,.41),(-.68+i*.27,.60,.41),.033,m['metal'],8)
        for i in range(4):
            cube(f'contained-ember-{i}',(-.47+i*.30,.15,.49),(.16,.23,.07),m['accent'],.02)

    if kind == 'furnace':
        cube('slag-foundation',(0,0,.12),(1.82,1.30,.12),m['stone'],.035)
        inset_hearth()
        # One chimney rising from the chamber; no decorative array of pipes.
        trough('chimney',(0,.60,2.24),(.28,.28,.80),.09,open_bottom=True)
        for z in (1.56,2.15,2.79):
            for side in (-1,1):
                cube(f'chimney-band-{z}-{side}',(side*.30,.60,z),(.025,.32,.035),m['metal'],.006)
        # Layered bellows feed a tuyere into the side of the firebox.
        for level in range(5):
            cube(f'bellows-fold-{level}',(1.42,.16,.58+level*.075),(.25-level*.012,.42,.025),m['body'],.016)
        for z in (.51,.94):
            cube(f'bellows-board-{z}',(1.42,.16,z),(.28,.45,.04),m['body'],.016)
        tube('bellows-tuyere',(1.41,-.16,.69),(.88,-.16,.69),.075,m['metal'],12)
        tube('bellows-lever',(1.42,.49,.94),(1.57,.96,1.30),.045,m['body'],10)
        # Accessible front anvil, stock tray and an open quench vessel.
        cube('anvil-stump',(-.61,-.83,.60),(.32,.31,.32),m['body'],.025)
        cube('anvil-waist',(-.61,-.83,.97),(.20,.18,.15),m['metal'],.02)
        cube('anvil-face',(-.61,-.83,1.15),(.42,.24,.055),m['metal'],.016)
        tube('anvil-horn',(-.95,-.83,1.14),(-1.20,-.83,1.12),.09,m['metal'],8)
        trough('open-quench-trough',(-1.42,.15,.57),(.25,.49,.28))
        cube('quench-water',(-1.42,.15,.61),(.19,.43,.015),m['metal'],0)
        stock('emberiron-bar-stock',.60,-.75,.40,4,m['metal'])
        hand_tool('forge-hammer',-.21,-1.06,.67,True)
        for x in (-.30,.28):
            tube(f'tongs-arm-{x}',(x,-1.05,.62),(x+.06,-1.05,.99),.018,m['metal'],7)
        for x in (-.88,.88):
            fasten(f'hearth-apron-bolt-{x}',x,-1.03,.29)

    elif kind == 'silence':
        for side in (-1,1):
            for end in (-1,1):
                cube(f'rimewood-leg-{side}-{end}',(side*1.18,end*.60,.60),(.10,.10,.50),m['stone'],.02)
                cyl(f'damping-foot-{side}-{end}',(side*1.18,end*.60,.10),.16,.10,m['body'],12)
                fasten(f'apron-pin-{side}-{end}',side*1.18,end*.71,1.04)
        for i in range(6):
            cube(f'worktop-slat-rimewood-{i}',(-1.28+i*.51,0,1.18),(.24,.86,.10),m['stone'],.018)
        drawer_bank('precision-drawers',1.13,-.76,.86,3,m)
        # A horizontal lapping wheel with axle, supported crank and a lens on its face.
        cube('lapping-wheel-bearing',(-.66,-.12,1.41),(.24,.24,.13),m['metal'],.012)
        cyl('iceglass-lapping-wheel',(-.66,-.12,1.57),.44,.09,m['metal'],24)
        cyl('lens-being-ground',(-.74,-.13,1.64),.16,.045,m['accent'],16)
        tube('lapping-wheel-crank',(-.66,-.12,1.63),(-.66,-.12,1.83),.025,m['metal'],8)
        tube('lapping-crank-arm',(-.66,-.12,1.83),(-.44,-.12,1.83),.025,m['metal'],8)
        tube('lapping-crank-handle',(-.44,-.12,1.83),(-.44,-.12,1.95),.033,m['stone'],8)
        press('rimewood-lamination-press',.72,.26,1.36,.46,.30,.69)
        stock('clamped-rimewood-layers',.72,.26,1.44,2,m['stone'])
        stock('rimesilver-blanks',-.65,.61,1.34,3,m['metal'])
        hand_tool('rime-chisel',1.17,-.44,1.34,flat=True)
        # One inspection lens in a proper rim and stand, aimed at the working area.
        tube('inspection-stand',(-1.11,.51,1.30),(-1.11,.51,1.99),.04,m['metal'],9)
        tube('inspection-arm',(-1.11,.51,1.99),(-.68,.19,1.99),.035,m['metal'],8)
        torus('inspection-lens-rim',(-.68,.19,1.99),.21,.025,m['metal'])
        cyl('inspection-lens',(-.68,.19,1.99),.19,.025,m['body'],20)
        for i in range(4):
            cube(f'measuring-rule-mark-{i}',(-.14,-.60+i*.12,1.302),(.05,.01,.008),m['metal'],0)

    elif kind == 'anchor':
        cube('shardstone-bed',(0,0,.21),(1.90,1.28,.21),m['stone'],.04)
        cube('anvil-monolith',(0,0,.98),(1.12,.65,.55),m['stone'],.04)
        cube('anvil-cap',(0,0,1.55),(1.42,.78,.10),m['metal'],.02)
        for side in (-1,1):
            for end in (-1,1):
                tube(f'titanbone-brace-{side}-{end}',(side*1.50,end*.95,.35),(side*.97,end*.59,1.56),.12,m['body'],12)
                cube(f'brace-footplate-{side}-{end}',(side*1.50,end*.95,.40),(.22,.19,.05),m['metal'],.012)
                for j in range(2):
                    fasten(f'brace-bolt-{side}-{end}-{j}',side*1.50+(j-.5)*.20,end*1.14,.40)
            cube(f'press-guide-{side}',(side*.93,.23,2.04),(.10,.12,.56),m['metal'],.018)
        cube('press-bridge',(0,.23,2.65),(1.08,.16,.14),m['metal'],.018)
        tube('press-ram',(0,.23,2.65),(0,.23,1.91),.09,m['metal'],12)
        cube('press-hammer',(0,.23,1.89),(.44,.30,.14),m['metal'],.025)
        # A ratchet winch and chain connect the lift mechanism to the hammer.
        cyl('winch-drum',(-.74,.23,2.85),.18,.23,m['metal'],14,(math.pi/2,0,0))
        tube('winch-supported-axle',(-.74,.02,2.85),(-.74,.44,2.85),.045,m['metal'],10)
        for y in (.04,.42):
            cube(f'winch-bearing-{y}',(-.74,y,2.78),(.12,.05,.13),m['metal'],.012)
            cube(f'winch-bearing-foot-{y}',(-.74,y,2.70),(.19,.08,.03),m['metal'],.008)
        for i in range(10):
            a=i*math.tau/10
            cube(f'ratchet-tooth-{i}',(-.74+math.cos(a)*.20,.075,2.85+math.sin(a)*.20),(.032,.035,.035),m['metal'],.008,(0,-a,0))
        tube('winch-crank',(-.74,.07,2.85),(-1.03,.07,2.85),.025,m['metal'],8)
        tube('winch-grip',(-1.03,.07,2.85),(-1.03,-.10,2.85),.04,m['body'],9)
        tube('winch-lift-cable',(-.74,.23,2.97),(0,.23,2.97),.023,m['metal'],8)
        torus('lift-pulley',(0,.23,2.84),.14,.026,m['metal'],(math.pi/2,0,0))
        tube('pulley-support',(0,.23,2.65),(0,.23,2.84),.04,m['metal'],8)
        api['hanging_chain']('press-lift-chain',(0,.23,2.64),6,.12,m['metal'])
        # Twin die rails cradle a Titanbone plate directly below the hammer.
        for side in (-1,1):
            cube(f'forming-die-rail-{side}',(side*.32,.23,1.72),(.06,.31,.07),m['metal'],.012)
        cube('titanbone-plate-workpiece',(0,.23,1.72),(.25,.25,.05),m['body'],.015)
        stock('shardstone-block-stock',1.34,-.70,.50,3,m['stone'])
        trough('prism-setting-tray',(-.86,-.40,1.69),(.30,.27,.09),.025,m['metal'])
        for i in range(3):
            cyl(f'fracture-prism-workpiece-{i}',(-1.04+i*.18,-.40,1.90),.075,.31,m['accent'],6)
        for side in (-1,1):
            cube(f'die-lock-{side}',(side*.55,-.47,1.74),(.15,.08,.07),m['metal'],.012)
            tube(f'die-lock-handle-{side}',(side*.55,-.47,1.74),(side*.55,-.67,1.82),.026,m['body'],8)

    elif kind == 'crown':
        cube('bone-gravel-dais',(0,0,.18),(1.80,1.42,.18),m['stone'],.04)
        for side in (-1,1):
            for end in (-1,1):
                cube(f'rotwood-leg-{side}-{end}',(side*1.15,end*.64,.70),(.12,.12,.40),m['body'],.02)
        for i in range(6):
            cube(f'worktop-slat-reliquary-{i}',(-1.20+i*.48,0,1.10),(.225,.91,.11),m['body'],.018)
        drawer_bank('reliquary-drawers',1.12,-.79,.79,3,m)
        # A screw-driven seal stamp, with amber work seated in its die.
        press('amber-seal-press',-.67,-.10,1.28,.44,.32,1.11)
        cyl('amber-seal-die',(-.67,-.10,1.38),.18,.06,m['metal'],16)
        cyl('amber-seal-workpiece',(-.67,-.10,1.425),.13,.028,m['accent'],12)
        press('rotwood-lamination-clamp',.68,.26,1.28,.43,.29,.68)
        stock('rotwood-laminate-workpiece',.68,.26,1.36,2,m['body'])
        # A mortar containing bone fragments explains the composite preparation.
        vertices=[];faces=[];n=16
        for radius,z in ((.27,1.25),(.34,1.59),(.27,1.59),(.15,1.32)):
            vertices.extend((.66+radius*math.cos(i*math.tau/n),-.52+radius*math.sin(i*math.tau/n),z) for i in range(n))
        for ring in range(3):
            for i in range(n):
                j=(i+1)%n
                faces.append((ring*n+i,ring*n+j,(ring+1)*n+j,(ring+1)*n+i))
        faces.extend((tuple(reversed(range(n))),tuple(range(3*n,4*n))))
        mesh=bpy.data.meshes.new('OssuaryMortar');mesh.from_pydata(vertices,[],faces);mesh.update()
        obj=bpy.data.objects.new('ossuary-mortar',mesh);bpy.context.scene.collection.objects.link(obj)
        bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
        finish(obj,'ossuary-mortar',m['stone'])
        tube('bone-pestle',(.60,-.52,1.38),(.82,-.52,1.84),.065,m['stone'],10)
        for i in range(4):
            cube(f'ground-bone-fragment-{i}',(.60+(i%2)*.10,-.60+(i//2)*.12,1.37),(.04,.035,.025),m['stone'],0,(0,0,i*.6))
        # A mounted sealed Deepstone reliquary retains the station's ritual identity.
        for side in (-1,1):
            tube(f'reliquary-back-post-{side}',(side*.76,.75,1.20),(side*.76,.75,2.65),.075,m['body'],10)
        cube('reliquary-mounted-case',(0,.75,2.20),(.52,.19,.40),m['stone'],.025)
        cube('reliquary-case-seal',(0,.54,2.20),(.36,.025,.28),m['accent'],.015)
        for side in (-1,1):
            cube(f'reliquary-case-band-{side}',(side*.41,.51,2.20),(.035,.025,.42),m['metal'],.008)
            for z in (1.92,2.46):
                fasten(f'case-lock-{side}-{z}',side*.41,.47,z)
        cube('crown-crest-base',(0,.75,2.68),(.50,.10,.045),m['metal'],.012)
        for i in range(5):
            tube(f'crown-crest-point-{i}',(-.40+i*.20,.75,2.70),(-.40+i*.20,.75,2.89+.12*(i==2)),.035,m['metal'],7)
        hand_tool('seal-trimming-knife',-.96,-.58,1.25,flat=True)
        stock('finished-ossuary-plates',1.08,.52,1.24,3,m['stone'])
    else:
        raise ValueError(kind)
