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

Source: the master prompt given on 2026-10-27, kept in the maintainer's notes (outside the repository); a historical source since
2026-11-09: the user's decisions and [DECISIONS](DECISIONS.md) prevail. Inherited
references give technical facts, never an old roadmap.

Out of scope (2026-10-31): ACT/IINACT and combat parsers ([DEC-009](DECISIONS.md)); any game automation or any advantage in combat or PvP
(Dalamud rules, see [CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)). Exception of 2026-11-04: Beastmaster, information only,
PvE only ([DEC-032](DECISIONS.md)); game advice in PvE only ([DEC-033](DECISIONS.md)). The whole plugin stops in PvP and ranked content
([DEC-038](DECISIONS.md)). No submission to the official Dalamud repository, but its rules are kept ([DEC-039](DECISIONS.md),
[PUBLISHING](PUBLISHING.md)).

## Extensions asked for by the user

The user requests (R-01 to R-14, table of [ROADMAP](ROADMAP.md)) and the [global plan](GLOBAL-PLAN.md) add domains outside the 17 plugins, without
removing any. Every page of the code, by group, is in the table of the [README](../../README.md) (« Overview »), written from the page list
(`MainWindow.Pages.cs`) by `tools/docgen.py` at every version.

The first of these requests (R-07 free company, R-08 reminders, R-09 seasonal events, R-11 gear) are in the table of [ROADMAP](ROADMAP.md), with the version that did each one.

Whether each one is validated in game: the boxes of [TESTING](TESTING.md) and the [global plan](GLOBAL-PLAN.md). Fixes and interface
requests (R-01 to R-06, R-10) are followed in the same table of [ROADMAP](ROADMAP.md). Order of item sources: game data first
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

Source : prompt maître fourni le 2026-10-27, conservé dans les notes du mainteneur (hors du dépôt) ; source historique depuis le
2026-11-09 : les décisions de l'utilisateur et [DECISIONS](DECISIONS.md) priment. Les
références héritées fournissent des faits techniques, jamais une ancienne feuille de route.

Hors périmètre (2026-10-31) : ACT/IINACT et parsers de combat ([DEC-009](DECISIONS.md)) ; toute automatisation de jeu ou tout avantage en
combat/JcJ (règles Dalamud, voir [CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)). Exception du 2026-11-04 : dresseur,
informations seulement, JcE seulement ([DEC-032](DECISIONS.md)) ; conseils de jeu en JcE seulement ([DEC-033](DECISIONS.md)). Le plugin
entier s'arrête en JcJ et en contenu classé ([DEC-038](DECISIONS.md)). Pas de soumission au dépôt officiel Dalamud, mais ses règles sont
tenues ([DEC-039](DECISIONS.md), [PUBLISHING](PUBLISHING.md)).

## Extensions demandées par l'utilisateur

Les demandes de l'utilisateur (R-01 à R-14, tableau de la [ROADMAP](ROADMAP.md)) et le [plan global](GLOBAL-PLAN.md) ajoutent des domaines hors des 17 plugins, sans en
retirer aucun. Toutes les pages du code, par groupe, sont dans le tableau du [README](../../README.md) (« Vue d'ensemble »), écrit depuis la liste
des pages (`MainWindow.Pages.cs`) par `tools/docgen.py` à chaque version.

Les premières de ces demandes (R-07 compagnie libre, R-08 rappels, R-09 événements saisonniers, R-11 équipement) sont dans le tableau de la [ROADMAP](ROADMAP.md), avec la version qui a fait chacune.

Validation en jeu de chacune : les cases de [TESTING](TESTING.md) et le [plan global](GLOBAL-PLAN.md). Les corrections et l'interface
(R-01 à R-06, R-10) sont suivies dans le même tableau de la [ROADMAP](ROADMAP.md). Ordre des sources d'objets : données du jeu d'abord
([DEC-011](DECISIONS.md)).
