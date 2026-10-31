# CRAFT-LOG-001 — Aide macro près du carnet de synthèse

2026-10-29, version 0.72.0. Demande utilisateur : une interface à côté du carnet natif présentant une macro générée, dans l’esprit de la capture Craftimizer fournie. Ce document distingue l’implémentation et les preuves de l’observation en jeu, encore absente.

## Parcours utilisateur

Recharger C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll (0.72.0.0). Ouvrir le carnet de synthèse du jeu et sélectionner une recette du métier équipé. Le panneau Tensho s’affiche à droite du carnet, ou à gauche si la droite déborde, et reste dans la zone de travail. Il fonctionne même si la fenêtre principale Tensho est fermée. Sa hauteur est bornée à l’écran, avec défilement pour les textes longs.

Dans Craft lié, « Simuler cette recette » demande maintenant l’ouverture du carnet natif sur cette recette. « Simulateur détaillé » conserve l’ancien accès au scénario manuel. Le panneau donne l’icône et le nom du résultat, métier/niveau, progression, qualité, solidité et attributs. Si la recette n’est pas reliée au catalogue, si le métier diffère ou si les attributs sont inconnus/anciens, un motif remplace les propositions.

Après 350 ms de sélection stable, calcul en arrière-plan de la macro proposée et comparaison des séquences enregistrées de cette recette. Les sections indiquent qualité finale simulée, nombre d’actions et durée des attentes. Jusqu’à 30 icônes avec tooltips, compteur du reste, texte intégral et boutons de copie par bloc de 15 lignes maximum. « Modifier / enregistrer cette séquence » charge actions, attributs et résultat dans le simulateur existant ; l’enregistrement reste explicite dans cet outil.

Réglages → Artisanat : « Afficher l’aide macro à côté du carnet de synthèse », préférence sauvegardée, activée par défaut conformément à cette demande de panneau automatique. Les limites de recherche et options d’export existantes sont réutilisées. Cliquer explicitement « Simuler cette recette » réactive cette préférence si nécessaire, avec rollback en cas d’échec de sauvegarde. Aucun prix ni observation de synthèse n’est activé par cette option.

## Acquisition et lifecycle

CraftLogReader possède framework.Update et client.Logout ; rollback de l’abonnement initial et Dispose symétrique. Lecture à 5 Hz quand le carnet est visible/prêt et l’option active. RecipeNote.RecipeList doit être prête, non nulle, avec Recipes non nul, 1–20 000 entrées et SelectedIndex strictement dans les bornes. Une seule RecipeEntry est copiée : RecipeId, ItemId, CraftType, plus le métier courant et le rectangle du carnet. Propriétaire, métier, liste/index et identités sont revérifiés après copie. Aucun pointeur/span natif n’entre dans l’UI ou le worker.

La demande d’ouverture est un message managé consommé une fois sur le framework, avec propriétaire et identité de recette vérifiés dans le catalogue. AgentRecipeNote.OpenRecipeByRecipeId utilise l’ID de ligne normal ; le wrapper du binding applique lui-même la représentation interne. Aucune commande de craft ou de macro n’est appelée.

La fermeture, désactivation, perte de couverture ou déconnexion retire l’observation et la demande en attente. Une révision distingue réouverture/reconnexion, même pour le même personnage. L’UI vérifie cette révision, une sélection de moins d’une seconde, puis des attributs de moins de trois secondes appartenant au même personnage/métier. Les publications de stats seules ne relancent pas le calcul si les valeurs n’ont pas changé. Le worker est annulé au prochain rendu quand le contexte disparaît ; résultat tardif rejeté par génération/jeton et Dispose. Si le rendu est suspendu, le travail déjà lancé reste borné et aucun ancien résultat ne sera présenté comme courant à la reprise.

CraftLogSession copie ingrédients/presets/actions avant de lancer le worker ; catalogues managés partagés. Réutilise CraftSimulation, CraftSolver et CraftMacroExporter. Au plus 100 presets sont évalués ; seuls ceux qui terminent avec la même recette/objet/métier sont comparés, priorité qualité puis nombre d’actions. Le classement est limité aux séquences compatibles enregistrées, sans prétention d’optimum universel. Aucun travail massif ni lecture native dans Draw.

## Limites explicites

Condition normale et qualité initiale zéro, sans déduire la qualité des ingrédients HQ. Manipulation exclue car son déblocage de quête n’est pas observé. Les autres limites du simulateur restent appliquées : recettes spéciales/expertes/non modélisées refusées avec motif. Une recherche bornée peut terminer sous la cible ou ne rien trouver ; le panneau le dit. Aucune probabilité/garantie HQ, synthèse lancée, macro écrite dans le jeu ou bloc exécuté automatiquement. Les séquences exportées se copient et se lancent manuellement.

## Preuves

- COMPILED / TESTED : scripts/Verify.ps1 final, 442/442 tests, zéro avertissement/erreur ; identité/API/version, paquet, SHA-256 exacts DLL/manifeste et exclusion des assemblies hôte passants. Journal checkpoints/0.72.0-verify.log.
- Sept groupes ajoutés : géométrie/fraîcheur et ouverture bornée consommée une fois ; jointure exacte recette/objet/métier/propriétaire/attributs ; comparaison des presets et macro exportable ; résultats tardifs après changement recette/owner/révision ; annulation/Dispose ; copies des producteurs et erreurs ; ancienne configuration v1, défaut et préférence indépendante.
- LOCAL_VERIFIED : ApiProbe sur les assemblies installées pour RecipeNote, RecipeData, RecipeEntry, AddonRecipeNote et AgentRecipeNote. Noms/types/signatures seulement, aucune lecture de client en exécution. Journaux craft-log-api-types.log et craft-log-api-selection.log. La première tentative avec le nom supposé RecipeNoteData a échoué ; le type réellement présent RecipeNote.RecipeData a été découvert puis vérifié. Cette tentative n’est pas une preuve de compatibilité.
- Sources primaires CURRENT_VERIFIED au 2026-10-29 : [RecipeNote](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/Game/UI/RecipeNote.cs), [AgentRecipeNote](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/Agent/AgentRecipeNote.cs). Lecture des champs nommés et de la convention d’ouverture, pas de nouveaux offsets locaux inventés.
- VALIDATED_IN_GAME : non. Lecture effective de la sélection, ouverture du carnet et placement/échelle encore à confirmer dans le client. La capture utilisateur montre le résultat souhaité d’un autre outil, pas une validation Tensho.

Les 17 domaines restent PARTIAL. Les anciens essais projection/Macro Chain restent différés. Source : checkpoints/TenshoAssistant-0.72.0-source.zip ; la demande présente est implémentée, l’objectif autonome antérieur reste en pause à la réserve demandée.
