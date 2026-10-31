# CRAFT-ACTIONS-002 — actions déterministes avancées

Version 0.39.0.0, 2026-10-28. IMPLEMENTED / COMPILED / TESTED. VALIDATED_IN_GAME : non. Craftimizer reste PARTIAL.

## Extension du moteur commun

Neuf actions ajoutées à la palette native, à la simulation, à la recherche bornée et à l'export de macros. Les 17 actions de CRAFT-SIM-001 sont conservées : 26 familles, 208 variantes pour les huit métiers. Noms et descriptions affichés dans la langue du jeu ; les IDs ci-dessous désignent la variante menuisier canonique, les variantes métier sont résolues par les liens CRP..CUL.

| Action française native | ID | Niveau | PS | Solidité de base | Règle modélisée |
|---|---|---|---|---|---|
| Travail minutieux | 100323 | 76 | 32 | 10 | Progression 100 %, puis 150 % dès 94 ; qualité 100 % séparée ; consomme les deux effets de gain applicables |
| Ouvrage préparatoire | 100299 | 71 | 40 | 20 | Qualité 200 %, deux degrés de Calme intérieur, plafond 10 |
| Travail économe | 100427 | 88 | 18 | 5 | Progression 180 % ; interdit sous Parcimonie I ou II |
| Ouvrage parcimonieux | 100227 | 66 | 25 | 5 | Qualité 100 % ; interdit sous Parcimonie I ou II |
| Ouvrage raffiné | 100443 | 92 | 24 | 10 | Qualité 100 %, deux degrés après Ouvrage de base, un sinon ; termine ce combo |
| Main preste | 100283 | 80 | 250 | 0 | Premier tour, au moins dix niveaux de plus que la recette, qualité maximale et un degré de Calme intérieur |
| Main divine | 100435 | 90 | 32 | 0 | Dix degrés requis, qualité 100 % avec les dix degrés conservés |
| Réparation totale | 100467 | 98 | 112 | 0 | Solidité entièrement restaurée et plafonnée à la valeur de la recette |
| Main suprême | 100475 | 100 | 0 | 0 | Une utilisation par synthèse ; annule la prochaine dépense de solidité |

Main suprême reste active après Observe, les effets ou une réparation. Une action dont le coût de base en solidité est positif consomme la protection ; les suivantes retrouvent leur coût normal. L'historique « déjà utilisée » et la protection « encore active » sont deux champs distincts inclus dans l'égalité des états du solveur. La protection n'autorise pas les actions économes sous Parcimonie. Elle rend le coût effectif de Travail préparatoire nul, évitant son malus de solidité insuffisante.

Travail minutieux sépare les efficacités progression/qualité : son trait 94 n'améliore que la progression. Il reste simulable sur une recette à qualité maximale nulle ; le gain de qualité est plafonné à zéro et Calme intérieur augmente normalement. Main preste exige le niveau exact de la recette dans la transition commune ; les recettes expertes et autres recettes spéciales restent déjà exclues par le périmètre initial.

Les noms, niveaux et PS des nouvelles actions sont vérifiés par le loader existant ; toute incompatibilité empêche la publication du catalogue complet. Aucun offset, lecture de synthèse native ou automatisation ajouté. Les quêtes/déblocages ne sont toujours pas inférés ; la confirmation de Manipulation reste manuelle.

## Recherche et macros

Le solveur développe les nouvelles actions via CraftSimulation.Apply. Une fois la cible atteinte, il écarte les actions uniquement de qualité, mais conserve Travail minutieux puisqu'il peut finir la progression. Il évite aussi Réparation totale à solidité pleine. Les drapeaux de limites et les trois résultats atteint/sous cible/absent restent explicites. La recherche n'est ni complète ni optimale universellement.

Les neuf actions utilisent une attente proposée de trois secondes en macro, y compris Main suprême et Réparation totale. Les six effets précédemment à deux secondes restent inchangés. Délais ajustables, copie manuelle par blocs de 15 lignes maximum, aucune garantie d'activation réelle.

## Preuves actuelles

LOCAL_VERIFIED : CraftSimulationProbe sur les fichiers installés : 208 variantes, niveaux/PS/descriptions/icônes vérifiés. 11 361 recettes admises et 2 531 explicitement exclues comme auparavant. Scénario fictif Sirop de bouleau 2819 : deux actions trouvées, qualité 2 104, 442 transitions, replay concordant. Les nombres de transitions de 0.38 sont historiques et ne décrivent pas la palette élargie.

Contrôle supplémentaire à attributs fictifs 100 / Habileté 5 000 / Contrôle 5 000 / PS 700 : recette native niveau 100 n°5618, objet 44104, cible 12 000. Le faisceau par défaut trouve une synthèse terminée de 19 actions à qualité 10 513 après 69 602 transitions, mesuré environ 79 ms sur cette machine. Cible non atteinte : résultat partiel, pas preuve d'impossibilité ni garantie de performance sur d'autres machines. Aucune observation du personnage connecté.

CURRENT_VERIFIED le 2026-10-28 : [Craftimizer Actions](https://github.com/WorkingRobot/Craftimizer/tree/1d4a2c67ea47a25d68701e8e69c8744dbbd56d86/Simulator/Actions), neuf fichiers homonymes ; Simulator.CalculateDurabilityCost, IncreaseQualityRaw et Effects pour la protection persistante et les degrés de Calme intérieur. Le code natif et ces sources primaires déterminent les contraintes ci-dessus ; implémentation originale, aucune reprise d'offset ou de code externe.

COMPILED / TESTED : Verify.ps1 Release 0.39.0.0, 260/260 tests et package/versions/ZIP/assemblies hôtes verts, zéro avertissement/erreur. Huit groupes ajoutés : trait 94 et double gain/consommation, deux degrés et combo raffiné, actions économes/interdictions, Main preste niveau/ouverture, Main divine, réparation totale, protection persistante/une fois/interactions, intégration recherche/export/identité des états. Les 252 tests précédents passent avec la palette étendue, dont l'oracle bas niveau et les tests de concurrence.

## Essai en jeu différé

DLL `C:\Plugins-ff14\src\TenshoAssistant\bin\x64\Release\TenshoAssistant.dll`, version 0.39.0.0. Dans une recette ordinaire, vérifier les neuf icônes et descriptions, puis comparer en synthèse d'essai les mêmes attributs/étapes en condition normale. Contrôler Ouvrage de base → Ouvrage raffiné, les deux degrés d'Ouvrage préparatoire, le refus des actions économes sous Parcimonie, et Main suprême → Observe → Travail préparatoire : une seule dépense protégée. Une seconde Main suprême doit être rejetée sans mutation. Vérifier Main preste sur recette dix niveaux plus basse. Aucun de ces essais n'est déclaré réalisé ; Macro Chain reste reporté.
