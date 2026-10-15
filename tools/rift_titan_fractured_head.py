"""R13: asymmetric stepped cortex and variable recessed fault, one closed head.

Reuse the preceding head's construction/attachment routine, replacing its
regular section layout and four cortex quads before construction. This keeps
the retained body recipe frozen and the six neck seating checks intact.
"""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/rift_titan_cracked_head.py').read_text(encoding='utf-8')
source=source.replace('rift-titan-cracked-head-r12','rift-titan-fractured-head-r13')
start=source.index(' stations=(');end=source.index(' me=bpy.data.meshes.new(HEAD)',start)
source=source[:start]+'''
 # Explicit perimeter, not scaled circular/elliptical cross-sections. The
 # upper shaft has long straight edges with abrupt ledges; lower triangular
 # breaks form the wings and taper. Heights differ on the two sides.
 left=((- .030,0),(-.235,.18),(-.263,.20),(-.278,.36),(-.205,.38),
       (-.200,.70),(-.128,.72),(-.137,.92),(-.082,.93),(-.055,1))
 right=((.025,.02),(.230,.20),(.268,.23),(.254,.41),(.183,.43),
        (.180,.83),(.125,.845),(.120,.95),(.061,.97),(.008,.94))
 outline=left+tuple(reversed(right))
 vertices=[];faces=[];roles=[];materials=[];depths=[]
 for rear in (False,True):
  for i,(x,t) in enumerate(outline):
   front=.600+.25*t-(.025 if i<10 else .018)
   vertices.append((x*(.87 if rear else 1),front-(.17 if rear else 0),base+(tip-base)*t))
 fault=((.070,-.016,.012,.035,.045,.050),(.275,-.035,.044,.048,.030,.115),
        (.595,.018,.021,.040,.045,.090),(.910,-.027,.006,.022,.025,.040))
 # Four columns: left lip, right lip, left floor, right floor. The opening
 # closes below the tip and above the base, leaving an unbroken rear slab.
 for column in range(4):
  for j,(t,x,half,bl,br,depth) in enumerate(fault):
   front=.600+.25*t
   px=x-half-bl if column==0 else x+half+br if column==1 else x-half if column==2 else x+half
   py=front+(.012 if j%2 else -.006) if column<2 else front-depth-(.006 if column==3 else 0)
   vertices.append((px,py,base+(tip-base)*t))
 depths=[f[-1] for f in fault]
 def face(ids,role=1):
  faces.append(tuple(ids));roles.append(role);materials.append(1 if role==3 else 0)
 def polygon(ids,role=1):
  points=[Vector((vertices[i][0],vertices[i][2],0)) for i in ids]
  triangles=geometry.tessellate_polygon([points]);assert len(triangles)==len(ids)-2
  for tri in triangles:face([ids[v if isinstance(v,int) else points.index(v)] for v in tri],role)
 # Twenty boundary segments define the entire extruded slab, including each
 # silhouette notch. The crack is a recessed hole in its front face only.
 for i in range(20):face((i,(i+1)%20,(i+1)%20+20,i+20))
 polygon(list(range(20,40)))
 front_start=len(faces)
 polygon(list(range(10))+[43,42,41,40])
 polygon(list(range(10,20))+[44,45,46,47])
 face((9,10,47,43));face((19,0,40,44))
 front_end=len(faces)
 lips=[40,41,42,43,47,46,45,44]
 floors=[48,49,50,51,55,54,53,52]
 for j in range(8):face((lips[j],lips[(j+1)%8],floors[(j+1)%8],floors[j]),2)
 polygon(floors,3)
 # Add four real cortex fracture points without changing the perimeter or
 # budget. Select broad front triangles near the intended longitudinal seams.
 chosen=set()
 for target_x,target_t in ((-.15,.52),(-.10,.82),(.15,.28),(.11,.65)):
  target=Vector((target_x,.6,base+(tip-base)*target_t));candidates=[]
  for i in range(front_start,front_end):
   if i in chosen or len(faces[i])!=3:continue
   a,b,c=[Vector(vertices[v]) for v in faces[i]];area=(b-a).cross(c-a).length
   if area>.004:candidates.append(((sum((a,b,c),Vector())/3-target).length,i))
  _,i=min(candidates);chosen.add(i);q=faces[i]
  p=sum((Vector(vertices[v]) for v in q),Vector())/3;p.y+=.032
  n=len(vertices);vertices.append(tuple(p));faces[i]=(q[0],q[1],n)
  face((q[1],q[2],n));face((q[2],q[0],n))
 assert len(vertices)==60
''' +source[end:]
source=source.replace("for v in me.vertices:me.attributes['head_station'].data[v.index].value=v.index//8+1;me.attributes['head_section'].data[v.index].value=v.index%8+1", "for v in me.vertices:me.attributes['head_station'].data[v.index].value=(v.index-40)%4+1 if 40<=v.index<56 else 0;me.attributes['head_section'].data[v.index].value=((v.index-40)//4+1) if 40<=v.index<56 else 0")
source=source.replace('len(me.loop_triangles)==108','len(me.loop_triangles)==116')
source=source.replace('new_head_triangles=108,total_triangles=1792','new_head_triangles=116,total_triangles=1800')
source=source.replace('purple_floor_triangles=8','purple_floor_triangles=6').replace('stations=7,vertices_per_station=8','outline_vertices=20,fault_stations=4')
source=source.replace("acceptance='head interpretation correction; not final boss or 85% concept-match acceptance'", "acceptance='unaccepted replacement after user rejected the still-oval first r13 attempt',outline='extruded polygonal slab: straight upper shaft, abrupt shoulders and triangular lower breaks; no tapered ring shell',secondary_cortex_vertices=4")
exec(compile(source,str(__file__),'exec'))
