# Documentation de Tensho Assistant

| Dossier / fichier | Pour qui | Contenu |
|---|---|---|
| [../README.md](../README.md) | tout le monde | présentation, fonctions, installation, crédits |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | contributeurs | compiler, tester, conventions, preuves, licences, publication Dalamud |
| [../CHANGELOG.md](../CHANGELOG.md) | tout le monde | ce qui a changé à chaque version |
| [../CLAUDE.md](../CLAUDE.md) | agent IA | point d'entrée de reprise, procédure par version, consignes |
| [ai/](ai/) | agent IA | [RULES](ai/RULES.md) (règles durables), [STATUS](ai/STATUS.md) (état, à valider, prochaine action), [GOAL](ai/GOAL.md) (objectif actif), [PROMPT-MAITRE](ai/PROMPT-MAITRE.md) (cadre d'origine) |
| [project/](project/) | contributeurs et agent | [PROJECT](project/PROJECT.md) (périmètre), [ROADMAP](project/ROADMAP.md) (17 plugins de référence), [BACKLOG](project/BACKLOG.md), [USER-REQUESTS](project/USER-REQUESTS.md) (demandes utilisateur à planifier), [DECISIONS](project/DECISIONS.md), [ARCHITECTURE](project/ARCHITECTURE.md), [TESTING](project/TESTING.md) (protocoles), [REFERENCES](project/REFERENCES.md) (API et sources vérifiées), [PUBLISHING](project/PUBLISHING.md) (publication Dalamud) |
| [goals/](goals/) | contributeurs et agent | un document par objectif livré ou en cours (`<ID>.md`) : demande, preuves, protocole de test en jeu |
| [evidence/](evidence/) | contributeurs et agent | sorties de sondes et extraits de journaux cités par le code (preuves des liens de données) |
| [../Localization/README.md](../Localization/README.md) | traducteurs | ajouter ou compléter une langue |

Règle de cohérence : le code fait foi. Un document qui contredit le code est corrigé ; un fait non vérifié est marqué comme tel, avec sa date.
Les documents de `goals/` sont des archives datées : leur « état » est celui de la date indiquée ; l'état courant est dans [ai/STATUS.md](ai/STATUS.md).
