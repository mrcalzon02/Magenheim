from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-rift-titan-silhouette-r7.py').read_text(encoding='utf-8').replace('rift-titan-silhouette-r7','rift-titan-crystal-limbs-r9')
exec(compile(source,str(__file__),'exec'))
