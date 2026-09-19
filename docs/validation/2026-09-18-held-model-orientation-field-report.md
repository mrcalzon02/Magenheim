# Held-model orientation — field report, 0.0.77 (2026-09-18)

Observed in game by the user on 0.0.77, with the attach-space frame diagnostics added in 0.0.76.
Not yet fixed. This file exists so the next session starts from measurement rather than from a
fresh guess.

## Reported

| Weapon | State on 0.0.77 |
|---|---|
| Battleaxe | **Wrong — new failure.** Held "like a guitar": haft across the body. On 0.0.73 it was upside down; the 0.0.75 `Reach()` change traded one wrong orientation for another. |
| Spear | **Wrong — "still slightly wrong."** Gripped too near the head; haft angles back and down. |
| Crossbow | Not reported as wrong this run. Prod reads as across the body in the screenshot. Treat as unconfirmed, not fixed. |
| Axe, greatsword, bow, knife | Not reported. |

## Measured

Every line below is from `LogOutput.log`, Magenheim 0.0.77. `s=` is attach-space bounds size.

```
axe         (90,0,0)     donor s=0.449,0.105,0.977   ours s=0.501,0.119,1.174
greatsword  (90,0,0)     donor s=0.245,0.052,1.911   ours s=0.540,0.234,2.076
bow         (90,0,0)     donor s=0.852,0.480,1.728   ours s=0.293,0.103,1.845
knife       (90,0,0)     donor s=0.133,0.026,0.543   ours s=0.190,0.133,0.930
atgeir      (90,0,0)     donor s=1.230,0.453,2.803   ours s=0.588,0.156,2.260
sword       (90,180,0)   donor s=0.195,0.059,1.288   ours s=0.315,0.136,1.561
battleaxe   (270,0,0)    donor s=0.401,0.128,1.611   ours s=0.898,0.144,1.556
spear       (270,180,0)  donor s=0.286,0.147,2.432   ours s=0.154,0.136,1.983
mace        (0,90,90)    donor s=0.436,0.147,0.944   ours s=0.421,0.410,1.303
crossbow    (270,180,0)  donor s=1.272,0.268,1.725   ours s=1.322,0.174,0.927
```

## What the numbers say

**1. The donor frame is shared, and the right answer is (90,0,0).** Five weapons resolve to exactly
(90,0,0) and none of them has been reported wrong. Every weapon reported wrong resolved to something
else. The per-model measurement is not discovering a per-model truth; it is producing noise around a
constant.

**2. Two of the three failures are near-ties in the secondary axes.** Rank ordering is decided by a
comparison that is effectively a coin flip when two extents are close:

- mace: ours X=0.421 vs Y=0.410 — a 2.6% difference decides the mapping → (0,90,90)
- spear: ours X=0.154 vs Y=0.136 — 12% → (270,180,0)

Both are the axes *perpendicular* to the weapon, where the true orientation is carried by the donor,
not by which of two similar numbers happens to be larger.

**3. The battleaxe is a sign flip, not a mapping error.** It differs from the axe — same weapon
family, same donor frame, correct at (90,0,0) — only by 180° about X. `Reach()` picks direction from
which end of the axis reaches furthest from the attach origin; on a double-headed axe whose heads
sit either side of the haft that measurement is again near-tied. Centre of mass failed here for the
same reason in 0.0.73.

**4. The crossbow's declared forward axis is wrong.** `ForwardAxisOverride["crystal-weapon-crossbow"] = 1`
declares Y. The attach-space measurement says ours is `1.322, 0.174, 0.927`: X is the prod, Y is the
*thinnest* axis at 0.174, and the stock runs along **Z** at 0.927. The override should be `2`. It was
set from an earlier model-space measurement of 1.32/0.93 read as X/Y, which was simply the wrong pair.

## Direction for the fix

Do not add another per-model rotation on top. The evidence is that bounds ranking is the wrong
instrument for the perpendicular axes: it is stable for the long axis and unstable for the other two,
and the other two are exactly where the failures are.

Worth trying, in order:

1. Correct the crossbow override to `2`. Independent of everything else and well evidenced.
2. Take only the *long* axis from measurement and take the remaining two from the donor's frame
   directly, rather than re-deriving them from our own bounds ranking.
3. Resolve direction from the donor's own near/far extents rather than from ours, so a symmetric
   model like a double-headed axe cannot flip.
4. Use the trim table in `HeldModelAlignment` for anything still off after that. It exists and is
   empty; an entry is a deliberate claim that a model needs hand-authored trim.

Re-read the `Held model` lines after any change — five weapons currently at (90,0,0) are the
regression test, and any of them moving off it means the change is wrong.
