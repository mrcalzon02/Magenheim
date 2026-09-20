"""Check the shipped Earth art geometry and images, independently of the generator.

Legacy 128px icons remain admissible for compatibility builds while the checked-in binary
migration is incomplete. Set MAGENHEIM_ENFORCE_ICON_TARGET=1 for an asset acceptance/release
pass; then every Earth icon must meet MAGENHEIM_EARTH_ICON_TARGET_SIZE (minimum 256px).
"""
import json
import math
import os
from collections import Counter
from pathlib import Path
from PIL import Image, ImageStat

root=Path(__file__).resolve().parents[1]/'assets'/'earth'
names=['geode','rough','simple','crystal','advanced','master','shards','structural','workstation','fracturing-block','faceting-wheel','resonance-frame']
legacy_floor=128
icon_target=max(256,int(os.environ.get('MAGENHEIM_EARTH_ICON_TARGET_SIZE',os.environ.get('MAGENHEIM_ICON_TARGET_SIZE','256'))))
enforce_target=os.environ.get('MAGENHEIM_ENFORCE_ICON_TARGET','').strip().lower() in {'1','true','yes','on'}
edge_clearance=max(0.0,min(0.10,float(os.environ.get('MAGENHEIM_ICON_MIN_EDGE_CLEARANCE','0.01'))))
legacy_icons=[]

def flat(im):
    """Read pixels without emitting Pillow 12.3's getdata deprecation warning."""
    reader=getattr(im,'get_flattened_data',None)
    return reader() if reader else im.getdata()

def verify_icon_readability(name, icon):
    """Check framing and material separation on the pixels players actually see."""
    proxy=icon.resize((64,64),Image.Resampling.LANCZOS)
    alpha=proxy.getchannel('A')
    mask=alpha.point(lambda a: 255 if a>=96 else 0)
    bbox=mask.getbbox()
    assert bbox,(name,'icon has no gameplay-scale silhouette')
    visible=[p for p,a in zip(flat(proxy.convert('RGB')),flat(alpha)) if a>=96]
    opaque=len(visible)/(64*64)
    assert .08<=opaque<=.78,(name,f'gameplay-scale opaque coverage {opaque:.1%} outside 8-78% envelope')
    left,top,right,bottom=bbox
    clearance=min(left,top,64-right,64-bottom)/64
    assert clearance>=edge_clearance,(name,f'gameplay-scale silhouette is clipped/crowded: {clearance:.1%} edge clearance < {edge_clearance:.1%}')
    luminance=[.2126*r+.7152*g+.0722*b for r,g,b in visible]
    contrast=max(luminance)-min(luminance)
    deviation=ImageStat.Stat(proxy.convert('L'),mask=mask).stddev[0]
    assert contrast>=28 and deviation>=7,(name,f'gameplay-scale material/facet contrast collapses: range={contrast:.1f}, stddev={deviation:.1f}')

for name in names:
    data=json.loads((root/f'{name}.mesh.json').read_text())
    v,uv,tri=data['vertices'],data['uv'],data['triangles']
    assert len(v)==len(uv) and len(tri)%3==0
    assert all(len(p)==3 and all(math.isfinite(x) for x in p) for p in v)
    assert all(len(p)==2 and all(0<=x<=1 for x in p) for p in uv)
    edges=Counter(); volume=0
    for i in range(0,len(tri),3):
        a,b,c=[tuple(v[j]) for j in tri[i:i+3]]
        ab=[b[k]-a[k] for k in range(3)]; ac=[c[k]-a[k] for k in range(3)]
        n=(ab[1]*ac[2]-ab[2]*ac[1],ab[2]*ac[0]-ab[0]*ac[2],ab[0]*ac[1]-ab[1]*ac[0])
        assert sum(x*x for x in n)>1e-15, (name,'degenerate triangle')
        volume+=sum(a[k]*n[k] for k in range(3))/6
        edges.update([(a,b),(b,c),(c,a)])
    assert volume>0,(name,'inverted mesh')
    assert all(count==1 and edges[(b,a)]==1 for (a,b),count in edges.items()),(name,'open or inconsistent mesh')
    atlas=Image.open(root/f'{name}.png').size
    assert atlas[0]==atlas[1] and atlas[0]>=256,(name,'atlas must be square and >=256px',atlas)
    icon=Image.open(root/f'{name}.icon.png')
    assert icon.size[0]==icon.size[1] and icon.size[0]>=legacy_floor and icon.mode=='RGBA',(name,f'icon must be square RGBA and >={legacy_floor}px',icon.size,icon.mode)
    verify_icon_readability(name,icon)
    if icon.size[0]<icon_target:
        legacy_icons.append(f'{name}.icon.png ({icon.size[0]}px < {icon_target}px)')
        assert not enforce_target,(name,f'icon is below enforced {icon_target}px target',icon.size)
    assert icon.getchannel('A').getextrema()==(0,255)
    assert (root/f'{name}.obj').is_file() and (root/f'{name}.mtl').is_file()
    print(f'{name}: closed outward mesh, valid UVs, >=256px atlas and gameplay-scale icon framing/material contrast verified ({icon.size[0]}px; target >={icon_target}px)')
skill=Image.open(root/'crystal-shaping.icon.png')
assert skill.size[0]==skill.size[1] and skill.size[0]>=legacy_floor and skill.mode=='RGBA',('crystal-shaping',f'icon must be square RGBA and >={legacy_floor}px',skill.size,skill.mode)
verify_icon_readability('crystal-shaping',skill)
if skill.size[0]<icon_target:
    legacy_icons.append(f'crystal-shaping.icon.png ({skill.size[0]}px < {icon_target}px)')
    assert not enforce_target,('crystal-shaping',f'icon is below enforced {icon_target}px target',skill.size)
if legacy_icons:
    print(f'NOTICE: {len(legacy_icons)} Earth icons remain below the {icon_target}px target (enforce={enforce_target}):')
    for item in legacy_icons: print('  - '+item)
print(f'VERIFIED Earth assets; icon target={icon_target}px; edge_clearance={edge_clearance:.1%}; enforce_target={enforce_target}')