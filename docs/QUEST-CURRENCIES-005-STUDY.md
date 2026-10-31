# QUEST-CURRENCIES-005-STUDY — Monnaies sans Item direct

2026-10-29 — STUDIED ; identité de quinze monnaies UNKNOWN. DLL conservée en 0.68.0.0 ; aucun comportement de production modifié, aucun blocage global.

Le census ciblé de QuestRewardsProbe confirme quinze quêtes nommées avec CurrencyReward.RowId nul et CurrencyRewardCount positif. Toutes sont en ItemRewardType 1 et GCTypeReward vaut zéro dans les quinze cas. Neuf portent une condition GrandCompany explicite (trois séries avec compagnies 1/2/3) ; six n’en portent aucune. La quantité reste connue, l’Item de monnaie ne l’est pas.

IDs des quinze quêtes : 66584, 66640, 66641, 66642, 66664, 66665, 66666, 66667, 66668, 66669, 66693, 66694, 66731, 66732, 66733. Quantités 719–2 517 dans ces lignes. Le journal checkpoints/QUEST-CURRENCIES-005-census.log conserve les lignes et tous les contrôles de projection/index exécutés.

## Hypothèses confrontées aux sources

- CurrencyReward direct est une clé Item dans la définition primaire ; il est réellement nul ici. Aucun fallback typé n’est décrit dans la définition Quest SaintCoinach 0b22b78f97951bb66bfa78fb561aa4c7bf7af7cd.
- GCTypeReward ne distingue pas les cas : zéro aussi pour les monnaies directes connues. On ne peut pas le convertir en compagnie ou ItemId.
- GrandCompany de Quest renseigne une condition de quête, pas une clé de récompense. Déduire une monnaie du prérequis substituerait une relation non prouvée, et laisserait encore six cas sans condition.
- GrandCompany n’expose aucun champ typé de monnaie dans l’API installée. Ses Unknown0…Unknown7 ne sont pas réinterprétés comme des IDs.
- Les monnaies directes connues comprennent Items 20/21/22 (sceaux), 30363 (faits d’armes), 45041 (Savoir). Item.GrandCompany vaut zéro pour tous ; ItemUICategory = 63 « Autre » pour tous. Ni cette relation ni la catégorie ne fournit donc une correspondance unique de compagnie.
- Les noms français et numéros proches des sceaux ne deviennent pas un dictionnaire de production. Une ressemblance de titre de quête avec une compagnie ne prouve pas non plus le destinataire effectif de la récompense.

Les consommateurs primaires déjà lus GarlandTools Quests.cs (04cadd2e1e0de86c20aa9303faa082c7971f8d8b) et SaintCoinach QuestRewards.cs conservent le montant, sans fournir ici une nouvelle clé de monnaie. Lecture ciblée InventoryTools 70a9f410d9a8ab97bef6bcf795415ea2d7c75097 : QuestCompendiumType, ItemQuestSourceRenderer, QuestManagerService ; rendu de RewardItems, aucune résolution supplémentaire trouvée dans ces trois fichiers. Ceci ne prouve pas l’absence d’une solution dans tout le dépôt ou le client.

ApiProbe sur GrandCompany/Item/Quest et recherche de méthodes contenant QuestReward : aucun getter de résolution de monnaie confirmé dans les bindings examinés. Le résultat sans rapport RequestRewardDelivery appartient au courrier, n’est pas un calculateur de récompense de quête et n’a pas été exécuté. Aucun getter ou mutateur non établi n’est invoqué.

## Décision et suite

Conserver QuestUnresolvedReward avec quantité, sans Item fictif, variante de compagnie inventée ou choix du personnage. Les quinze références restent exclues de l’index Item avec couverture explicitement partielle. Pas de suppression de capacité du périmètre des 17 outils. Une résolution ultérieure doit s’appuyer sur une relation native ou un consommateur précis établissant le fallback ; une résolution dépendante du personnage nécessiterait aussi une portée/observation distincte des récompenses statiques.

LOCAL_VERIFIED : census/projection/index de production exécutés après extension du probe, 5 373 quêtes/9 313 lignes, 3 496 Items/8 288 associations ; inconnus inchangés, toutes variantes concordantes. Aucun code plugin modifié : le dernier Verify.ps1 434/434 et les hashes du paquet 0.68 restent la validation de livraison ; aucune nouvelle exécution inutile de la suite. Aucun état en jeu observé, aucun essai demandé.

Reprise indépendante BASE-AUDIT-003 : cohérence des 17 outils/documents, chemins canoniques DLL et paquet, liens/versions, couverture de validation et problèmes concrets de base. Réserve utilisateur de 5 % conservée, objectif global actif.
