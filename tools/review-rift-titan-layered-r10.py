from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/review-rift-titan-silhouette-r7.py').read_text(encoding='utf-8').replace('rift-titan-silhouette-r7','rift-titan-layered-r10')
exec(compile(source,str(__file__),'exec'))
