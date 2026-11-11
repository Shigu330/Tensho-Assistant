# Proposals and provisional choices of step g) — to review together

> *Traduction française plus bas.*

This document is kept during the development of step g) (the gaps of GAPS.md). It records the ideas spotted while coding and the
choices made provisionally, to review them together with the user (step h and the decisions at the end). What has been decided since is marked
as such (DEC-026 and later); the rest is still open.
The provisional choices are also recalled in the [ROADMAP](ROADMAP.md), "Decisions postponed to the end".
Reread on 2026-11-03 (0.446): an alternative done since then is marked "DONE x.y" in its column; what remains is the choice itself to confirm.
**DECIDED 2026-11-03 (DEC-026)**: all the provisional choices below are confirmed by the user, except X-13 (sending to the chat on click
added in 0.447); their alternatives remain ideas. X-04: simple link (0.447). Gaps: A-03 / A-04 studied in session; N-06 and N-14:
community source to look for, licence first; N-08: journal with typed durations (0.448).

## Decisions

<!-- gen:decisions:g-en -->
The choices noted in this file are settled: DEC-023, DEC-026, DEC-032, DEC-033, DEC-035, DEC-045, DEC-048 ([DECISIONS](DECISIONS.md)). Original text of the sections: [archive](archive/DECISIONS-EN-ATTENTE-2026-11-09.md). What remains open is listed in the [ROADMAP](ROADMAP.md), "To decide / to review".
<!-- /gen:decisions:g-en -->

## Ideas spotted while coding (not done)

| Spotted in | Idea | Why not yet |
|---|---|---|
| 0.344 | Alert on the weekly cap of tomestones (in addition to the total cap) | Outside the user's choice for I-02 |
| 0.345 | "Where is this item?" on all characters (tooltip and item sheet), by extending ItemHoldings | Big change to the stocks; to do after in-game validation of I-03 |
| 0.345 | Retainers' gil in the characters view | The game only gives it when the bell is opened; to probe |
| 0.347 | List of all annotated items (Items page) with a filter by label | Small; to do with the other item lists (I-12) |
| 0.346 | Undercutting: also show the recent average sale price before pasting (safeguard against an offer that is too low) | Needs one more Universalis call |
| 0.345 | Only rewrite a character's file if its quantities changed | Saves disk writes; today one write every 2 minutes per storage read automatically |

Triage of the proposals of the four audits (2026-11-05): [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md).
Review of 2026-11-05 with the user: [global plan](GLOBAL-PLAN.md). I-15, N-13 and X-14 are set aside; N-10 is taken up again.

## Gaps left for a session with the user

