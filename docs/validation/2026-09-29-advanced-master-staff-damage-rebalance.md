# Advanced / Master staff damage rebalance — 2026-09-29

Status: **source implemented and remote-read-back verified on main; live combat acceptance still required.**

## Intent

The last live test reported that Advanced and Master staff attacks were materially underpowered for
their progression cost and visual scale. Raise both tiers' damaging output by 50% without changing
Simple/Crystal tiers, projectile counts, burst cadence, stamina costs, crowd-control strength,
status durations, force/stagger tuning, recipes or durability.

The rebalance is applied to both the staff's primary damage definition and the tier-specific
secondary impact/field damage. Utility-only portions of an ability are deliberately not multiplied.

## Authoritative values

| Family | Advanced primary | Advanced payload | Master primary | Master payload |
|---|---:|---:|---:|---:|
| Fire | 14 -> 21 fire | Scorch 3 -> 4.5 fire | 9 -> 13.5 fire | Meteor burn 4 -> 6 fire |
| Frost | 8 -> 12 frost | Rime 2.5 -> 3.75 frost | 5 -> 7.5 frost | Torrent rime 2 -> 3 frost |
| Storm | 16 -> 24 lightning | Arc discharge 9 -> 13.5 lightning | 10 -> 15 lightning | Thunderhead pulse 4 -> 6 lightning |
| Earth | 10 -> 15 blunt | Seismic ring 10 -> 15 blunt | 20 -> 30 blunt | Worldshaker 20 -> 30 blunt |
| Venom | 10 -> 15 poison | Miasma 3.5 -> 5.25 poison | 8 -> 12 poison | Plague field 3 -> 4.5 poison |
| Radiance | 4/8 -> 6/12 pierce/spirit | Flash 2 -> 3 spirit | 18/38 -> 27/57 pierce/spirit | Sanctuary 6 -> 9 spirit |
| Seidr | 9 -> 13.5 spirit | Witchweave 1.5 -> 2.25 spirit | 8 -> 12 spirit | Fate knot 2 -> 3 spirit |
| Spirit | 14 -> 21 spirit | Chorus echo 2 -> 3 spirit | 16 -> 24 spirit | Reliquary echo 3.5 -> 5.25 spirit |

## Explicitly unchanged

- Simple and Crystal staff damage;
- Advanced/Master stamina costs;
- projectile counts, burst counts and burst intervals;
- projectile velocity and accuracy;
- force and stagger multipliers;
- Frost slow/Brittle strength;
- Venom Corrosion/Deep Corrosion status strength and duration;
- Radiance/Seidr control geometry;
- Spirit outgoing-damage suppression percentages;
- crafting requirements and station levels;
- durability.

This keeps the requested change legible: Advanced and Master attacks hit harder without making their
utility simultaneously 50% stronger.

## Live acceptance

Use the same target, skill state and staff tier before comparing families. Verify that Advanced and
Master attacks produce approximately 1.5x the pre-rebalance damage for equivalent direct hits and
secondary damaging payload ticks while preserving their existing cadence and resource costs.
Multi-projectile attacks should be judged both per projectile and per full cast so their total
damage does not exceed the intended encounter envelope simply because every projectile connects.
