<!-- Draft prepared by the AI agent at the author's request (first written 2026-10-31, checked against the code on 2026-11-09, version 0.971).
No submission to the official repository is planned (DEC-039, 2026-11-05): this draft is kept in case the author changes his mind, and as a
summary of what the plugin does. The Dalamud AI policy asks that the AI disclosure and the pull request description be written by a human:
rewrite it in your own words before posting. Do not post it as it is.
Traduction française plus bas. -->

# Tensho Assistant

Tensho Assistant brings everyday tools together in one window (`/tensho`), with a skippable first-run guide and help on every page: items
and storage (bags, retainers, saddlebags, armoury chest, glamour dresser, armoire), inventory sorting rules, market listings and prices,
retainer sale prices compared on a click, a French–English lexicon of the game's words, recipes, a crafting simulator and solver, gathering
and fishing with Eorzea time windows and weather, ocean fishing, the island sanctuary, custom deliveries, a map that replaces the game map,
weather, FATEs, alarms and reminders, gardening, free company voyages and builds, NPC vendors, duty and monster loot, quests and rewards,
seasonal events, collections with how to obtain each item, Masked Carnivale, relic weapons, allied societies, Grand Companies, special
deliveries, Eureka and Bozja, deep dungeons, a gear planner (materia, try on, build sharing), the chocobo, guildleves, the hunting log,
Beastmaster information, Fashion Report hints, Mini Cactpot and Triple Triad helpers, macro chains, the player's own tools, and a local base
of what the player discovers while playing.

- Source: https://github.com/Shigu330/Tensho-Assistant (GPL-3.0)
- Game actions start on a click: teleport, applying a sort, macro chains, writing crafting macros into the game's macros, opening the Try
  On window, equipping a gearset, wearing a title, sending the hunt train call to the chat channel the player picks. Game alarms use the
  game's `/alarm` command, sent when the player adds an alarm with the « game alarms » setting on (off by default) or presses « Send »;
  automatic alarms never create one.
- One setting, off by default, acts without a click: wearing the title the player chose for a job when the job changes.
  <!-- Kept by the author's decision (DEC-035): explain it here in your own words. -->
- The whole plugin stops in PvP and ranked content, with no setting (DEC-038).
- No automation of crafting, dialogues or loot, no PvP feature, no parser. The solver and the suggested move only display advice. Triple
  Triad advice works against NPCs only, never in tournaments or against a player. The Beastmaster page lists game data. Nearby monsters
  (enemies the game shows as targetable, 100 at most) are used for information only: a mark on the Tensho map, and optionally an icon
  above a monster's head in the game view, for the player's lists, hunting log, blue magic, Beastmaster, collections or FATEs.
- Network: Universalis (world + item id), Garland Tools, FFXIV Console Games Wiki, Teamcraft's public bite time reports, PaissaDB (open
  plots, on a click or with its optional watch), data files hosted on GitHub (LuminaSupplemental, GatherBuddy, FFXIVGachaSpreadsheet) and
  this repository (community tool gallery and help videos, on a click), over HTTPS, only when the matching page is opened, an item is
  hovered, the player clicks or a setting asks for it, with bounded responses, cached on disk. Each data source has a switch, and one box
  turns them all off. No player data is sent, no telemetry.
- Discord (optional, off by default): Tensho alarms, currency cap alerts and « duty ready » alerts can be posted to a Discord webhook the
  player enters (Discord hosts only, HTTPS POST). Only the message is sent, plus the character's name if the player enables it; the
  address is never logged.
- Reporting a problem opens a prefilled GitHub form in the player's browser; the plugin sends nothing.
- The local discoveries base stays on the player's disk: game rows and places only (monsters' places, item sources, fish caught…), never
  another player's name, position or id; import and export are files the player handles. Sharing is optional: only after the player
  connects GitHub (a code approved on github.com, no password in the game), the exchange sends the player's own sightings, dated to the
  day only, to a secret gist of theirs, with one issue in this repository named by their GitHub login; the token stays on the PC,
  encrypted for the Windows user.
