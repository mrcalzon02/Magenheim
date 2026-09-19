"""Reproducible original meshes, painted texture atlases, and matching inventory icons.
Requires Python 3 and Pillow. Outputs are checked in; no Python needed by players.

Set MAGENHEIM_EARTH_OUTPUT_ROOT to stage generated files outside the repository. Model-backed
legacy icon reconstruction always reads its authoritative mesh/atlas inputs from assets/earth, so
a freshness check can regenerate into a temporary directory without overwriting its own sources.
"""
import json
import math
import os
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageStat

def flat(im):
    reader=getattr(im,'get_flattened_data',None)
    return list(reader() if reader else im.getdata())

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets' / 'earth'
OUT = Path(os.environ.get('MAGENHEIM_EARTH_OUTPUT_ROOT', str(SOURCE))).resolve()
OUT.mkdir(parents=True, exist_ok=True)
STAGED = OUT != SOURCE.resolve()
ICON_SIZE = max(256, int(os.environ.get('MAGENHEIM_EARTH_ICON_SIZE', '256')))
PREVIEW_ICON_SIZE = max(ICON_SIZE, int(os.environ.get('MAGENHEIM_EARTH_PREVIEW_ICON_SIZE', str(ICON_SIZE))))
GAMEPLAY_READABILITY_SIZE = 64
EDGE_CLEARANCE = max(0.0,min(0.10,float(os.environ.get('MAGENHEIM_ICON_MIN_EDGE_CLEARANCE','0.01'))))
LEGACY_MODEL_ICON_NAMES=('workstation','fracturing-block','faceting-wheel','resonance-frame')

def sub(a, b): return tuple(x-y for x,y in zip(a,b))
def cross(a,b): return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def dot(a,b): return sum(x*y for x,y in zip(a,b))
def normal(a):
    length = math.sqrt(dot(a,a))
    return tuple(x/length for x in a)

def font(size, bold=False):
    candidates = []
    configured = os.environ.get('MAGENHEIM_PREVIEW_FONT')
    if configured: candidates.append(configured)
    candidates.extend(['C:/Windows/Fonts/segoeuib.ttf' if bold else 'C:/Windows/Fonts/segoeui.ttf','/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf' if bold else '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf','/System/Library/Fonts/Supplemental/Arial Bold.ttf' if bold else '/System/Library/Fonts/Supplemental/Arial.ttf'])
    for candidate in candidates:
        try: return ImageFont.truetype(candidate, size)
        except (OSError, IOError): pass
    return ImageFont.load_default()

def validate_gameplay_icon(name, icon):
    proxy=icon.resize((GAMEPLAY_READABILITY_SIZE,GAMEPLAY_READABILITY_SIZE),Image.Resampling.LANCZOS)
    alpha=proxy.getchannel('A'); mask=alpha.point(lambda a: 255 if a>=96 else 0); bbox=mask.getbbox()
    if not bbox: raise RuntimeError(f'{name}: icon has no visible silhouette at {GAMEPLAY_READABILITY_SIZE}px')
    coverage=sum(1 for value in flat(alpha) if value>=96)/(GAMEPLAY_READABILITY_SIZE**2)
    if not .08<=coverage<=.78: raise RuntimeError(f'{name}: {coverage:.1%} opaque coverage is outside the 8-78% inventory readability envelope')
    left,top,right,bottom=bbox; clearance=min(left,top,GAMEPLAY_READABILITY_SIZE-right,GAMEPLAY_READABILITY_SIZE-bottom)/GAMEPLAY_READABILITY_SIZE
    if clearance<EDGE_CLEARANCE: raise RuntimeError(f'{name}: gameplay-scale silhouette is clipped/crowded: {clearance:.1%} edge clearance < {EDGE_CLEARANCE:.1%}')
    rgb=proxy.convert('RGB'); pixels=[p for p,a in zip(flat(rgb),flat(alpha)) if a>=96]
    luminance=[.2126*r+.7152*g+.0722*b for r,g,b in pixels]
    contrast=max(luminance)-min(luminance) if luminance else 0
    deviation=ImageStat.Stat(proxy.convert('L'),mask=alpha).stddev[0]
    if contrast<28 or deviation<7: raise RuntimeError(f'{name}: facet/material contrast collapses at {GAMEPLAY_READABILITY_SIZE}px (range={contrast:.1f}, stddev={deviation:.1f})')
    print(f'{name}: gameplay readability {coverage:.1%} coverage, {clearance:.1%} edge clearance, luminance range {contrast:.1f}, stddev {deviation:.1f}')

