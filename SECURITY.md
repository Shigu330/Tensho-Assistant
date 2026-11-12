# Security Policy

> *Traduction française plus bas.*

Thank you for helping keep Tensho Assistant secure.

## Supported versions

Only the latest published release (the version in `repo.json`, tag `Stable-x.y.z`) receives security fixes; a fix ships as a new
version. Builds of the `Devloppement` branch and local builds are not supported. The scope below describes the `Devloppement` branch at
the version named in its first line, which may be newer than the latest release.

## Reporting a vulnerability

If you find a security vulnerability in this project, please report it privately and responsibly, through GitHub's private security
reporting flow for this repository:

https://github.com/Shigu330/Tensho-Assistant/security/advisories/new

Please do not open a public issue or disclose details about the vulnerability before a fix has been published.

## Scope

What the plugin does that matters for security (read against the code of the `Devloppement` branch on 2026-11-09; each later change is added here with the version that brings it):

- **Network**: HTTPS requests with a timeout and a size limit (`src/TenshoAssistant/Community/BoundedHttp.cs`, or the client's own
  limit for Universalis) to the sources below (produced from the code by `tools/docgen.py`: every file that makes a request is in a row, with the setting that turns its source off):

<!-- docgen:network-en -->
| Source | Hosts | Off with | Code |
|---|---|---|---|
| **Universalis**: prices, listings and history, by world and item id; with the wishlist sale alert (off by default), one request every 30 minutes at most for up to 20 wishes that have a price | `universalis.app` | « Universalis (market prices) » | `Market/MarketService.cs`, `Market/MarketBoardService.cs`, `Market/RetainerSales.cs` |
| **Garland Tools**: item sources, duty and monster loot | `www.garlandtools.org` | « Garland Tools » | `Community/GarlandClient.cs`, `Duties/DutyLootService.cs` |
| **FFXIV Console Games Wiki**: item acquisition, monster drops, duty guides, sightseeing log, blue magic spells, Beastmaster and island pasture pages | `ffxiv.consolegameswiki.com` | « Wiki (consolegameswiki) » | `Community/WikiItemAcquisition.cs`, `Duties/WikiDutyGuide.cs`, `Maps/WikiVistas.cs`, `Monsters/BeastWikiIndex.cs`, `Monsters/BlueSpellWikiIndex.cs`, `Monsters/WikiMonsterSources.cs`, `Island/IslandAnimals.Dalamud.cs`, `Island/IslandAnimals.cs` |
| **Teamcraft**: public reports: one GraphQL POST query carrying the fishing spot id | `gubal.ffxivteamcraft.com` | « Teamcraft (bite times) » | `Community/FishBiteTimes.cs` |
| **LuminaSupplemental**: community data files (CSV) hosted on GitHub | `raw.githubusercontent.com` | « LuminaSupplemental » | `Community/LuminaSupplemental.cs` |
| **GatherBuddy**: fish data (baits, mooches, windows) hosted on GitHub | `raw.githubusercontent.com`, `api.github.com` | « GatherBuddy (fishing) » | `Community/GatherBuddyFish.cs` |
| **Fashion Report (FFXIVGachaSpreadsheet)**: the week's Fashion Report data hosted on GitHub | `raw.githubusercontent.com` | « Fashion Report (FFXIVGachaSpreadsheet) » | `Fashion/FashionDataService.cs` |
| **PaissaDB**: open housing plots, by world id, on a click or with its optional watch | `paissadb.zhu.codes` | « Plots for sale (PaissaDB) » | `Community/PaissaDb.cs` |
| **This repository**: community tool gallery and help videos, on a click, data only, never code | `raw.githubusercontent.com` | — (on a click) | `MyTools/ToolGallery.cs`, `Help/CommunityHelp.cs` |
| **Shared discoveries exchange**: the common files of this repository; after « Connect GitHub » only, the player's own sightings to an unlisted gist and one issue that links it (described below); on « Exchange now » or with « Automatic exchange » | `raw.githubusercontent.com`, `api.github.com`, `github.com`, `gist.github.com` | « Automatic exchange » | `Discoveries/GitHubExchange.cs`, `Discoveries/CommunityDiscoveries.cs` |
| **Discord webhook**: HTTPS POST of alarms and alerts to the player's own webhook (described below) | `discord.com`, `discordapp.com`, `ptb.discord.com`, `canary.discord.com` | « Post alarms to Discord » | `Alarms/DiscordNotifier.cs`, `Alarms/DiscordWebhook.cs` |
| **Desktop app update**: desktop app only: at its start and on « Check for update », this repository's release of the installed plugin's version (else the latest Stable one), then TenshoAssistant.Desktop.zip and its SHA-256 from that release; GET only, no player data (limit described below) | `api.github.com`, `github.com` | « Automatic update: on » | `TenshoAssistant.Desktop/DesktopUpdater.cs`, `TenshoAssistant.Desktop/DesktopInstall.cs`, `TenshoAssistant.Desktop/DesktopUpdate.cs` |
| **Desktop app install**: plugin, on « Install » or « Update » in Settings › Advanced › Desktop application only: this repository's release of the plugin's version (else the latest Stable one), then TenshoAssistant.Desktop.zip and its SHA-256 from that release, unpacked beside; GET only, no player data; the app is never started without a click | `api.github.com`, `github.com` | — (on a click) | `Link/DesktopInstaller.cs`, `TenshoAssistant.Desktop/DesktopInstall.cs`, `TenshoAssistant.Desktop/DesktopUpdate.cs` |
<!-- /docgen:network-en -->

  The desktop application (`src/TenshoAssistant.Desktop`, X-121) updates itself from this repository's releases (row « Desktop app
  update »): it installs a new `TenshoAssistant.Desktop.zip` beside the running one (`%LOCALAPPDATA%\TenshoAssistant\Desktop\versions`)
  only when the release is a published `Stable-x.y.z` of this repository, the file comes from this repository's download address, and its
  SHA-256 is the one in `TenshoAssistant.Desktop.zip.sha256` of the same release; the copy before it stays for going back. **Limit:** the
  application is not code-signed, so the SHA-256 protects against a damaged or incomplete download, not against a stolen GitHub account,
  which could publish both a bad zip and its matching SHA-256.

  All are GET requests except Teamcraft's query, the GitHub sign-in and uploads of the exchange, and the Discord webhook. Requests carry
  item, world, spot or page identifiers only; no player data, no telemetry (the exchange and Discord are the player's choice, below).
  LuminaSupplemental, GatherBuddy, Teamcraft, the Fashion Report data (FFXIVGachaSpreadsheet), Universalis, Garland Tools and the wiki
  can each be turned off (Settings › Advanced); a switch stops every request to its source, requests already queued included. PaissaDB
  has its own switch there too, and « Game data and what you observed only » turns these eight sources off at once (each one's choice
  comes back when it is unticked). Copies already kept stay visible for Universalis (Market page), Garland duty and monster loot, wiki
  monsters and the sightseeing log, and a recent Fashion Report copy; Garland item sources, wiki item acquisition, Teamcraft,
  LuminaSupplemental and GatherBuddy data are hidden while their switch is off.
- **Shared discoveries exchange (GitHub)**: World › Discoveries, on « Exchange now » or with the « Automatic exchange » setting (off by
  default, once a day at most). It downloads the common files of this repository (`raw.githubusercontent.com`): since 0.1137 the index of
  `community-discoveries/v3/` and only the compressed file of each kind that changed, each checked against the hash and size the index gives
  and read within bounds (DEC-052); the older single `discoveries.json` while that set is missing. Only after the player
  presses « Connect GitHub » and approves the code shown on github.com (GitHub's device sign-in, no password in the game), it also sends
  the player's own sightings, never imported ones, to an unlisted gist of theirs (GitHub calls it secret; anyone with its link can read it, and the link is in the public issue; one compressed file per kind and their index, 0.1137), with one issue in this repository titled with their GitHub
  login (`api.github.com`); a warning says so before connecting. A sighting holds game rows and places with the day only, never the
  time, and never a player's name or id; a fish caught or a bite holds the character's exact place (DEC-045), so with the day and the
  issue it can tell where that GitHub account fished on a given day; only fields the player read themself are sent, never dates, levels or
  places that came from an import (0.1134); the files this repository publishes from them keep the day only too, and are under
  CC0-1.0, which the warning and the player's issue state (0.1012). Since 0.1161 the sign-in is the repository's GitHub App (X-175): it asks
  only Issues (read and write), on this repository alone, and the account's Gists (read and write), nothing else; Tensho uses them only for the
  player's one gist, always created secret, and the one issue. Before 0.1161 it was the repository's OAuth app, with the scopes `gist` and
  `public_repo` (GitHub gives no narrower ones): a token of that OAuth app is forgotten and the player connects again; the App's token expires after 8 hours and is renewed before an
  upload with its refresh token (6 months), without any client secret; a refused renewal disconnects. The token, and the refresh token,
  are kept on the player's PC, encrypted for the Windows user (DPAPI), never in a backup copy; « Disconnect » deletes them
  (`src/TenshoAssistant/Discoveries/GitHubExchange.cs`, `GitHubSignIn.cs`, `CommunityDiscoveries.cs`).
- **Developer mode page sweep (DEC-040)**: in developer mode only, after the self-check, Tensho draws every page once, at most once every
  two hours. A page may then make the requests it makes when the player opens it, within the player's source switches; the Market page
  (Universalis) is left out. Outside developer mode, nothing runs.
- **Discord webhook (R-12)**: off by default. When the player turns it on and enters a webhook address (only Discord's own hosts are
  accepted: `discord.com`, `ptb.discord.com`, `canary.discord.com`, `discordapp.com`; one address per category may be set the same way), a
  ringing Tensho alarm, the currency cap alerts and the « duty ready » alerts are posted there (HTTPS POST): the message and local time,
  and the character's name only when the player enables it; mentions are limited to the role the player sets (`allowed_mentions`). Each
  alarm has its own Discord box, off for a new alarm (an alarm added to a group takes the group's choice). The address is a secret: stored
  in the plugin configuration on the player's PC, masked in the settings, never written to the logs nor to the support report (which only
  says whether it is valid) (`src/TenshoAssistant/Alarms/DiscordNotifier.cs`, `DiscordWebhook.cs`). Clearing it or turning sending off
  drops the queued messages.
- **Local link with the desktop application (X-119)**: off by default. While Settings › Advanced « Link with the desktop app » is on,
  the plugin offers a local named pipe, out only, open to the current Windows user and denied to network logons (no network port);
  once a second it writes exactly this, and reads nothing back: the plugin's version; whether a character is logged in; whether Tensho
  is paused (PvP, ranked content); the zone's number and name; the job's abbreviation and level, the gathering points and their maximum,
  the free slots of the four bags and their size; and notices that a kind of saved data changed, as categories of a closed list
  (`settings`, `bank`, `characters`, `gil-journal`, `craft-lists`, `my-tools`, `voyages`, `achievements`, `beast-teams`, `duties`,
  `discoveries`, `data`), never a file name, a file's content, a character's name or id; and, since 0.1155, the last alerts that rang in
  the game apart from the Tensho alarms (currencies, duty ready, GP, gear wear, important items, hunt marks: at most 4, 200 characters
  each, as the chat line said them, kept 2 minutes), for the application to ring them too; and, since 0.1156, how many of each item of
  the alarms with a quantity needed the character holds (at most 32 items, only when seen), so the application stays silent when it is
  already gathered. The desktop application connects to it as the same
  user only and reads the plugin's settings file itself, read-only, bounded, without the Discord addresses and with a closed list of types (`src/TenshoAssistant/Link/LocalLink.cs`, `docs/evidence/x119-desktop-link.md`).
  It copies the saved data it shows into its own folder (%LOCALAPPDATA%\TenshoAssistant\Desktop\mirror), the plugin's folder only read;
  the player's data file `TenshoData.db` by SQLite's backup, opened read-only (0.1147); the discoveries database `Discoveries/Discoveries.db` the
  same way, that file only (never the folder's GitHub sign-in, imports or exports), at most every 5 minutes, and never written back (0.1152).
- **Links opened in the browser**: item pages on Universalis, Garland Tools, Teamcraft, the FFXIV Console Games Wiki or the Lodestone,
  a Teamcraft list import link (built locally), the Partake events page, the prefilled GitHub form of « report a problem », help
  videos (YouTube, or this repository), a link of a « My tools » tool, and GitHub's sign-in page of the discoveries exchange. They open in
  the player's own browser only on a click or a `/tensho wiki|lodestone` command; help videos and tool links ask for a confirmation
  that shows the address first. Tensho itself sends nothing.
- **Local files**, in the plugin's Dalamud configuration folder: the configuration (through Dalamud); the disk cache (`DataCache-v1/`);
  the saved storages (since 0.1149 in the player's data file `TenshoData.db`, saved per character in one transaction; the files of before,
  `InventoryBank/` with one file per character, are kept once in `before-sqlite-<date>/`, a former `InventoryBank.json` as `.migrated`); the gil journal
  (90 days and 20,000 entries per character at most; since 0.1139 in the player's data file `TenshoData.db`, one SQLite database at the
  root of the folder, saved whole per character in one transaction; the files of before, `GilJournal/<id>.json`, are kept once in
  `GilJournal/before-sqlite-<date>/`); the achievement progress seen per character (since 0.1140 in `TenshoData.db` too; the file of
  before, `AchievementProgress.json`, is kept once in `before-sqlite-<date>/`); what Tensho last read for each of the player's characters
  (`Characters/`); the local discoveries base (`Discoveries/`: since 0.1136 one SQLite database, `Discoveries.db`, the player's own sightings and the
  imported ones, written row by row; the files of before, `Discoveries.json` and `Shared/`, are kept once in `before-sqlite-<date>/`; the market prices seen at the market board in `MarketSeen.json`, with no seller name or id,
  only whether an offer is one of the player's retainers', the `Import/` and `Export/` folders, and for the GitHub exchange `github.json`,
  `community.sha256`, `community-v3.json` (the hash of each common file taken), `github.lock` (one exchange at a time), the encrypted `github.token` and, with the GitHub App, the encrypted `github.refresh`); `GameDataSnapshot.json` (the shape of 37 game sheets, to list what a game update adds); the voyage history (since 0.1148 in `TenshoData.db`;
  the file of before, `VoyageHistory.json`, is kept once in `before-sqlite-<date>/`), `MyTools.json`, `CraftLists.json`, notebook exports
  (`Exports/`); in developer mode, `SelfCheckSeen.json` and `PageSweepLast.txt`. The discoveries base keeps game rows and places only,
  never another player's name, position or id; import merges the files the player puts in `Import/`, export writes a file to `Export/`;
  only the GitHub exchange above sends anything. Translators can put language JSON files beside the DLL; they are validated when
  reloaded.
- **Native libraries** beside the DLL: Raphael's crafting solver (`raphael_bindings.dll`) and SQLite (`e_sqlite3.dll`, from SQLitePCLRaw
  2.1.14, SQLite 3.54, 0.1136). They run in the game's process, open only the files named above and make no network request.
- **Game**: game memory is read on the framework thread and copied. The actions that reach the game start on a click: teleport, applying
  an inventory sort, macro chains, writing crafting macros into the game's macros, opening the Try On window, equipping a gearset, wearing
  a title, sending the hunt train call to the chat channel the player picks, and `/alarm` chat commands for game alarms (when the player
  adds an alarm with the « game alarms » setting on, off by default, or presses « Send »; automatic alarms never create one). One setting,
  off by default, acts without a click: wearing the title chosen for a job on a job change (DEC-035). Two hooks: the game's macro
  function (macro chains), and a read-only hook on the game's action function while the crafting recorder records (the game's own
  function runs first and its result is returned unchanged). The whole plugin stops in PvP and ranked content, with no setting (DEC-038).
  Only the local character's content id is used, as a key for its own saved data.
- **Support report** (Settings › Advanced): copied to the clipboard on a click: version, language, shown page, data states, errors,
  source settings, the market world name, alarm counts, the Discord state (on or off, address valid or not, counts) and the context item
  id.

Reports about these surfaces are welcome; issues in the game, Dalamud or the community services themselves belong to their owners.

## Information to include in your report

- Description of the vulnerability
- Steps to reproduce it
- Potential impact and affected component
- Affected version or build
- Screenshots, logs, or relevant excerpts
- Suggested remediation, if available

## What you can expect

- Acknowledgement as quickly as possible
- Assessment of the issue and its severity
- Collaboration to validate and fix the issue
- Updates on the progress of the fix
- Public or private credit, if appropriate and agreed upon

## Prohibited actions

- Do not publicly disclose the vulnerability before a fix is available
- Do not exploit a vulnerability in production or on third-party systems
- Do not collect or request sensitive data from third parties
- Do not perform intrusive or unauthorized security testing on systems you do not own or have explicit permission to test

## Response expectations

Critical issues are handled first, by impact and ease of exploitation. We will investigate promptly and release a fix as soon as it is
safe and feasible.

## Thanks

Thank you for helping keep Tensho Assistant safe for all users.

---

# Politique de sécurité

Merci d'aider à garder Tensho Assistant sûr.

## Versions prises en charge

Seule la dernière release publiée (la version de `repo.json`, tag `Stable-x.y.z`) reçoit les corrections de sécurité ; une correction
sort dans une nouvelle version. Les compilations de la branche `Devloppement` et les compilations locales ne sont pas prises en charge.
Le périmètre ci-dessous décrit la branche `Devloppement` à la version nommée dans sa première ligne, qui peut être plus récente que la
dernière release.

## Signaler une vulnérabilité

Si vous trouvez une vulnérabilité dans ce projet, signalez-la en privé et de façon responsable, par le signalement privé de sécurité de
GitHub pour ce dépôt :

https://github.com/Shigu330/Tensho-Assistant/security/advisories/new

N'ouvrez pas de ticket public et ne divulguez pas de détails sur la vulnérabilité avant la publication d'une correction.

## Périmètre

Ce que fait le plugin et qui compte pour la sécurité (relu contre le code de la branche `Devloppement` le 2026-11-09 ; chaque changement d'après est ajouté ici avec la version qui l'apporte) :

- **Réseau** : requêtes HTTPS avec un délai et une taille limités (`src/TenshoAssistant/Community/BoundedHttp.cs`, ou la limite propre
  du client pour Universalis) vers les sources ci-dessous (tableau produit depuis le code par `tools/docgen.py` : chaque fichier qui fait une requête y est, avec le réglage qui coupe sa source) :

<!-- docgen:network-fr -->
| Source | Hôtes | Coupée par | Code |
|---|---|---|---|
| **Universalis** : prix, annonces et historique, par monde et numéro d'objet ; avec l'alerte de vente des souhaits (désactivée par défaut), une requête toutes les 30 minutes au plus pour 20 souhaits au plus qui ont un prix | `universalis.app` | « Universalis (prix du marché) » | `Market/MarketService.cs`, `Market/MarketBoardService.cs`, `Market/RetainerSales.cs` |
| **Garland Tools** : sources d'objets, butin des contenus et des monstres | `www.garlandtools.org` | « Garland Tools » | `Community/GarlandClient.cs`, `Duties/DutyLootService.cs` |
| **FFXIV Console Games Wiki** : acquisition des objets, butin des monstres, guides des contenus, carnet d'exploration, sorts de mage bleu, pages du dresseur et du pâturage de l'île | `ffxiv.consolegameswiki.com` | « Wiki (consolegameswiki) » | `Community/WikiItemAcquisition.cs`, `Duties/WikiDutyGuide.cs`, `Maps/WikiVistas.cs`, `Monsters/BeastWikiIndex.cs`, `Monsters/BlueSpellWikiIndex.cs`, `Monsters/WikiMonsterSources.cs`, `Island/IslandAnimals.Dalamud.cs`, `Island/IslandAnimals.cs` |
| **Teamcraft** : rapports publics : une requête GraphQL POST portant le numéro du lieu de pêche | `gubal.ffxivteamcraft.com` | « Teamcraft (temps de touche) » | `Community/FishBiteTimes.cs` |
| **LuminaSupplemental** : fichiers de données de la communauté (CSV) hébergés sur GitHub | `raw.githubusercontent.com` | « LuminaSupplemental » | `Community/LuminaSupplemental.cs` |
| **GatherBuddy** : données des poissons (appâts, remises à l'eau, fenêtres) hébergées sur GitHub | `raw.githubusercontent.com`, `api.github.com` | « GatherBuddy (pêche) » | `Community/GatherBuddyFish.cs` |
| **Fashion Report (FFXIVGachaSpreadsheet)** : les données de la Revue de mode de la semaine, hébergées sur GitHub | `raw.githubusercontent.com` | « Revue de mode (FFXIVGachaSpreadsheet) » | `Fashion/FashionDataService.cs` |
| **PaissaDB** : terrains libres, par numéro de monde, sur un clic ou avec sa surveillance facultative | `paissadb.zhu.codes` | « Terrains en vente (PaissaDB) » | `Community/PaissaDb.cs` |
| **This repository** : galerie d'outils de la communauté et vidéos d'aide, sur un clic, des données seulement, jamais de code | `raw.githubusercontent.com` | — (sur un clic) | `MyTools/ToolGallery.cs`, `Help/CommunityHelp.cs` |
| **Shared discoveries exchange** : les fichiers communs de ce dépôt ; seulement après « Connecter GitHub », les observations du joueur vers un gist non répertorié et un ticket qui y renvoie (décrit plus bas) ; sur « Échanger maintenant » ou avec « Échange automatique » | `raw.githubusercontent.com`, `api.github.com`, `github.com`, `gist.github.com` | « Échange automatique » | `Discoveries/GitHubExchange.cs`, `Discoveries/CommunityDiscoveries.cs` |
| **Discord webhook** : envoi HTTPS POST des alarmes et alertes vers le webhook du joueur (décrit plus bas) | `discord.com`, `discordapp.com`, `ptb.discord.com`, `canary.discord.com` | « Envoyer les alarmes sur Discord » | `Alarms/DiscordNotifier.cs`, `Alarms/DiscordWebhook.cs` |
| **Desktop app update** : application de bureau seulement : à son démarrage et sur « Chercher une mise à jour », la release de ce dépôt qui a la version du plugin installé (sinon la dernière Stable), puis TenshoAssistant.Desktop.zip et son SHA-256 de cette release ; GET seulement, aucune donnée du joueur (limite décrite plus bas) | `api.github.com`, `github.com` | « Mise à jour automatique : active » | `TenshoAssistant.Desktop/DesktopUpdater.cs`, `TenshoAssistant.Desktop/DesktopInstall.cs`, `TenshoAssistant.Desktop/DesktopUpdate.cs` |
| **Desktop app install** : plugin, seulement sur « Installer » ou « Mettre à jour » dans Paramètres › Avancé › Application de bureau : la release de ce dépôt qui a la version du plugin (sinon la dernière Stable), puis TenshoAssistant.Desktop.zip et son SHA-256 de cette release, dépaquetés à côté ; GET seulement, aucune donnée du joueur ; l'application n'est jamais lancée sans un clic | `api.github.com`, `github.com` | — (sur un clic) | `Link/DesktopInstaller.cs`, `TenshoAssistant.Desktop/DesktopInstall.cs`, `TenshoAssistant.Desktop/DesktopUpdate.cs` |
<!-- /docgen:network-fr -->

  L'application de bureau (`src/TenshoAssistant.Desktop`, X-121) se met à jour depuis les releases de ce dépôt (ligne « Desktop app
  update ») : elle installe un nouveau `TenshoAssistant.Desktop.zip` à côté de celle qui tourne (`%LOCALAPPDATA%\TenshoAssistant\Desktop\versions`)
  seulement si la release est une `Stable-x.y.z` publiée de ce dépôt, si le fichier vient de l'adresse de téléchargement de ce dépôt et
  si son SHA-256 est celui de `TenshoAssistant.Desktop.zip.sha256` de la même release ; la copie d'avant reste pour revenir en arrière.
  **Limite :** l'application n'est pas signée, donc le SHA-256 protège d'un téléchargement abîmé ou incomplet, pas d'un
  compte GitHub volé, qui pourrait publier à la fois un mauvais zip et le SHA-256 qui va avec.

  Toutes sont des requêtes GET, sauf celle de Teamcraft, la connexion et les envois de l'échange GitHub, et le webhook Discord. Les
  requêtes ne portent que des numéros d'objet, de monde, de lieu ou de page ; aucune donnée du joueur, aucune télémétrie (l'échange et
  Discord sont au choix du joueur, ci-dessous). LuminaSupplemental, GatherBuddy, Teamcraft, les données de la Revue de mode
  (FFXIVGachaSpreadsheet), Universalis, Garland Tools et le wiki se coupent chacun (Paramètres › Avancé) ; un interrupteur arrête toute
  requête vers sa source, y compris celles déjà en file. PaissaDB y a aussi son interrupteur, et « Données du jeu et observées
  seulement » coupe ces huit sources d'un coup (le choix de chacune revient quand la case est décochée). Les copies déjà gardées restent
  visibles pour Universalis (page Marché), le butin des contenus et des monstres de Garland, les monstres du wiki et le carnet
  d'exploration, et une copie récente de la Revue de mode ; les sources d'objets de Garland, l'acquisition du wiki, Teamcraft,
  LuminaSupplemental et GatherBuddy sont masqués tant que leur interrupteur est coupé.
