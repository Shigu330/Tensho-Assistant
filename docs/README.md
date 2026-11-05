# Documentation de Tensho Assistant

| Dossier / fichier | Pour qui | Contenu |
|---|---|---|
| [../README.md](../README.md) | tout le monde | présentation (anglais puis français), pages et fonctions, installation, compilation, crédits |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | contributeurs | compiler, tester, conventions, preuves, licences, versions et releases, publication Dalamud |
| [../SECURITY.md](../SECURITY.md) | tout le monde | signaler une vulnérabilité, surfaces sensibles du plugin (réseau, actions dans le jeu) |
| [../CHANGELOG.md](../CHANGELOG.md) | tout le monde | ce qui a changé à chaque version (français, puis `### English`) |
| [../CLAUDE.md](../CLAUDE.md) | agent IA | point d'entrée de reprise, procédure par version, consignes |
| [ai/](ai/) | agent IA | [RULES](ai/RULES.md) (règles durables), [STATUS](ai/STATUS.md) (état, à valider, prochaine action ; historique dans [STATUS-ARCHIVE](ai/STATUS-ARCHIVE.md)), [GOAL](ai/GOAL.md) (objectif actif), [PROMPT-MAITRE](ai/PROMPT-MAITRE.md) (cadre d'origine) |
| [project/](project/) | contributeurs et agent | [PROJECT](project/PROJECT.md) (périmètre), [ROADMAP](project/ROADMAP.md) (17 plugins de référence), [GLOBAL-PLAN](project/GLOBAL-PLAN.md) (plan global), [PROPOSALS-TRIAGE](project/PROPOSALS-TRIAGE.md) (tri des propositions), [GAPS](project/GAPS.md) (manques face aux plugins Dalamud et choix retenus), [MAP-PROPOSALS](project/MAP-PROPOSALS.md) (améliorations de la carte à choisir), [ITEMS-PROPOSALS](project/ITEMS-PROPOSALS.md) (objets, banque, tri, marché), [CRAFT-GATHER-PROPOSALS](project/CRAFT-GATHER-PROPOSALS.md) (artisanat, récolte), [TOOLS-PROPOSALS](project/TOOLS-PROPOSALS.md) (alarmes, compagnie libre, quêtes, équipement, mini-jeux), [G-PROPOSALS](project/G-PROPOSALS.md) (propositions et choix provisoires de l'étape g), [BACKLOG](project/BACKLOG.md), [USER-REQUESTS](project/USER-REQUESTS.md) (demandes utilisateur R-01 à R-14), [DECISIONS](project/DECISIONS.md), [ARCHITECTURE](project/ARCHITECTURE.md), [TESTING](project/TESTING.md) (protocoles), [REFERENCES](project/REFERENCES.md) (API et sources vérifiées), [PUBLISHING](project/PUBLISHING.md) (publication Dalamud) |
| [goals/](goals/) | contributeurs et agent | un document par objectif livré ou en cours (`<ID>.md`, par exemple [R-06](goals/R-06.md), [R-11](goals/R-11.md)), plus des études (`*-STUDY.md`) et des plans (`*-PLAN.md`) : demande, preuves, protocole de test en jeu |
| [evidence/](evidence/) | contributeurs et agent | sorties de sondes et extraits de journaux cités par le code (preuves des liens de données), le plus souvent nommés `<version>-<sujet>` |
| [../Localization/README.md](../Localization/README.md) | traducteurs | ajouter ou compléter une langue |
| [../community-help/README.md](../community-help/README.md) · [../community-templates/README.md](../community-templates/README.md) | communauté | vidéos et guides de l'aide · galerie des modèles de Mes outils |
| [../publish/d17/](../publish/d17/) | auteur | brouillon de soumission au dépôt officiel Dalamud : [PULL_REQUEST](../publish/d17/PULL_REQUEST.md), `testing/live/TenshoAssistant/manifest.toml`, icône |
| `../tools/` · `../scripts/` | contributeurs | sondes de données en lecture seule (`ShopCurrencyProbe`, `ApiProbe`…) · `Verify.ps1` (contrôles) et `Release.ps1` (commit et release) |

Règle de cohérence : le code fait foi. Un document qui contredit le code est corrigé ; un fait non vérifié est marqué comme tel, avec sa date.
Les documents de `goals/` sont des archives datées : leur « état » est celui de la date indiquée ; l'état courant est dans [ai/STATUS.md](ai/STATUS.md).
