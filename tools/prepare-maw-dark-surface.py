from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];T=ROOT/'tools'
s=(T/'refine-maw-antialiased.py').read_text()
hook="source=source.replace(\"bs.inputs['Emission Strength'].default_value=1.4\",\"bs.inputs['Emission Strength'].default_value=1.4;bs.inputs['Specular IOR Level'].default_value=.18\")\nsource=source.replace(\"normal.uv_map='BlackwaterMawAtlas';\",\"normal.uv_map='BlackwaterMawAtlas';normal.inputs['Strength'].default_value=.30;\")\n"
s=s.replace("exec(compile(source,str(__file__),'exec'))",hook+"exec(compile(source,str(__file__),'exec'))")
(T/'refine-maw-dark-surface.py').write_text(s)
p=T/'refine-blackwater-maw-integrated-r14.py';s=p.read_text().replace('tools/refine-maw-antialiased.py','tools/refine-maw-dark-surface.py');p.write_text(s)
print('Prepared lower broad-surface sheen')