- NuGet dependency: Raphael.Net 5.0.0 (MIT, native solver library), the version Craftimizer uses.
- Embedded community data, licences kept in the files: SubmarineTracker and FFXIVGachaSpreadsheet (MIT), LuminaSupplemental airship and
  festival tables (GPL-3.0), Character Panel Refined level table (MIT), FFXIV Gardening Tracker (GPL-3.0), FFXIV Crossbreed Helper (MIT).
  Every source is on the Credits page.
- Only the local character's content id is used, as a key for its own saved data.
- Every window uses the Dalamud window system, panels beside game windows included. The only `ImGui.Begin` left: invisible click zones over
  the Fashion Report slots (no background, no border). Drawings over the game: Mini Cactpot and Triple Triad advice (foreground), frames on
  the gathering window rows and icons above monsters (background).

## AI disclosure

- Code: **Auto**. The plugin was written by an AI coding agent (Claude). I define the features, check versions in game and report
  what works or not; the agent proves game data links with read-only probes (kept in `docs/evidence/`).
- Icon: generated by the AI agent (drawn with code), declared in the plugin description.
- Translations: English and French; German and Japanese only have 35 keys, not reviewed by native speakers.

---

<!-- Traduction française du brouillon, pour l'auteur. -->

# Tensho Assistant (traduction)

Tensho Assistant réunit des outils de tous les jours dans une seule fenêtre (`/tensho`), avec un guide de première prise en main que l'on
peut passer et une aide sur chaque page : objets et rangements (sacs, servants, sacoches, arsenal, coffre à apparence, armoire), règles
de tri de l'inventaire, annonces et prix du marché, prix de vente des servants comparés sur un clic, un lexique français–anglais des mots
du jeu, recettes, simulateur et solveur d'artisanat, récolte et pêche avec les créneaux en heure d'Éorzéa et la météo, pêche en mer,
Félicité insulaire, commandes spéciales, une carte qui remplace celle du jeu, météo, ALEA, alarmes et rappels, jardinage, voyages et
montages de la compagnie libre, PNJ vendeurs, butin des contenus et des monstres, quêtes et récompenses, événements saisonniers,
collections avec la façon d'obtenir chaque objet, Grande Mascarade, armes reliques, peuples alliés, Grandes compagnies, livraisons
spéciales, Eureka et Bozja, donjons profonds, un planificateur d'équipement (matérias, essayage, partage de montage), le chocobo, les
mandats, le bestiaire, des informations pour le dresseur, des indices de la Revue de mode, des aides au Mini Cactpot et à la Triple
Triade, des chaînes de macros, les outils du joueur, et une base locale de ce que le joueur découvre en jouant.

- Source : https://github.com/Shigu330/Tensho-Assistant (GPL-3.0)
- Les actions dans le jeu partent d'un clic : téléportation, application d'un tri, chaînes de macros, écriture des macros d'artisanat
  dans les macros du jeu, ouverture de la cabine d'essayage, équiper un ensemble d'équipement, porter un titre, envoyer l'appel du train
  de chasse dans le canal choisi par le joueur. Les alarmes du jeu utilisent la commande `/alarm` du jeu, envoyée quand le joueur ajoute
  une alarme avec le réglage « alarmes du jeu » actif (désactivé par défaut) ou appuie sur « Envoyer » ; les alarmes automatiques n'en
  créent jamais.
- Un réglage, désactivé par défaut, agit sans clic : porter le titre choisi par le joueur pour un job quand le job change.
  <!-- Gardé par décision de l'auteur (DEC-035) : l'expliquer ici avec vos mots. -->
- Le plugin entier s'arrête en JcJ et en contenu classé, sans réglage (DEC-038).
- Aucune automatisation de l'artisanat, des dialogues ou du butin, aucune fonction JcJ, aucun parser. Le solveur et le coup conseillé ne
  font qu'afficher un conseil. Le conseil de Triple Triade ne marche que contre les PNJ, jamais en tournoi ni contre un joueur. La page du
  dresseur liste des données du jeu. Les monstres proches (ennemis que le jeu montre comme ciblables, 100 au plus) ne servent qu'à
  informer : une marque sur la carte Tensho, et au choix une icône au-dessus de la tête d'un monstre dans la vue du jeu, pour les listes
  du joueur, le bestiaire, la magie bleue, le dresseur, les collections ou les ALEA.
