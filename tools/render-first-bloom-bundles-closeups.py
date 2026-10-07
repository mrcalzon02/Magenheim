from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/render-first-bloom-head-closeups.py').read_text().replace('first-bloom-head-r20','first-bloom-bundles-r21').replace('first-bloom-shoulders-r19','first-bloom-head-r20')
exec(compile(source,str(__file__),'exec'))