| Gap | Reason |
|---|---|
| I-05 (retainers' market announcements) | First version DONE 0.508, to try in game; the rest below |
| A-03, A-04 (Raphael solver and adaptive suggestions) | A-03 DONE 0.465 (DEC-028, as an option). A-04 DONE 0.509, to try in game |
| N-06 (squadron, composition of missions) | DONE 0.451 (DEC-027): planner from the game sheets and GcArmyManager; percentage "≈" until the in-game probe (TESTING 0.451); affinities not counted ([evidence](../evidence/0.448-n06-n14-sources.md)) |
| N-07 (island sanctuary, map of resources and materials) | Map done in 0.667 (unit of the positions to compare on the island); materials of the creations and of an agenda done in 0.673, without your stocks ([evidence](../evidence/0.673-island-materials.md)) |
| N-10 (island workshop, solver) | Big: reading the game's supply and demand and a solver; to do with the user, see below |
| N-04 (housing lottery) | State of the player's entries: structure of the game not found in FFXIVClientStructs (to probe in game) |
| N-12 (spawn conditions of the S ranks), N-13 (visible furniture), X-14 (housing tools) | Community rules per mark, reading of the furniture and display fixes: sources and risks to decide together |
| N-08 (gardening journal) | DONE 0.448 (decided on 2026-11-03, DEC-026): journal with durations typed by the player, Reminders page |
| N-14 (gardening crossbreeding planner) | DONE 0.450 (DEC-027): MIT pairs from nick75g linked to the items through the English Item sheet, displayed in the game's language ([evidence](../evidence/0.448-n06-n14-sources.md)) |
| I-15 (search in the game's inventory windows) | Touches the game's interface: to do with the user, see below |

## Gaps left for an in-game session (2026-11-04)

| Point | Why not autonomously | What would be needed |
|---|---|---|
| I-15, search in the inventory windows | Goes through native pointers on every frame, 15 unknowns (tab numbers, names of the retainers' grids, order of the slots). Coding blindly may crash the game | First a read-only probe, with the user in front of the windows |
| N-10, island workshop | Done without the game in 0.671 and 0.672, unknowns listed in `docs/evidence/0.671-island-workshop.md` | Tests and capture in front of the island's agenda; forecast of the week later |
| I-05, continued | Reading of the game's offers, view of all the retainers | After a try of 0.508 |
| Beastmaster, continued | Set aside by the user | Probe already in place (0.501) |

## Proposal X-100: regrouping of the tools and settings (2026-11-10) — **validated (DEC-048)**; no. 9: automatic off by default (DEC-048)

Finding (0.991): the navigation has 8 groups; "Progression" has 16 pages and "World" 11; the settings are in 7 tabs and also on the pages (MapView 92, Settings 44, Advanced 24, list panels 13…, 320 settings in all, docs/project/SETTINGS.md).

| Proposed group | Pages | Change |
|---|---|---|
| Items | Items, Storage, Sort, Market | Lexicon goes to "References" |
| Production | Crafting, Gathering, Ocean fishing, Island sanctuary, Macro chain | unchanged (without the deliveries) |
| World | Map, Weather, FATEs, Vendors, Duty loot, Monster loot, Discoveries | without the timers |
| Tracking (new) | Alarms, Reminders, Garden, Free company | the timers and reminders together |
| Progression | Quests, Festivals, Collections, Guildleves, Hunting log, Allied societies, Grand Companies, Deliveries (special and custom), Gear, Chocobo | 10 pages instead of 16; the two deliveries merged |
| Content (new, X-90) | Eureka/Bozja/Occult Crescent, Deep dungeons, Relics, Masked Carnivale, Beastmaster, later strategy boards | one family per content (X-92) |
| Gold Saucer | Fashion Report, Triple Triad, Mini Cactpot | unchanged |
| References (new) | Lexicon, Help, Credits | what one consults |
| Preferences | Settings, Diagnostics (developer) | Home and My tools stay at the top, outside any group |

Settings: each setting also stays next to its tool (as today), and the Settings page groups them by the same groups as the navigation:
General (language, theme, fonts, chat, sounds) · Panels beside the game's windows (gathering, crafting, collectable, retainers' sales, mission search, Fashion Report, Triple Triad, beastmaster board…) · Map · Tracking and notifications (alarms, reminders, Discord, server bar: currencies, gil) · Data and sources (community sources, cache, discoveries) · Advanced · Debug.
Plus a search field at the top of the Settings that finds any setting by its name (all 320) and opens its tab.

## Proposal 11 (X-40): nearby monsters, other uses — **decided (DEC-049)**, coded in 0.1002

| No. | Proposal | Alternatives |
|---|---|---|
| 11 | Nearby monsters, other uses (X-40); today: your lists, hunting log, FATEs, blue mage, missing collections, beastmaster, guildleve targets | Add (each one a layer of the map, ticked in Settings › Map and on its page): a) the monster drops an ingredient of one of your crafting lists (loot seen in game, then wiki); b) target of a relic book in progress (Zodiac: "defeat N …"); c) the monster drops an item from your wishes | Only a); or nothing |

---

# Propositions et choix provisoires de l'étape g) — à revoir ensemble

Ce document est tenu pendant le développement de l'étape g) (les écarts de GAPS.md). On y note les idées repérées en codant et les
choix faits à titre provisoire, pour les revoir ensemble avec l'utilisateur (étape h et décisions de la fin). Ce qui a été décidé
depuis est marqué comme tel (DEC-026 et suivantes) ; le reste est encore ouvert. Les choix provisoires sont aussi rappelés dans la [ROADMAP](ROADMAP.md), « Décisions reportées à la fin ».
Relu le 2026-11-03 (0.446) : une alternative faite depuis est marquée « FAIT x.y » dans sa colonne ; il reste alors le choix lui-même à confirmer.
**DÉCIDÉ 2026-11-03 (DEC-026)** : tous les choix provisoires ci-dessous sont confirmés par l'utilisateur, sauf X-13 (envoi au chat sur clic
ajouté en 0.447) ; leurs alternatives restent des idées. X-04 : simple lien (0.447). Écarts : A-03 / A-04 étudiés en séance ; N-06 et N-14 :
source communautaire à chercher, licence d'abord ; N-08 : journal avec durées tapées (0.448).

