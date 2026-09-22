"""Fail packaging if any tracked text file contains unresolved merge/stash markers."""
import re, subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1]
files=set(subprocess.check_output(['git','ls-files','-z'],cwd=root).decode().split('\0'))
marker=re.compile(rb'^(?:<{7}|>{7})(?: .*)?\r?$|^\|{7} .*\r?$',re.M)
failures=[]
for name in sorted(files):
 p=root/name
 if not name or not p.is_file():continue
 data=p.read_bytes()
 if b'\0' in data:continue
 for match in marker.finditer(data):failures.append(f'{name}:{data[:match.start()].count(bytes([10]))+1}')
if failures:raise SystemExit('Unresolved conflict markers: '+', '.join(failures))
print('PASS: tracked text is free of merge/stash conflict markers.')
