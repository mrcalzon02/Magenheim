#!/usr/bin/env python3
"""Fail-closed source quality floor against a fresh in-game Valheim Wolf donor export.

The Wolf is the Mycelial Stalker's declared host chassis in
docs/UNDERWORLD_CONTENT_PROTOTYPES.md. This checks source geometry and PBR map
dimensions, not concept-art silhouette, deformation or runtime acceptance.
"""
import json
import os
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'assets/models/source/underworld-creature-mycelial-stalker.blend'
TEXTURES=ROOT/'assets/textures/underworld/creatures/mycelial-stalker'
STEMS=('root-hide','mycelial-cord','shelf-fungus','sensory-pit')

def png_size(path):
 with path.open('rb') as stream: head=stream.read(24)
 if len(head)!=24 or head[:8]!=bytes((137,80,78,71,13,10,26,10)):
  raise RuntimeError(f'Missing or invalid PNG: {path}')
 return struct.unpack('>II',head[16:24])

def obj_triangle_count(path):
 total=0
 for line in path.read_text(encoding='utf-8-sig').splitlines():
  if line.startswith('f '):
   corners=len(line.split())-1
   if corners<3: raise RuntimeError(f'Invalid donor OBJ face: {line}')
   total+=corners-2
 if total<=0: raise RuntimeError(f'Empty donor OBJ: {path}')
 return total

def donor_floor(donor_dir):
 manifest=json.loads((donor_dir/'manifest.json').read_text(encoding='utf-8-sig'))
 matches=[row for row in manifest.get('models',[]) if str(row.get('prefab','')).casefold()=='wolf' and row.get('file')]
 if len(matches)!=1: raise RuntimeError('Expected exactly one measured Wolf donor')
 donor=matches[0]
 triangles=int(donor.get('triangles',0))
 if triangles<=0: raise RuntimeError('Wolf donor triangle count invalid')
 obj=donor_dir/donor['file']
 actual=obj_triangle_count(obj)
 if actual!=triangles: raise RuntimeError(f'Wolf donor manifest triangle count {triangles} differs from OBJ {actual}')
 mtl=obj.with_suffix('.mtl')
 maps=[]
 for line in mtl.read_text(encoding='utf-8-sig').splitlines():
  if line.startswith(('map_Kd ','map_Bump ','map_Ke ')):
   maps.append(png_size(donor_dir/line.split(maxsplit=1)[1].strip()))
 if not maps: raise RuntimeError('Wolf donor texture resolution unmeasured')
 minimum=(max(w for w,h in maps),max(h for w,h in maps))
 return triangles,minimum

def main():
 export=os.environ.get('MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR')
 if not export: raise RuntimeError('Set MAGENHEIM_VALHEIM_DONOR_EXPORT_DIR to a fresh in-game Wolf export')
 donor_dir=Path(export)
 triangles,minimum=donor_floor(donor_dir)
 for stem in STEMS:
  for kind in ('albedo','normal','roughness'):
   path=TEXTURES/f'{stem}-{kind}.png'
   size=png_size(path)
   if size[0]<minimum[0] or size[1]<minimum[1]:
    raise RuntimeError(f'{path.name}: {size} below Wolf donor {minimum}')
 if not SOURCE.is_file(): raise RuntimeError(f'Missing authored source: {SOURCE}')
 import bpy
 bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
 if bpy.context.scene.get('magenheim_model_id')!='underworld-creature-mycelial-stalker':
  raise RuntimeError('Wrong creature source')
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 source_triangles=sum(len(p.vertices)-2 for o in meshes for p in o.data.polygons)
 if source_triangles<max(7000,triangles):
  raise RuntimeError(f'Mycelial Stalker {source_triangles} triangles below Wolf {triangles} / 7000 floor')
 print(f'PASS: Mycelial Stalker {source_triangles} triangles >= Wolf {triangles}; PBR maps >= {minimum[0]}x{minimum[1]}',flush=True)

if __name__=='__main__':
 main()