## Décisions

<!-- gen:decisions:g -->
Les choix notés dans ce fichier sont tranchés : DEC-023, DEC-026, DEC-032, DEC-033, DEC-035, DEC-045, DEC-048 ([DECISIONS](DECISIONS.md)). Texte d'origine des sections : [archive](archive/DECISIONS-EN-ATTENTE-2026-11-09.md). Ce qui reste ouvert est listé dans [ROADMAP](ROADMAP.md), « À décider / à revoir ».
<!-- /gen:decisions:g -->

## Idées repérées en codant (non faites)

| Repérée en | Idée | Pourquoi pas encore |
|---|---|---|
| 0.344 | Alerte sur le plafond hebdomadaire des mémoquartz (en plus du plafond total) | Hors du choix de l'utilisateur pour I-02 |
| 0.345 | « Où est cet objet ? » sur tous les personnages (infobulle et fiche d'objet), en étendant ItemHoldings | Gros changement des stocks ; à faire après validation en jeu de I-03 |
| 0.345 | Gils des servants dans la vue des personnages | Le jeu ne les donne qu'à l'ouverture de la cloche ; à sonder |
| 0.347 | Liste de tous les objets annotés (page Objets) avec filtre par étiquette | Petit ; à faire avec les autres listes d'objets (I-12) |
| 0.346 | Sous-cotation : afficher aussi le prix moyen de vente récent avant de coller (garde-fou contre une offre trop basse) | Demande un appel Universalis de plus |
| 0.345 | Ne réécrire le fichier d'un personnage que si ses quantités ont changé | Économie d'écritures disque ; aujourd'hui une écriture toutes les 2 minutes par rangement relevé d'office |

Tri des propositions des quatre audits (2026-11-05) : [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md).
Revue du 2026-11-05 avec l'utilisateur : [plan global](GLOBAL-PLAN.md). I-15, N-13 et X-14 sont écartés ; N-10 est repris.

## Écarts laissés pour une séance avec l'utilisateur

| Écart | Raison |
|---|---|
| I-05 (annonces des servants au marché) | Première version FAITE 0.508, à essayer en jeu ; la suite plus bas |
| A-03, A-04 (solveur Raphael et suggestions adaptatives) | A-03 FAIT 0.465 (DEC-028, en option). A-04 FAIT 0.509, à essayer en jeu |
| N-06 (escouade, composition des missions) | FAIT 0.451 (DEC-027) : planificateur depuis les feuilles du jeu et GcArmyManager ; pourcentage « ≈ » jusqu'à la sonde en jeu (TESTING 0.451) ; affinités non comptées ([preuve](../evidence/0.448-n06-n14-sources.md)) |
| N-07 (île sanctuaire, carte des ressources et matériaux) | Carte faite en 0.667 (unité des positions à comparer sur l'île) ; matériaux des créations et d'un agenda faits en 0.673, sans vos stocks ([preuve](../evidence/0.673-island-materials.md)) |
| N-10 (atelier de l'île, solveur) | Gros : lecture de l'offre et de la demande du jeu et solveur ; à faire avec l'utilisateur, voir plus bas |
| N-04 (loterie immobilière) | État des mises du joueur : structure du jeu non trouvée dans FFXIVClientStructs (à sonder en jeu) |
| N-12 (conditions d'apparition des rangs S), N-13 (meubles visibles), X-14 (outils d'aménagement) | Règles communautaires par marque, lecture des meubles et correctifs d'affichage : sources et risques à décider ensemble |
| N-08 (journal de jardinage) | FAIT 0.448 (décidé le 2026-11-03, DEC-026) : journal avec durées tapées par le joueur, page Rappels |
| N-14 (planificateur de croisements de jardinage) | FAIT 0.450 (DEC-027) : paires MIT de nick75g reliées aux objets par la feuille Item anglaise, affichées dans la langue du jeu ([preuve](../evidence/0.448-n06-n14-sources.md)) |
| I-15 (recherche dans les fenêtres d'inventaire du jeu) | Touche l'interface du jeu : à faire avec l'utilisateur, voir plus bas |

## Écarts laissés pour une séance en jeu (2026-11-04)

| Point | Pourquoi pas en autonomie | Ce qu'il faudrait |
|---|---|---|
| I-15, recherche dans les fenêtres d'inventaire | Passe sur des pointeurs natifs à chaque image, 15 inconnues (numéros d'onglets, noms des grilles de servants, ordre des cases). Coder à l'aveugle peut faire planter le jeu | D'abord une sonde en lecture seule, avec l'utilisateur devant les fenêtres |
| N-10, atelier de l'île | Fait sans le jeu en 0.671 et 0.672, inconnues listées dans `docs/evidence/0.671-island-workshop.md` | Essais et capture devant l'agenda de l'île ; prévision de la semaine plus tard |
| I-05, suite | Relevé des offres du jeu, vue de tous les servants | Après un essai de la 0.508 |
| Dresseur, suite | Mis de côté par l'utilisateur | Sonde déjà en place (0.501) |

## Proposition X-100 : regroupement des outils et des réglages (2026-11-10) — **validée (DEC-048)** ; n° 9 : automatique coupé par défaut (DEC-048)

Constat (0.991) : la navigation a 8 groupes ; « Progression » compte 16 pages et « Monde » 11 ; les réglages sont dans 7 onglets et aussi sur les pages (MapView 92, Paramètres 44, Avancé 24, panneaux des listes 13…, 320 réglages au total, docs/project/SETTINGS.md).

| Groupe proposé | Pages | Changement |
|---|---|---|
| Objets | Objets, Rangements, Tri, Marché | Lexique part dans « Références » |
| Production | Artisanat, Récolte, Pêche en mer, Île sanctuaire, Enchaînement de macros | inchangé (sans les livraisons) |
| Monde | Carte, Météo, ALÉA, Marchands, Butin des contenus, Butin des monstres, Découvertes | sans les minuteurs |
| Suivi (nouveau) | Alarmes, Rappels, Jardin, Compagnie libre | les minuteurs et rappels ensemble |
| Progression | Quêtes, Fêtes, Collections, Mandats, Carnet de chasse, Peuples alliés, Grandes compagnies, Livraisons (spéciales et sur mesure), Équipement, Chocobo | 10 pages au lieu de 16 ; les deux livraisons réunies |
| Contenus (nouveau, X-90) | Eurêka/Bozja/Croissant, Donjons profonds, Reliques, Carnaval masqué, Dresseur, plus tard tableaux stratégiques | une famille par contenu (X-92) |
| Gold Saucer | Revue de mode, Triple Triade, Mini Cactpot | inchangé |
| Références (nouveau) | Lexique, Aide, Crédits | ce qu'on consulte |
| Préférences | Paramètres, Diagnostics (développeur) | Accueil et Mes outils restent en tête, hors groupe |

Réglages : chaque réglage reste aussi à côté de son outil (comme aujourd'hui), et la page Paramètres les regroupe par les mêmes groupes que la navigation :
Général (langue, thème, polices, discussion, sons) · Panneaux à côté des fenêtres du jeu (récolte, artisanat, collectionnable, ventes des servants, recherche de mission, revue de mode, Triple Triade, plateau du dresseur…) · Carte · Suivi et notifications (alarmes, rappels, Discord, barre du serveur : monnaies, gils) · Données et sources (sources communautaires, cache, découvertes) · Avancé · Débogage.
Plus un champ de recherche en haut des Paramètres qui trouve n'importe quel réglage par son nom (les 320) et ouvre son onglet.

## Proposition 11 (X-40) : monstres proches, autres usages — **décidée (DEC-049)**, codée en 0.1002

| N° | Proposition | Autres choix |
|---|---|---|
| 11 | Monstres proches, autres usages (X-40) ; aujourd'hui : vos listes, carnet de chasse, ALÉA, mage bleu, collections manquantes, dresseur, cibles de mandat | Ajouter (chacun une couche de la carte, cochée dans Paramètres › Carte et sur sa page) : a) le monstre fait tomber un ingrédient d'une de vos listes d'artisanat (butin vu en jeu, puis wiki) ; b) cible d'un livre de relique en cours (Zodiaque : « vaincre N … ») ; c) le monstre fait tomber un objet de vos souhaits | Seulement a) ; ou rien |
