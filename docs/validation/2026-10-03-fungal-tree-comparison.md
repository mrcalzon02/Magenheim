# Mushroom / installed vanilla tree geometry comparison

Native measurements come from inspect-native-tree-cost.py against the installed game assets.
Counts are individual mesh triangles, not frame-rate measurements.

| Installed tree | Near mesh | Further meshes |
|---|---:|---|
| Beech1 | 432 | Lod1: 114, Lod3: 22 |
| PineTree | 22986 | stub: 23 |
| FirTree | 596 | native billboard / culling |

Pine is an expensive native outlier; Beech and Fir are the density budget references.

| Authored mushroom | Previous triangles | Rebuilt triangles | Reduction |
|---|---:|---:|---:|
| glowcap | 2776 | 576 | 79.3% |
| glowcap-bent | 7442 | 578 | 92.2% |
| glowcap-elder | 9422 | 576 | 93.9% |
| glowcap-twin | 11618 | 576 | 95.0% |
| spirestalk | 4720 | 574 | 87.8% |
| spirestalk-young | 3440 | 574 | 83.3% |
| spirestalk-towered | 7280 | 576 | 92.1% |
| tanglecap | 5008 | 578 | 88.5% |
| puffcap-clustered | 11674 | 578 | 95.0% |

The 24 Fungal Forest payloads retain their owned textures and have native LODGroup distance culling.
They retain multiple material parts and do not yet reproduce Beech’s intermediate/billboard LODs.
Equivalent live forest frame rate is pending measurement; triangle reduction alone does not establish it.
