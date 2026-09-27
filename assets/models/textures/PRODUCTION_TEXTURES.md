# Production material fallback textures

These files are the production replacements for the rejected 256x256 procedural diagnostic set.

Active runtime files live directly in this directory as `surface-<family>-authored.png` and are
1024x1024. They were produced from image-generation artwork on 2026-09-27 and then converted to
power-of-two indexed PNGs for runtime use.

| Family | Generation id | Runtime treatment |
| --- | --- | --- |
| stone | 7c5dd25c-385b-467c-a1fb-ef48f2c8e613 | color preserved |
| timber | 03dea17d-9c17-49e7-9d1d-5dd2294b7335 | color preserved |
| metal | cce69173-aed4-42a6-9bf7-39bf0f18b3b0 | color preserved |
| cloth | fb5e9ea9-f4ff-4fc8-83a3-775219274d79 | color preserved |
| bone | 5fdc44b7-b8c9-4579-aa46-6492d508efe9 | color preserved |
| liquid | bab1f2e1-c201-47c0-b4b4-2da12ff3150e | neutralized to preserve shader/material tint |
| leather | b20c056a-f882-4cbb-b64b-56c6f2a6827c | color preserved |
| crystal | 7453ca5d-476a-4231-9618-1ee484b80bb9 | neutralized to preserve elemental tint |
| generic | 0d75e308-011d-4802-9a97-d33952a72659 | color preserved |

Do not replace these files with mathematical noise, checker patterns, stripe patterns, or other
diagnostic placeholders and label them production artwork. Any future replacement should meet or
exceed the current painterly material readability at both full resolution and gameplay scale.
