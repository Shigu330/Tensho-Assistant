# QUEST-REWARDS-003 — Récompenses natives de quête

0.67.0.0 — 2026-10-29 — IMPLEMENTED / COMPILED / TESTED ; NOT VALIDATED_IN_GAME. Les 17 domaines restent PARTIAL.

La fiche de quête propose un cadre Récompenses renseignées : objets fixes, choix, variantes masculine/féminine, choix conditionnels d’événement, variantes de métier et quantités selon rang, monnaies et gils de base. Les groupes sont séparés et repliables ; les conditions du personnage ne sont pas choisies par supposition. Icône/nom de chaque Item résolu cliquable vers le contexte et les stocks communs. Une quête terminée ne prouve ni le choix effectué, ni la possession actuelle, ni la réception d’une récompense.

## Preuves et différences avec les anciennes sources

Sources primaires lues : GarlandTools Quests.cs commit 04cadd2e1e0de86c20aa9303faa082c7971f8d8b, et SaintCoinach QuestRewards/QuestRewardItem/GroupType/Group, définitions Quest/BeastRankBonus au commit 0b22b78f97951bb66bfa78fb561aa4c7bf7af7cd. Licence MIT Garland déjà vérifiée, WTFPL v2 SaintCoinach vérifiée dans LICENSE ; faits ciblés, aucune implémentation ou base externe importée.

La projection locale prime sur les constantes anciennes : les lignes Quest ont sept slots Reward/ItemCountReward, cinq choix/HQ et trois compléments ; les sous-lignes QuestClassJobReward ont quatre slots de récompense. L’ancien consommateur SaintCoinach limitait ses boucles à six et trois. Tous les slots réellement exposés sont parcourus.

Reward est polymorphe selon ItemRewardType : 1/3/5 → Item, 6 → QuestClassJobReward, 7 → BeastRankBonus. Un numéro de table n’est donc jamais passé à ItemCatalog.Find comme s’il était un Item. Type 1 : objets fixes et choix séparés ; type 3 : slots 0/1 masculins/féminins, sans choisir le genre du personnage ; type 5 : choix conditionnels, aucun droit journalier ou date d’événement déduits. Autres types futurs restent explicitement non interprétés.

Type 6 : toutes les sous-lignes et catégories ClassJobCategory copiées, quantités/slots, table, SubrowId et slot parent conservés. Pas de sélection du métier courant ni de fusion d’alternatives. Type 7 : toutes les quantités de la table par rang conservées, même zéro ; affichage « variante de rang », sans prétendre avoir prouvé l’association de chaque index à un rang nommé ou le rang courant du personnage. Aucun maximum ou cumul de variantes substitué au tableau.

Mémoquartz : jointure sur TomestonesItem.Tomestones.RowId, pas sur TomestonesItem.RowId. Type 1 → Item 28 poétique ; type 3 → Item 49 mnémonique dans ces données. Un choix de ligne par numéro aurait donné une ancienne monnaie incorrecte. Doublons identiques coalescés, clés ambiguës ou absentes non résolues. Les monnaies CurrencyReward à Item nul conservent leur quantité et une raison explicite ; aucune compagnie ou monnaie fictive choisie.

## Contrats et limites

QuestRewardSnapshot possède des listes readonly et la provenance Lumina.Excel/Quest + versions des dépôts du jeu, sans horodatage artificiel par ligne statique. Les variantes ne sont jamais sommées/dédupliquées par ItemId. Quantité directe zéro avec Item présent : inconnue, pas quantité un inventée ni ligne supprimée. Quantité zéro dans la série de rang reste une valeur du tableau conditionnel. Qualité HQ/NQ copiée seulement depuis OptionalItemIsHQReward ; les slots sans champ de qualité gardent Unknown. Teintes conservées, pas transformées en couleur/éligibilité du personnage.

Bornes : 128 sous-lignes par table de métier, 2 048 lignes finales et 128 références non résolues ; identités Item non nulles et indices non négatifs. Les descriptions d’expérience/actions/autres bonus ne sont pas calculées ; leur présence signale des récompenses supplémentaires. Gils affichés comme base renseignée, sans calcul de bonus. Quantités/monnaies personnelles plafonnées, choix et conditions ne sont pas observés.

QuestReader existant est étendu dans ses lots 32/2 ms, framework uniquement. Feuilles de bonus/métier et correspondance de monnaie partagées dans ce lecteur ; aucune lecture native dans Draw. Une table indisponible ou une ligne non interprétable conserve la progression et les autres métadonnées de quête ; logs bornés et reprise au cycle existant. Dispose libère les feuilles/références ajoutées. Invalidation de personnage efface la progression, conserve les faits statiques immuables. Aucun réseau, requête serveur, worker, commande ou lecteur supplémentaire.

## Validation exécutée

Verify.ps1 final : 426/426 tests, zéro avertissement/erreur ; package/API/versions/SHA-256 canonique/exclusion hôte verts. Journal checkpoints/0.67.0-verify.log. Cinq groupes nouveaux : alternatives/slots/HQ/métier/rang non sommés ; quantités et références inconnues ; clés de monnaie distinctes/ambiguïtés/copies ; propriété/provenance et indépendance de l’owner/progression ; bornes/invalides. GatheringProbe, qui partage QuestService, recompilé après ajout du modèle statique.

QuestRewardsProbe emploie la projection de production sur 5 373 quêtes nommées : modes 1=5 202, 3=20, 5=6, 6=115, 7=30. 37 tables de métier, 538 sous-lignes distinctes, maximum 21 par table. Résultat : 9 313 lignes, dont 2 704 fixes/compléments, 3 993 choix, 18 choix conditionnels, 331 monnaies, 20 masculines + 20 féminines, 1 987 variantes métier et 240 variantes de rang. Tous les Item et Stain projetés se résolvent. Quatre quantités restent inconnues, quinze monnaies directes restent non reliées avec quantité conservée. Mémoquartz types 1/3 vérifiés contre les identités 28/49. Premier build des fixtures corrigé pour les conversions uint?/littéraux de clé avant validation finale ; aucun résultat échoué déclaré passant.

La quête 70493 conserve notamment une référence Item directe sans quantité renseignée, au lieu de la perdre comme dans une projection qui ignorerait toutes les quantités zéro. Les récompenses réelles, choix, bonus, rangs et rendu ne sont pas validés en jeu. Essais craft/Macro Chain différés, non redemandés ; aucune intervention demandée.

Suite QUEST-ITEM-SOURCES-004 : index partagé managé des quêtes pouvant renseigner un Item, liens depuis Usages vers la fiche, sans convertir des variantes en obtention garantie. Les quinze monnaies dépendantes non résolues et noms de rang restent au périmètre d’une étude ciblée ultérieure.
