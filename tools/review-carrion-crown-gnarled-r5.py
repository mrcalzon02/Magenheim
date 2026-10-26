from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
s=(ROOT/'tools/review-carrion-crown-anatomy-r3.py').read_text().replace('carrion-crown-anatomy-r3','carrion-crown-gnarled-r5').replace('2026-10-16','2026-10-25')
exec(compile(s,str(__file__),'exec'))