- **Échange des découvertes partagées (GitHub)** : Monde › Découvertes, sur « Échanger maintenant » ou avec le réglage « Échange
  automatique » (désactivé par défaut, une fois par jour au plus). Il télécharge les fichiers communs de ce dépôt
  (`raw.githubusercontent.com`) : depuis 0.1137 l'index de `community-discoveries/v3/` et seulement le fichier compressé de chaque genre qui a
  changé, chacun vérifié contre l'empreinte et la taille que donne l'index et lu dans des bornes (DEC-052) ; l'ancien fichier unique
  `discoveries.json` tant que cet ensemble manque. Seulement après que le joueur a appuyé sur « Connecter GitHub » et approuvé sur github.com le code
  affiché (connexion par appareil de GitHub, aucun mot de passe dans le jeu), il envoie aussi ce que le joueur a vu lui-même, jamais ce
  qu'il a importé, dans un gist non répertorié à son nom (GitHub le dit secret ; qui a son lien peut le lire, et ce lien est dans le ticket public ; un fichier compressé par genre et leur index, 0.1137), avec un ticket dans ce dépôt titré de son identifiant GitHub (`api.github.com`) ; un
  avertissement le dit avant la connexion. Une observation contient des lignes et des lieux du jeu avec le jour seulement, jamais
  l'heure, et jamais le nom ou l'identifiant d'un joueur ; un poisson pris ou une touche porte l'endroit exact du personnage (DEC-045),
  donc avec le jour et le ticket elle peut dire où ce compte GitHub a pêché un jour donné ; seuls les champs relevés par le joueur
  lui-même partent, jamais des dates, niveaux ou lieux venus d'un import (0.1134) ; les fichiers que ce dépôt en publie ne gardent eux aussi que le
  jour, sous CC0-1.0, ce que l'avertissement et le ticket du joueur disent (0.1012). Depuis 0.1161 la connexion passe par l'application GitHub
  du dépôt (X-175) : elle ne demande que Issues (lecture et écriture), sur ce seul dépôt, et les Gists du compte (lecture et écriture),
  rien d'autre ; Tensho ne s'en sert que pour le seul gist du joueur, toujours créé secret, et le seul ticket. Avant 0.1161 c'était
  l'application OAuth du dépôt, avec les droits `gist` et `public_repo` (GitHub n'en donne pas de plus étroits) : un jeton de cette
  application OAuth est oublié et le joueur se reconnecte ; le jeton de l'application expire après 8 heures et se renouvelle avant un envoi par son jeton de renouvellement
  (6 mois), sans aucun secret client ; un renouvellement refusé déconnecte. Le jeton, et le jeton de renouvellement, sont gardés sur le PC
  du joueur, chiffrés pour l'utilisateur Windows (DPAPI), jamais dans une copie de sauvegarde ; « Déconnecter » les efface
  (`src/TenshoAssistant/Discoveries/GitHubExchange.cs`, `GitHubSignIn.cs`, `CommunityDiscoveries.cs`).
