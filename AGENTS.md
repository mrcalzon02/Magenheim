# Visual repairs and equipment production

Read `docs/validation/2026-09-30-0.0.158-integrated-candidate.md`, especially the
October 1 screenshot repairs, before changing authored materials or armor production.

- Independent authored meshes use the native Rock_4 material template through
  ModelAssets.SurfaceMaterialProvider, as confirmed working on the Crystal Dais.
  Gameplay prefab donors supply behavior; never inherit their arbitrary vertex shaders.
  Keep donor auxiliary maps/displacement cleared before applying owned textures/PBR.
- Armor starts from vanilla equipment donors. Retain their meshes, skinning, cloth and
  body-overlay mapping; modify owned materials/textures. Raw generated armor is retired.
  Use tools/rebuild-underworld-armour.ps1 for native palette production.
- Legacy Earth loaders, single-mesh structures and mesh effects use ModelAssets.CreateSurfaceMaterial.
  Particle and line effects use NativeEffectMaterials; never construct a material from a missing
  built-in Standard or Sprites/Default shader.
- Native particle effects require a supported native material and texture; Unity's
  default particle material is unavailable in Valheim and produces magenta squares.
- A requested grip correction applies only to the named weapon's model identity.
  Preserve all other weapon orientations and trims.
- Mycelial Bench uses native workbench placement flags/colliders, without extra biome
  or terrain admission checks. Other stations retain their own siting contracts.
- Record build checks separately from live visual acceptance. Follow the user's current
  installation instructions; the October 1 installation hold was lifted after the comparison.
