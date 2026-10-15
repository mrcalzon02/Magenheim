"""Retain attachment/rig checks and compare every non-head component with r12."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/verify-rift-titan-cracked-head-r12.py').read_text(encoding='utf-8').replace('rift-titan-cracked-head-r12','rift-titan-fractured-head-r13')
s=s.replace('len(faces)==108 and len(ids)==56','len(faces)==116 and len(ids)==60')
s=s.replace('{1:76,2:24,3:8}','{1:94,2:16,3:6}')
s=s.replace('range(1,8)','range(1,5)').replace('set(range(1,9))','set(range(1,5))')
s=s.replace('for j in range(1,5):', "from mathutils.bvhtree import BVHTree\nhead_tree=BVHTree.FromPolygons([obj.matrix_world@v.co for v in me.vertices],[tuple(f.vertices) for f in faces],all_triangles=True)\nfor j in range(1,5):")
s=s.replace('lip=(ring[2].y+ring[5].y)/2;floor=(ring[3].y+ring[4].y)/2;rear=(ring[7].y+ring[8].y)/2', "lip=(ring[1].y+ring[2].y)/2;floor=(ring[3].y+ring[4].y)/2\n origin=(ring[3]+ring[4])/2-Vector((0,.0001,0));hit=head_tree.ray_cast(origin,Vector((0,-1,0)));assert hit[0] is not None,('missing rear wall',j)\n rear=hit[0].y")
s=s.replace("metrics['triangles']==1792","metrics['triangles']==1800").replace("['head-crystal']==108","['head-crystal']==116")
s=s.replace('vertices=56,triangles=108','vertices=60,triangles=116').replace('total_actor_triangles=1792','total_actor_triangles=1800')
s=s.replace('assets/models/studies/underworld-boss-rift-titan-notched-r11.blend','assets/models/studies/underworld-boss-rift-titan-cracked-head-r12.blend')
s=s.replace("['total']=1792","['total']=1800").replace('1792 triangles','1800 triangles')
s=s.replace('saved r11','saved r12')
exec(compile(s,str(__file__),'exec'))