- **Passage des pages en mode développeur (DEC-040)** : en mode développeur seulement, après l'auto-contrôle, Tensho dessine une fois
  chaque page, au plus une fois toutes les deux heures. Une page peut alors faire les requêtes qu'elle fait quand le joueur l'ouvre, dans
  les interrupteurs de sources du joueur ; la page Marché (Universalis) est exclue. Hors mode développeur, rien ne se lance.
- **Webhook Discord (R-12)** : désactivé par défaut. Quand le joueur l'active et saisit une adresse de webhook (seuls les hôtes de
  Discord sont acceptés : `discord.com`, `ptb.discord.com`, `canary.discord.com`, `discordapp.com` ; une adresse par catégorie peut être
  saisie de la même façon), une alarme Tensho qui sonne, les alertes de plafond de monnaie et les alertes « mission prête » y sont
  envoyées (HTTPS POST) : le message et l'heure locale, et le nom du personnage seulement si le joueur l'active ; les mentions sont
  limitées au rôle choisi par le joueur (`allowed_mentions`). Chaque alarme a sa propre case Discord, décochée pour une nouvelle alarme
  (une alarme ajoutée à un groupe prend le choix du groupe). L'adresse est un secret : gardée dans la configuration du plugin sur le PC
  du joueur, masquée dans les réglages, jamais écrite dans les journaux ni dans le rapport d'assistance (qui dit seulement si elle est
  valide) (`src/TenshoAssistant/Alarms/DiscordNotifier.cs`, `DiscordWebhook.cs`). L'effacer ou couper l'envoi vide les messages en
  attente.
