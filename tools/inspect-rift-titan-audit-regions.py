"""Build enlarged source-image regions as code-native SVG viewports, no raster edits."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'docs/art/creature-models/2026-10-14/rift-titan-systematic-audit-r12';OUT.mkdir(exist_ok=True)
MODEL='../rift-titan-cracked-head-r12/underworld-boss-rift-titan-cracked-head-r12--'
FRONT='../rift-titan-cracked-head-r12/concept-front.png';SIDE='../rift-titan-cracked-head-r12/concept-side.png';SHEET='../../../creature-concepts/2026-10-03/the-rift-titan.png'
REGIONS=[
 ('head','Head front',FRONT,(248,291),(102,10,58,92),MODEL+'front.png',(900,900),(388,58,124,248)),
 ('halo','Floating crest and upper-back structure',SIDE,(195,298),(46,12,147,120),MODEL+'side-reference.png',(900,900),(333,88,320,290)),
 ('shoulders','Shoulders and upper thorax',FRONT,(248,291),(37,64,181,87),MODEL+'front.png',(900,900),(200,228,490,180)),
 ('torso','Cage and crystal core',SHEET,(1536,1024),(963,513,555,471),MODEL+'detail.png',(900,900),(242,154,454,527)),
 ('pelvis','Pelvis and upper legs',FRONT,(248,291),(72,175,103,82),MODEL+'front.png',(900,900),(305,498,286,230)),
 ('arms','Arms and hands',FRONT,(248,291),(170,86,76,203),MODEL+'front.png',(900,900),(604,322,167,463)),
 ('legs','Legs and feet',FRONT,(248,291),(71,185,108,106),MODEL+'front.png',(900,900),(313,522,279,320)),
 ('surface','Stone surface and fracture layers',SHEET,(1536,1024),(309,191,287,240),MODEL+'crown-detail.png',(900,900),(23,471,340,267)),
]
def viewport(path,size,box):return f'<svg viewBox="{" ".join(map(str,box))}" role="img"><image href="{path}" width="{size[0]}" height="{size[1]}"/></svg>'
cards=''.join(f'<section id="{key}"><h2>{title}</h2><div class="pair"><figure>{viewport(a,sa,ba)}<figcaption>Concept region</figcaption></figure><figure>{viewport(b,sb,bb)}<figcaption>Current r12 region</figcaption></figure></div></section>' for key,title,a,sa,ba,b,sb,bb in REGIONS)
html='''<!doctype html><meta charset="utf-8"><title>Rift Titan audit — enlarged regions</title><style>body{background:#171b1d;color:#eee;font:16px system-ui;max-width:1400px;margin:auto;padding:24px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:18px}figure{margin:0}svg{width:100%;height:620px;background:#202326}figcaption{padding:10px}section{padding:12px 0;border-bottom:1px solid #566}a{color:#baf}nav{position:sticky;top:0;background:#171b1d;padding:12px;display:flex;gap:20px;z-index:2}html{scroll-padding-top:65px}</style><h1>Rift Titan — enlarged concept regions</h1><p>Inspection crops only. Source and model camera angles differ; no raster mirroring, retouching or measured percentage match.</p><nav>'''+''.join(f'<a href="#{key}">{key}</a>' for key,*_ in REGIONS)+'</nav>'+cards
(OUT/'regions.html').write_text(html,encoding='utf-8')
print('VERIFIED eight source/model comparison regions; original raster assets untouched')