def geode():
    points, faces, colors = [], [], []
    sides, rings = 16, 12
    for r in range(1,rings):
        p=math.pi*r/rings
        for s in range(sides):
            t=2*math.pi*s/sides; bump=1+.07*math.sin(3*t+2*p)+.05*math.cos(5*t-p)
            points.append((.21*math.sin(p)*math.cos(t)*bump,.155*math.cos(p)*bump,.18*math.sin(p)*math.sin(t)*bump))
    top, bottom=len(points),len(points)+1; points.extend([(0,.156,0),(.012,-.15,0)])
    for s in range(sides):
        n=(s+1)%sides; faces.extend([(top,s,n),(bottom,(rings-2)*sides+n,(rings-2)*sides+s)])
    for r in range(rings-2):
        for s in range(sides):
            a=r*sides+s; b=r*sides+(s+1)%sides; c=a+sides; d=b+sides; faces.extend([(a,c,b),(b,c,d)])
    rng=random.Random(413)
    for i,f in enumerate(faces):
        center=tuple(sum(points[j][k] for j in f)/3 for k in range(3)); seam=center[2]>.06 and abs(center[1]-.22*center[0])<.023
        base=(161,112,49) if seam else ((83,86,65) if center[1]>.045 and i%4==0 else (111,101,85)); shift=rng.randint(-16,16)
        colors.append(tuple(max(0,min(255,c+shift)) for c in base))
    return points,faces,colors

def crystal(tier):
    points,faces,colors=[],[],[]; rng=random.Random(810+tier); shards=[(0,0,0,.079,.27)]
    if tier==0: shards=[(-.045,-.04,0,.069,.19),(.065,-.035,.035,.06,.22),(0,-.045,-.065,.05,.15)]
    elif tier>=2: shards += [(-.085,-.055,.025,.042,.17),(.09,-.055,-.015,.034,.13)]
    if tier>=3: shards += [(.015,-.06,.085,.033,.14),(-.025,-.06,-.08,.038,.16)]
    if tier==5: shards=[(-.045,-.015,0,.031,.105),(.032,-.026,.022,.027,.085),(.006,-.03,-.04,.02,.075)]
    for ox,oy,oz,r,h in shards:
        start=len(points); sides=6
        for y,rad,twist in [(-h*.4,r*.72,0),(h*.23,r,0),(h*.43,r*.69,.04)]:
            for s in range(sides):
                a=s*math.pi/3+twist; points.append((ox+rad*math.cos(a),oy+y,oz+rad*math.sin(a)))
        points.extend([(ox+r*.12,oy+h*.73,oz),(ox,oy-h*.49,oz)])
        for s in range(sides):
            n=(s+1)%sides; faces.extend([(start+19,start+n,start+s),(start+12+s,start+12+n,start+18)])
            for ring in range(2):
                a=start+ring*6+s; b=start+ring*6+n; faces.extend([(a,b,a+6),(b,b+6,a+6)])
        for _ in range(36):
            base=[(117,76,35),(158,103,42),(198,143,62),(224,180,88),(238,207,133),(165,111,44)][tier]; shift=rng.randint(-23,19); colors.append(tuple(max(0,min(255,c+shift)) for c in base))
    return points,faces,colors

