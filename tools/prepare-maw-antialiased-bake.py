from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];T=ROOT/'tools'
s=(T/'refine-blackwater-maw.py').read_text()
hook="source=source.replace('width=512,height=512','width=1024,height=1024')\nsource=source.replace('scene.render.bake.margin=2','scene.render.bake.margin=4')\nsource=source.replace('path=TEX/(ID+','im.scale(512,512)\\n path=TEX/(ID+')\n"
s=s.replace("exec(compile(source,str(__file__),'exec'))",hook+"exec(compile(source,str(__file__),'exec'))")
(T/'refine-maw-antialiased.py').write_text(s)
p=T/'refine-blackwater-maw-teeth-eyes-r13.py';s=p.read_text();needle="source=(ROOT/'tools/refine-blackwater-maw-spined-fins.py').read_text()";s=s.replace(needle,needle+".replace('tools/refine-blackwater-maw.py','tools/refine-maw-antialiased.py')")
p.write_text(s)
print('Prepared higher-resolution bake, retaining four final 512px maps')