- Réseau : Universalis (monde + numéro d'objet), Garland Tools, FFXIV Console Games Wiki, rapports publics de temps de touche de Teamcraft,
  PaissaDB (terrains libres, sur un clic ou avec sa surveillance facultative), fichiers de données hébergés sur GitHub (LuminaSupplemental,
  GatherBuddy, FFXIVGachaSpreadsheet) et ce dépôt (galerie d'outils de la communauté et vidéos d'aide, sur un clic), en HTTPS, seulement
  quand la page concernée est ouverte, qu'un objet est survolé, que le joueur clique ou qu'un réglage le demande, avec des réponses bornées
  et gardées en cache sur disque. Chaque source de données a un interrupteur, et une case les coupe toutes. Aucune donnée du joueur n'est
  envoyée, aucune télémétrie.
- Discord (facultatif, désactivé par défaut) : les alarmes Tensho, les alertes de plafond de monnaie et les alertes « mission prête »
  peuvent être envoyées à un webhook Discord saisi par le joueur (hôtes de Discord seulement, HTTPS POST). Seul le message est envoyé,
  plus le nom du personnage si le joueur l'active ; l'adresse n'est jamais journalisée.
- Signaler un problème ouvre un formulaire GitHub prérempli dans le navigateur du joueur ; le plugin n'envoie rien.
- La base locale de découvertes reste sur le disque du joueur : des lignes du jeu et des lieux seulement (lieux des monstres, sources des
  objets, poissons pris…), jamais le nom, la position ou l'identifiant d'un autre joueur ; import et export sont des fichiers que le joueur
  manipule. Le partage est facultatif : seulement après que le joueur a connecté GitHub (un code approuvé sur github.com, aucun mot de
  passe dans le jeu), l'échange envoie ce que le joueur a vu lui-même, daté du jour seulement, dans un gist secret à son nom, avec un
  ticket dans ce dépôt au nom de son identifiant GitHub ; le jeton reste sur le PC, chiffré pour l'utilisateur Windows.
- Dépendance NuGet : Raphael.Net 5.0.0 (MIT, bibliothèque native de solveur), la version qu'utilise Craftimizer.
- Données de la communauté embarquées, licences gardées dans les fichiers : SubmarineTracker et FFXIVGachaSpreadsheet (MIT), tables des
  dirigeables et des fêtes de LuminaSupplemental (GPL-3.0), table de niveaux de Character Panel Refined (MIT), FFXIV Gardening Tracker
  (GPL-3.0), FFXIV Crossbreed Helper (MIT). Chaque source est sur la page Crédits.
- Seul l'identifiant de contenu du personnage local sert, comme clé de ses propres données enregistrées.
- Chaque fenêtre utilise le système de fenêtres de Dalamud, panneaux à côté des fenêtres du jeu compris. Le seul `ImGui.Begin` restant :
  des zones de clic invisibles sur les emplacements de la Revue de mode (sans fond ni bord). Dessins par-dessus le jeu : conseils du Mini
  Cactpot et de la Triple Triade (premier plan), cadres sur les lignes de la fenêtre de récolte et icônes au-dessus des monstres
  (arrière-plan).

## Déclaration d'usage de l'IA

- Code : **Auto**. Le plugin a été écrit par un agent de code IA (Claude). Je définis les fonctions, vérifie les versions en jeu et dis ce
  qui marche ou non ; l'agent prouve les liens avec les données du jeu par des sondes en lecture seule (gardées dans `docs/evidence/`).
- Icône : générée par l'agent IA (dessinée par du code), déclarée dans la description du plugin.
- Traductions : anglais et français ; l'allemand et le japonais n'ont que 35 clés, non relues par des locuteurs natifs.
