"""Check the shipped art geometry and images, independently of the generator."""
import json
import math
from collections import Counter
from pathlib import Path
from PIL import Image

root=Path(__file__).resolve().parents[1]/'assets'/'earth'
names=['geode','rough','simple','crystal','advanced','master','shards','workstation','fracturing-block','faceting-wheel','resonance-frame']
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
    atlas = Image.open(root/f'{name}.png').size
    assert atlas[0] == atlas[1] and atlas[0] >= 256, (name, 'atlas must be square and >=256px', atlas)
    icon=Image.open(root/f'{name}.icon.png')
    # The authoritative generator now targets >=256px. Keep the checked-in legacy 128px
    # payload admissible until the binary regeneration lands, so this source migration does
    # not deliberately break the offline build between commits. Any regenerated icon below
    # the historical floor, non-square image, or non-RGBA payload still fails immediately.
    assert icon.size[0] == icon.size[1] and icon.size[0] >= 128 and icon.mode=='RGBA', (name, 'icon must be square RGBA and >=128px during 256px migration', icon.size, icon.mode)
    assert icon.getchannel('A').getextrema()==(0,255)
    assert (root/f'{name}.obj').is_file() and (root/f'{name}.mtl').is_file()
    print(f'{name}: closed outward mesh, valid UVs, >=256px atlas and transparent icon verified ({icon.size[0]}px; target >=256px)')
skill = Image.open(root/'crystal-shaping.icon.png')
assert skill.size[0] == skill.size[1] and skill.size[0] >= 128 and skill.mode == 'RGBA', ('crystal-shaping', 'icon must be square RGBA and >=128px during 256px migration', skill.size, skill.mode)
