#!/usr/bin/env python3
"""Tie committed generated assets to the generators that own them.

Three separate defects on 2026-09-18 were the same shape: a generator or its gate evolved while
the committed output did not, and nothing noticed until a full build finally ran.

  * the Sporeling texture set was four generator commits and five gate commits stale, so the
    fidelity gate had been tuned five times against output the generator no longer produced;
  * the Sporeling source blend was two author commits stale, because the author had been raising
    AttributeError on Blender 5.0 since the day the animation actions were added;
  * the review renderer had never produced a plate at all, for the same class of reason.

`verify-model-assets.py` already compares model payloads against their sources. Generated textures,
icons and source blends had no equivalent, so this gate supplies one. It records, per generator: the
SHA-256 of the generator itself, and the SHA-256 of every file it owns.

  * the generator's hash changing means the committed outputs may no longer be what it produces;
  * an output's hash changing on its own means a generated file was hand-edited, which is the
    "patch the generated output and leave the generator defective" failure the execution protocol
    prohibits;
  * the output set changing means a file was added or removed outside the generator.

Verification is pure hashing: no Blender, no network, no regeneration, so it is cheap enough to run
early in every build. Refreshing the manifest is not: `--update` *runs the recorded command* before
it records anything, so the manifest cannot be brought back into agreement without actually
regenerating. That is deliberate. A manifest you can refresh without regenerating would record
staleness rather than reject it.

An entry's `hash` selects what "unchanged" means for its outputs:

  * `bytes` (default) for a generator that writes byte-reproducible files. The Sporeling texture
    set is one: two consecutive runs produced seventeen byte-identical maps.
  * `image-pixels` where the encoder varies but the image does not.
  * `generator-only` where the generator is not reproducible at all. Only the output *names* are
    recorded, so files added or removed outside the generator are still caught, but content is not.

Blender is the reason `generator-only` exists. Re-rendering the 32 staff icons produced 20 files
differing from the committed ones -- but 16 of those differed by one LSB on a handful of pixels,
which is render jitter, and only 4 had really changed (about 10% of their pixels, with RGB deltas
above 220). An exact content hash would therefore report spurious drift on half the family after
every render, which is worse than not checking: a gate that cries wolf gets refreshed blindly, and
a blindly refreshed manifest records staleness instead of rejecting it. `.blend` output is not
reproducible either and has no pixel equivalent.

An entry may also list `inputs`: files the generator imports, such as a shared Blender authoring
module. Their combined hash is recorded as `inputs_sha256`, so editing a shared module marks every
generator built on it stale, exactly as editing the generator itself would.

What `generator-only` still gives is the guard that matters: the generator hash is exact, so an
author or renderer that moves on without its outputs is caught -- which is the defect that actually
occurred three times on 2026-09-18. What it gives up is detecting a hand-edited output, and that is
stated here rather than papered over.

Usage:
    python tools/verify-generated-freshness.py            # gate
    python tools/verify-generated-freshness.py --update   # regenerate, then re-record
    python tools/verify-generated-freshness.py --update <id> [<id> ...]
"""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / 'assets/generated.manifest.json'


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open('rb') as handle:
        for block in iter(lambda: handle.read(1 << 20), b''):
            digest.update(block)
    return digest.hexdigest()


def image_pixels_sha256(path: Path) -> str:
    """Hash what the image shows, not how the encoder wrote it.

    For a generator whose pixels are stable but whose encoder is not, this separates real drift
    from file-format noise. It is not enough for a renderer whose pixels themselves jitter -- see
    `generator-only` in the module docstring.
    """
    from PIL import Image  # imported lazily so a bytes-only manifest needs no Pillow

    with Image.open(path) as image:
        rgba = image.convert('RGBA')
        digest = hashlib.sha256()
        digest.update(f'{rgba.width}x{rgba.height}\n'.encode('ascii'))
        digest.update(rgba.tobytes())
        return digest.hexdigest()


HASHERS = {'bytes': sha256, 'image-pixels': image_pixels_sha256, 'generator-only': None}


def records_content(entry: dict) -> bool:
    return entry.get('hash', 'bytes') != 'generator-only'


def output_hash(entry: dict, path: Path) -> 'str | None':
    mode = entry.get('hash', 'bytes')
    if mode not in HASHERS:
        raise SystemExit(f"{entry['id']}: unknown hash mode {mode!r}; expected one of {', '.join(sorted(HASHERS))}")
    hasher = HASHERS[mode]
    return hasher(path) if hasher else None


