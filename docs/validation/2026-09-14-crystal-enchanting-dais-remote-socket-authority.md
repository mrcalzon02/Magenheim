# Crystal Enchanting Dais remote socket authority — static validation

Date: 2026-09-14

## Scope

This record covers the source repair that admits the dedicated `Magenheim_CrystalEnchantingDais` to the existing socket-management authority path for remote clients without broadening crystal extraction.

The Dais already exposed the local socket-management overlay through `CrystalEnchantingDaisSocketPatch`, but `SocketOperationRpc` still validated only the Geologist's Workstation on both the remote client submission gate and the server proximity gate. That divergence meant a non-host client could use the Dais UI but could not successfully submit open-slot or install-crystal operations through the server-authoritative RPC path.

## Repair

`CrystalEnchantingDaisSocketPatch` now owns all Dais-specific station admission at the existing integration boundary:

- local `SocketWorkstationOverlay.TryGetMagenheimStation` admits the Dais;
- remote-client `SocketOperationRpc.TryGetCurrentMagenheimStation` admits the Dais;
- server `SocketOperationRpc.TryResolveNearbyMagenheimStation` can resolve an in-range Dais by its display name and then verifies the exact Magenheim prefab identity plus normal station use distance before admission.

The original Geologist's Workstation path is not replaced or weakened. Each postfix exits immediately when the original authoritative check already succeeded.

## Extraction boundary preserved

The repair does not make the Dais a Faceting Wheel host. Remote extraction still passes through the existing server-side `HasFacetingWheel(station)` requirement. A Dais has no Faceting Wheel extension because that extension belongs to the Geologist's Workstation, so remote extraction remains rejected there while socket opening and crystal installation can use the dedicated enchanting station.

This keeps the intended division of labor:

- Crystal Enchanting Dais: open equipment sockets and install shaped crystals;
- Geologist's Workstation + Faceting Wheel: risky crystal extraction and mineral-processing progression.

## Static verification performed

Read-back on authoritative `main` confirms the Dais integration patch contains Harmony postfixes for the local overlay gate, remote-client gate, and remote-server proximity gate. The server Dais fallback additionally verifies exact prefab identity and `InUseDistance` before setting the RPC station result successful.

The current source/package identity is 0.0.43. `MagenheimPlugin.PluginVersion` and `Magenheim.Runtime.csproj` both read 0.0.43 after reconciliation.

## Runtime admission boundary

No Valheim/Jötunn runtime, host/client session, or local managed-assembly build was available in this connector-only cycle. Therefore this is source/static admission only.

Required runtime acceptance remains:

1. build/install 0.0.43 in the normal Valheim development profile;
2. join as a non-host client and open the Dais UI;
3. successfully open a socket through the Dais and confirm server acknowledgement/exact-item mutation;
4. successfully install a Simple-or-better Magenheim crystal through the Dais;
5. attempt extraction at the Dais and confirm it is refused without item mutation;
6. repeat extraction at the Geologist's Workstation with a Faceting Wheel and confirm the existing extraction path remains functional;
7. verify stale response, duplicate/replay, authority mismatch, and descriptor-spoof rejection remain unchanged.

## Next dependency-valid step

Run the 0.0.43 compile/deterministic suite and the host/remote-client Dais socket matrix above. Any compile or live station-resolution failure is repaired at the Dais/socket authority integration boundary before expanding enchanting functionality.
