# Tensho Assistant

Plugin Dalamud neuf réunissant les fonctions utiles de 17 plugins dans une interface commune. Le All-in-One partage services, modèles, caches, recherche, diagnostics et données ; il ne juxtapose pas 17 clones.

Le périmètre autoritaire figure dans ROADMAP.md : Avant-Garde, Blue Mage Helper, Character Panel Refined, Collections, Craftimizer, Duty Loot Preview, ezMiniCactpot, GatherBuddy, Item Use, LazyGatherer, Macro Chain, Mappy, Monster Loot Hunter, Price Insight, QuestTracker, SortaKinda et TriadBuddy. Aucun ne peut être supprimé silencieusement.

Domaines : objets/recettes/artisanat ; inventaire/règles ; récolte/pêche/cartes ; collections/quêtes/sources ; marché ; statistiques ; mini-jeux ; macros.

Priorités initiales : A Craftimizer/GatherBuddy/SortaKinda ; B Collections/QuestTracker/Monster Loot Hunter/Price Insight ; C Avant-Garde/Blue Mage Helper/Duty Loot Preview/Item Use/Mappy/TriadBuddy ; D Character Panel Refined/ezMiniCactpot/LazyGatherer/Macro Chain.

Architecture : un adaptateur Dalamud fin, des données managées communes et des services métier ajoutés par petites tranches utiles. Première chaîne visée : recette → ingrédients → inventaire → manquants → sources → récolte → carte → disponibilité → prix.

Source : prompt maître fourni le 2026-10-27, conservé dans docs/ai/PROMPT-MAITRE.md. Les références héritées fournissent des faits techniques, jamais une ancienne roadmap.

Hors périmètre (2026-10-31) : ACT/IINACT et parsers de combat ([DEC-009](DECISIONS.md)) ; toute automatisation de jeu ou tout avantage en combat/JcJ (règles Dalamud, voir [CONTRIBUTING](../../CONTRIBUTING.md#publication-dalamud)).

## Extensions demandées par l'utilisateur (état au 2026-11-02, 0.315)

Les demandes [USER-REQUESTS](USER-REQUESTS.md) ajoutent des domaines hors des 17 plugins, sans en retirer aucun :

| Demande | Domaine | État dans le code | En jeu |
|---|---|---|---|
| R-07 | Compagnie libre : vaisseaux lus dans l'atelier, meilleurs trajets de sous-marin, montages | page Compagnie libre (0.307, 0.309, 0.315) ; pas de planificateur de trajets de dirigeable (aucune donnée sous licence compatible) | non validé |
| R-08 | Rappels : minuteurs du calendrier du jeu, icône d'alarme dans la barre du serveur | page Rappels (0.300–0.302, expéditions 0.307) ; reste escouade de Grande Compagnie, Planeurs d'Ehcatl | non validé |
| R-09 | Événements saisonniers | pas commencé | — |
| R-11 | Équipement : planificateur façon fenêtre Personnage, mirage, matérias, sources et possession, essayage, partage ; même style pour les vaisseaux | page Équipement (0.310–0.314) ; montages de vaisseaux (0.315) ; étape 4 à décider ([R-11](../goals/R-11.md)) | 0.310–0.314 validés ; 0.315 non validé |

Les corrections et l'interface (R-01 à R-06, R-10) sont suivies dans [USER-REQUESTS](USER-REQUESTS.md) et [R-06](../goals/R-06.md).
Couverture des 17 plugins : tableau de [STATUS](../ai/STATUS.md). Ordre des sources d'objets : données du jeu d'abord ([DEC-011](DECISIONS.md)).