def repo_path(value: str) -> Path:
    return ROOT / value


def declared_outputs(entry: dict) -> list[str]:
    """Resolve an entry's output set from disk, sorted, as repo-relative forward-slash paths."""
    matches: set[Path] = set()
    for pattern in entry['outputs']:
        matches.update(ROOT.glob(pattern))
    return sorted(p.relative_to(ROOT).as_posix() for p in matches if p.is_file())


def regenerate(entry: dict) -> None:
    command = entry['command']
    print(f"  running {' '.join(command)}", flush=True)
    result = subprocess.run(command, cwd=ROOT)
    if result.returncode != 0:
        raise SystemExit(
            f"{entry['id']}: regeneration failed with exit {result.returncode}; "
            'the manifest is deliberately not updated from a failed generator run'
        )


def inputs_hash(entry: dict) -> 'str | None':
    """One hash over every declared input, in declared order; None when the entry has none."""
    names = entry.get('inputs') or []
    if not names:
        return None
    digest = hashlib.sha256()
    for name in names:
        path = repo_path(name)
        if not path.is_file():
            raise SystemExit(f"{entry['id']}: declared input {name} is missing")
        digest.update(name.encode('utf-8') + b'|' + sha256(path).encode('ascii') + b'|')
    return digest.hexdigest()


def main() -> int:
    manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
    entries = manifest['generators']
    arguments = sys.argv[1:]
    update = '--update' in arguments
    selected = [a for a in arguments if a != '--update']
    if selected:
        known = {e['id'] for e in entries}
        unknown = sorted(set(selected) - known)
        if unknown:
            raise SystemExit(f"Unknown generator id(s): {', '.join(unknown)}. Known: {', '.join(sorted(known))}")
        entries = [e for e in entries if e['id'] in selected]
    elif selected and not update:
        raise SystemExit('Selecting ids is only meaningful with --update.')

    failures: list[str] = []
    checked = 0
    for entry in entries:
        generator = repo_path(entry['generator'])
        if not generator.is_file():
            failures.append(f"{entry['id']}: generator {entry['generator']} is missing")
            continue
        if update:
            print(f"{entry['id']}:", flush=True)
            regenerate(entry)
            entry['generator_sha256'] = sha256(generator)
            if entry.get('inputs'):
                entry['inputs_sha256'] = inputs_hash(entry)
            entry['output_sha256'] = {name: output_hash(entry, repo_path(name)) for name in declared_outputs(entry)}
            if not entry['output_sha256']:
                raise SystemExit(f"{entry['id']}: produced no files matching {entry['outputs']}")
            print(f"  recorded {len(entry['output_sha256'])} outputs", flush=True)
            continue

        current = sha256(generator)
        if current != entry['generator_sha256']:
            failures.append(
                f"{entry['id']}: {entry['generator']} has changed since its outputs were generated. "
                f"Run: python tools/verify-generated-freshness.py --update {entry['id']}"
            )
            continue

        if inputs_hash(entry) != entry.get('inputs_sha256'):
            failures.append(
                f"{entry['id']}: an input of {entry['generator']} ({', '.join(entry['inputs'])}) has changed since its "
                f"outputs were generated. Run: python tools/verify-generated-freshness.py --update {entry['id']}"
            )
            continue

        recorded = entry['output_sha256']
        present = declared_outputs(entry)
        added = sorted(set(present) - set(recorded))
        removed = sorted(set(recorded) - set(present))
        for name in added:
            failures.append(f"{entry['id']}: {name} is not produced by {entry['generator']}")
        for name in removed:
            failures.append(f"{entry['id']}: {name} is recorded but missing from disk")
        if records_content(entry):
            for name in sorted(set(present) & set(recorded)):
                if output_hash(entry, repo_path(name)) != recorded[name]:
                    failures.append(
                        f"{entry['id']}: {name} was modified without its generator. Repair "
                        f"{entry['generator']} and regenerate rather than editing generated output."
                    )
        checked += len(recorded)

    if update:
        MANIFEST.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
        print(f'UPDATED {MANIFEST.relative_to(ROOT).as_posix()} for {len(entries)} generator(s)')
        return 0

    if failures:
        print('FAIL: generated asset freshness', file=sys.stderr)
        for failure in failures:
            print('  - ' + failure, file=sys.stderr)
        return 1

    print(
        f'PASS: {checked} generated files across {len(entries)} generators match the generators that own them.'
    )
    return 0


if __name__ == '__main__':
    sys.exit(main())
