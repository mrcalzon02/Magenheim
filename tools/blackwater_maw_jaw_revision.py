"""Interpret the frontal gape as a mouth between jaws, not a perspective cone."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/blackwater_maw_refinement.py').read_text().replace('blackwater-maw-refinement-r3','blackwater-maw-jaws-r5-raw')
source=source.replace('for y,rx,rz,cz in rows:','for j,(y,rx,rz,cz) in enumerate(rows):')
source=source.replace('n=18;count=len(rows)*n','n=19;count=len(rows)*n')
source=source.replace('x=(rx-layer*.065)*math.cos(a);z=cz+(1 if upper else -1)*(rz-layer*.065)*math.sin(a)',"thickness=.10 if upper else .13\n     x=(rx-layer*thickness)*math.cos(a);z=cz+(1 if upper else -1)*(rz-layer*thickness)*math.sin(a)\n     t=(y-rows[0][0])/(rows[-1][0]-rows[0][0]);curved_y=y-.60*t*abs(math.cos(a))**1.5\n     if layer:z+=.04*math.sin(j*1.7+k*.65)")
source=source.replace('verts.append((x,y,z))','verts.append((x,curved_y,z))')
source=source.replace('     if layer:z+=.04*math.sin(j*1.7+k*.65)','')
source=source.replace('[(1.52,.56,.43,.12),(1.96,.83,.67,.16),(2.40,.99,.84,.16),(2.78,1.02,.94,.17),(3.12,1.06,1.01,.18),(3.44,1.08,1.04,.18)]','[(1.60,.56,.43,.12),(1.90,.91,.88,.23),(2.40,.99,1.00,.18),(2.83,1.04,1.05,.16),(3.16,1.08,1.04,.14),(3.44,1.10,1.04,.12)]')
source=source.replace('[(1.58,.40,.29,-.13),(2.06,.68,.48,-.20),(2.50,.88,.70,-.24),(2.97,1.00,.85,-.25),(3.46,1.06,.91,-.22)]','[(1.65,.48,.23,-.12),(2.06,.82,.50,-.25),(2.50,1.01,.70,-.34),(2.98,1.08,.85,-.30),(3.47,1.06,.96,-.22)]')
source=source.replace('ys=[2.16,2.49,2.82,3.12]','ys=[1.82,2.18,2.51,2.78]')
rear_wall=" posterior=B.mass('PosteriorBuccalWall',(0,1.84,-.06),(.79,.14,.65),'oral-tissue','Head',16);record(posterior,'Head','mouth-folds');parts[posterior.name]=posterior\n pending.append((posterior,list(range(16)),body,'buccal_neck_root',.025))\n"
source=source.replace(' # The rear lining carries relief folds',rear_wall+' # The rear lining carries relief folds')
source=source.replace('[(-.33,1.89,z),(0,1.94,z-.055),(.33,1.89,z)]','[(-.33,2.015,z),(0,2.055,z-.055),(.33,2.015,z)]')
source=source.replace("body,'fold_left',.02","posterior,'fold_left',.02").replace("body,'fold_right',.02","posterior,'fold_right',.02")
source=source.replace("(0,1.94,.04),(.11,.085,.11)","(0,2.055,.04),(.11,.085,.11)")
rigid_helper=""" def jaw_contact(point,host):
  host_tree=tree(host);candidates=[]
  for tri in host.data.loop_triangles:
   weights=[{host.vertex_groups[g.group].name:round(g.weight,6) for g in host.data.vertices[i].groups if g.weight>1e-7} for i in tri.vertices]
   if not all(w=={'Jaw':1.0} for w in weights):continue
   coords=[host.matrix_world@host.data.vertices[i].co for i in tri.vertices];normal=(coords[1]-coords[0]).cross(coords[2]-coords[0])
   if normal.length<1e-10:continue
   hit=geometry.closest_point_on_tri(point,*coords);candidates.append(((hit-point).length,hit,sum(coords,Vector())/3,normal.normalized()))
  for distance,hit,center,normal in sorted(candidates,key=lambda v:v[0]):
   for inset in (.10,.25,.50):
    for depth in (.022,.012,.035):
     for sign in (-1,1):
      p=hit.lerp(center,inset)+normal*depth*sign
      if inside(p,host_tree):return p,{'Jaw':1.0}
  raise ValueError('no rigid jaw cheek contact')
