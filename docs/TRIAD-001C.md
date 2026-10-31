# TRIAD-001C — Inversion et Tueur d'as

État IMPLEMENTED / TESTED, version 0.23.0, 2026-10-28. Moteur/simulateur/solveur intégrés ; aucune validation en jeu revendiquée. Couverture Triple Triade toujours PARTIAL.

Deux règles sélectionnables : Inversion et Tueur d'as (nom français natif de Fallen Ace), seules ou combinées. Les règles sont portées par la position immutable et conservées dans chaque succession de coups ; le worker reçoit donc exactement la combinaison utilisée pour reconstruire le plateau. Un changement de règle réinitialise le plateau et l'historique, annule le calcul précédent et retire la confirmation des règles. Les mains initiales sont conservées. Les flags inconnus sont rejetés avant simulation.

Inversion utilise une comparaison strictement inférieure ; les égalités ne capturent toujours pas. Tueur d'as ajoute la capture 1→A sans Inversion, ou A→1 avec Inversion. Le calcul de capture ordinaire reste actif dans le sens opposé. Cette asymétrie ne doit pas être remplacée par un classement global qui intervertirait 1 et A.

Sources primaires CURRENT_VERIFIED le 2026-10-28 : [guide officiel français](https://fr.finalfantasyxiv.com/lodestone/playguide/contentsguide/goldsaucer/tripletriad/) pour les noms et principes, [guide anglais](https://na.finalfantasyxiv.com/lodestone/playguide/contentsguide/goldsaucer/tripletriad/) pour la capture supplémentaire et sa combinaison ; [moteur original TriadBuddy, TriadGameModifierFallenAce/Reverse](https://github.com/MgAl2O4/FFTriadBuddy/blob/425f72ecb3ba1580943e62b83097a0bf5a4e69e7/sources/gamelogic/TriadGameModifier.cs) pour vérifier explicitement le maintien de la capture ordinaire A→1 et le comportement asymétrique. L'ancienne formulation française seule ne suffisait pas à déduire ce dernier détail. Inspection ciblée du comportement, aucune implémentation copiée.

COMPILED / TESTED : Verify.ps1 Release sans avertissement/erreur, 139/139 tests et package cohérent. Quatre groupes ajoutés : matrice de toutes les valeurs 1..A, quatre directions et quatre combinaisons (1 600 captures), flags inconnus/défaut normal, 96 fins de partie dont chaque coup est comparé au moteur exhaustif indépendant, remplacement du calcul par une autre combinaison. Les 80 fins de partie normales de la tranche précédente restent testées.

LOCAL_VERIFIED : GatheringProbe sur les cartes du jeu : fins exactes à trois coups avec six candidats pour Inversion, Tueur d'as et leur combinaison. Tous les contrôles du carnet, des icônes et des identités restent passants. Ces calculs sur données natives ne prouvent pas une validation du match en jeu.

Essai ultérieur : choisir les règles dans Simulateur & conseil, renseigner deux mains connues et reconstruire quelques coups incluant 1/A. Vérifier les résultats et que modifier une règle efface plateau/conseils puis impose une nouvelle confirmation. Aucun test supplémentaire demandé maintenant ; Macro Chain reste reporté par l'utilisateur.

Suite : Identique et Plus, avec détection simultanée et propagation des combos, à vérifier sur source primaire et cas indépendants avant activation. Ordre, Chaos, Bonus/Malus, Mort subite, mains cachées et lecture native restent non couverts ; les écrans le précisent.
