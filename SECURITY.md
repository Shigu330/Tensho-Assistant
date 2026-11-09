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
  limit for Universalis) to:
  - Universalis (`universalis.app`: prices, listings and history, by world and item id); when the wishlist sale alert is on (off by default, 0.1000), one request every 30 minutes at most for up to 20 wishes that have a price;
  - Garland Tools (`www.garlandtools.org`: item sources, duty and monster loot);
  - the FFXIV Console Games Wiki (`ffxiv.consolegameswiki.com`: item acquisition, monster drops, sightseeing log, blue magic spells,
    Beastmaster and island pasture pages);
  - Teamcraft's public reports (`gubal.ffxivteamcraft.com`: one GraphQL POST query carrying the fishing spot id);
  - GitHub-hosted data (`raw.githubusercontent.com`, `api.github.com`): LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet
    (Fashion Report), and this repository (community tool gallery and help videos, on a click, data only, never code);
  - PaissaDB (`paissadb.zhu.codes`: open housing plots, by world id, on a click or with its optional watch);
  - the shared discoveries exchange below (GitHub);
  - the Discord webhook below.

  All are GET requests except Teamcraft's query, the GitHub sign-in and uploads of the exchange, and the Discord webhook. Requests carry
  item, world, spot or page identifiers only; no player data, no telemetry (the exchange and Discord are the player's choice, below).
  LuminaSupplemental, GatherBuddy, Teamcraft, the Fashion Report data (FFXIVGachaSpreadsheet), Universalis, Garland Tools and the wiki
  can each be turned off (Settings › Advanced); a switch stops every request to its source, requests already queued included. PaissaDB
  has its own switch there too, and « Game data and what you observed only » turns these eight sources off at once (each one's choice
  comes back when it is unticked). Copies already kept stay visible for Universalis (Market page), Garland duty and monster loot, wiki
  monsters and the sightseeing log, and a recent Fashion Report copy; Garland item sources, wiki item acquisition, Teamcraft,
  LuminaSupplemental and GatherBuddy data are hidden while their switch is off.
- **Shared discoveries exchange (GitHub)**: World › Discoveries, on « Exchange now » or with the « Automatic exchange » setting (off by
  default, once a day at most). It downloads the common file of this repository (`raw.githubusercontent.com`). Only after the player
  presses « Connect GitHub » and approves the code shown on github.com (GitHub's device sign-in, no password in the game), it also sends
  the player's own sightings, never imported ones, to a secret gist of theirs, with one issue in this repository titled with their GitHub
  login (`api.github.com`); a warning says so before connecting. A sighting holds game rows and places with the day only, never the
  time, and never a player's name, position or id; the files this repository publishes from them keep the day only too, and are under
  CC0-1.0, which the warning and the player's issue state (0.1012). The token is kept on the player's PC, encrypted for the Windows user (DPAPI);
  « Disconnect » deletes it (`src/TenshoAssistant/Discoveries/GitHubExchange.cs`, `CommunityDiscoveries.cs`).
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
- **Links opened in the browser**: item pages on Universalis, Garland Tools, Teamcraft, the FFXIV Console Games Wiki or the Lodestone,
  a Teamcraft list import link (built locally), the Partake events page, the prefilled GitHub form of « report a problem », help
  videos (YouTube, or this repository), a link of a « My tools » tool, and GitHub's sign-in page of the discoveries exchange. They open in
  the player's own browser only on a click or a `/tensho wiki|lodestone` command; help videos and tool links ask for a confirmation
  that shows the address first. Tensho itself sends nothing.
- **Local files**, in the plugin's Dalamud configuration folder: the configuration (through Dalamud); the disk cache (`DataCache-v1/`);
  the saved storages (`InventoryBank/`, one file per character; a former `InventoryBank.json` is kept as `.migrated`); the gil journal
  (`GilJournal/`, one file per character, 90 days and 20,000 entries at most); what Tensho last read for each of the player's characters
  (`Characters/`); the local discoveries base (`Discoveries/`: the player's own sightings in `Discoveries.json`, the imported ones in
  `Shared/`, one compressed file per kind, the market prices seen at the market board in `MarketSeen.json`, with no seller name or id,
  only whether an offer is one of the player's retainers', the `Import/` and `Export/` folders, and for the GitHub exchange `github.json`,
  `community.sha256` and the encrypted `github.token`); `GameDataSnapshot.json` (the shape of 37 game sheets, to list what a game update adds); `VoyageHistory.json`, `MyTools.json`, `CraftLists.json`, notebook exports
  (`Exports/`); in developer mode, `SelfCheckSeen.json` and `PageSweepLast.txt`. The discoveries base keeps game rows and places only,
  never another player's name, position or id; import merges the files the player puts in `Import/`, export writes a file to `Export/`;
  only the GitHub exchange above sends anything. Translators can put language JSON files beside the DLL; they are validated when
  reloaded.
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
  du client pour Universalis) vers :
  - Universalis (`universalis.app` : prix, annonces et historique, par monde et numéro d'objet) ; quand l'alerte de vente des souhaits est activée (désactivée par défaut, 0.1000), une requête toutes les 30 minutes au plus pour 20 souhaits avec un prix au plus ;
  - Garland Tools (`www.garlandtools.org` : sources d'objets, butin des contenus et des monstres) ;
  - le FFXIV Console Games Wiki (`ffxiv.consolegameswiki.com` : acquisition des objets, butin des monstres, carnet d'exploration, sorts
    de mage bleu, pages du dresseur et du pâturage de l'île) ;
  - les rapports publics de Teamcraft (`gubal.ffxivteamcraft.com` : une requête GraphQL POST portant le numéro du lieu de pêche) ;
  - des données hébergées sur GitHub (`raw.githubusercontent.com`, `api.github.com`) : LuminaSupplemental, GatherBuddy,
    FFXIVGachaSpreadsheet (Revue de mode), et ce dépôt (galerie d'outils de la communauté et vidéos d'aide, sur un clic, des données
    seulement, jamais de code) ;
  - PaissaDB (`paissadb.zhu.codes` : terrains libres, par numéro de monde, sur un clic ou avec sa surveillance facultative) ;
  - l'échange des découvertes partagées ci-dessous (GitHub) ;
  - le webhook Discord ci-dessous.

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
  automatique » (désactivé par défaut, une fois par jour au plus). Il télécharge le fichier commun de ce dépôt
  (`raw.githubusercontent.com`). Seulement après que le joueur a appuyé sur « Connecter GitHub » et approuvé sur github.com le code
  affiché (connexion par appareil de GitHub, aucun mot de passe dans le jeu), il envoie aussi ce que le joueur a vu lui-même, jamais ce
  qu'il a importé, dans un gist secret à son nom, avec un ticket dans ce dépôt titré de son identifiant GitHub (`api.github.com`) ; un
  avertissement le dit avant la connexion. Une observation contient des lignes et des lieux du jeu avec le jour seulement, jamais
  l'heure, et jamais le nom, la position ou l'identifiant d'un joueur ; les fichiers que ce dépôt en publie ne gardent eux aussi que le
  jour, sous CC0-1.0, ce que l'avertissement et le ticket du joueur disent (0.1012). Le jeton est gardé sur le PC du joueur, chiffré pour
  l'utilisateur Windows (DPAPI) ; « Déconnecter » l'efface (`src/TenshoAssistant/Discoveries/GitHubExchange.cs`,
  `CommunityDiscoveries.cs`).
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
- **Liens ouverts dans le navigateur** : pages d'objet sur Universalis, Garland Tools, Teamcraft, le FFXIV Console Games Wiki ou le
  Lodestone, un lien d'import de liste Teamcraft (construit sur place), la page d'événements de Partake, le formulaire GitHub prérempli
  de « Signaler un problème », les vidéos d'aide (YouTube, ou ce dépôt), un lien d'un outil de « Mes outils », et la page de connexion
  GitHub de l'échange des découvertes. Ils s'ouvrent dans le navigateur du joueur seulement sur un clic ou une commande
  `/tensho wiki|lodestone` ; les vidéos d'aide et les liens des outils demandent d'abord une confirmation qui montre l'adresse. Tensho
  lui-même n'envoie rien.
- **Fichiers locaux**, dans le dossier de configuration Dalamud du plugin : la configuration (par Dalamud) ; le cache disque
  (`DataCache-v1/`) ; les rangements enregistrés (`InventoryBank/`, un fichier par personnage ; un ancien `InventoryBank.json` est gardé
  en `.migrated`) ; le journal des gils (`GilJournal/`, un fichier par personnage, 90 jours et 20 000 entrées au plus) ; ce que Tensho a
  lu en dernier pour chacun des personnages du joueur (`Characters/`) ; la base locale de découvertes (`Discoveries/` : ce que le joueur
  a vu lui-même dans `Discoveries.json`, ce qu'il a importé dans `Shared/`, un fichier compressé par sorte, les prix vus au tableau des
  ventes dans `MarketSeen.json`, sans nom ni identifiant de vendeur, seulement si une offre est celle d'un servant du joueur, les
  dossiers `Import/` et `Export/`, et pour l'échange GitHub `github.json`, `community.sha256` et le `github.token` chiffré) ;
  `GameDataSnapshot.json` (la forme de 37 feuilles du jeu, pour lister ce qu'une mise à jour ajoute) ; `VoyageHistory.json`, `MyTools.json`, `CraftLists.json`, les exports des carnets (`Exports/`) ; en mode développeur,
  `SelfCheckSeen.json` et `PageSweepLast.txt`. La base de découvertes ne garde que des lignes du jeu et des lieux, jamais le nom, la
  position ou l'identifiant d'un autre joueur ; l'import fusionne les fichiers que le joueur place dans `Import/`, l'export écrit un
  fichier dans `Export/` ; seul l'échange GitHub ci-dessus envoie quelque chose. Les traducteurs peuvent poser des fichiers de langue JSON à côté de la DLL ; ils sont validés au rechargement.
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
