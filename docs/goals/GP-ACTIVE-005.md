# GP-ACTIVE-005 — effets déjà payés, 0.50.0, 2026-10-29

Le même outil PR accepte maintenant des effets explicitement déclarés dans un scénario manuel. Le panneau repliable présente les icônes natives des familles de rendement, de chances supplémentaires et d’Évangile, plus le bonus d’Abondance déjà payé pour la première tentative. Le résultat indique les dépenses supplémentaires, la base sans nouvelle action et les seuils utiles. Aucun statut ou effet réel n’est observé par cette nouvelle tranche.

## Valeurs de départ et dépenses futures

Les valeurs saisies contiennent les influences déclarées : le rendement permanent inclut le bonus permanent, les chances supplémentaires incluent les cadeaux déjà actifs. Le rendement exclut les objets du supplément et le bonus d’Abondance limité. Les familles déclarées actives sont retirées des nouveaux achats. Évangile déjà actif fait passer le supplément de un à deux objets sans repayer son coût.

Le bonus d’Abondance déclaré pour la première tentative (0 = aucun, sinon 1–3) s’ajoute une seule fois. Les nouvelles utilisations commencent à la deuxième tentative lorsqu’il existe ; elles peuvent avoir un bonus différent de celui déjà payé. Une seule tentative avec tous les effets déjà actifs ne permet pas de racheter Abondance sur la même tentative. Le budget représente les PR encore disponibles, sans régénération ni remboursement de coûts passés.

Formule du modèle : `p × [n × (b + q × t) + u + k × a]`, avec chances courantes p/q après les nouvelles actions, b rendement permanent courant après un éventuel nouvel achat, t supplément (1 ou 2), u bonus limité déjà payé, k nombre de nouveaux achats et a leur bonus. Si u > 0, k ≤ n−1 ; sinon k ≤ n. Le coût contient seulement les actions nouvellement proposées. Calcul decimal et frontière strictement utile conservés.

Les déclarations exigent des niveaux compatibles : rendement 30, premier cadeau 15, second 50, Évangile81, bonus limité 24 et bonus >1 à partir de68. Un rendement déclaré déjà majoré doit être ≥2. Un taux de supplément positif doit contenir au moins les hausses déclarées (+10/+30 et un point initial). Un taux explicitement nul reste nul pour une ligne sans supplément, même si un effet global est déclaré. Un effet hors modèle déclaré interdit tout résultat.

Les chances de réussite déjà modifiées par une action antérieure et les effets temporaires non représentés restent hors modèle : ne pas utiliser cette interface comme décodeur exhaustif des influences du jeu. Les choix de familles correspondent aux règles explicites du modèle ; aucun optimum global de toutes les mécaniques de récolte n’est annoncé.

## Intégration et durée de vie

GpActiveEffects est immuable et optionnel dans GpScenario. Null/None conserve le scénario historique. Le noyau GpOptimizer partagé reste unique ; aucun second moteur natif, worker ou abonnement supplémentaire. Les éditions d’effets invalident le résultat comme les autres paramètres. Un bonus de première tentative présent déplace les instructions des nouveaux achats à partir de la deuxième tentative.

La préparation depuis le point observé 0.49 continue de copier seulement PR/niveau/métier/tentatives. Taux, rendement, bonus et déclarations sont remis à renseigner : aucune absence d’effet ne devient une observation native. Les copies de ressources gardent leur propriétaire/date ; les saisies demeurent des scénarios manuels. Les autres fonctions GatherBuddy, Mini Cactpot et craft restent conservées.

## Preuves exécutées

COMPILED / TESTED : Verify.ps1 Release 0.50.0.0, 327/327 tests, package/versions/ZIP/exclusion des assemblies hôtes passants, zéro avertissement et erreur. Sept groupes nouveaux : calcul manuel des effets payés, une seule tentative, achats suivants, bonus payé différent du futur, oracle indépendant, compatibilité historique/immutabilité, entrées incohérentes/effets non couverts.

L’oracle énumère les sous-ensembles d’actions puis additionne séparément réussite/échec et supplément/absence par tentative. Il couvre les 16 combinaisons de familles × 4 bonus limités × 2 métiers =128 scénarios déterministes, avec budgets et valeurs variés ; coût, espérance, dépenses réellement sélectionnées et premiers seuils de frontière sont comparés. Il ne reprend pas la formule compacte du moteur.

GatheringActionsProbe exécuté sur sqpack installé : anciens scénarios MIN/BTN toujours à base 4,6, optimum 18,2 pour550 PR et seuil 700 PR. Nouvel état déclaré dans chaque métier : 4 tentatives, rendement 3, taux100/100, familles déjà payées et premier bonus3, budget300 PR → base 23, trois achats d’Abondance supplémentaires pour300 PR, espérance32. Les métadonnées utilisées sont celles des 20 actions déjà vérifiées ; aucun acteur natif observé.

CURRENT_VERIFIED : [Yield1](https://github.com/Hiroa/LazyGatherer/blob/0bb8ec9a47dc4521411c705221cdeda361015d75/LazyGatherer/Gathering/Actions/Yield1.cs), Yield2, Gift1/Gift2, Tidings et Bountiful1/2 dans la même source primaire, règles de familles et effets relues le 2026-10-29. Dépôt AGPL-3.0, faits seulement ; aucune implémentation, dépendance ou fixture externe importée. Énumération, modèle et oracle originaux. Descriptions et coûts des actions relus depuis les fichiers du jeu dans le probe commun.

L’étude0.49 reste valable : StatusGainSelf=0 pour20/20 actions du modèle ; les cinq métadonnées Status étudiées ne fournissent pas de magnitude. Ces zéros ne sont pas utilisés comme preuve d’absence. Le décodeur natif, les magnitudes et la couverture des autres influences restent à établir avant liaison aux taux du point.

VALIDATED_IN_GAME : non. Protocole conservé : renseigner le cas déclaré du probe dans LazyGatherer, vérifier icônes, base 23/coût300/résultat32, changer premier bonus/effets et observer l’invalidation ; déclarer un effet hors modèle doit retirer le résultat. Aucune nouvelle demande d’essai projection/Macro Chain, déjà différés par l’utilisateur.

DLL canonique C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. Les 17 domaines restent PARTIAL ; goal global actif, réserve utilisateur5 % du quota. Suite GP-METADATA-006 : vérifier les données et seuils d’Abondance pour enrichir le catalogue partagé, sans confondre cette aide avec une optimisation native complète.
