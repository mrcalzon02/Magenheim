from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-rift-titan-wrap-r6.py').read_text(encoding='utf-8').replace('rift-titan-wrap-r6','rift-titan-silhouette-r7')
# Keep the guarded -X side camera; provide a front-biased side comparison and
# clay front/profile views without changing the source mesh or maps.
hook="""source=source.replace("('attack',(1,2,.55))","('front-clay',(0,2,.06)),('side-clay',(-2,0,.06)),('side-reference',(-2,.65,.06)),('front-reference',(0,2,0)),('side-reference-clay',(-2,.9,0)),('attack',(1,2,.55))")
source=source.replace("view=='clay'","view in ('clay','front-clay','side-clay','front-reference','side-reference-clay')").replace("if view=='side':","if view in ('side','side-clay','side-reference','side-reference-clay'):")
source=source.replace(' target=center;span=size',' target=center;span=size\\n scene.render.resolution_x=scene.render.resolution_y=900\\n if view in ("front-reference","side-reference-clay"):scene.render.resolution_x=600;scene.render.resolution_y=700;target=center+Vector((0,0,size*(.02 if view=="front-reference" else .045)))')
source=source.replace('cd.ortho_scale=span*1.18','cd.ortho_scale=span*(1/.94 if view=="front-reference" else 1/.89 if view=="side-reference-clay" else 1.18)')
"""
source=source.replace("exec(compile(source,str(__file__),'exec'))",hook+"exec(compile(source,str(__file__),'exec'))")
exec(compile(source,str(__file__),'exec'))
