"""Shape existing cap and tissue vertices; retain revision 3 topology/budgets."""
import bpy,math,hashlib
from mathutils import Vector
import first_bloom_concept_sculpt as previous
materials=previous.materials
unwrap_atlas=previous.unwrap_atlas

def phase(name):
 return int(hashlib.sha256(name.encode()).hexdigest()[:8],16)/0xffffffff*math.tau

def cap_displacement(co,rx,ry,p):
 a=math.atan2(co.y/ry,co.x/rx);r=min(1.12,math.hypot(co.x/rx,co.y/ry))
 # Gravity bends the free rim most strongly. Front-facing lobes droop farther;
 # the stalk-side center remains seated, with unequal edge scallops.
 front=.45+.55*max(0,math.sin(a))
 lobe=.78+.22*math.sin(a*3+p)
 sag=(.035+.075*rx)*r**2.6*front*lobe
 fold=.012*math.sin(a*5+p)*r*r
 return Vector((co.x*.022*math.sin(a*4+p)*r*r,co.y*.022*math.sin(a*3-p)*r*r,-sag+fold))

def build(B,arm,record,profile,tube,cap):
 trunk=previous.build(B,arm,record,profile,tube,cap)
 caps={}
 for o in list(bpy.context.scene.objects):
  if o.type!='MESH':continue
  if not o.name.startswith(('CathedralMantle','RearTrunkBracket','LimbBracket','RootSkirt')):continue
  rx=max(abs(v.co.x) for v in o.data.vertices)/1.025
  ry=max(abs(v.co.y) for v in o.data.vertices)/1.025
  p=phase(o.name);caps[o.name]=(o.matrix_world.copy(),rx,ry,p)
  for v in o.data.vertices:v.co+=cap_displacement(v.co,rx,ry,p)
  o['precision_change']='free rim sag, unequal scallops and radial folds; topology retained'

 for o in list(bpy.context.scene.objects):
  if o.type!='MESH':continue
  if o.name.startswith(('MantleCurtain','ChestVeil','LimbTissue')):
   # Solidify preserves a front/back pair of rails. Work on their centerlines,
   # retaining thickness and the same two/three longitudinal spans.
   n=len(o.data.vertices)//4
   original=[v.co.copy() for v in o.data.vertices]
   centers=[(original[2*j]+original[2*j+1])*.5 for j in range(n)]
   widths=[(original[2*j+1]-original[2*j])*.5 for j in range(n)]
   p=phase(o.name);targets=[]
   anchor=Vector((0,0,0))
   if o.name.startswith('MantleCurtain'):
    index=o.name.removeprefix('MantleCurtain').split('_')[0]
    matrix,rx,ry,cp=caps['CathedralMantle'+index]
    local=matrix.inverted()@centers[0]
    anchor=matrix.to_3x3()@cap_displacement(local,rx,ry,cp)
   for j,c in enumerate(centers):
    t=j/(n-1);target=c+anchor
    if o.name.startswith('ChestVeil'):
     # These rails frame the chamber instead of falling as parallel flat strips.
     s=-1 if c.x<0 else 1;k=int(o.name[-1])
     if j==1:target.x+=s*(.012+.012*k);target.y+=.018
     elif j==2:target.x=s*(.36+.052*k);target.y+=.015+.008*k
     elif j==n-1:target.x=s*(.31+.09*k);target.z-=.026+.014*k
    else:
     target.x+=math.sin(t*math.pi)*(.020+.013*math.sin(p))
     target.y+=math.sin(t*math.pi)*(.018+.012*math.cos(p))
     target.z-=(.035+.015*math.sin(p))*t*t
    targets.append(target)
   for v in o.data.vertices:
    i=v.index%(2*n);j,rail=divmod(i,2);t=j/(n-1)
    # Twist toward the weight-bearing frame, pinch intervening necks, and let
    # the tip taper; keep its existing front/back solidification offset.
    twist=.50*math.sin(t*math.pi+p*.3)*t
    w=widths[j];rot=Vector((w.x*math.cos(twist),w.x*math.sin(twist)+w.y,w.z))
    pinch=1 if j==0 else (.76+.17*math.sin(p+j*1.8))
    offset=original[v.index]-(centers[j]+widths[j]*(1 if rail else -1))
    v.co=targets[j]+rot*pinch*(1 if rail else -1)+offset
   for poly in o.data.polygons:poly.use_smooth=True
   o['precision_change']='curved frame rails, tapered necks, twist and gravity; topology retained'
  elif o.name.startswith('VeilCrosslink'):
   # Recurve the existing three-section branches into an arched scaffold.
   p=phase(o.name)
   for v in o.data.vertices:
    j=v.index//4
    if j==1:v.co.y+=.030;v.co.z+=.016*math.sin(p)
    elif j==2:v.co.y+=.012
  elif o.name.startswith('ChamberRib'):
   # Give each existing rib a slightly different arch and slim its middle.
   p=phase(o.name)
   for v in o.data.vertices:
    if 5<=v.index<10:v.co.y+=.014+.008*math.sin(p)
 return trunk
