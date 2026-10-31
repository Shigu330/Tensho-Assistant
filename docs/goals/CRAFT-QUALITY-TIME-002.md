# CRAFT-QUALITY-TIME-002 — Collectionnables, matériaux et temps

2026-10-29, version 0.74.0. Demandes successives : collectionnable refusé, Bénédiction de Byregot et Grands progrès, qualité des ingrédients HQ, séquence la plus rapide avec priorité au temps avant le nombre d’actions. Le cas fourni comporte 6 000 de qualité initiale avec deux farines HQ et les attributs de référence 5 622 habileté/5 081 contrôle/565 PS.

## Correction des collectionnables

Le refus systématique de Collectable est supprimé pour les recettes ordinaires. Les recettes expertes, ajustées, spécialisées ou à qualité obligatoire restent refusées avec motif. Un collectionnable sans qualité maximale positive reste inconnu/incompatible. Pas de déduction de paliers de remise ni de conversion arbitraire qualité → valeur de collection.

RecipeSimulationCopy.Copy projette les paramètres natifs pour le chargeur et le probe. Jointure stricte Recipe → RecipeLevelTable ; la qualité dépend des facteurs de table si HQ possible, collectionnable ou qualité imposée. Le booléen HQ n’est pas assimilé à la présence d’une qualité de collection. Les produits/facteurs et maximum initial conservent les arrondis entiers existants. CraftRecipeQuality.Maximum est testé indépendamment des API natives.

## Matériaux HQ sélectionnés

Le lecteur du carnet copie désormais les textes typés AddonRecipeNote.SelectedRecipeStartingQuality et SelectedRecipeMaximumQuality, uniquement visibles, décodés via SeString et parse numérique existant. Texte borné à 128 octets et hiérarchie à 32 parents ; sélection/propriétaire/métier/liste sont revérifiés après copie. Aucun texte/pointeur n’est transmis au worker.

Pour une recette avec qualité, une valeur initiale absente ou une qualité maximale discordante suspend le calcul. Valeur initiale négative ou au-delà du maximum initial de la recette refusée. Une recette réellement sans qualité peut avoir un départ zéro, sans déduire la qualité d’un collectionnable inconnu. Les poids d’ingrédients ne sont pas réinventés : le panneau prend la valeur finale affichée par le jeu, comprenant les HQ réellement sélectionnés.

La qualité initiale fait partie de la clé de calcul ; tout changement annule la proposition précédente et relance après stabilité. Recherche, presets comparés et transfert au simulateur partagent la même valeur initiale. Le texte « qualité initiale 0 » est remplacé par la valeur observée. Les traces/timestamps seuls ne relancent pas le calcul sans changement de valeurs. La lecture finale en jeu reste à confirmer ; les captures antérieures ne prouvent pas cette nouvelle copie.

## Calme intérieur et finisseur

Le modèle conservait déjà l’efficacité de Byregot 100 + 20 × degrés (maximum 300 à 10), l’exigence d’au moins un degré et la consommation complète des degrés. Bonus global de Calme intérieur et multiplicateur de qualité restent appliqués séparément avant l’arrondi final. Grands progrès dure trois étapes et est consommé par la prochaine action de qualité ; aucune exécution automatique ni condition favorable inventée.

Les tests couvrent la progression de degrés 2 → 3 → 5 → 6 → 8 → 10 → 10 → 10 → 0 : Réflexion initiale, Ouvrage de base, bonus de combo d’Ouvrage raffiné, action ordinaire, actions à deux degrés, plafond, Grands progrès sans nouveau degré, puis Byregot. Ouvrage raffiné sans combo ne donne qu’un degré. Test supplémentaire : interdiction de Byregot sans degré et calcul à 10 degrés/efficacité 300 avec Grands progrès.

La recherche avait un biais : le score de degrés pouvait perdre la branche après leur conversion en qualité. Une fraction bornée du beam conserve des états de niveaux de Calme intérieur différents et des chemins ayant déjà encaissé le gain. Les candidats Grands progrès à 10 degrés ou permettant au Byregot suivant d’atteindre la cible sont évalués par la transition partagée, dans les limites PS/solidité/niveau/profondeur et en comptant l’aperçu dans le budget. Les finisseurs ayant atteint la cible sont préservés. Ce n’est pas une insertion forcée : ajouter Grands progrès quand Byregot suffit déjà rallongerait la séquence.

## Objectif temporel

