# Install in r2modman

This development build is distributed as `Local-Magenheim-0.0.1.zip`. `Local` is an unpublished local package namespace, not a claimed Thunderstore team.

1. Select the Valheim profile in r2modman.
2. Open **Settings**, find **Import local mod**, and select the ZIP.
3. Confirm the name Magenheim, author Local and version 0.0.1 in the local-import dialog.
4. Verify Magenheim appears in **Installed**, enabled once.
5. Start modded and check `BepInEx/LogOutput.log` for `Loading [Magenheim 0.0.1]` and `foundation ready`.

Import the ZIP itself, not the repository or `dist` directory. Tests and game libraries must not be installed as plugins. The package includes both the definition and localization files beside the DLL.

A bare DLL copied into BepInEx can load without becoming a managed entry. `manifest.json`, README and icon supply package metadata; the local-import operation records the Installed entry. Public Online search requires publishing to Thunderstore under a real team. This package has not been published.

Build a fresh package with `./package.ps1`. Use `-Author YourTeam` when a real publishing namespace is chosen. The website field is deliberately empty until a real project URL exists.

Package format references: [Thunderstore package requirements](https://wiki.thunderstore.io/mods/creating-a-package) and [r2modman package layout](https://github.com/ebkr/r2modmanPlus/wiki/Structuring-your-Thunderstore-package).
