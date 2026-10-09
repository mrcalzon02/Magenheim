"""Offline geometry regression for the existing 18 two-section Shelf Lurker claws."""
import math
import sys
import unittest
from collections import Counter
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from shelf_lurker_talon_geometry import talon_mesh_data,RINGS,SIDES

def claws():
    for side in (-1,1):
        for index,(y0,y1) in enumerate(((.38,.58),(-.05,-.02),(-.48,-.62)),1):
            tip=(side*1.48,y1+.12,.54)
            for digit,offset in enumerate((-.15,0,.15),1):
                a=(tip[0]+side*.035,tip[1]+offset*.70,tip[2]-.02)
                k=(tip[0]+side*.27,tip[1]+offset,tip[2]-.11)
                b=(tip[0]+side*.14,tip[1]+offset*1.15,tip[2]-.25)
                c1=((k[0]-a[0])/math.pi,(k[1]-a[1])/math.pi,0)
                c2=((k[0]-b[0])/math.pi,(k[1]-b[1])/math.pi,0)
                yield side,index,digit,a,k,b,c1,c2

def center(verts,ring):
    return tuple(sum(verts[ring*SIDES+j][k] for j in range(SIDES))/SIDES for k in range(3))

def volume(verts,faces):
    total=0
    for face in faces:
        p=verts[face[0]]
        for j in range(1,len(face)-1):
            q,r=verts[face[j]],verts[face[j+1]]
            cross=(q[1]*r[2]-q[2]*r[1],q[2]*r[0]-q[0]*r[2],q[0]*r[1]-q[1]*r[0])
            total+=sum(p[k]*cross[k] for k in range(3))/6
    return total

class TalonGeometryTests(unittest.TestCase):
    def test_all_36_segments_are_closed_outward_and_dense(self):
        n=0
        for side,index,digit,a,k,b,c1,c2 in claws():
            for start,end,r0,r1,bow in ((a,k,.052,.035,c1),(k,b,.035,.008,c2)):
                n+=1
                verts,faces=talon_mesh_data(start,end,r0,r1,bow)
                self.assertEqual((len(verts),len(faces),sum(len(f)-2 for f in faces)),(108,98,212))
                edges=Counter()
                oriented=Counter()
                for face in faces:
                    for p,q in zip(face,face[1:]+face[:1]):
                        edges[tuple(sorted((p,q)))]+=1
                        oriented[(p,q)]+=1
                self.assertTrue(all(v==2 for v in edges.values()))
                self.assertTrue(all(oriented[(q,p)]==1 for p,q in oriented))
                self.assertGreater(volume(verts,faces),1e-7)
        self.assertEqual(n,36)

    def test_exact_joins_and_bilateral_hook_silhouette(self):
        for side,index,digit,a,k,b,c1,c2 in claws():
            vb,_=talon_mesh_data(a,k,.052,.035,c1)
            vh,_=talon_mesh_data(k,b,.035,.008,c2)
            for ring,target,verts in ((0,a,vb),(8,k,vb),(0,k,vh),(8,b,vh)):
                self.assertLess(math.dist(center(verts,ring),target),1e-9)
            for j in range(SIDES):
                self.assertLess(math.dist(vb[8*SIDES+j],vh[j]),1e-8)
            self.assertGreater(side*(k[0]-a[0]),.20)
            self.assertLess(side*(b[0]-k[0]),-.09)
            self.assertLess(b[2],k[2])

    def test_bowed_centerlines_taper_and_stable_frames(self):
        for side,index,digit,a,k,b,c1,c2 in claws():
            for start,end,r0,r1,bow in ((a,k,.052,.035,c1),(k,b,.035,.008,c2)):
                verts,_=talon_mesh_data(start,end,r0,r1,bow)
                self.assertGreater(math.dist(center(verts,4),tuple((x+y)/2 for x,y in zip(start,end))),.015)
                axes=[]
                for ring in range(RINGS):
                    c=center(verts,ring)
                    d=tuple(verts[ring*SIDES][i]-c[i] for i in range(3))
                    axes.append(tuple(x/math.sqrt(sum(y*y for y in d)) for x in d))
                self.assertTrue(all(sum(x*y for x,y in zip(u,v))>.866 for u,v in zip(axes,axes[1:])))
                self.assertAlmostEqual(math.dist(verts[0],center(verts,0)),r0,delta=1e-8)
                self.assertAlmostEqual(math.dist(verts[8*SIDES],center(verts,8)),r1,delta=1e-8)

    def test_rejects_invalid_anatomy(self):
        a=(1.5,0,.52);b=(1.73,0,.43)
        for start,end,r0,r1,bow in ((a,a,.052,.035,(0,0,0)),(a,b,-.052,.035,(0,0,0)),
                                     (a,b,.052,.06,(0,0,0)),(a,b,.052,.035,(1,0,0)),
                                     ((float('nan'),0,.52),b,.052,.035,(0,0,0)),
                                     (a,(1.73,0,.6),.052,.035,(0,0,0))):
            with self.assertRaises(ValueError):
                talon_mesh_data(start,end,r0,r1,bow)

if __name__=='__main__': unittest.main()
