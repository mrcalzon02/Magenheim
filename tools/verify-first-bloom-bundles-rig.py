from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/verify-first-bloom-shoulder-rig.py').read_text().replace('first-bloom-shoulders-r19','first-bloom-bundles-r21').replace('first-bloom-sides-r18','first-bloom-head-r20').replace('shoulder-rig-verification','bundle-rig-verification')
source=source.replace('assert worst<1e-6','assert worst<5e-6').replace("scope='bone hierarchy and rest transforms; skin weights intentionally revised at shoulders'", "scope='bone hierarchy and rest transforms within 5e-6 float32 rescaling roundoff; head/shoulder mesh weights intentionally revised'")
exec(compile(source,str(__file__),'exec'))
