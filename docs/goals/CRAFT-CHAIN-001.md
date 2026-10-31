# CRAFT-CHAIN-001 — préparation des sous-recettes

État : DONE côté code et vérification managée, version 0.15.1 ; rendu en jeu à confirmer. Verify.ps1 : 93/93 tests et package valides.

Extension 0.15.1 : panneau séparé pour choisir explicitement une variante (métier, niveau, rendement, ID recette), puis recalculer. Le moteur accepte seulement une recette produisant l'objet demandé ; les choix sont figés avant le worker. Modifier un choix marque le calcul précédent comme à refaire ; Réinitialiser les choix les efface. Deux tests vérifient résolution, refus d'une recette étrangère et copie des choix.

Implémentation : CraftChain (calcul pur), CraftChainSession (worker, annulation, génération et contexte), MainWindow.CraftChain (listes à icônes avec clipper, étapes et besoins, copie et liens Lieux/Prix). Les listes sources sont copiées et les résultats protégés contre les mutations. Tests : rendement, stock partagé, surplus, variantes, cycles, profondeur/nombre de besoins, inconnus/caches, overflow, annulation et publications après changement de contexte/Dispose.

Vérification sur les fichiers du jeu installé via tools/GatheringProbe : Sirop de bouleau, 7 objets demandés → 9 produits, 9 Sèves de bouleau et 24 Éclats de feu. Jointures Recipe/Ingredient/rendement vérifiées par exécution ; aucune lecture des stocks réels ni validation du rendu en jeu déduite du probe.

Précision d'interface : un changement de personnage, objet, cible ou catalogue annule et efface le calcul. Les nouvelles références d'observations inventaire/banque marquent le résultat daté comme à recalculer, au lieu de le faire disparaître à chaque nouvelle observation périodique ; la copie conserve cet avertissement. Les imports manuels et inconnus restent explicitement signalés.

Essai après chargement de 0.15.0 : ouvrir une recette dans Craft lié, choisir une quantité puis Calculer la chaîne des sous-recettes. Vérifier étapes, rendements, icônes, stocks partagés et branches à choisir. Modifier quantité/objet ou changer de personnage pendant le calcul ; aucun ancien résultat ne doit apparaître. Déplacer un ingrédient puis recalculer. Vérifier copie et liens Lieux/Prix ; recharger le plugin pendant un calcul et consulter les logs.

Objectif : ajouter à la préparation une liste des fabrications intermédiaires et des matières premières à compléter pour la quantité demandée, en utilisant les recettes et stocks partagés.

Périmètre borné : calcul manuel sur worker avec copies managées des observations et catalogue immuable ; stocks NQ/HQ utilisables une seule fois sur toute la chaîne ; réutilisation des surplus liés au rendement ; ne pas déduire le stock du résultat final puisque la cible signifie objets à produire. Développer automatiquement uniquement les sous-recettes univoques. Plusieurs variantes, cycle ou limite de profondeur restent des exigences à résoudre explicitement, jamais un choix silencieux.

Algorithme proposé : stock disponible partagé par objet, décrémenté à chaque besoin ; pour le manque, résoudre l'unique recette, arrondir au rendement, développer ses ingrédients puis créditer le surplus. Conserver les étapes en ordre de dépendance sans fusion d'étapes qui invaliderait cet ordre. Agréger seulement les matières à acquérir. Limites : profondeur 12, nombre de besoins 2 048, arithmétique checked et annulation. Un cycle ou une ambiguïté rend la proposition partielle. Un stock inconnu n'est pas zéro prouvé ; les imports restent datés et potentiellement périmés.

UI : bouton Calculer la chaîne dans la préparation, liste des étapes avec icônes et matières à compléter, clic vers contexte/Lieux/Prix ; avertissements pour branches non résolues et données anciennes. Résultat lié au personnage, à la recette, à la quantité et aux références des observations utilisées ; invalider en cas de changement, annuler au Dispose. Pas de lecture native ni accès aux dictionnaires UI depuis le worker. Utiliser ItemHoldings.Build sur snapshots capturés.

Tests attendus : rendement arrondi, matières partagées sans double consommation, surplus intermédiaire réutilisé, plusieurs recettes, cycles, limites/overflow, annulation, observation inconnue et propriété des données. Verify.ps1 avant tout essai demandé.
