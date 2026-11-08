# Tensho Assistant

> *Traduction française plus bas.*

A new Dalamud plugin that brings the useful features of 17 plugins together in one common interface. The all-in-one shares services,
models, caches, search, diagnostics and data; it does not set 17 clones side by side.

The authoritative scope is in [ROADMAP.md](ROADMAP.md): Avant-Garde, Blue Mage Helper, Character Panel Refined, Collections, Craftimizer,
Duty Loot Preview, ezMiniCactpot, GatherBuddy, Item Use, LazyGatherer, Macro Chain, Mappy, Monster Loot Hunter, Price Insight,
QuestTracker, SortaKinda and TriadBuddy. None of them may be removed silently.

Domains: items/recipes/crafting; inventory/rules; gathering/fishing/maps; collections/quests/sources; market; statistics; mini-games;
macros.

Initial priorities: A Craftimizer/GatherBuddy/SortaKinda; B Collections/QuestTracker/Monster Loot Hunter/Price Insight; C Avant-Garde/Blue
Mage Helper/Duty Loot Preview/Item Use/Mappy/TriadBuddy; D Character Panel Refined/ezMiniCactpot/LazyGatherer/Macro Chain.

Architecture: a thin Dalamud adapter, shared managed data and domain services added in small useful slices. First chain aimed at: recipe →
ingredients → inventory → missing items → sources → gathering → map → availability → price. Code map: [ARCHITECTURE.md](ARCHITECTURE.md).

Source: the master prompt given on 2026-10-27, kept in the agent's files (`docs/ai/PROMPT-MAITRE.md`, outside the repository). Inherited
references give technical facts, never an old roadmap.

Out of scope (2026-10-31): ACT/IINACT and combat parsers ([DEC-009](DECISIONS.md)); any game automation or any advantage in combat or PvP
(Dalamud rules, see [CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)). Exception of 2026-11-04: Beastmaster, information only,
PvE only ([DEC-032](DECISIONS.md)); game advice in PvE only ([DEC-033](DECISIONS.md)). The whole plugin stops in PvP and ranked content
([DEC-038](DECISIONS.md)). No submission to the official Dalamud repository, but its rules are kept ([DEC-039](DECISIONS.md),
[PUBLISHING](PUBLISHING.md)).

## Extensions asked for by the user

The requests of [USER-REQUESTS](USER-REQUESTS.md) and the [global plan](GLOBAL-PLAN.md) add domains outside the 17 plugins, without
removing any. In the code today (version 0.971, 2026-11-09), they are these pages of the navigation (`MainWindow.Pages.cs`), besides the
pages of the 17 plugins:

| Group | Pages added |
|---|---|
| Items | Storage, Lexicon |
| Production | Ocean fishing, Island sanctuary, Custom deliveries |
| World | Weather, FATEs, Alarms, Reminders, Garden, Free company, NPC vendors, Discoveries |
| Progression | Events, Masked Carnivale, Relics, Allied societies, Grand Companies, Special deliveries, Eureka and Bozja, Deep dungeons, Gear, Chocobo, Guildleves, Hunting log, Beastmaster |
| Preferences and top of the list | Help, Credits, My tools |

The first of these requests, as they were planned:

| Request | Domain | Where in the code |
|---|---|---|
| R-07 | Free company: vessels read in the workshop, best submarine routes, builds | Free company page (vessels, best routes, builds, airship sectors and planner, voyage history) |
| R-08 | Reminders: timers of the game's calendar, alarm icon in the server info bar | Reminders page; server info bar entries (`Alarms/*DtrEntry.Dalamud.cs`) |
| R-09 | Seasonal events | Events page |
| R-11 | Gear: planner in the style of the Character window, glamour, materia, sources and ownership, try on, sharing; the same style for vessels | Gear page; vessel builds; chocobo barding (Chocobo page) |

Whether each one is validated in game: the boxes of [TESTING](TESTING.md) and the [global plan](GLOBAL-PLAN.md). Fixes and interface
requests (R-01 to R-06, R-10) are followed in [USER-REQUESTS](USER-REQUESTS.md). Order of item sources: game data first
([DEC-011](DECISIONS.md)).

---

# Tensho Assistant (français)

Plugin Dalamud neuf réunissant les fonctions utiles de 17 plugins dans une interface commune. Le tout-en-un partage services, modèles,
caches, recherche, diagnostics et données ; il ne juxtapose pas 17 clones.

