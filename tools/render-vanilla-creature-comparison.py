"""Render native donor bind-pose meshes using the station evidence importer."""
from pathlib import Path
source=Path(__file__).with_name('render-vanilla-station-comparison.py').read_text()
source=source.replace("dist/vanilla-station-comparison","dist/vanilla-creature-comparison")
source=source.replace("uv[p[1]-1] for p in items", "(uv[p[1]-1] if len(p)>1 and 0<p[1]<=len(uv) else (0,0)) for p in items")
source=source.replace("normals[p[2]-1] for p in items", "(normals[p[2]-1] if len(p)>2 and 0<p[2]<=len(normals) else (0,0,1)) for p in items")
exec(compile(source,str(Path(__file__)), 'exec'))
