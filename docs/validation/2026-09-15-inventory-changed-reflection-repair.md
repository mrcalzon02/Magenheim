# Inventory.Changed reflection repair and reflection-binding gate — 2026-09-15

Target version: 0.0.49. Static validation only. No runtime or world acceptance is claimed.

## Defect

Valheim 1.0.12 changed the private `Inventory` notification method from `Changed()` to
`Changed(bool success = false, bool cheatedStateChanged = false)`.

Three Magenheim call sites bound it by bare name and invoked it with no arguments:

| Call site | Path |
| --- | --- |
| `WorkshopInventoryTransactions.RollBack` | `src/Magenheim.Runtime/WorkshopOperations.cs` |
| `SocketWorkstationOverlay.NotifyInventoryChanged` | `src/Magenheim.Runtime/SocketWorkstationOverlay.cs` |
| `SocketOperationRpc.NotifyInventoryChanged` | `src/Magenheim.Runtime/SocketOperationRpc.cs` |

Each used `AccessTools.Method(typeof(Inventory), "Changed")` followed by
`Invoke(inventory, Array.Empty<object>())`. `MethodInfo.Invoke` does not apply a
parameter's declared default value, so a zero-length argument array against a
two-parameter method raises `TargetParameterCountException`.

The binding is resolved reflectively, so neither the C# compiler nor
`tools/verify-patch-targets.ps1` (which only inspects `[HarmonyPatch]` declarations)
reported it. The 0.0.48 package built and passed every existing gate while carrying the
defect.

Expected runtime impact, by severity:

1. `WorkshopInventoryTransactions.RollBack` is called from the `catch` block that recovers
   a failed workshop transaction. An exception thrown inside that handler escapes the
   `catch` entirely, so a failed refinement or geode-opening transaction would surface an
   unhandled exception after the rollback had only partially completed.
2. Socket install/extraction wrote `magenheim.sockets.v1` metadata and then threw during
   the inventory notification, leaving the UI unrefreshed after a committed mutation.
3. The same failure applies to the remote-client socket path.

## Repair

`RuntimeGameApi` is the project's declared single boundary for private game access. The
binding moved there, pinned to the installed signature:

```csharp
private static readonly MethodInfo InventoryChanged = AccessTools.Method(
    typeof(Inventory), "Changed", new[] { typeof(bool), typeof(bool) })
    ?? throw new MissingMethodException(typeof(Inventory).FullName, "Changed(bool, bool)");
```

`RuntimeGameApi.NotifyInventoryChanged(Inventory?)` passes `false, false`. IL inspection of
`Inventory::Changed` confirms both parameters feed only the cheated-item achievement popup
(`$achievements_picked_up_cheated_item` when `success && cheatedStateChanged`, otherwise the
`m_cheatedPopup` drop check). Ordinary Magenheim mutations must make no cheated-item claim,
and the values match both the declared defaults and the game's own neutral call sites
(`Inventory.MoveAll`, `Inventory.StackAll`). All three call sites now use this boundary, so
the duplicate ad-hoc lookups are gone.

## New gate

`tools/verify-reflection-targets.ps1` reads the compiled `Magenheim.dll` IL, recovers every
reflection binding whose target type and member name are literals, and resolves each against
the installed game assemblies. A method bound without an explicit parameter-type array must
resolve to a single parameterless method, so arity drift fails the build. It is wired into
`build.ps1` immediately after `verify-patch-targets.ps1`.

Bindings whose type or member name is computed at runtime (optional foreign-mod APIs such as
Jewelcrafting, and the field-name helper wrappers) cannot be recovered statically. The tool
reports that count rather than implying full coverage.

## Observed evidence

Positive — repaired build:

```
Verified 7 literal reflection bindings against installed assemblies
(9 resolved dynamically and not statically checkable).
exit 0
```

Negative — same gate run against the pre-repair `dist/Local-Magenheim-0.0.48/Magenheim/Magenheim.dll`:

```
SocketWorkstationOverlay::NotifyInventoryChanged -> Inventory.Changed now takes parameters
  (Boolean success, Boolean cheatedStateChanged). ...
WorkshopInventoryTransactions::RollBack -> Inventory.Changed now takes parameters ...
SocketOperationRpc::NotifyInventoryChanged -> Inventory.Changed now takes parameters ...
Reflection binding verification failed for 3 call site(s).
exit 1
```

The gate therefore fails on the exact defect it was written for and passes once repaired.

Supporting checks at the same revision:

- core harness: 36,868 deterministic assertions passed, re-run after rebasing onto the
  incoming Underworld authority composition commits (36,859 before that merge);
- Release runtime build against the installed game assemblies and Jotunn 2.30.0: zero warnings, zero errors;
- `verify-patch-targets.ps1`: 20 Harmony patch targets and named argument bindings verified;
- `LauncherMetadata.Tests.ps1`: passed.

A full sweep of the other reflected game members used through helper wrappers
(`Aoe`, `Projectile`, `Destructible`, `ZNetView`, `SE_Stats`, `InventoryGui`,
`ImageConversion`) resolved against the installed assemblies at this revision; no other
missing member was found.

## Installer checksum verification defect, found during delivery

The first `closeout.ps1 -Offline` run of 0.0.49 failed its package integrity check naming
every file in the package at once. The payload was correct; the verifier was not.

`install-local.ps1` parsed the checksum manifest as:

```powershell
$entries = @(Get-Content -LiteralPath $checksumPath -Raw | ConvertFrom-Json)
```

Windows PowerShell writes a top-level JSON array to the pipeline as a single item, so the
surrounding `@(...)` produced a one-element array containing the whole array. `$entries.Count`
was 1 instead of 55, `$entry.path` member-enumerated to every path at once, and the single
comparison failed. Observed directly:

```
entries.Count = 1
entries[0] type = System.Object[]
entries[0].path type = System.Object[]
```

Binding the parse result before wrapping it yields `entries.Count = 55`, and all 55 recorded
hashes match their files. The repair keeps the verification semantics unchanged and adds an
explicit empty-manifest rejection so a truncated manifest cannot verify nothing.

This defect gated delivery, not gameplay, and would recur on any host whose PowerShell has
these pipeline semantics.

## Delivered

`closeout.ps1 -Offline` completed against the active Central Fuckery profile after the
repair. Independent readback of the destination:

- installed `Magenheim.dll` FileVersion `0.0.49.0`;
- installed SHA-256 `1566007F39B809DBED07E3DDD7C6664E98F2191C8CE8CEB671A06A53D5CCDA2B`,
  identical to the built artifact;
- launcher catalog entry `Magenheim v0.0.49 by Local (enabled)` carrying the 0.0.49
  description;
- payload backup `backups/Local-Magenheim-20260915-112600.zip`;
- catalog backup `backups/mods-20260915-112601-497.yml`.

Installation is verified. Startup, world and gameplay acceptance are not: the game has not
been launched against this build.

## Not claimed

No disposable-world, startup, multiplayer, persistence or visual acceptance is claimed for
0.0.49. The repair is verified statically and by IL evidence only. Confirming that workshop
rollback and socket install/extraction now complete without exception requires the live
matrix in `TESTING.md`.
