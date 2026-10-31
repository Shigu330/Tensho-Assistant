# TRIAD-001D — Identique, Plus et combos

État IMPLEMENTED / TESTED, 0.24.0, 2026-10-28. Rendu et match natif non validés en jeu. TriadBuddy reste PARTIAL.

Identique et Plus sont sélectionnables dans le simulateur, seuls ou avec Inversion/Tueur d'as. Toutes les paires adjacentes sont évaluées sur les valeurs et couleurs précédant la capture. Les cartes alliées comptent comme partenaires, mais seules les cartes adverses nouvellement prises démarrent une chaîne. Plusieurs groupes de sommes peuvent capturer simultanément. Cases vides et bords ne comptent pas comme partenaires.

Les captures ordinaires initiales restent appliquées et ne démarrent pas un combo. Après les prises spéciales, une file bornée propage les captures de voisinage sans nouveau déclenchement d'Identique ou Plus. Inversion s'applique dans les chaînes ; l'exception supplémentaire Tueur d'as ne s'y applique pas. Chaque carte retournée devient alliée immédiatement et ne peut être remise dans la file, qui contient au plus huit cases. Le chemin normal sans Identique/Plus reste distinct pour éviter ce travail supplémentaire.

Sources primaires CURRENT_VERIFIED le 2026-10-28 : [guide officiel français](https://fr.finalfantasyxiv.com/lodestone/playguide/contentsguide/goldsaucer/tripletriad/) pour Identique/Plus et partenaires alliés ; [TriadGameModifier](https://github.com/MgAl2O4/FFTriadBuddy/blob/425f72ecb3ba1580943e62b83097a0bf5a4e69e7/sources/gamelogic/TriadGameModifier.cs) et [TriadGameSimulation.CheckCaptures](https://github.com/MgAl2O4/FFTriadBuddy/blob/425f72ecb3ba1580943e62b83097a0bf5a4e69e7/sources/gamelogic/TriadGameSimulation.cs) pour les interactions pendant les chaînes : Inversion conservée, captures spéciales et exception Tueur d'as limitées à la pose initiale. Aucune implémentation copiée ; moteur original par masques et file sur la pile.

UI : deux cases à cocher ajoutées aux règles. Toute modification efface plateau/historique, annule l'ancien calcul et impose une nouvelle confirmation ; les mains sont conservées. Annulation d'un coup et snapshots du solveur conservent les mêmes règles. Pas de lecture automatique du match, d'inférence de carte cachée ou d'exécution native.

COMPILED / TESTED : Verify.ps1 Release sans avertissement/erreur ; 144/144 tests et package cohérent. Cinq groupes ajoutés : alliés/murs/voisin unique, plusieurs sommes, combo sur plusieurs étapes avec Inversion, absence de chaîne depuis une capture normale ou de nouveau Plus/Tueur d'as pendant celle-ci, 192 fins de partie sur les seize combinaisons. Le moteur de référence utilise tableaux, ensembles et propagation par niveaux, sans appeler les transitions de production. Il vérifie chaque coup de reconstruction puis chaque score minimax de la fin de partie. Les fixtures couvrent explicitement 1/A et les chaînes ; les tirages alternent rangs 1..5 favorisant les correspondances et rangs 1..A.

LOCAL_VERIFIED : GatheringProbe sur les cartes natives conserve ses contrôles catalogue/icônes/objets et vérifie une fin exacte à trois coups, six candidats, pour chacune des seize combinaisons (normale incluse). Ce contrôle statique ne remplace pas une observation du match.

Essai ultérieur : sélectionner Identique/Plus dans Simulateur & conseil, reconstruire une prise spéciale avec allié partenaire puis une chaîne, et vérifier les couleurs. Vérifier aussi le reset lors du changement de règles et l'invalidation d'un ancien conseil. Aucun test redemandé maintenant ; Macro Chain reste reporté par l'utilisateur.

Suite : Ordre puis Bonus/Malus et leurs interactions, avec refus des combinaisons incompatibles et validation indépendante. Chaos, Mort subite, informations cachées et lecture native restent non couverts.