Le périmètre qui fait foi est dans [ROADMAP.md](ROADMAP.md) : Avant-Garde, Blue Mage Helper, Character Panel Refined, Collections,
Craftimizer, Duty Loot Preview, ezMiniCactpot, GatherBuddy, Item Use, LazyGatherer, Macro Chain, Mappy, Monster Loot Hunter, Price
Insight, QuestTracker, SortaKinda et TriadBuddy. Aucun ne peut être supprimé silencieusement.

Domaines : objets/recettes/artisanat ; inventaire/règles ; récolte/pêche/cartes ; collections/quêtes/sources ; marché ; statistiques ;
mini-jeux ; macros.

Priorités initiales : A Craftimizer/GatherBuddy/SortaKinda ; B Collections/QuestTracker/Monster Loot Hunter/Price Insight ; C
Avant-Garde/Blue Mage Helper/Duty Loot Preview/Item Use/Mappy/TriadBuddy ; D Character Panel Refined/ezMiniCactpot/LazyGatherer/Macro
Chain.

Architecture : un adaptateur Dalamud fin, des données managées communes et des services métier ajoutés par petites tranches utiles.
Première chaîne visée : recette → ingrédients → inventaire → manquants → sources → récolte → carte → disponibilité → prix. Carte du code :
[ARCHITECTURE.md](ARCHITECTURE.md).

Source : prompt maître fourni le 2026-10-27, conservé dans les fichiers de l'agent (`docs/ai/PROMPT-MAITRE.md`, hors du dépôt). Les
références héritées fournissent des faits techniques, jamais une ancienne feuille de route.

Hors périmètre (2026-10-31) : ACT/IINACT et parsers de combat ([DEC-009](DECISIONS.md)) ; toute automatisation de jeu ou tout avantage en
combat/JcJ (règles Dalamud, voir [CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)). Exception du 2026-11-04 : dresseur,
informations seulement, JcE seulement ([DEC-032](DECISIONS.md)) ; conseils de jeu en JcE seulement ([DEC-033](DECISIONS.md)). Le plugin
entier s'arrête en JcJ et en contenu classé ([DEC-038](DECISIONS.md)). Pas de soumission au dépôt officiel Dalamud, mais ses règles sont
tenues ([DEC-039](DECISIONS.md), [PUBLISHING](PUBLISHING.md)).

## Extensions demandées par l'utilisateur

Les demandes de [USER-REQUESTS](USER-REQUESTS.md) et le [plan global](GLOBAL-PLAN.md) ajoutent des domaines hors des 17 plugins, sans en
retirer aucun. Dans le code aujourd'hui (version 0.971, 2026-11-09), ce sont ces pages de la navigation (`MainWindow.Pages.cs`), en plus
des pages des 17 plugins :

| Groupe | Pages ajoutées |
|---|---|
| Objets | Rangements, Lexique |
| Production | Pêche en mer, Félicité insulaire, Commandes spéciales |
| Monde | Météo, ALÉA, Alarmes, Rappels, Jardinage, Compagnie libre, PNJ vendeurs, Découvertes |
| Progression | Événements, La Grande Mascarade, Reliques, Peuples alliés, Grandes compagnies, Livraisons spéciales, Eureka et Bozja, Donjons profonds, Équipement, Chocobo, Mandats, Bestiaire, Dresseur |
| Préférences et haut de la liste | Aide, Crédits, Mes outils |

Les premières de ces demandes, telles qu'elles ont été prévues :

| Demande | Domaine | Où dans le code |
|---|---|---|
| R-07 | Compagnie libre : vaisseaux lus dans l'atelier, meilleurs trajets de sous-marin, montages | page Compagnie libre (vaisseaux, meilleurs trajets, montages, secteurs et planificateur des dirigeables, historique des voyages) |
| R-08 | Rappels : minuteurs du calendrier du jeu, icône d'alarme dans la barre d'infos du serveur | page Rappels ; entrées de la barre d'infos du serveur (`Alarms/*DtrEntry.Dalamud.cs`) |
| R-09 | Événements saisonniers | page Événements |
| R-11 | Équipement : planificateur façon fenêtre Personnage, mirage, matérias, sources et possession, essayage, partage ; même style pour les vaisseaux | page Équipement ; montages de vaisseaux ; barde de chocobo (page Chocobo) |

Validation en jeu de chacune : les cases de [TESTING](TESTING.md) et le [plan global](GLOBAL-PLAN.md). Les corrections et l'interface
(R-01 à R-06, R-10) sont suivies dans [USER-REQUESTS](USER-REQUESTS.md). Ordre des sources d'objets : données du jeu d'abord
([DEC-011](DECISIONS.md)).