Le dernier ordre explicite de l’utilisateur prévaut : synthèse terminée et cible de qualité atteinte, puis **attentes cumulées minimales**, puis nombre d’actions minimal à attente égale. PS restants et qualité départagent ensuite. Si aucune cible atteinte n’est trouvée, le résultat inférieur est signalé ; il ne satisfait pas la demande de qualité.

BetterCompletion est commun au solveur et au choix des presets. Le solveur conserve le coût temporel de chaque chemin, remplace un doublon d’état par son chemin plus rapide et inclut ExtraWait dans ce coût. Il ne s’arrête plus uniquement à la première profondeur gagnante : la borne inférieure de durée des profondeurs restantes doit empêcher une amélioration, ou les limites s’appliquent. Recherche par beam toujours bornée : élagage, budget et profondeur peuvent empêcher de trouver le véritable minimum global.

Le temps optimisé est la somme des attentes émises par les macros : 2 s pour les buffs concernés, 3 s pour les autres actions modélisées, plus le supplément configuré par action. Ce n’est pas une mesure exacte du temps client : latence, overhead des lignes et lancement manuel des blocs ne sont pas prévisibles. L’interface parle donc d’attentes planifiées. Une séquence avec plus d’actions peut être préférée si elle attend moins longtemps.

## Cas utilisateur et preuves

- Données locales : Tacos de carne asada collectionnables, Recipe 35829 → Item 44232, niveau 100, progression 6 600, qualité 12 000, solidité 80, collectionnable vrai, qualité obligatoire 0. Projection commune acceptée. 12 521 recettes supportées et 1 371 explicitement exclues dans ce census.
- Actions natives Cuisine : Bénédiction de Byregot 100346/icône 1975 ; Grands progrès 260/icône 1955. Métadonnées des 208 actions métiers et 12 effets/icônes contrôlées.
- Sans qualité initiale, le cas de référence n’atteint pas 12 000 dans la recherche vérifiée ; résultat inférieur affiché, pas de succès inventé.
- Avec les **6 000 initiaux fournis**, les recherches 128/100 000 puis 512/500 000 retrouvent **13 actions / 37 s d’attentes / 12 000 de qualité / synthèse terminée**. Des voisinages par suppression/remplacement et insertion de Grands progrès avec réparation locale n’ont pas trouvé mieux. Byregot sous Innovation suffit dans cette séquence : Grands progrès supplémentaire n’est pas utile. Le beam a été élagué : **37 s n’est pas un minimum absolu prouvé**.
- Verify.ps1 final : **453/453**, zéro avertissement/erreur ; identité/API/versions, SHA-256 DLL/manifeste, paquet et exclusion hôte passants. Onze groupes supplémentaires depuis 0.72 (6 collectionnables/IQ, 5 matériaux/objectifs). L’oracle temporel énumère toutes les séquences jusqu’à quatre actions pour des scènes de niveau 21 et compare durée puis actions ; cette preuve ne s’étend pas à toutes les recettes.
- Le premier essai de cet oracle exigeait aussi l’absence d’élagage ; les valeurs de durée/actions étaient déjà concordantes mais ce drapeau était vrai. L’assertion a été recentrée sur la comparaison exhaustive indépendante, sans exiger un drapeau qui n’est pas nécessaire à la preuve de ces cas.
- Probe de production : docs/evidence/0.74.0-native-craft-probe.log ; tests/package : checkpoints/0.74.0-verify.log. Les chiffres saisis dans le probe ne sont pas lus dans un client en cours.

## Sources et validation en jeu

Sources primaires consultées au 2026-10-29 : [ByregotsBlessing](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Actions/ByregotsBlessing.cs), [GreatStrides](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Actions/GreatStrides.cs), [Simulator](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Simulator.cs), [AddonRecipeNote](https://github.com/aers/FFXIVClientStructs/blob/main/FFXIVClientStructs/FFXIV/Client/UI/AddonRecipeNote.cs). Les noms de champs correspondent aux métadonnées locales déjà inspectées ; aucun offset manuel ajouté.

Les captures utilisateur confirment l’affichage du panneau et d’une macro 12 000/13 actions/37 s, mais n’affichent pas la version de DLL et ne valident pas la nouvelle lecture 0.74. Les tests de projection live/Macro Chain restent différés. Les 17 domaines restent PARTIAL, optimum global et cas spéciaux encore ouverts. DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll, 0.74.0.0. Source : checkpoints/TenshoAssistant-0.74.0-source.zip.
