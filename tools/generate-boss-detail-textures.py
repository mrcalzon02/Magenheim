"""Owned multiscale, pixel-painted material studies for donor-quality revision."""
from pathlib import Path
import importlib.util
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'assets/textures/underworld/bosses/concept-detail-r2'
spec=importlib.util.spec_from_file_location('profiles',Path(__file__).with_name('generate-boss-concept-textures.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)

def main():
 import numpy as np
 from PIL import Image
 OUT.mkdir(parents=True,exist_ok=True);n=512
 v,u=np.mgrid[0:n,0:n].astype(np.float32)/n
 for seed,(key,(color,rough,family,emit)) in enumerate(module.PROFILES.items(),921):
  rng=np.random.default_rng(seed)
  def noise(freq):
   lattice=rng.random((freq,freq),dtype=np.float32)
   x=u*freq;y=v*freq;ix=x.astype(int);iy=y.astype(int)
   tx=x-ix;ty=y-iy;tx=tx*tx*(3-2*tx);ty=ty*ty*(3-2*ty)
   return (lattice[iy%freq,ix%freq]*(1-tx)+lattice[iy%freq,(ix+1)%freq]*tx)*(1-ty)+(lattice[(iy+1)%freq,ix%freq]*(1-tx)+lattice[(iy+1)%freq,(ix+1)%freq]*tx)*ty
  macro=noise(6);mid=noise(22);fine=noise(90);grain=noise(170)
  height=.37*macro+.30*mid+.20*fine+.07*grain
  stain=np.clip((macro-.37)*2,0,1)
  if family=='fiber':
   path=u*44+(mid-.5)*2.3+np.sin(v*math_tau*3)*.31
   strand=np.abs(np.sin(path*math_tau));cracks=np.exp(-strand*18)
   pores=np.clip((.25-fine)*4,0,1)
   height+=.13*strand-.26*cracks-.09*pores
   tone=.56+.64*height-.34*cracks
   tint=np.stack([1-.08*stain,1+.13*stain,1-.09*stain],-1)
  elif family=='shelf':
   rings=np.sin((v*14+macro*.7)*math_tau)*.5+.5
   scar=np.clip((.25-mid)*3.8,0,1)
   height+=.12*rings-.18*scar
   tone=.57+.60*height-.30*scar
   tint=np.stack([1+.05*stain,1-.10*stain,1-.23*stain],-1)
  elif family=='hide':
   # Uneven scales and pores rather than smooth sinusoidal paint bands.
   sx=u*35+np.sin(v*math_tau*18)*.12;sy=v*30
   scale=np.abs(np.sin(sx*math_tau)*np.sin(sy*math_tau))
   pore=np.clip((.30-fine)*3,0,1)
   height+=.09*scale-.12*pore
   tone=.50+.74*height-.15*pore
   tint=np.stack([1-.13*stain,1+.03*stain,1+.07*stain],-1)
  else:
   # Warped intersecting fault boundaries, cavity stains and chipped light edges.
   a=np.abs(np.sin((u*7+v*3+(mid-.5)*.70)*math_tau))
   b=np.abs(np.sin((v*9-u*2+(macro-.5)*.95)*math_tau))
   fault=np.minimum(a,b);crack=np.exp(-fault*35)
   rim=np.exp(-np.abs(fault-.075)*42)
   height-=.35*crack;height+=.07*rim
   tone=.40+.83*height-.34*crack+.12*rim
   tint=np.stack([1-.08*stain,1-.02*stain,1+.05*stain],-1)
   if family=='ice':tone=.57+.64*height-.15*crack+.15*rim
  albedo=np.clip(np.array(color)[None,None,:]*tone[:,:,None]*tint,0,1)
  # Limit palette steps while retaining designed light/dark information.
  albedo=np.round(albedo*48)/48
  dx=(np.roll(height,-1,1)-np.roll(height,1,1))*1.8
  dy=(np.roll(height,-1,0)-np.roll(height,1,0))*1.8
  normal=np.stack([-dx,dy,np.ones_like(dx)],-1);normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
  roughmap=np.clip(rough+(mid-.5)*.17,0,1)
  packed=np.zeros((n,n,4),np.float32);packed[:,:,3]=1-roughmap
  maps={'albedo':albedo,'normal':normal*.5+.5,'roughness':roughmap,'metallic-smoothness':packed}
  if emit:
   mask=np.clip((macro-.30)*1.65,0,1)*(1-.35*mid)
   maps['emission']=np.array(emit)[None,None,:]*mask[:,:,None]
  for kind,values in maps.items():
   Image.fromarray(np.rint(values*255).astype('uint8')).resize((2048,2048),Image.Resampling.NEAREST).save(OUT/(key+'-'+kind+'.png'))
  print('AUTHORED detailed material '+key,flush=True)

math_tau=6.283185307179586
if __name__=='__main__':main()
