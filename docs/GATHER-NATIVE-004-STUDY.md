# GATHER-NATIVE-004 — étude préparatoire, 2026-10-29

Suite de la base compilée 0.48.0. Aucun nouvel adaptateur Gathering implémenté dans cette étude ; ne pas annoncer une observation disponible. Le calcul GP-001 reste un scénario manuel sans effets actifs. Les tests projection et Macro Chain sont toujours différés ; ne pas les redemander.

## Éléments déjà lus

LOCAL_VERIFIED via ApiProbe : AddonGathering, ItemIds/ItemFlags (spans), GatheredItemComponentCheckbox, IntegrityLeftover/IntegrityTotal (AtkTextNode), GatherStatus. GatheringEventHandler expose GatheringType, GatheringLevel, GatheringPointBonus0/1 ; leurs bonus ne sont pas encore interprétés. Ces métadonnées ne sont pas une capture du client en cours.

[FFXIVClientStructs AddonGathering](https://github.com/aers/FFXIVClientStructs/blob/385b0821bc4573b77fbde84c930774eeca8283ec/FFXIVClientStructs/FFXIV/Client/UI/AddonGathering.cs), source primaire lue : addon « Gathering », huit IDs et huit composants de lignes. GatherStatus documenté 1 = UI ouverte, 2 = récolte rapide. Utiliser les membres nommés de la dépendance, aucun offset recopié.

[LazyGatherer GatheringItemComponent](https://github.com/Hiroa/LazyGatherer/blob/0bb8ec9a47dc4521411c705221cdeda361015d75/LazyGatherer/Components/GatheringItemComponent.cs) et [GatheringController](https://github.com/Hiroa/LazyGatherer/blob/0bb8ec9a47dc4521411c705221cdeda361015d75/LazyGatherer/Controller/GatheringController.cs), dépôt AGPL-3.0 déjà signalé dans GP-001, lus pour faits techniques uniquement. Le composant de ligne consulte les nœuds texte 10 (chance) et 16 (boon) ; son composant icône 31 contient un texte quantité 7. Le contrôleur lit IntegrityLeftover et vérifie les effets déjà actifs pour interpréter ces valeurs. Aucune implémentation de ce dépôt importée.

## Limite qui décide la tranche suivante

Les taux et quantités UI peuvent déjà inclure des actions actives. Les donner directement comme valeurs de base à GpOptimizer ferait compter les mêmes bonus une deuxième fois. Les bonus Abondance, Évangile, rendement et chances ne peuvent être déclarés absents par défaut. Le modèle manuel doit rester explicite tant que l’état des influences n’est pas établi.

Une première capacité utile possible : observation opt-in du point, liste des huit objets avec icônes/contexte et taux affichés clairement datés ; copier uniquement les valeurs dont la signification est établie (PR/niveau/tentatives), en laissant les hypothèses de base manuelles. Ne pas faire passer un panneau de diagnostic vide pour un outil fini. Si des taux de base sont proposés, démontrer la prise en compte des statuts avant intégration au solveur.

Avant implémentation : inspecter les signatures natives des nœuds/composants et leur type, contrôler visibilité, hiérarchie/longueur et parser strictement ; tiret, vide ou donnée malformée restent inconnus. Prévoir stabilité avant/après, propriétaire, âge, copie managée et nettoyage ; distinguer collecte ordinaire, récolte rapide et collectionnable. Pas de callback, de hook ou de commande de récolte. Étendre les tests du modèle et exécuter Verify.ps1 avant tout essai utilisateur.

Le point de reprise est ici : la source et les champs ci-dessus ont déjà été lus, pas besoin de recommencer cette recherche. L’objectif global et les 17 domaines restent actifs/PARTIAL ; réserve utilisateur de quota 5 %.