"""
source=source.replace(' records=[]\n for o,indices,host,label,depth in pending:',rigid_helper+' records=[]\n for o,indices,host,label,depth in pending:')
triangulation=""" import bmesh
 # A vertical oval with converging jaw edges, rather than a circular aperture.
 hn=len(head.data.vertices)//12
 jaw_rows=[(1.65,.48,.23,-.12),(2.06,.82,.50,-.25),(2.50,1.01,.70,-.34),(2.98,1.08,.85,-.30),(3.47,1.06,.96,-.22)]
 for item in parts.values():
  if item.name=='HingedGumBowl':
   for v in item.data.vertices:
    layer=v.index//jcount;j=(v.index%jcount)//jn
    if j==0:continue
    y,rx,rz,cz=jaw_rows[j];rx-=layer*.13;rz-=layer*.13
    if v.co.z<=cz:
     u=min(1,abs(v.co.x)/rx);s=math.sqrt(max(0,1-u*u));v.co.z=cz-rz*(.35*s+.65*(1-u))
  elif item.name=='VaultedSkull':
   for v in item.data.vertices:
    layer=v.index//hcount;j=(v.index%hcount)//hn
    if j<4:continue
    y,rx,rz,cz=[(3.16,1.08,1.04,.14),(3.44,1.10,1.04,.12)][j-4];rx-=layer*.10;rz-=layer*.10
    if v.co.z>=cz:
     u=min(1,abs(v.co.x)/rx);s=math.sqrt(max(0,1-u*u));v.co.z-=.25*rz*(s-(1-u))
  elif item.name.startswith(('LowerFang','LowerInner','UpperFang','UpperInner')):
   c=sum((v.co for v in item.data.vertices[:6]),Vector())/6;lower=item.name.startswith('Lower');inner='Inner' in item.name
   rx=(1.025 if inner else (1.05 if lower else 1.09));rz=(.84 if inner else .955) if lower else (1.01 if inner else 1.035)
   u=min(1,abs(c.x)/rx);s=math.sqrt(max(0,1-u*u));delta=(.65 if lower else -.25)*rz*(s-(1-u))
   for v in item.data.vertices:v.co.z+=delta
  if item.name.startswith(('VaultedSkull','HingedGumBowl','FleshyCommissure','UpperFang','UpperInner','LowerFang','LowerInner','CheekPlate','Sensory','RaisedMouthFloor','PosteriorBuccalWall','ThroatRuga','InnerThroatOrgan')):
   for v in item.data.vertices:v.co.x*=.84;v.co.z*=1.16
  item.data.update()
 # Freeze the actual surface before seating contacts. Rewinding a nonplanar
 # quad during export can otherwise select a different diagonal beneath a root.
 for item in parts.values():
  bm=bmesh.new();bm.from_mesh(item.data);bmesh.ops.triangulate(bm,faces=list(bm.faces),quad_method='FIXED',ngon_method='EAR_CLIP');bm.to_mesh(item.data);bm.free();item.data.update()
"""
source=source.replace(' ordered=sorted(parts.values(),key=lambda o:o.name);',triangulation+' ordered=sorted(parts.values(),key=lambda o:o.name);')
source=source.replace('p,w=embed(o.matrix_world@o.data.vertices[index].co,host,depth);assign(o,index,p,w)',"if label in ('skull_neck','jaw_hinge'):\n     p=o.matrix_world@o.data.vertices[index].co;w={'Head':1.0};assert inside(p,tree(host)),'neck envelope does not contain the original root'\n    else:p,w=jaw_contact(o.matrix_world@o.data.vertices[index].co,host) if label=='lower_cheek' else embed(o.matrix_world@o.data.vertices[index].co,host,depth)\n    assign(o,index,p,w)")
source=source.replace('a=.12+(math.pi-.24)*k/(total-1);rx=1.065 if not inner else .99;rz=1.025 if upper else .89\n    y=3.40 if not inner else 3.08;z=(.18+rz*math.sin(a)) if upper else (-.22-rz*math.sin(a));x=rx*math.cos(a)',"a=.12+(math.pi-.24)*k/(total-1)\n    rx=(1.09 if upper else 1.05) if not inner else 1.025\n    rz=(1.035 if upper else .955) if not inner else (1.01 if upper else .84)\n    y=(3.42 if upper else 3.45) if not inner else 3.08\n    y-= (.60 if not inner else .48)*abs(math.cos(a))**1.5\n    z=((.12 if not inner else .14)+rz*math.sin(a)) if upper else ((-.22 if not inner else -.30)-rz*math.sin(a));x=rx*math.cos(a)")
source=source.replace('direction=-1 if upper else 1',"length*=.35+.65*math.sqrt(math.sin(a))\n    if not upper:length*=.82\n    direction=-1 if upper else 1")
source=source.replace('y+.06,z+direction*length*.42','y-.035,z+direction*length*.42').replace('y+.12,z+direction*length*.82','y-.08,z+direction*length*.82').replace('y+.10,z+direction*length','y-.12,z+direction*length')
marker=' # Unequal, curved outer and inner fang rows.'
addition=" tongue=B.mass('RaisedMouthFloor',(0,2.78,-.84),(.36,.59,.16),'oral-tissue','Jaw',8);record(tongue,'Jaw','mouth-folds');parts[tongue.name]=tongue\n pending.append((tongue,list(range(8)),jaw,'mouth_floor_root',.025))\n"
assert marker in source;source=source.replace(marker,addition+marker)
source=source.replace("'anatomy.json'","'anatomy.json'").replace('armored upper vault, articulated thick lower gum bowl, lateral cheek commissures and deep rugae-lined open gape','broad near-parallel palate, curved upper tooth bed, U-shaped projecting lower jaw, cheek commissures, raised fleshy mouth floor and rear throat folds; perspective depth is not a flared cone')
exec(compile(source,str(__file__),'exec'))
