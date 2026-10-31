# GP-METADATA-006 — preuve de divergence, 2026-10-29

Étude effectuée après le checkpoint compilé 0.50.0. Aucun calcul automatique du bonus d’Abondance ajouté à la production. Le champ du scénario reste explicitement manuel. Le décodeur d’effets et l’optimisation native complète restent au périmètre, sans validation runtime revendiquée.

## Deux sources différentes

[LazyGatherer GatheringController](https://github.com/Hiroa/LazyGatherer/blob/0bb8ec9a47dc4521411c705221cdeda361015d75/LazyGatherer/Controller/GatheringController.cs) utilise GatheringItem.GatheringItemLevel.RowId comme clé d’ItemLevel.Gathering et des comparaisons strictes `Collecte > exigence × 1,1` et `> exigence × 0,9`.

[GatherBuddyReborn AutoGather.Actions](https://github.com/FFXIV-CombatReborn/GatherBuddyReborn/blob/a204f2f45cb98302dbe938921255c3a6d617c775/GatherBuddy/AutoGather/AutoGather.Actions.cs), CalculateBountifulBonus, utilise une table embarquée IlvConvertTable.BaseGathering, des seuils entiers inclusifs `Collecte >= base × 11 / 10` et `>= base × 9 / 10`, et vérifie le déblocage de la quête associée à Abondance II. La table est chargée par WorldData depuis un CSV, pas depuis ItemLevel. Le dépôt est signalé Apache-2.0 par l’API GitHub ; aucun contrôleur, commande, callback ou table externe n’a été intégré au plugin.

Les sources ne concordent donc ni sur la donnée de départ ni sur les frontières. Aucune n’est une preuve de capture du client installé ; choisir la première silencieusement aurait créé un bonus supposé exact.

## Probe exécuté

GatheringRequirementsProbe lit les feuilles installées et, uniquement avec `--compare-source`, télécharge en mémoire le CSV de ce commit primaire pour comparer des faits. Limites HTTP : 15 secondes, 1 Mo ; aucun contenu externe exécuté ou persisté dans la production. Commande : dotnet run --project tools/GatheringRequirementsProbe -c Release -- <sqpack> --compare-source.

Résultats LOCAL_VERIFIED : 2 148 lignes GatheringItem, 1 737 références standard Item, aucune clé ItemLevel manquante et aucune exigence à zéro dans cette jointure. Cependant 51 IDs Item possèdent plusieurs exigences suivant les lignes GatheringItem. Exemples : Item48635 est référencé aux clés10/690/730 ; Item48641 aux clés10/690/700. Un simple dictionnaire ItemId→première exigence perdrait le contexte et choisirait arbitrairement une variante.

Comparaison avec le CSV : 131 clés comparées, 61 différences, une clé absente. Exemple clé1 : CSV25 contre ItemLevel.Gathering85 ; clé10 : CSV65 contre130. ItemLevel.Gathering vaut330 à la clé50,615 à100,2036 à420 et4965 à690. Ces champs ne peuvent pas être promus « exigence d’Abondance native » sur cette seule jointure. Le probe explore et imprime les différences ; son code0 ne signifie pas que les deux sources concordent.

ApiProbe confirme que GatheringItemLevelConvertTable expose seulement GatheringItemLevel et Stars dans la feuille installée ; aucun BaseGathering natif dans ce type. GatheringItem possède une référence à cette table, une référence polymorphe Item et PerceptionReq. Les données supplémentaires inconnues ne sont pas renommées ou interprétées comme exigences.

## Reprise concrète

Établir l’origine de l’exigence utilisée par le client et son lien avec le contexte exact du point. Les 51 objets ambigus ne peuvent pas être résolus par l’ID Item seul. Conserver IDs de GatheringItem/point/variante si une liaison est prouvée ; une variante inconnue reste inconnue. Comparer également les frontières inclusives/strictes et le déblocage d’Abondance II avant de copier un bonus.

La source GatheringReader/ItemSlot du second dépôt contient une lecture d’octets de niveaux via des valeurs AtkValues groupées. Elle n’a pas été adaptée : la sélection des groupes et l’indexation des huit lignes doivent être établies indépendamment avant usage. Les getters typés et les nœuds UI de notre observation0.49 restent inchangés ; aucun offset ni callback de cette source n’est repris.

Si une donnée externe datée est envisagée, garder sa provenance, sa licence, les trous de couverture et son statut distinct des données natives. Une estimation ne doit pas être présentée comme bonus courant observé. Les autres travaux des 17 domaines peuvent continuer pendant cette étude ; ce n’est pas un blocage global.

Base courante : Release0.50.0.0,327/327 tests/package verts, oracle PR128 scénarios et probes d’actions passants. Aucun changement de code plugin après cette vérification, seulement le nouveau probe et la présente étude. Tests projection/Macro Chain toujours explicitement différés ; ne pas les redemander. Réserve utilisateur5%, goal global actif.

Complément de recherche (continuation suivante) : ApiProbe dispose maintenant de --find-method=, recherche de métadonnées uniquement. Recherche Gather/Yield et inspection PlayerState/GatheringEventHandler/AtkComponentIcon : aucun getter nommé de bonus de récolte dans les bindings installés. ProcessYield est un traitement d’événement, pas un calcul à appeler ; il n’a pas été exécuté. L’icône expose QuantityText, pas de quantité numérique nommée confirmée. Ceci limite les bindings étudiés, pas toutes les possibilités du client. La recherche Quest ne donne pas de getter simple de disponibilité complète non plus ; ne pas détourner des fonctions de calcul de quêtes quotidiennes à paramètres non établis.

Pas de nouvelle correction spéculative de formule. La réserve de quota est encore disponible ; poursuite indépendante sur USE-002 pour les usages natifs d’objets (mandats d’artisanat, catalogue de livraisons GC et aquarium). GP reste au périmètre avec ses inconnus et le scénario déclaré0.50.
