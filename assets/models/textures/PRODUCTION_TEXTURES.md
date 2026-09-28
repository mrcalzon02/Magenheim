# Production material fallback textures

The generic material artwork has two deliberate layers.

## Runtime fallback layer

`assets/models/textures/surface-<family>-authored.png` is the packaged Valheim-scale fallback set.
These nine maps are **256x256**, neutral/tint-compatible derivatives of the approved high-resolution
artwork. This matches the established shared world/equipment floor and avoids shipping nine 1024px
fallbacks for materials whose Base Color already supplies the model's hue.

They are a rendering floor, not a substitute for model-specific art. Existing authored textures win,
and material slots graduate away from the shared family maps as model-specific passes are completed.

## Source/reference layer

The accepted 1024x1024 image-generation artwork is retained unmodified under
`assets/models/texture-source/`. That directory is repository source/reference art and is not
included by the Runtime csproj's non-recursive `assets/models/textures/*.png` package rule.

| Family | Generation id | Runtime treatment |
| --- | --- | --- |
| stone | 7c5dd25c-385b-467c-a1fb-ef48f2c8e613 | 256px neutral derivative |
| timber | 03dea17d-9c17-49e7-9d1d-5dd2294b7335 | 256px neutral derivative |
| metal | cce69173-aed4-42a6-9bf7-39bf0f18b3b0 | 256px neutral derivative |
| cloth | fb5e9ea9-f4ff-4fc8-83a3-775219274d79 | 256px neutral derivative |
| bone | 5fdc44b7-b8c9-4579-aa46-6492d508efe9 | 256px neutral derivative |
| liquid | bab1f2e1-c201-47c0-b4b4-2da12ff3150e | 256px neutral derivative |
| leather | b20c056a-f882-4cbb-b64b-56c6f2a6827c | 256px neutral derivative |
| crystal | 7453ca5d-476a-4231-9618-1ee484b80bb9 | 256px neutral derivative |
| generic | 0d75e308-011d-4802-9a97-d33952a72659 | 256px neutral derivative |

Close-held or hero assets may use 512px or larger authored maps where their own asset standard calls
for it. The shared fallback itself is not justification for raising every placeable to that size.
