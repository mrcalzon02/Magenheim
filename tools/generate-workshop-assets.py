"""Original workshop models in meters; painted wood/stone/bronze atlas and icons."""
import importlib.util
import math
from pathlib import Path
from PIL import Image, ImageDraw

spec=importlib.util.spec_from_file_location('minerals',Path(__file__).with_name('generate-earth-assets.py'))
art=importlib.util.module_from_spec(spec);spec.loader.exec_module(art)
WOOD=(108,73,43)
LIGHTWOOD=(146,104,62)
DARKWOOD=(70,49,32)
STONE=(109,113,103)
BRONZE=(154,111,57)
IRON=(74,79,76)

class Model:
    def __init__(self): self.vertices=[];self.faces=[];self.colors=[]
    def add(self, vertices, faces, colors):
        n=len(self.vertices);self.vertices+=vertices
        self.faces += [tuple(n+j for j in f) for f in faces]
        self.colors+=colors
    def box(self, center, size, color, angle=0):
        # Closed cuboid with outward winding; rotate around Z for braces/tools.
        raw=[(x*size[0]/2,y*size[1]/2,z*size[2]/2) for x,y,z in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
        c,s=math.cos(angle),math.sin(angle)
        vertices=[(center[0]+c*x-s*y,center[1]+s*x+c*y,center[2]+z) for x,y,z in raw]
        quads=[(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(0,1,5,4),(3,7,6,2)]
        faces=[f for a,b,c,d in quads for f in [(a,b,c),(a,c,d)]]
        colors=[]
        for i in range(12):
            shift=[-8,3,-3,7,0,12][i//2]
            colors.append(tuple(max(0,min(255,x+shift)) for x in color))
        self.add(vertices,faces,colors)
    def cylinder(self,center,radius,height,color,sides=12,axis='y'):
        verts=[]
        for y in [-height/2,height/2]:
            for i in range(sides):
                a=2*math.pi*i/sides
                p=(radius*math.cos(a),y,radius*math.sin(a))
                if axis=='z': p=(p[0],-p[2],p[1])
                verts.append(tuple(p[k]+center[k] for k in range(3)))
        for y in [-height/2,height/2]:
            p=(0,y,0) if axis=='y' else (0,0,y)
            verts.append(tuple(p[k]+center[k] for k in range(3)))
        faces=[];colors=[]
        for i in range(sides):
            j=(i+1)%sides
            faces += [(i,i+sides,j),(j,i+sides,j+sides),(2*sides,i,j),(2*sides+1,j+sides,i+sides)]
            shade=int(9*math.cos(i*2.3))
            colors += [tuple(max(0,min(255,x+shade)) for x in color)]*4
        self.add(verts,faces,colors)
    def mineral(self,kind,center,scale):
        vertices,faces,colors=art.geode() if kind=='geode' else art.crystal(kind)
        if kind=='geode':
            faces=[(a,c,b) if art.dot(art.cross(art.sub(vertices[b],vertices[a]),art.sub(vertices[c],vertices[a])),vertices[a])<0 else (a,b,c) for a,b,c in faces]
        else: faces=[(a,c,b) for a,b,c in faces]
        self.add([tuple(center[k]+v[k]*scale for k in range(3)) for v in vertices],faces,colors)
    def raw(self): return self.vertices,self.faces,self.colors

def bench():
    m=Model()
    for x in [-.68,.68]:
        for z in [-.32,.32]:
            m.box((x,.43,z),(.17,.86,.17),WOOD)
            m.box((x,.18,z),(.181,.065,.181),IRON)
    for z in [-.32,.32]: m.box((0,.65,z),(1.53,.16,.11),DARKWOOD)
    for z in [-.32,0,.32]: m.box((0,.93,z),(1.82,.14,.30),LIGHTWOOD)
    for x in [-.76,.76]: m.box((x,1.0085,0),(.055,.017,.94),IRON)
    for z in [-.22,0,.22]: m.box((0,.24,z),(1.4,.075,.20),WOOD)
    m.box((-.34,1.033,0),(.76,.075,.65),STONE)
    m.mineral('geode',(-.36,1.19,-.04),.68)
    m.box((-.06,1.10,.24),(.30,.045,.035),LIGHTWOOD)
    m.box((-.20,1.12,.24),(.10,.095,.13),IRON)
    m.box((-.55,1.09,.24),(.25,.025,.022),BRONZE)
    m.box((.49,1.012,0),(.51,.025,.60),DARKWOOD)
    for x in [.225,.755]: m.box((x,1.055,0),(.032,.10,.64),WOOD)
    for z in [-.31,.31]: m.box((.49,1.055,z),(.56,.10,.032),WOOD)
    m.mineral(0,(.40,1.10,-.12),.46)
    m.mineral(1,(.55,1.10,.13),.52)
    m.mineral('geode',(.28,.37,0),.62)
    return m

def fracture():
    m=Model();m.cylinder((0,.32,0),.36,.64,WOOD,10)
    m.cylinder((0,.08,0),.375,.075,IRON,10)
    m.cylinder((0,.55,0),.375,.055,IRON,10)
    m.cylinder((0,.67,0),.38,.09,STONE,10)
    m.mineral('geode',(-.06,.82,0),.68)
    m.box((.20,.86,.08),(.042,.30,.035),BRONZE,angle=-.24)
    m.box((.08,.74,.24),(.34,.045,.04),LIGHTWOOD)
    m.box((-.06,.78,.24),(.14,.12,.12),IRON)
    return m

def wheel():
    m=Model()
    for x in [-.35,.35]:
        for z in [-.28,.28]:m.box((x,.38,z),(.115,.76,.115),WOOD)
    m.box((0,.20,0),(.83,.08,.68),DARKWOOD)
    m.box((0,.68,0),(.90,.11,.73),LIGHTWOOD)
    for x in [-.32,.32]:m.box((x,.92,0),(.10,.46,.13),WOOD)
    m.cylinder((0,1.03,0),.34,.16,STONE,20,axis='z')
    m.cylinder((0,1.03,0),.085,.24,BRONZE,10,axis='z')
    m.cylinder((0,1.03,0),.026,.72,IRON,8,axis='z')
    m.box((.36,.83,.25),(.05,.31,.045),BRONZE)
    m.cylinder((.36,.99,.31),.037,.17,DARKWOOD,8,axis='z')
    m.box((0,.32,.40),(.43,.055,.28),LIGHTWOOD,angle=.15)
    return m

def frame():
    m=Model()
    for x in [-.49,.49]:
        m.box((x,.09,0),(.22,.18,.83),DARKWOOD)
        m.box((x,.88,0),(.14,1.65,.14),WOOD)
        for y in [.29,1.45]:m.box((x,y,0),(.16,.07,.16),BRONZE)
    for y in [.40,1.65]:m.box((0,y,0),(1.10,.13,.15),LIGHTWOOD)
    radius=.39
    for i in range(6):
        angle=math.pi/3*i; mid=angle+math.pi/6
        x=radius*math.cos(mid)*math.cos(math.pi/6)
        y=1.03+radius*math.sin(mid)*math.cos(math.pi/6)
        m.box((x,y,0),(radius,.05,.06),BRONZE,angle=mid+math.pi/2)
    m.box((0,1.46,0),(.027,.29,.027),IRON)
    m.box((0,.60,0),(.028,.28,.028),IRON)
    m.mineral(4,(0,.96,0),1.05)
    m.box((0,.40,.16),(.32,.035,.12),STONE)
    return m

models=[('workstation',"Geologist's Workstation",bench()),('fracturing-block','Fracturing Block',fracture()),('faceting-wheel','Faceting Wheel',wheel()),('resonance-frame','Resonance Frame',frame())]
sheet=Image.new('RGB',(1600,760),(27,31,30));d=ImageDraw.Draw(sheet)
# Use the Earth asset pipeline's cross-platform font resolver. The previous hard-coded
# C:/Windows font paths made authoritative workshop/icon regeneration fail on Linux/macOS.
title=art.font(43,bold=True);font=art.font(24)
d.text((45,30),'MAGENHEIM / GEOLOGIST WORKSHOP',font=title,fill=(235,215,173))
d.text((48,91),'Original buildable workbench and three station upgrades',font=font,fill=(159,166,151))
for i,(name,label,model) in enumerate(models):
    icon,count=art.emit(name,model.raw(),outward=True)
    preview=icon.resize((390,390),Image.Resampling.LANCZOS)
    sheet.paste(preview,(i*400,195),preview)
    d.text((i*400+25,614),label,font=font,fill=(236,219,185))
    d.text((i*400+25,656),f'{count} triangles / 512px atlas',font=font,fill=(159,166,151))
    print(f'{name}: {count} triangles, OBJ/MTL, mesh, texture, icon')
sheet.save(art.OUT/'workshop-preview.png')