- **Lien local avec l'application de bureau (X-119)** : désactivé par défaut. Tant que Paramètres › Avancé « Lien avec l'application de
  bureau » est activé, le plugin ouvre un canal nommé local, en sortie seulement, ouvert à l'utilisateur Windows courant et refusé aux
  connexions réseau (aucun port réseau) ; une fois par seconde il écrit exactement ceci, et ne lit rien en retour : la version du
  plugin ; si un personnage est connecté ; si Tensho est en pause (JcJ, contenu classé) ; le numéro et le nom de la zone ; l'abréviation
  et le niveau de la classe, les points de récolte et leur maximum, les places libres des quatre sacs et leur taille ; et des avis qu'un
  genre de données enregistrées a changé, en catégories d'une liste fermée (`settings`, `bank`, `characters`, `gil-journal`,
  `craft-lists`, `my-tools`, `voyages`, `achievements`, `beast-teams`, `duties`, `discoveries`, `data`), jamais un nom de fichier, le contenu
  d'un fichier, le nom ou l'identifiant d'un personnage ; et, depuis 0.1155, les dernières alertes qui ont sonné en jeu en dehors des
  alarmes Tensho (monnaies, mission prête, PR, usure de l'équipement, objets importants, marques de chasse : 4 au plus, 200 caractères
  chacune, telles que la ligne du chat les a dites, gardées 2 minutes), pour que l'application les fasse sonner aussi ; et, depuis 0.1156,
  combien le personnage possède de chaque objet des alarmes qui demandent une quantité (32 objets au plus, seulement ceux vus), pour que
  l'application se taise quand il est déjà récolté. L'application de bureau s'y connecte sous le même utilisateur seulement et
  lit elle-même le fichier de réglages du plugin, en lecture seule, borné, sans les adresses Discord et avec une liste fermée de types (`src/TenshoAssistant/Link/LocalLink.cs`, `docs/evidence/x119-desktop-link.md`).
  Elle copie les données enregistrées qu'elle montre dans son propre dossier (%LOCALAPPDATA%\TenshoAssistant\Desktop\mirror), le dossier
  du plugin seulement lu ; le fichier de données du joueur `TenshoData.db` par la sauvegarde de SQLite, ouvert en lecture seule (0.1147) ; la base
  des découvertes `Discoveries/Discoveries.db` de la même façon, ce fichier seulement (jamais la connexion GitHub, les imports ni les exports du
  dossier), au plus toutes les 5 minutes, et jamais réécrite (0.1152).
