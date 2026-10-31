# Tensho Assistant

Plugin Dalamud neuf réunissant les fonctions utiles de 17 plugins dans une interface commune. Le All-in-One partage services, modèles, caches, recherche, diagnostics et données ; il ne juxtapose pas 17 clones.

Le périmètre autoritaire figure dans ROADMAP.md : Avant-Garde, Blue Mage Helper, Character Panel Refined, Collections, Craftimizer, Duty Loot Preview, ezMiniCactpot, GatherBuddy, Item Use, LazyGatherer, Macro Chain, Mappy, Monster Loot Hunter, Price Insight, QuestTracker, SortaKinda et TriadBuddy. Aucun ne peut être supprimé silencieusement.

Domaines : objets/recettes/artisanat ; inventaire/règles ; récolte/pêche/cartes ; collections/quêtes/sources ; marché ; statistiques ; mini-jeux ; macros.

Priorités initiales : A Craftimizer/GatherBuddy/SortaKinda ; B Collections/QuestTracker/Monster Loot Hunter/Price Insight ; C Avant-Garde/Blue Mage Helper/Duty Loot Preview/Item Use/Mappy/TriadBuddy ; D Character Panel Refined/ezMiniCactpot/LazyGatherer/Macro Chain.

Architecture : un adaptateur Dalamud fin, des données managées communes et des services métier ajoutés par petites tranches utiles. Première chaîne visée : recette → ingrédients → inventaire → manquants → sources → récolte → carte → disponibilité → prix.

Source : prompt maître fourni le 2026-10-27, conservé dans docs/ai/PROMPT-MAITRE.md. Les références héritées fournissent des faits techniques, jamais une ancienne roadmap.

Hors périmètre (2026-10-31) : ACT/IINACT et parsers de combat ([DEC-009](DECISIONS.md)) ; toute automatisation de jeu ou tout avantage en combat/JcJ (règles Dalamud, voir [CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)).