def emit(name, raw, outward=False):
    points,faces,colors=raw
    if name=='geode': faces=[(a,c,b) if dot(cross(sub(points[b],points[a]),sub(points[c],points[a])),points[a])<0 else (a,b,c) for a,b,c in faces]
    elif not outward: faces=[(a,c,b) for a,b,c in faces]
    atlas=Image.new('RGB',(512,512)); d=ImageDraw.Draw(atlas); vertices,uvs,indices=[],[],[]; columns=max(20, math.ceil(math.sqrt(len(faces)))); tile=512//columns
    for i,(f,color) in enumerate(zip(faces,colors)):
        x=(i%columns)*tile; y=(i//columns)*tile; d.rectangle((x,y,x+tile-1,y+tile-1),fill=color); d.polygon([(x+2,y+2),(x+tile-3,y+2),(x+3,y+8)],fill=tuple(min(255,c+10) for c in color)); d.polygon([(x+2,y+tile-3),(x+tile-3,y+tile-3),(x+tile-3,y+tile-7)],fill=tuple(max(0,c-8) for c in color))
        for j,(u,v) in zip(f,[(3,3),(tile-4,3),(tile//2,tile-4)]): vertices.append(points[j]); uvs.append(((x+u)/512,1-(y+v)/512)); indices.append(len(indices))
    data={'vertices':vertices,'uv':uvs,'triangles':indices}; (OUT/f'{name}.mesh.json').write_text(json.dumps(data,separators=(',',':'))+'\n'); atlas.save(OUT/f'{name}.png')
    obj=['# Original Magenheim Earth asset; meters; Y up',f'mtllib {name}.mtl',f'usemtl {name}']+['v '+' '.join(f'{x:.6f}' for x in p) for p in vertices]+['vt '+' '.join(f'{x:.6f}' for x in p) for p in uvs]+[f'f {i+1}/{i+1} {i+2}/{i+2} {i+3}/{i+3}' for i in range(0,len(indices),3)]
    (OUT/f'{name}.obj').write_text('\n'.join(obj)+'\n'); (OUT/f'{name}.mtl').write_text(f'newmtl {name}\nKd 1 1 1\nmap_Kd {name}.png\n')
    icon=render(points,faces,colors); validate_gameplay_icon(name,icon); icon.resize((ICON_SIZE,ICON_SIZE),Image.Resampling.LANCZOS).save(OUT/f'{name}.icon.png'); return icon,len(faces)

def render(points,faces,colors):
    right=normal((1,0,-1)); up=normal((-.45,1,-.45)); view=normal(cross(right,up)); project=lambda p:(dot(p,right),dot(p,up),dot(p,view)); projected=[project(p) for p in points]; lo=[min(p[k] for p in projected) for k in range(2)]; hi=[max(p[k] for p in projected) for k in range(2)]; scale=380/max(hi[k]-lo[k] for k in range(2)); center=[(hi[k]+lo[k])/2 for k in range(2)]; img=Image.new('RGBA',(512,512)); d=ImageDraw.Draw(img); light=normal((-.4,1,1))
    for i in sorted(range(len(faces)),key=lambda i:sum(projected[j][2] for j in faces[i])):
        f=faces[i]; n=normal(cross(sub(points[f[1]],points[f[0]]),sub(points[f[2]],points[f[0]])))
        if dot(n,view)<=0: continue
        shade=.55+.45*max(0,dot(n,light)); c=tuple(int(v*shade) for v in colors[i])+(255,); screen=[(256+(projected[j][0]-center[0])*scale,256-(projected[j][1]-center[1])*scale) for j in f]; d.polygon(screen,fill=c)
    return img

def render_existing_model_icon(name):
    """Re-author a legacy process icon from authoritative mesh + painted atlas inputs."""
    mesh=json.loads((SOURCE/f'{name}.mesh.json').read_text()); points=[tuple(p) for p in mesh['vertices']]; uvs=mesh['uv']; indices=mesh['triangles']; atlas=Image.open(SOURCE/f'{name}.png').convert('RGB'); w,h=atlas.size; faces=[]; colors=[]
    for i in range(0,len(indices),3):
        f=tuple(indices[i:i+3]); faces.append(f); u=sum(uvs[j][0] for j in f)/3; v=sum(uvs[j][1] for j in f)/3; colors.append(atlas.getpixel((min(w-1,max(0,int(u*w))),min(h-1,max(0,int((1-v)*h))))))
    icon=render(points,faces,colors); validate_gameplay_icon(name,icon); icon.resize((ICON_SIZE,ICON_SIZE),Image.Resampling.LANCZOS).save(OUT/f'{name}.icon.png'); print(f'{name}: re-authored {ICON_SIZE}px icon from {len(faces)} authored mesh faces + atlas'); return icon

def main():
    names=['geode','rough','simple','crystal','advanced','master','shards']; icons=[]
    for i,name in enumerate(names):
        icon,count=emit(name,geode() if i==0 else crystal(i-1)); icons.append(icon); print(f'{name}: {count} triangles; mesh + OBJ + 512px atlas + {ICON_SIZE}px icon')
    skill=icons[1].copy(); d=ImageDraw.Draw(skill); d.polygon([(105,95),(128,76),(365,353),(337,385)],fill=(113,81,47,255)); d.polygon([(312,321),(343,298),(402,373),(390,412),(354,397)],fill=(199,194,171,255)); validate_gameplay_icon('crystal-shaping',skill); skill.resize((ICON_SIZE,ICON_SIZE),Image.Resampling.LANCZOS).save(OUT/'crystal-shaping.icon.png')
    if not STAGED: skill.resize((256,256),Image.Resampling.LANCZOS).save(ROOT/'icon.png')
    for legacy_name in LEGACY_MODEL_ICON_NAMES: render_existing_model_icon(legacy_name)
    sheet=Image.new('RGB',(1600,840),(28,31,30)); d=ImageDraw.Draw(sheet); body_font=font(26); title_font=font(44, bold=True); d.text((46,28),'MAGENHEIM / EARTH MINERALS',font=title_font,fill=(232,215,174)); d.text((48,88),'Original low-poly models, painted atlases, and inventory icons',font=body_font,fill=(153,162,151)); preview_size=PREVIEW_ICON_SIZE; display_size=min(270, preview_size)
    for i,(name,icon) in enumerate(zip(names+['Crystal Shaping'],icons+[skill])):
        x=30+(i%4)*395; y=155+(i//4)*330; preview=icon.resize((preview_size,preview_size),Image.Resampling.LANCZOS); preview=preview.resize((display_size,display_size),Image.Resampling.LANCZOS) if display_size != preview_size else preview; sheet.paste(preview,(x+48,y),preview); d.text((x+45,y+274),name.title(),font=body_font,fill=(228,216,188))
    sheet.save(OUT/'earth-content-preview.png')
    print(f'Earth asset output: {OUT}' + (' (staged; authoritative inputs preserved)' if STAGED else ''))

if __name__ == '__main__': main()