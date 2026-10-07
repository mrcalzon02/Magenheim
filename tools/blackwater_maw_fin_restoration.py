"""Preserve four dorsal fins, add forward paired fins and build a broad tail fan."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
recipe=(ROOT/'tools/blackwater_maw_fins_texture.py').read_text().replace('blackwater-maw-fins-r6-raw','blackwater-maw-restored-fins-r7-raw')
old="[('AnteriorDorsalFin',[1.12,.90,.62,.34,.05,-.16,-.34],[.10,.55,1.05,1.20,.97,.40,.06]),('PosteriorDorsalFin',[-.55,-.72,-.94,-1.16,-1.38,-1.60,-1.80],[.08,.45,.82,.96,.72,.35,.06])]"
new="[('AnteriorDorsalFin',[1.48,1.25,1.02,.79,.58,.40,.22],[.08,.50,1.15,1.35,1.10,.45,.06]),('MainSecondDorsalFin',[.02,-.15,-.36,-.55,-.73,-.91,-1.05],[.07,.65,1.22,1.30,1.03,.44,.06]),('AftDorsalFin',[-1.20,-1.34,-1.50,-1.67,-1.84,-2.0,-2.12],[.05,.35,.80,.75,.52,.22,.04]),('CaudalDorsalFin',[-2.27,-2.38,-2.5,-2.63,-2.74,-2.85,-2.96],[.035,.13,.30,.40,.28,.14,.03])]"
assert old in recipe;recipe=recipe.replace(old,new)
extra='''
# Preserve the original paired fins, bulk them, and add a forward pair.
source=source.replace('def pectoral(s,side):','def pectoral(s,side,forward=False):')
source=source.replace("root=Vector((s*(.89+.055*math.sin(u*math.pi)),.92-u*.46,-.18))", "root=Vector((s*(.88+.04*math.sin(u*math.pi)),1.22-u*.50,-.18)) if forward else Vector((s*(.89+.055*math.sin(u*math.pi)),.92-u*.46,-.18))")
source=source.replace("tip=Vector((s*(2.02-.33*u+.08*math.sin(k*1.7)),.52-u*1.65,-.43-.11*math.sin(u*math.pi)))", "tip=Vector((s*(2.55-.25*u+.06*math.sin(k*1.7)),1.05-u*1.45,-.85-.40*math.sin(u*math.pi))) if forward else Vector((s*(2.28-.20*u+.05*math.sin(k*1.7)),-.38-u*1.50,-.55-.28*math.sin(u*math.pi)))")
source=source.replace(".045*math.sin(k*math.pi/2)*t*(1-t)+(.014 if layer==0 else -.014)", ".085*math.sin(k*math.pi/2)*t*(1-t)+(.032 if layer==0 else -.032)")
source=source.replace("mesh('PleatedPectoral'+side", "mesh(('ForwardPectoral' if forward else 'PleatedPectoral')+side")
source=source.replace("body,'pectoral_root',.035", "body,'forward_pectoral_root' if forward else 'pectoral_root',.055 if forward else .10")
source=source.replace("pectoral(-1,'L');pectoral(1,'R')", "pectoral(-1,'L');pectoral(1,'R');pectoral(-1,'L',True);pectoral(1,'R',True)")
start=source.index(' # Forked caudal fan');end=source.index(' for s in (-1,1):',start)
tail=""" # Broad forked tail fan with full lobe surfaces and radial pleats.
 outline=[(-3.00,.13),(-3.20,.40),(-3.38,.85),(-4.45,1.70),(-4.62,1.08),(-4.50,.65),(-4.40,.32),(-4.18,.06),(-4.40,-.30),(-4.52,-.65),(-4.62,-1.10),(-4.45,-1.58),(-3.35,-.72),(-3.18,-.28),(-3.00,-.02)]
 tn=len(outline);count=tn*2+1;center=Vector((0,-3.55,.055));vertices=[]
 for layer in range(2):
  for radius in (1,.60):
   for k,(y,z) in enumerate(outline):
    p=center.lerp(Vector((0,y,z)),radius);p.x=(-1 if layer else 1)*(.024+.012*math.sin(k*2.1)**2*(1-radius));vertices.append(p)
  vertices.append(center+Vector((-.022 if layer else .022,0,0)))
 faces=[]
 for layer in range(2):
  base=layer*count
  for k in range(tn):
   b=(k+1)%tn;quad=(base+k,base+b,base+tn+b,base+tn+k);tri=(base+2*tn,base+tn+k,base+tn+b)
   faces.extend([quad if layer==0 else tuple(reversed(quad)),tri if layer==0 else tuple(reversed(tri))])
 for k in range(tn):b=(k+1)%tn;faces.append((k,count+k,count+b,b))
 tail=mesh('BroadCaudalFan',vertices,faces,'fin-membrane','Tail_2','fins')
 pending.append((tail,[0,tn-1,count,count+tn-1],body,'tail_root',.025))
"""
source=source[:start]+tail+source[end:]
source=source.replace('separate anterior and posterior dorsal membranes, and forked caudal fan','four preserved dorsal fins with enlarged forward/main sails, two pairs of broad lateral fins and a full pleated caudal fan')
'''
marker='source=source[:start]+dorsals+source[end:]'
assert recipe.count(marker)==1;recipe=recipe.replace(marker,marker+'\n'+extra)
exec(compile(recipe,str(__file__),'exec'))
