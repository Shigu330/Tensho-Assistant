# DUTY-001 — Étude des sources, 2026-10-28

MISE À JOUR 0.32.0 : modèle, service, catalogue et UI sont maintenant intégrés/testés ; [DUTY-001.md](DUTY-001.md) fait autorité pour l'état courant. Les paragraphes CORE_PARTIAL et prochaine tranche ci-dessous conservent l'état de l'étude 0.31.1.

État : CORE_PARTIAL, modèle/parser managé original implémenté en 0.31.1.0, 181 tests/package verts. Service réseau et UI non commencés ; aucun outil rendu disponible dans l'interface.

Parser : maximum 1 Mio UTF-8, profondeur 32, 4 096 sources par catégorie et 16 384 entrées distinctes, identité InstanceContent attendue obligatoire. Rewards/fights/coffers séparés, doublons retirés seulement dans une même source, références d'objets inconnues conservées. Catégories absentes distinguées de tableaux vides ; exhaustivité toujours fausse. Dates de récupération en UTC, aucune date de mise à jour inventée. Entrées mal formées rejetées et annulation observée. Aucun appel réseau ajouté par cette tranche.

## Preuve locale

Les métadonnées Lumina installées exposent `InstanceContent.InstanceContentRewardItem`. La feuille correspondante ne contient que `Unknown0` et `Unknown1` (sbyte), sans référence Item interprétée. `Treasure` expose des textes/champs inconnus et un modèle SGB, pas une liste typée d'objets. Ce constat porte sur les feuilles examinées ; il ne prouve pas l'absence de toute information de butin dans tous les fichiers du client.

GatheringProbe vérifie maintenant l'identité de Sastasha depuis le sqpack installé : InstanceContent 4 → ContentFinderCondition 4 → territoire 1036, ContentLinkType 1. Le retour ContentFinderCondition.Content doit correspondre à la ligne InstanceContent. Ce contrôle est passé le 2026-10-28. Ne jamais appliquer aveuglément Content.RowId à tous les types de contenu (roulettes, contenus publics, etc.).

## Sources primaires consultées

- [DutyLootPreview, DutyInfoService](https://github.com/grittyfrog/DutyLootPreview/blob/1ea0e9fd49b4b843d513a29eae0336de7a05228e/DutyLootPreview/Data/DutyInfoService.cs) : index des objets par contenu à partir des feuilles supplémentaires DungeonBossDrop, DungeonBossChest et DungeonChestItem. Auteur/dépôt identifiés ; aucune implémentation copiée. Aucune licence de dépôt n'a été trouvée dans l'arbre examiné, donc pas de réutilisation du code présumée autorisée.
- [LuminaSupplemental, génération des objets des coffres](https://github.com/Critical-Impact/LuminaSupplemental/blob/bd265c2b39868387937ae5b0c038593af997e0f9/src/LuminaSupplemental.SpaghettiGenerator/Steps/DungeonChestItemStep.cs) : import ChestDropsV2, généré hors client ; [génération des drops de boss](https://github.com/Critical-Impact/LuminaSupplemental/blob/bd265c2b39868387937ae5b0c038593af997e0f9/src/LuminaSupplemental.SpaghettiGenerator/Steps/DungeonBossDropStep.cs) : GTParser. Dépôt déclaré GPL-3.0 par GitHub ; aucun paquet, fichier CSV ou code intégré au projet.
- [Réponse Garland Tools Sastasha](https://www.garlandtools.org/db/doc/instance/en/2/4.json) : requête manuelle de lecture réussie, `instance.id=4`, `name=Sastasha`, champs rewards/fights/coffers. Exemples du premier coffre de combat : IDs 2684, 2683 ; du premier coffre de zone : 3781, 3786. `rewards` est une catégorie distincte, pas assimilée sans vérification aux coffres. Les noms des objets resteront ceux du catalogue français local.

Le document externe ne fournit pas de date d'observation ni de garantie d'exhaustivité. La date de téléchargement ne doit pas devenir une date de mise à jour de la source. Une entrée absente ne signifie pas « aucun butin ». Aucun taux de drop ne peut être inventé à partir d'une simple liste.

## Prochaine tranche d'implémentation

Modèle managé original : identité InstanceContent / ContentFinderCondition / territoire distincte, source Garland Tools, date de récupération et couverture explicitement partielle. Parser JSON borné et validant l'ID demandé ; coffres de combat, coffres de zone et récompenses séparés, identifiants Item résolus par le catalogue commun et inconnus conservés. Pas d'inclusion aveugle de tous les `partials` comme drops.

Service HTTP possédé, consultation explicite, cache borné et horodaté, annulation/révision, erreurs conservées distinctes de listes vides ; aucun accès réseau implicite par survol. Interface par contenu avec icônes et sélection vers le contexte commun. Pas de modification native du journal ou du menu de mission nécessaire pour la première tranche.

Tests à couvrir avant exposition : mauvais ID/provider, champs absents/mal formés, doublons multi-sources conservant leur provenance, Item inconnu, annulation/changement de sélection/réponse tardive, taille bornée, cache et erreur réseau. Compiler/versionner avant tout essai en jeu. Ne pas déclarer Duty Loot Preview remplacé sur cette seule tranche.
