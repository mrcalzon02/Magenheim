"""Offline mouth shape, silhouette, continuity and topology regression gates."""
import math
import sys
import unittest
from collections import Counter
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from shelf_lurker_mouth_geometry import mouth_barb_mesh_data, RINGS, SIDES


def all_segments():
    for digit in range(8):
        theta=2*math.pi*digit/8
        x,y=math.cos(theta),math.sin(theta)
        root=(x*.23,.38+y*.23,1.29)
        flare=(x*.30,.38+y*.30,1.13)
        tip=(x*.115,.38+y*.115,.98)
        base_bow=(x*.07/math.pi,y*.07/math.pi,0.)
        hook_bow=(x*.185/math.pi,y*.185/math.pi,0.)
        yield digit,'Base',root,flare,.036,.026,base_bow
        yield digit,'Hook',flare,tip,.026,.004,hook_bow


def center(vertices,ring):
    return tuple(sum(vertices[ring*SIDES+j][k] for j in range(SIDES))/SIDES for k in range(3))


def volume(vertices,faces):
    value=0.
    for face in faces:
        p=vertices[face[0]]
        for i in range(1,len(face)-1):
            q,r=vertices[face[i]],vertices[face[i+1]]
            cross=(q[1]*r[2]-q[2]*r[1],q[2]*r[0]-q[0]*r[2],q[0]*r[1]-q[1]*r[0])
            value+=sum(p[k]*cross[k] for k in range(3))/6
    return value


class MouthGeometryTests(unittest.TestCase):
    def test_all_sixteen_sections_are_dense_closed_manifolds(self):
        for digit,part,a,b,r0,r1,bow in all_segments():
            with self.subTest(digit=digit,part=part):
                vertices,faces=mouth_barb_mesh_data(a,b,r0,r1,bow)
                self.assertEqual((len(vertices),len(faces)),(108,98))
                self.assertEqual(sum(len(f)-2 for f in faces),212)
                edges=Counter()
                oriented=Counter()
                for face in faces:
                    self.assertEqual(len(face),len(set(face)))
                    for p,q in zip(face,face[1:]+face[:1]):
                        edges[tuple(sorted((p,q)))]+=1
                        oriented[(p,q)]+=1
                self.assertTrue(all(n==2 for n in edges.values()))
                self.assertTrue(all(oriented[(q,p)]==1 for p,q in oriented))
                self.assertGreater(volume(vertices,faces),1e-7)

    def test_attachment_points_and_curved_outer_flare(self):
        for digit,part,a,b,r0,r1,bow in all_segments():
            with self.subTest(digit=digit,part=part):
                vertices,_=mouth_barb_mesh_data(a,b,r0,r1,bow)
                self.assertLess(math.dist(center(vertices,0),a),1e-9)
                self.assertLess(math.dist(center(vertices,RINGS-1),b),1e-9)
                midpoint=tuple((a[k]+b[k])/2 for k in range(3))
                self.assertGreater(math.dist(center(vertices,4),midpoint),.018)

    def test_funnel_stays_open_and_points_down(self):
        for digit in range(8):
            base,hook=list(all_segments())[2*digit:2*digit+2]
            vb,_=mouth_barb_mesh_data(*base[2:])
            vh,_=mouth_barb_mesh_data(*hook[2:])
            self.assertLess(math.dist(center(vb,8),center(vh,0)),1e-9)
            self.assertGreater(center(vb,0)[2],center(vb,8)[2])
            self.assertGreater(center(vh,0)[2],center(vh,8)[2])
            self.assertGreater(math.hypot(center(vb,8)[0],center(vb,8)[1]-.38),.295)
            self.assertGreater(math.hypot(center(vh,8)[0],center(vh,8)[1]-.38),.11)
            self.assertLess(math.hypot(center(vh,8)[0],center(vh,8)[1]-.38),.12)

    def test_flare_ring_surfaces_match_without_visible_gap(self):
        for digit in range(8):
            base,hook=list(all_segments())[2*digit:2*digit+2]
            vb,_=mouth_barb_mesh_data(*base[2:])
            vh,_=mouth_barb_mesh_data(*hook[2:])
            for j in range(SIDES):
                self.assertLess(math.dist(vb[8*SIDES+j],vh[j]),1e-8)

    def test_smooth_near_vertical_join_at_flaring_apex(self):
        for digit in range(8):
            base,hook=list(all_segments())[2*digit:2*digit+2]
            _,_,a,b,_,_,c=base
            _,_,p,q,_,_,d=hook
            tangent_base=tuple(b[k]-a[k]-math.pi*c[k] for k in range(3))
            tangent_hook=tuple(q[k]-p[k]+math.pi*d[k] for k in range(3))
            self.assertLess(math.hypot(*tangent_base[:2]),1e-9)
            self.assertLess(math.hypot(*tangent_hook[:2]),1e-9)
            self.assertLess(abs(tangent_base[2]-tangent_hook[2]),.02)

    def test_section_taper_and_finite_uv_parameterization(self):
        for digit,part,a,b,r0,r1,bow in all_segments():
            vertices,_=mouth_barb_mesh_data(a,b,r0,r1,bow)
            for ring in (0,4,8):
                c=center(vertices,ring)
                radial=sum(math.dist(vertices[ring*SIDES+j],c) for j in range(SIDES))/SIDES
                expected=r0+(r1-r0)*ring/8
                self.assertAlmostEqual(radial,expected,delta=.0005)
            self.assertTrue(all(math.isfinite(v) for vertex in vertices for v in vertex))

    def test_cross_section_frames_never_quarter_turn(self):
        # The former world-axis threshold turned the +/-Y hooks by 90 degrees
        # even though their meshes passed manifold and attachment tests.
        for digit,part,a,b,r0,r1,bow in all_segments():
            with self.subTest(digit=digit,part=part):
                vertices,_=mouth_barb_mesh_data(a,b,r0,r1,bow)
                axes=[]
                for ring in range(RINGS):
                    c=center(vertices,ring)
                    direction=tuple(vertices[ring*SIDES][k]-c[k] for k in range(3))
                    length=math.sqrt(sum(v*v for v in direction))
                    axes.append(tuple(v/length for v in direction))
                for u,v in zip(axes,axes[1:]):
                    angle=math.degrees(math.acos(max(-1.,min(1.,sum(x*y for x,y in zip(u,v))))))
                    self.assertLess(angle,30.,'cross-section twist pinches the feeding funnel')

    def test_rejects_malformed_or_unsafe_mouth(self):
        a=(.23,.38,1.29); b=(.30,.38,1.13)
        for end,r0,r1,bow in ((a,.036,.026,(0,0,0)),
                              (b,-.01,.004,(0,0,0)),
                              (b,.036,.050,(0,0,0)),
                              (b,.036,.026,(1,0,0)),
                              ((math.nan,0,0),.036,.026,(0,0,0)),
                              (b,.036,.026,(math.inf,0,0))):
            with self.subTest(end=end,r0=r0,r1=r1,bow=bow),self.assertRaises(ValueError):
                mouth_barb_mesh_data(a,end,r0,r1,bow)


if __name__=='__main__':unittest.main()
