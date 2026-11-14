# X-344 — where Dalamud takes an installed plugin's icon (0.1330)

Read on 2026-11-14 on the user's machine (Dalamud 15.0.4.0, plugins installed from their repositories), before the fix.

## What was seen

- Tensho 0.1329 installed from this repository (normal mode): Dalamud's plugin list shows its « ? » placeholder, no icon.
- `repo.json` on main has `IconUrl` (`src/TenshoAssistant/images/icon.png` on main, a PNG of 512 × 512, served with status 200).
- The installed manifest, `%APPDATA%\XIVLauncher\installedPlugins\TenshoAssistant\0.1329.0.0\TenshoAssistant.json`, has `"IconUrl": null`;
  it is the manifest of `latest.zip` (no `IconUrl`) plus the install fields Dalamud adds (`InstalledFromUrl`, `WorkingPluginId`, …).
- `latest.zip` holds no `images/` folder (DalamudPackager writes `images/` beside the zip, for a repository, not inside it).

## Comparison with the other installed plugins (same machine)

Every installed plugin whose list entry shows its own icon from its own host (CharacterPanelRefined, Craftimizer, DistantSeas, HuntHelper,
ItemUse, LazyGatherer, PriceInsight, QuestTracker, TidyChat, Collections) has a non-empty `IconUrl` in its installed manifest, and none has
`images/icon.png` in its installed folder. The official plugins without `IconUrl` get theirs from the official repository's address, which
`Dalamud.dll` builds itself (its strings: `…/PluginDistD17/main/{0}/{1}/images/{2}` and `icon.png`); that does not apply to a third-party plugin.
The same strings `images` and `icon.png` serve the local icon of a dev plugin (`images/icon.png` beside the DLL), which is why the icon showed
while Tensho was loaded as a dev plugin.

## Conclusion

The icon of an installed third-party plugin comes from the `IconUrl` of the manifest inside `latest.zip`. The source manifest
(`src/TenshoAssistant/TenshoAssistant.json`) now carries it; `Verify.ps1` refuses a package without it, `Release.ps1` refuses a `repo.json`
whose icon differs, the CI build checks it, and `PluginIcon1330Tests` covers the sources and the built package.
