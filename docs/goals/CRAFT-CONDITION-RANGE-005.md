# CRAFT-CONDITION-RANGE-005 — 0.47.0, 2026-10-29

Dans Simulation d’artisanat, une séquence simulée dispose du panneau « Variations de qualité · séquence fixe ». Il compare la qualité finale normale avec une enveloppe sur les transitions ordinaires du modèle. Le scénario commence Normale ; aucune condition future réelle n’est annoncée. L’utilisateur peut considérer le facteur favorable standard ×1,50, amélioré ×1,75, ou les deux facteurs du modèle. Il s’agit d’un scénario manuel, sans identification implicite de l’outil équipé.

## Calcul

Transitions : depuis Normale, considérer Normale/Favorable/Excellente ; Favorable et Mauvaise imposent ensuite Normale ; Excellente impose Mauvaise. Aucune fréquence, probabilité HQ ou liste de conditions propre à une recette n’est supposée. Autoriser toutes les branches ordinaires donne une enveloppe conservatrice ; elle ne certifie pas que les deux bornes seront réalisables pour chaque recette et influence réelle.

CraftConditionEnvelope réutilise CraftSimulation.Run puis Apply pour les actions effectivement simulées. Au plus quatre conditions sont retenues, avec deux qualités extrêmes par condition. Les gains de qualité sont monotones, y compris plafonnement et Main preste : les extrêmes suffisent. Les ressources autres que la qualité sont comparées à l’état normal après chaque transition ; divergence ou inadmissibilité provoque un refus explicite. Les actions aléatoires, conditionnelles et recettes spéciales restent hors du moteur courant.

Le calcul utilise des parcours cohérents, pas un choix arbitraire du pire facteur à chaque action. Les minimums par étape peuvent provenir de parcours différents et ne forment pas nécessairement une seule séquence de conditions. Dans le scénario des deux bonus, monotonie et tests confirment les mêmes bornes que l’union des deux scénarios à outil constant. Fin, casse, erreur et actions non exécutées du résultat normal restent conservées.

Entrées limitées à 100 actions par le moteur partagé, annulation respectée, résultats détachés/immutables. Le panneau est repliable ; calcul et erreurs sont mis en cache par identité du résultat et scénario d’outil. Modifier la séquence ou les attributs invalide déjà le résultat normal et donc l’enveloppe. Aucune persistance, lecture native ou commande de jeu ajoutée.

## Preuves

CURRENT_VERIFIED : [source primaire Craftimizer Simulator](https://github.com/WorkingRobot/Craftimizer/blob/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Simulator.cs), méthode StepCondition et facteurs de qualité, lus le 2026-10-29. Implémentation originale ; aucune fréquence du simulateur externe reprise.

COMPILED / TESTED : Verify.ps1 Release 0.47.0.0, 302/302 tests, package/versions/ZIP/exclusion hôte verts, zéro avertissement et erreur. Sept groupes supplémentaires couvrent : premier tour Normal, Excellente sur un buff puis Mauvaise sur une touche (67–540, normale 135), compression comparée à une énumération exhaustive sur petits scénarios avec buffs/combos/plafonds, deux bonus contre scénarios constants, fin/casse/invalide, Main preste et annulation/facteur invalide/copies immuables.

Premier build refusé parce que la nouvelle classe pure n’était pas incluse dans le projet de tests à inclusions explicites ; inclusion corrigée puis vérification complète réussie. Aucun échec de calcul masqué.

VALIDATED_IN_GAME : non. Les tests projection et Macro Chain restent explicitement différés ; ne pas les redemander. DLL canonique : C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll. Les 17 domaines restent PARTIAL, le goal global reste actif.

Suite indépendante : CACT-LIVE-002, étude d’une copie native opt-in du Mini Cactpot vers le solveur partagé.
