"""Owned 2K surface studies for the October 3 boss construction targets.

These are authored material maps, never crops of the concept paintings.
Run before author-boss-concept-models.py. Periodic fields keep the UV seam clean.
"""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/bosses/concept-pass-1'
# color, roughness, surface family, optional organ emission
PROFILES={
 'root-bark':((.23,.17,.10),.91,'fiber',None),
 'ochre-shelf':((.49,.34,.15),.83,'shelf',None),
 'living-gill':((.63,.40,.17),.58,'fiber',(.95,.38,.075)),
 'wet-hide':((.12,.18,.19),.42,'hide',None),
 'fin-membrane':((.16,.23,.23),.49,'fiber',None),
 'oral-tissue':((.25,.095,.085),.37,'hide',None),
 'ivory':((.66,.60,.43),.67,'fiber',None),
 'basalt':((.22,.23,.22),.93,'stone',None),
 'molten-organ':((.92,.28,.035),.44,'stone',(1,.30,.012)),
 'ancient-ice':((.48,.65,.72),.33,'ice',None),
 'frozen-core':((.19,.39,.53),.29,'ice',(.12,.42,.64)),
 'rift-strata':((.37,.35,.39),.89,'stone',None),
 'binding-prism':((.40,.21,.59),.35,'ice',(.48,.12,.78)),
 'decay-root':((.24,.22,.12),.91,'fiber',None),
 'carrion-sepal':((.43,.27,.13),.81,'shelf',None),
 'amber-organ':((.69,.37,.08),.51,'hide',(.90,.36,.055)),
 'light-organ':((.15,.48,.49),.30,'hide',(.12,.68,.70)),
}

def main():
 import numpy as np
 from PIL import Image
 OUT.mkdir(parents=True,exist_ok=True)
 n=2048
 v,u=np.mgrid[0:n,0:n].astype(np.float32)/n
 grain=(np.sin(2*np.pi*(31*u+17*v))+.45*np.sin(2*np.pi*(97*u-53*v)))/1.45
 broad=np.sin(2*np.pi*(3*u+2*v))*np.cos(2*np.pi*(u-4*v))
 for name,(color,rough,family,emit) in PROFILES.items():
  if family=='fiber':
   veins=np.sin(2*np.pi*(26*u)+1.9*np.sin(2*np.pi*3*v))
   relief=.45*broad+.35*veins+.10*grain
  elif family=='shelf':
   relief=.40*broad+.35*np.sin(2*np.pi*(7*v)+.5*np.sin(2*np.pi*3*u))+.10*grain
  elif family=='hide':
   relief=.25*broad+.13*np.sin(2*np.pi*47*u)*np.sin(2*np.pi*43*v)+.13*grain
  else:
   # Intersecting fault bands / cloudy crystalline laminations.
   fissure=np.exp(-np.abs(np.sin(2*np.pi*(5*u+3*v)+.4*np.sin(2*np.pi*4*v)))*32)
   relief=.38*broad+.09*grain-.50*fissure
  brightness=np.clip(.82+.28*relief,.34,1.2)
  albedo=np.stack([np.clip(c*brightness,0,1) for c in color],axis=-1)
  dx=(np.roll(relief,-1,axis=1)-np.roll(relief,1,axis=1))*n*.003
  dy=(np.roll(relief,-1,axis=0)-np.roll(relief,1,axis=0))*n*.003
  normal=np.stack([-dx,dy,np.ones_like(dx)],axis=-1)
  normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
  roughmap=np.clip(rough+.10*relief,0,1)
  packed=np.zeros((n,n,4),np.float32);packed[:,:,3]=1-roughmap
  maps={'albedo':albedo,'normal':normal*.5+.5,'roughness':roughmap,'metallic-smoothness':packed}
  if emit:
   mask=np.clip(.52+.48*broad,0,1)
   maps['emission']=np.stack([mask*c for c in emit],axis=-1)
  for kind,array in maps.items():
   Image.fromarray(np.rint(array*255).astype(np.uint8)).save(OUT/(name+'-'+kind+'.png'))
  print('AUTHORED 2048px '+name,flush=True)

if __name__=='__main__':main()
