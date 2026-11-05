# Security Policy

Thank you for helping keep Tensho Assistant secure.

## Supported versions

Only the latest published release (the version in `repo.json`, tag `Stable-x.y.z`) receives security fixes; a fix ships as a new
version. Builds of the `Devloppement` branch and local builds are not supported. The scope below describes the `Devloppement` branch at
the version named in its first line, which may be newer than the latest release.

## Reporting a Vulnerability

If you identify a security vulnerability in this project, please report it privately and responsibly.

Please use GitHub's private security reporting flow for this repository:

https://github.com/Shigu330/Tensho-Assistant/security/advisories/new

Please do not open a public issue or disclose sensitive details about the vulnerability before a fix has been published.

## Scope

What the plugin does that matters for security (checked against the code on 2026-11-04, version 0.512 of the `Devloppement` branch):

- **Network**: HTTPS requests with a timeout and a size limit (`src/TenshoAssistant/Community/BoundedHttp.cs`, including Teamcraft's
  POST since 0.321; or the client's own limit for Universalis) to Universalis, Garland Tools, the FFXIV Console Games Wiki, Teamcraft's public reports
  (`gubal.ffxivteamcraft.com`, one GraphQL POST query carrying the fishing spot id) and GitHub-hosted data (`raw.githubusercontent.com`,
  `api.github.com`: LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet), PaissaDB (open plots, on a click or with its optional watch), and this repository (community tool gallery and help videos, on a click, data only, never code); all others are GET, except the Discord webhook below.
  Requests carry item, world, spot or page identifiers only; no player data, no telemetry. LuminaSupplemental, GatherBuddy,
  Teamcraft, the Fashion Report data (FFXIVGachaSpreadsheet), Universalis, Garland Tools and the wiki can each be turned off
  (Settings › Advanced); since 0.324 a switch stops every request to its source (0.326: requests already queued too). Copies
  already kept stay visible for Universalis (Market page), Garland duty and monster loot, wiki monsters and the sightseeing log,
  and a recent Fashion Report copy; Garland item sources, wiki item acquisition, Teamcraft, LuminaSupplemental and GatherBuddy
  data are hidden while their switch is off.
- **Discord webhook (R-12, 0.319)**: off by default. When the player turns it on and enters a webhook address (only Discord's own
  hosts are accepted: `discord.com`, `ptb.discord.com`, `canary.discord.com`, `discordapp.com`), a ringing Tensho alarm is posted
  there (HTTPS POST): the alarm's message and local time (since 0.344 also currency cap alerts, in their own category), the character's
  name only when the player enables it; mentions are limited
  to the role the player sets (`allowed_mentions`). The address is a secret: stored in the plugin configuration on the player's PC,
  masked in the settings, never written to the logs nor to the diagnostic report (`src/TenshoAssistant/Alarms/DiscordNotifier.cs`,
  `DiscordWebhook.cs`). Clearing it or turning sending off drops the queued messages.
- **Links opened in the browser (0.349)**: an item's page on the FFXIV Console Games Wiki or the Lodestone opens in the player's own
  browser only on a click (right-click menu, item sheet) or a `/tensho wiki|lodestone` command; Tensho itself sends nothing. The same goes for
  the prefilled GitHub form of « report a problem » (0.504), which the player reads and sends, and for help videos (YouTube, or this repository).
- **Local files**: configuration through Dalamud, the disk cache (`DataCache-v1`) and the saved storages (one file per character in
  `InventoryBank/` since 0.345, the former `InventoryBank.json` kept as `.migrated`), the gil journal (one file per character in
  `GilJournal/` since 0.351, 90 days) in the
  plugin's Dalamud configuration folder; translators can put language JSON files beside the DLL, which are validated when reloaded.
- **Game**: game memory is read and copied; the actions that reach the game start on a click: teleport, applying an inventory sort, macro chains,
  writing crafting macros into the game's macros, opening the Try On window, equipping a job, wearing a title, sending the hunt train call, and `/alarm` chat commands for game alarms (when the player adds an alarm with the « game alarms » setting on, off by default, or on « Send »).
  One setting, off by default, acts without a click: wearing the title chosen for a job on a job change. Only the local character's content id is used, as a key for its own saved data.
- **Support report** (Settings › Advanced): copied to the clipboard on click; version, language, shown page, data states, errors,
  source settings, the market world name, alarm counts and the context item id.

Reports about these surfaces are welcome; issues in the game, Dalamud or the community services themselves belong to their owners.

## Information to Include in Your Report

- Description of the vulnerability
- Steps to reproduce it
- Potential impact and affected component
- Affected version or build
- Screenshots, logs, or relevant excerpts
- Suggested remediation, if available

## What You Can Expect

- Acknowledgement as quickly as possible
- Assessment of the issue and severity
- Collaboration to validate and remediate the issue
- Updates on the remediation progress
- Public or private credit, if appropriate and agreed upon

## Prohibited Actions

- Do not publicly disclose the vulnerability before a fix is available
- Do not exploit a vulnerability in production or on third-party systems
- Do not collect or request sensitive data from third parties
- Do not perform intrusive or unauthorized security testing on systems you do not own or have explicit permission to test

## Response Expectations

Critical issues will be prioritized based on impact and exploitability. We will do our best to investigate promptly and release a fix as soon as it is safe and feasible.

## Thanks

Thank you for helping keep Tensho Assistant safe for all users.
