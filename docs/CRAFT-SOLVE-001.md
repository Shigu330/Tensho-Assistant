# CRAFT-SOLVE-001 — recherche bornée de séquences normales

Version 0.38.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED ; VALIDATED_IN_GAME : non. Craftimizer reste PARTIAL, les 17 domaines sont conservés.

Extension actuelle 0.39 : [CRAFT-ACTIONS-002](CRAFT-ACTIONS-002.md), palette portée à 26 actions ; les comptes de transitions et la palette de 17 actions ci-dessous sont historiques. Nouveau contrôle niveau 100 : une proposition sous cible est conservée et explicitement signalée.

## Usage

Ouvrir une recette ordinaire dans Craft lié, puis Simuler cette recette. Renseigner ou copier les attributs selon [CRAFT-SIM-001](CRAFT-SIM-001.md). Ouvrir **Rechercher une séquence**, fixer la qualité cible, puis **Chercher une proposition**. Une nouvelle recette initialise la cible au maximum de sa qualité ; zéro permet de rechercher la progression seule. La cible est un nombre de points de qualité, pas une probabilité HQ.

Le calcul s'exécute sur un worker et peut être annulé. Limites réglables : 1–100 étapes, 1–512 états conservés par étape, 1–500 000 transitions examinées. Valeurs par défaut : 40 / 128 / 100 000. Le résultat affiche transitions, profondeur, date UTC du calcul, états écartés et limites atteintes. Les nombres décrivent le travail du moteur et non des actions exécutées dans le jeu.

Trois résultats distincts : cible atteinte dans le modèle ; synthèse terminée sous la cible, explicitement signalée ; aucune synthèse terminée trouvée. Une absence de proposition ne prouve jamais l'impossibilité. La recherche peut écarter une route utile pour respecter sa largeur ou son budget, même avec des attributs suffisants.

**Utiliser cette séquence** remplace seulement la liste manuelle et expose son replay détaillé. En dessous de la cible, le bouton le dit expressément. Inspecter les étapes avant de préparer les [macros copiables](CRAFT-MACRO-001.md). Aucune action en jeu, écriture de macro ou sélection de matériaux. Attributs/conditions réels doivent correspondre lors d'un éventuel essai.

## Algorithme et portée

Le moteur manuel et la recherche appellent la même transition CraftSimulation.Apply. Les coûts, contraintes de niveau/ouverture, combos, effets, Manipulation et fins à zéro ne sont donc pas réimplémentés dans le solveur. Chaque proposition finale est rejouée par CraftSimulation.Run ; toute divergence d'état est une erreur, pas une proposition publiée.

Recherche par profondeur avec déduplication des états exactement égaux. Parmi les candidats viables non terminés, conserver un faisceau mêlant trois classements : qualité, équilibre qualité/progression et progression. Le classement tient aussi compte de PS, solidité, Calme intérieur et durées d'effets. Aucun de ces scores n'est un certificat de complétude ou d'optimalité. L'état inclut toutes les durées et le combo, empêchant de fusionner des états aux ressources égales mais effets différents.

Les actions déjà hors niveau ou Manipulation non confirmée sont exclues. Après atteinte de la qualité cible, ne plus développer les actions de qualité ni leurs deux effets dédiés ; éviter Master's Mend à solidité pleine. Ce sont des choix de recherche dans le modèle normal, pas des adaptations aux conditions réelles. Les 17 actions et limites de recette de CRAFT-SIM-001 restent en vigueur.

Parmi les synthèses terminées trouvées, une cible atteinte prime. Sinon garder la meilleure qualité terminée. Une fois la cible atteinte, finir la couche courante, puis choisir le moins d'étapes, le plus de PS restants et enfin la qualité. Le faisceau peut avoir supprimé d'autres routes : aucune prétention de minimum global. L'arrêt au budget peut aussi interrompre une couche ; le drapeau correspondant reste visible.

## Propriété et invalidation

CraftSolveSession possède CTS, génération, worker et état immuable publié sous verrou. Entrées copiées en données managées ; aucune feuille/pointeur ou service de jeu sur worker. Changement de personnage, RecipeId, attributs, qualité initiale/cible, options ou objet en contexte invalide la génération. Une réponse ou erreur tardive après remplacement/annulation/disposal ne peut pas se publier. Dispose annule et efface, puis le worker coopératif sort à son prochain contrôle.

L'UI valide le contexte même quand la page de simulation est masquée. Modifier la séquence manuelle seule ne change pas le scénario du solveur : l'application de la proposition demande un clic explicite avant de remplacer cette séquence. Le résultat conserve les attributs et états de son calcul, sans observation native d'une synthèse en cours.

## Preuves

COMPILED / TESTED : Verify.ps1 Release 0.38.0.0, 252/252 tests, package/versions/ZIP/assemblies hôtes verts ; zéro avertissement/erreur. Tous les tests précédents du moteur manuel passent après extraction de la transition commune.

Neuf nouveaux groupes : oracle indépendant à faible niveau, replay/fin à zéro, résultat sous la cible vs absence, bornes nœuds/profondeur/largeur, déterminisme/options/déblocage, scénario non trivial avec témoin faisable, invalidation owner/révision, remplacement/disposal, copie d'entrée/annulation/erreurs. L'oracle énumère indépendamment les deux actions disponibles au niveau 5 pour 36 combinaisons Habileté/progression/cible ; il compare faisabilité, nombre d'étapes et PS sans appeler la transition du solveur. Le témoin de haut niveau impose qualité 2 000 et progression 1 000 avec effets et gestion de solidité ; une recherche bornée trouve une solution effectivement rejouée.

LOCAL_VERIFIED : CraftSimulationProbe sur le jeu installé : paramètres natifs recette 2819 Sirop de bouleau et attributs explicitement fictifs 100 / 5 000 / 5 000 / 700. Recherche cible 2 104 : 2 actions, 204 transitions, cible atteinte, replay concordant. Les 136 actions/icônes, recettes et exports des huit métiers restent vérifiés. Cela ne prouve ni les attributs du personnage ni le comportement d'une synthèse réelle.

## Essai en jeu différé

DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.38.0.0. Recette ordinaire, mêmes attributs et qualité initiale : choisir une cible, chercher, inspecter puis utiliser la proposition. Changer un paramètre et vérifier la disparition du résultat. Annuler pendant une recherche large, changer de personnage ou naviguer sur un autre objet : aucun ancien résultat ne doit réapparaître. Essayer ensuite la séquence en synthèse d'essai, étape par étape en condition normale, avec le protocole CRAFT-SIM-001. Aucune validation en jeu revendiquée ; test Macro Chain toujours différé.
