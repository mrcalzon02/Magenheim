# Rootforged stash conflict resolution — 0.0.92

## Cause

The remote merge fast-forwarded main to ab1f7e1. Reapplying local work conflicted with Rootforged
assets already added upstream. Local commits 71c27dd and b4a3699 (both titled Auto stash before
merge) captured unresolved marker text. A subsequent stash application produced 24 unmerged paths:
11 GLBs, 11 runtime model payloads, catalog.json and MagenheimPlugin.cs. HEAD itself contained
nested marker blocks and invalid Rootforged JSON. There was no MERGE_HEAD; these were stash-apply
conflicts after the remote merge had completed, not a pending merge commit.

## Resolution

All working/conflict-stage versions were preserved in
backups/conflict-resolution-20260921-164546.zip. Existing stashes and commits were retained.
The current editable Rootforged Blender sources were exported through the standard exporter;
all eleven GLBs and runtime payloads plus both catalogs were reconciled as one asset set.
The plugin combines origin/main's atmosphere lifecycle with Rootforged registration and disposal.
The existing detailed source art, icons, textures, source tooling and native building behavior
remain present; no branch replacement, reset, stash deletion or history rewrite was performed.

## Prevention and verification

build.ps1 now rejects unresolved conflict markers in tracked text before model work. The gate
was checked against clean source, a stash-conflict fixture and binary data. Runtime compilation
passed without warnings/errors and the eleven Rootforged binding/dimension/material/icon checks
passed. Full closeout installation results are recorded in PROJECT_STATE.md. Native startup,
placement/support and multiplayer remain unverified. Old auto-stashes contain superseded content
and must not be reapplied as if they were new work.
