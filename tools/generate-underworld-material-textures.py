#!/usr/bin/env python3
"""Generate deterministic 512px source maps for shared Underworld production materials.

These are SOURCE textures, not runtime filenames. Blender authors pack them into materials and the
model exporter content-hashes those packed pixels into assets/models/textures so unrelated models
cannot overwrite each other by filename.
"""
from pathlib import Path
from math import sin, cos, sqrt, pi
import hashlib
import random
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/"assets"/"material-source"/"underworld"
OUT.mkdir(parents=True,exist_ok=True)
SIZE=512
PROXY=64

# key: base RGB 0..1, contrast, roughness mean, motif, optional emissive RGB
SPECS={
"worldroot-bark":((.30,.19,.085),.18,205,0.00,"wood",None),
"worldroot-heartwood":((.48,.32,.16),.14,190,0.00,"wood",None),
"understone":((.29,.31,.29),.13,214,0.00,"stone",None),
"forged-iron":((.15,.16,.17),.12,112,0.78,"metal",None),
"glowcap":((.34,.72,.38),.18,128,0.00,"fungal",(.10,.40,.13)),
"flowstone":((.29,.39,.41),.15,186,0.00,"stone",None),
"pale-fibre":((.54,.57,.52),.12,202,0.00,"fibre",None),
"pearl-metal":((.39,.50,.52),.10,104,0.50,"metal",None),
"blackwater-pearl":((.60,.82,.84),.14,92,0.08,"pearl",(.06,.22,.27)),
"slagstone":((.19,.15,.13),.17,222,0.00,"stone",None),
"charred-root":((.16,.075,.035),.18,220,0.00,"wood",None),
"emberiron":((.30,.16,.09),.14,98,0.84,"metal",None),
"ember-heat":((.86,.24,.035),.20,88,0.05,"heat",(.72,.08,.01)),
"rimewood":((.34,.44,.46),.12,180,0.00,"wood",None),
"rimesilver":((.72,.77,.79),.08,72,0.92,"metal",None),
"clear-ice":((.64,.86,.93),.12,62,0.00,"ice",(.08,.22,.30)),
"shardstone":((.25,.22,.28),.16,204,0.00,"stone",None),
"titanbone":((.54,.49,.39),.13,182,0.00,"bone",None),
"fracture-crystal":((.56,.39,.77),.16,78,0.00,"crystal",(.20,.07,.36)),
"rotwood":((.18,.105,.045),.19,224,0.00,"wood",None),
"bone":((.37,.33,.26),.12,188,0.00,"bone",None),
"carrion-amber":((.76,.43,.09),.16,86,0.05,"amber",(.34,.10,.01)),
"sporeweave-fibre":((.30,.40,.23),.15,198,0.00,"fibre",None),
}

def seed_for(key):
    return int(hashlib.sha256(key.encode()).hexdigest()[:8],16)

def motif_value(motif,x,y,phase):
    nx=x/SIZE; ny=y/SIZE
    if motif=="wood": return .62*sin(ny*2*pi*18+sin(nx*2*pi*3)*1.2)+.28*sin(ny*2*pi*57+phase)
    if motif=="stone": return .50*sin((nx+ny)*2*pi*13+phase)+.30*cos(nx*2*pi*29-ny*2*pi*17)
    if motif=="metal": return .55*cos((nx-ny)*2*pi*23+phase)+.20*sin(nx*2*pi*71)
    if motif=="fungal": return .52*sin(nx*2*pi*11+sin(ny*2*pi*4))+ .33*cos(ny*2*pi*17)
    if motif=="fibre": return .64*sin(nx*2*pi*38+sin(ny*2*pi*7)*.8)+.18*cos(ny*2*pi*26)
    if motif=="pearl": return .60*cos(sqrt((nx-.5)**2+(ny-.5)**2)*2*pi*18+phase)+.18*sin((nx+ny)*2*pi*12)
    if motif=="heat": return .70*sin((nx+ny)*2*pi*8+phase)+.22*cos(nx*2*pi*31)
    if motif=="ice": return .50*sin((nx-ny)*2*pi*16)+.35*cos((nx+ny)*2*pi*11+phase)
    if motif=="bone": return .58*sin(ny*2*pi*14+sin(nx*2*pi*5))+.22*cos(nx*2*pi*32)
    if motif=="crystal": return .52*sin((nx+ny)*2*pi*14)+.35*cos((nx*1.7-ny)*2*pi*9+phase)
    if motif=="amber": return .56*cos((nx+ny)*2*pi*9+phase)+.30*sin(ny*2*pi*19)
    return sin(nx*2*pi*15+phase)*cos(ny*2*pi*13)

def clamp(v): return max(0,min(255,int(round(v))))

def build(key,spec):
    base,contrast,rough_mean,metallic,motif,emissive=spec
    rng=random.Random(seed_for(key)); phase=rng.random()*2*pi
    alb=Image.new("RGB",(SIZE,SIZE)); ap=alb.load()
    rough_img=Image.new("L",(SIZE,SIZE)); rp=rough_img.load()
    normal=Image.new("RGB",(SIZE,SIZE)); np=normal.load()
    metalgloss=Image.new("RGBA",(SIZE,SIZE)); mp=metalgloss.load()
    field=[[0.0]*SIZE for _ in range(SIZE)]
    for y in range(SIZE):
        for x in range(SIZE):
            broad=motif_value(motif,x,y,phase)
            micro=.22*sin(x*.91+y*1.17+phase)+rng.uniform(-.14,.14)
            v=max(-1,min(1,broad*.78+micro*.22)); field[y][x]=v
            shade=1+contrast*v
            ap[x,y]=tuple(clamp(c*255*shade) for c in base)
            rough_value=clamp(rough_mean + 28*v + rng.uniform(-5,5))
            rp[x,y]=rough_value
            metal=clamp(metallic*255)
            mp[x,y]=(metal,metal,metal,255-rough_value)
    for y in range(SIZE):
        ym=(y-1)%SIZE; yp=(y+1)%SIZE
        for x in range(SIZE):
            xm=(x-1)%SIZE; xp=(x+1)%SIZE
            dx=(field[y][xp]-field[y][xm])*.75; dy=(field[yp][x]-field[ym][x])*.75
            mag=sqrt(dx*dx+dy*dy+1)
            np[x,y]=(clamp((-.5*dx/mag+.5)*255),clamp((-.5*dy/mag+.5)*255),clamp((.5/mag+.5)*255))
    alb.save(OUT/f"{key}-albedo.png")
    rough_img.save(OUT/f"{key}-roughness.png")
    normal.save(OUT/f"{key}-normal.png")
    metalgloss.save(OUT/f"{key}-metallic-smoothness.png")
    if emissive:
        em=Image.new("RGB",(SIZE,SIZE)); ep=em.load()
        for y in range(SIZE):
            for x in range(SIZE):
                gate=max(0.0,field[y][x]-.32)
                pulse=max(0.0,sin((x+y)*.07+phase))*.35
                strength=min(1.0,gate*1.8+pulse*gate)
                ep[x,y]=tuple(clamp(c*255*strength) for c in emissive)
        em.save(OUT/f"{key}-emission.png")
    print("AUTHORED",key,flush=True)

for key,spec in SPECS.items(): build(key,spec)
print("AUTHORED Underworld material source library:",len(SPECS),"families at",SIZE,"px",flush=True)
