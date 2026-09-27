#!/usr/bin/env python3
"""Verify Frost progression and the unresolved Rime Torrent cadence contract.

This is intentionally source-level: it protects the authored Frost progression while the
Valheim/Jotunn live-runtime gate remains external to repository-only validation.
"""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
FROST = ROOT / "src" / "Magenheim.Runtime" / "FrostStaffRegistrar.cs"
BURST = ROOT / "src" / "Magenheim.Runtime" / "StaffBurstContract.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


frost = FROST.read_text(encoding="utf-8")
burst = BURST.read_text(encoding="utf-8")

expected_order = [
    "Magenheim_Staff_Frost_Simple",
    "Magenheim_Staff_Frost_Crystal",
    "Magenheim_Staff_Frost_Advanced",
    "Magenheim_Staff_Frost_Master",
]
positions = [frost.find(identity) for identity in expected_order]
if any(position < 0 for position in positions):
    fail("canonical Frost staff family is incomplete")
if positions != sorted(positions) or len(set(positions)) != len(positions):
    fail("Frost staff progression is not Simple -> Crystal -> Advanced -> Master")

master = re.search(
    r'"Magenheim_Staff_Frost_Master".*?'
    r'40f,\s*7f,\s*(\d+),\s*(\d+),\s*([.\d]+)f,\s*PayloadKind\.Torrent',
    frost,
    re.DOTALL,
)
if not master:
    fail("could not resolve the Master Frost/Rime Torrent authored attack contract")

projectiles, bursts, interval = master.groups()
if (projectiles, bursts, interval) != ("2", "6", ".09"):
    fail(
        "Rime Torrent drifted from its authored 2 projectiles x 6 pulses @ 0.09s contract: "
        f"found {projectiles} x {bursts} @ {interval}s"
    )

if "StaffBurstContract.Apply(attack, definition.PrefabName, _log);" not in frost:
    fail("Frost registrar no longer passes authored burst attacks through StaffBurstContract")

required_fallback = [
    "attack.m_projectiles = total;",
    "attack.m_projectileBursts = 1;",
    "attack.m_burstInterval = 0f;",
]
for marker in required_fallback:
    if marker not in burst:
        fail("multi-burst compatibility fallback changed before a safe terminating cadence replaced it")

for forbidden in ("UnderworldSpawnPlayerListPatch", "UnderworldSpawnPlayerRangePatch"):
    if forbidden in frost or forbidden in burst:
        fail("Frost progression code must not acquire Underworld player-population tracking")

print("PASS: Frost progression is canonical; Rime Torrent remains authored as 2x6 @ 0.09s.")
print("PASS: current single-volley compatibility fallback remains explicit pending safe cadence lifecycle.")