- **Liens ouverts dans le navigateur** : pages d'objet sur Universalis, Garland Tools, Teamcraft, le FFXIV Console Games Wiki ou le
  Lodestone, un lien d'import de liste Teamcraft (construit sur place), la page d'événements de Partake, le formulaire GitHub prérempli
  de « Signaler un problème », les vidéos d'aide (YouTube, ou ce dépôt), un lien d'un outil de « Mes outils », et la page de connexion
  GitHub de l'échange des découvertes. Ils s'ouvrent dans le navigateur du joueur seulement sur un clic ou une commande
  `/tensho wiki|lodestone` ; les vidéos d'aide et les liens des outils demandent d'abord une confirmation qui montre l'adresse. Tensho
  lui-même n'envoie rien.
- **Fichiers locaux**, dans le dossier de configuration Dalamud du plugin : la configuration (par Dalamud) ; le cache disque
  (`DataCache-v1/`) ; les rangements enregistrés (depuis 0.1149 dans le fichier de données du joueur `TenshoData.db`, enregistrés par personnage en une
  transaction ; les fichiers d'avant, `InventoryBank/` avec un fichier par personnage, sont gardés une fois dans `before-sqlite-<date>/`,
  un ancien `InventoryBank.json` en `.migrated`) ; le journal des gils (90 jours et 20 000 entrées par personnage au plus ; depuis 0.1139 dans le fichier de données du joueur,
  `TenshoData.db`, une base SQLite à la racine du dossier, enregistré en entier par personnage en une transaction ; les fichiers d'avant,
  `GilJournal/<id>.json`, sont gardés une fois dans `GilJournal/before-sqlite-<date>/`) ; la progression des succès vue, par personnage
  (depuis 0.1140 dans `TenshoData.db` aussi ; le fichier d'avant, `AchievementProgress.json`, est gardé une fois dans `before-sqlite-<date>/`) ; ce que Tensho a
  lu en dernier pour chacun des personnages du joueur (`Characters/`) ; la base locale de découvertes (`Discoveries/` : depuis 0.1136
  une base SQLite, `Discoveries.db`, ce que le joueur a vu lui-même et ce qu'il a importé, écrite ligne par ligne ; les fichiers d'avant,
  `Discoveries.json` et `Shared/`, sont gardés une fois dans `before-sqlite-<date>/` ; les prix vus au tableau des
  ventes dans `MarketSeen.json`, sans nom ni identifiant de vendeur, seulement si une offre est celle d'un servant du joueur, les
  dossiers `Import/` et `Export/`, et pour l'échange GitHub `github.json`, `community.sha256`, `community-v3.json` (l'empreinte de chaque fichier commun pris), `github.lock` (un échange à la fois), le `github.token` chiffré et, avec l'application GitHub, le `github.refresh` chiffré) ;
  `GameDataSnapshot.json` (la forme de 37 feuilles du jeu, pour lister ce qu'une mise à jour ajoute) ; l'historique des voyages (depuis 0.1148 dans `TenshoData.db` ;
  le fichier d'avant, `VoyageHistory.json`, est gardé une fois dans `before-sqlite-<date>/`), `MyTools.json`, `CraftLists.json`, les exports des carnets (`Exports/`) ; en mode développeur,
  `SelfCheckSeen.json` et `PageSweepLast.txt`. La base de découvertes ne garde que des lignes du jeu et des lieux, jamais le nom, la
  position ou l'identifiant d'un autre joueur ; l'import fusionne les fichiers que le joueur place dans `Import/`, l'export écrit un
  fichier dans `Export/` ; seul l'échange GitHub ci-dessus envoie quelque chose. Les traducteurs peuvent poser des fichiers de langue JSON à côté de la DLL ; ils sont validés au rechargement.
- **Bibliothèques natives** à côté de la DLL : le solveur d'artisanat de Raphael (`raphael_bindings.dll`) et SQLite (`e_sqlite3.dll`,
  de SQLitePCLRaw 2.1.14, SQLite 3.54, 0.1136). Elles tournent dans le processus du jeu, n'ouvrent que les fichiers nommés plus haut et ne
  font aucune requête réseau.
- **Jeu** : la mémoire du jeu est lue sur le thread framework et copiée. Les actions qui atteignent le jeu partent d'un clic :
  téléportation, application d'un tri d'inventaire, chaînes de macros, écriture des macros d'artisanat dans les macros du jeu, ouverture
  de la cabine d'essayage, équiper un ensemble d'équipement, porter un titre, envoyer l'appel du train de chasse dans le canal choisi par
  le joueur, et les commandes `/alarm` des alarmes du jeu (quand le joueur ajoute une alarme avec le réglage « alarmes du jeu » actif,
  désactivé par défaut, ou appuie sur « Envoyer » ; les alarmes automatiques n'en créent jamais). Un réglage, désactivé par défaut, agit
  sans clic : porter le titre choisi pour un job au changement de job (DEC-035). Deux hooks : la fonction de macro du jeu (chaînes de
  macros), et un hook en lecture seule sur la fonction d'action du jeu pendant que l'enregistreur d'artisanat enregistre (la fonction du
  jeu s'exécute d'abord et son résultat est rendu tel quel). Le plugin entier s'arrête en JcJ et en contenu classé, sans réglage
  (DEC-038). Seul l'identifiant de contenu du personnage local sert, comme clé de ses propres données enregistrées.
- **Rapport d'assistance** (Paramètres › Avancé) : copié dans le presse-papiers sur un clic : version, langue, page affichée, états des
  données, erreurs, réglages des sources, nom du monde du marché, nombre d'alarmes, état de Discord (actif ou non, adresse valide ou
  non, compteurs) et numéro de l'objet en contexte.

Les signalements sur ces surfaces sont bienvenus ; les problèmes du jeu, de Dalamud ou des services communautaires eux-mêmes relèvent de
leurs propriétaires.

## Informations à joindre au signalement

- Description de la vulnérabilité
- Étapes pour la reproduire
- Impact possible et composant touché
- Version ou compilation touchée
- Captures, journaux ou extraits utiles
- Correction suggérée, si vous en avez une

## Ce que vous pouvez attendre

- Un accusé de réception le plus vite possible
- Une évaluation du problème et de sa gravité
- Une collaboration pour valider et corriger le problème
- Des nouvelles de l'avancement de la correction
- Une mention publique ou privée, si elle convient et est acceptée

## Actions interdites

- Ne divulguez pas publiquement la vulnérabilité avant qu'une correction soit disponible
- N'exploitez pas une vulnérabilité en production ou sur des systèmes tiers
- Ne collectez ni ne demandez de données sensibles à des tiers
- Ne faites pas de tests de sécurité intrusifs ou non autorisés sur des systèmes qui ne vous appartiennent pas ou sans permission
  explicite

## Délais de réponse

Les problèmes critiques passent en premier, selon leur impact et leur facilité d'exploitation. Nous enquêterons rapidement et publierons
une correction dès que ce sera sûr et faisable.

## Merci

Merci d'aider à garder Tensho Assistant sûr pour tous les joueurs.
