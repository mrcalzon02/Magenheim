"""Check delivered comparison links, render coverage and artifact provenance."""
from pathlib import Path
from html.parser import HTMLParser
import hashlib
import json

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/art/creature-models/2026-10-04/detail-r2'

class Links(HTMLParser):
    def handle_starttag(self, tag, attrs):
        for key, value in attrs:
            if key in ('src', 'href') and value and not value.startswith(('http:', 'https:', '#')):
                assert (OUT / value).is_file(), value

Links().feed((OUT / 'gallery.html').read_text(encoding='utf-8'))
rows = json.loads((OUT / 'boss-construction-manifest.json').read_text())
assert len(rows) == 6
for row in rows:
    for view in ('three-quarter', 'front', 'side', 'detail', 'clay', 'attack'):
        assert (OUT / (row['id'] + '--' + view + '.png')).is_file()
    assert (OUT / (row['id'] + '.glb')).is_file()
for donor in ('Troll', 'Serpent', 'StoneGolem', 'Greydwarf_Shaman'):
    for view in ('three-quarter', 'front', 'side', 'detail', 'clay'):
        assert (OUT / ('donor-' + donor + '--' + view + '.png')).is_file()
evidence = json.loads((OUT / 'provenance.json').read_text())
for path, expected in evidence.items():
    assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == expected, path
print(f'PASS gallery links, six GLBs, 56 comparison renders and {len(evidence)} artifact hashes')
