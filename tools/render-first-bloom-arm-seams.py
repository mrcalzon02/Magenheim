from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=(ROOT/'tools/inspect-first-bloom-arm-joins.py').read_text()
source=source.replace("[('before','first-bloom-bundles-r21'),('untrimmed','first-bloom-bundles-r21-raw')]", "[('after','first-bloom-arm-seams-r22')]")
exec(compile(source,str(__file__),'exec'))
