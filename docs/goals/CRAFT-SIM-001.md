# CRAFT-SIM-001 — simulation manuelle en condition normale

Version 0.36.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED ; VALIDATED_IN_GAME : non. Craftimizer reste PARTIAL.

Extension actuelle 0.39 : [CRAFT-ACTIONS-002](CRAFT-ACTIONS-002.md), 26 actions et 208 variantes natives. Les 17 actions et nombres de tests ci-dessous décrivent la tranche 0.36 historique. Le moteur reste partagé avec le solveur et l'export.

## Usage et portée

Depuis Craft lié, choisir une variante de recette puis **Simuler cette recette**. Une page dédiée conserve ce RecipeId précis et le résultat dans le contexte commun. Saisir niveau, Habileté, Contrôle, PS de départ et qualité initiale, ou copier les attributs frais du même métier observé. La copie utilise les PS de l'attribut, pas une lecture des PS restants pendant une synthèse. Le budget reste modifiable. Une déconnexion ou un changement de personnage efface les attributs copiés et le résultat ; aucune aptitude ou quête débloquée n'est inférée.

Ajouter les actions par leurs icônes/noms natifs, monter/descendre/retirer une étape, puis Simuler. Limite 100 actions. Les noms et descriptions sont en langue du jeu ; les coûts de base sont complétés par les réductions de combo dans le calcul. Manipulation exige une confirmation manuelle de son déblocage. Toute modification invalide le résultat précédent. Aucun lancement d'action, aucune écriture de macro ou consommation réelle de matériaux.

Le tableau expose progression/qualité et gains effectifs plafonnés, solidité, PS restants/coût et Calme intérieur. Les durées des effets après l'étape sont disponibles au survol du nom. La première action impossible est indiquée sans débiter les ressources ; la séquence s'arrête après réussite ou casse et compte les actions non exécutées. Un résultat terminé n'est pas une garantie HQ.

## Modèle vérifié

- Paramètres de Recipe et RecipeLevelTable : solidité, difficulté et qualité multipliées par leurs facteurs entiers /100 ; qualité nulle si ni CanHq ni RequiredQuality. Qualité initiale manuelle plafonnée par MaterialQualityFactor, sans conversion automatique des ingrédients HQ.
- Bases en simple précision : Habileté ×10 / diviseur +2 ; Contrôle ×10 / diviseur +35. Modificateurs de recette si niveau personnage <= niveau recette, puis troncature avant les multiplicateurs d'action.
- Gains d'action calculés avec facteurs entiers et troncature finale. Vénération et Mémoire musculaire s'additionnent ; Innovation et Grands progrès s'additionnent puis se multiplient au facteur de Calme intérieur. Pas de tirage aléatoire : condition normale à toutes les étapes.
- Traits de Travail de base, Travail prudent et Travail préparatoire aux niveaux 31, 82 et 86. Travail préparatoire divise son efficacité par deux si la solidité avant l'action est sous son coût effectif après Parcimonie.
- Calme intérieur à partir du niveau 11, plafond 10 ; Reflect apporte deux charges, Byregot utilise les charges avant de les consommer. Actions d'ouverture au premier tour uniquement. Grands progrès et Mémoire musculaire sont consommés par le prochain gain concerné.
- Combo Basic Touch → Standard Touch → Advanced Touch : chaque coût réduit est de 18 PS. Standard seul n'active pas le coût réduit d'Advanced ; Observe l'active. Toute autre action casse la chaîne.
- Les durées couvrent les étapes suivant le lancement. Parcimonie I/II se remplacent. Manipulation rend 5 de solidité après les étapes suivantes encore viables, sauf son propre lancement/renouvellement ; elle ne sauve pas une solidité tombée à zéro. Une progression terminée à solidité zéro reste une réussite.

17 actions déterministes prises en charge : Basic/Careful Synthesis, Groundwork, Basic/Standard/Advanced Touch, Byregot, Master's Mend, Observe, Veneration, Innovation, Great Strides, Waste Not I/II, Manipulation, Reflect et Muscle Memory. Les libellés affichés viennent des feuilles natives, pas de cette liste anglaise.

Recettes expertes, à niveau ajusté, spécialisées, collectionnables et à qualité imposée explicitement hors périmètre. Conditions favorables, autres actions, taux HQ, outils reliques, solveur automatique, suggestions en cours de synthèse et macros restent à développer. Ces limites ne retirent aucune fonction de la roadmap.

## Propriété et données

CraftActionCatalogLoader copie deux familles d'actions par update framework, résout les variantes CRP..CUL et vérifie métier/niveau/PS. Les effets partagés exigent le type de coût PS et aucun coût secondaire. Publication atomique de 136 métadonnées complètes ; incompatibilité = catalogue indisponible. Dispose désabonne et efface le service. Aucun RowRef/pointeur dans Draw ou dans le moteur pur.

RecipeDefinition transporte les paramètres immuables dans le catalogue déjà partagé. Le moteur borné à 100 actions n'effectue aucune lecture native, réseau ou tâche de fond ; il retourne des étapes immuables et accepte l'annulation. Aucun état de synthèse native n'est observé à ce stade.

## Preuves et références

LOCAL_VERIFIED : tools/CraftSimulationProbe sur les fichiers du jeu installé. 136 actions métiers : coûts/niveaux/descriptions/icônes vérifiés. 13 892 recettes : 11 361 acceptées dans ce périmètre, 2 531 explicitement exclues. Recette 2819 Sirop de bouleau : solidité 35, progression 270, qualité 2 104, plafond qualité initiale 0. Avec attributs fictifs explicitement étiquetés niveau 100 / Habileté 5 000 / Contrôle 5 000 / PS 700 : bases 726/1 055 et séquence test terminée. Ce scénario ne décrit pas le personnage connecté.

CURRENT_VERIFIED : [Craftimizer](https://github.com/WorkingRobot/Craftimizer/tree/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator), notamment SimulationInput, actions et effets ; [Teamcraft simulator](https://github.com/ffxiv-teamcraft/simulator/tree/74e167a05ba279526d2ddd457a048e234bedbad9/src/model/actions), progress-action, quality-action et Advanced Touch. Faits de formules, arrondis, combos et durées étudiés le 2026-10-28 ; implémentation originale, aucun code externe distribué. Les coefficients des actions modélisées permettent une arithmétique entière exacte après troncature des bases.

TESTED : Verify.ps1 Release 0.36.0.0, 239/239 tests, package/versions/exclusion des assemblies hôtes verts, zéro avertissement/erreur. Quatorze nouveaux cas : bases/modificateurs, traits, combos, effets additifs et consommation, expiration, Calme intérieur/Byregot, Parcimonie/Groundwork, Manipulation/casse, fin à zéro, invalidation sans mutation, limites/annulation, qualité plafonnée/immutabilité et catalogue complet par métier.

## Essai en jeu différé

DLL : `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.36.0.0. Ouvrir une recette ordinaire dans Craft lié, puis Simuler cette recette. Copier les attributs du bon métier, vérifier les descriptions/icônes, ajouter une courte séquence. Comparer des étapes réellement en condition normale, avec mêmes attributs et qualité initiale ; relever tout écart d'arrondi. Tester une action hors PS, une ouverture tardive et une casse : le tableau doit s'arrêter correctement. Modifier l'ordre et vérifier que le résultat disparaît jusqu'au nouveau calcul. Déconnexion après copie : les attributs copiés doivent s'effacer. Recette collectionnable : afficher la limite explicite. Pas de test natif déclaré réalisé ; Macro Chain reste reporté par l'utilisateur.
