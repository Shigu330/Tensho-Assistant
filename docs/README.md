# Tensho Assistant documentation

> *Traduction française plus bas.*

| Folder / file | For whom | Content |
|---|---|---|
| [../README.md](../README.md) | everyone | presentation (English, then French), pages and features, installation, build, credits |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | contributors | build, test, conventions, evidence, licences, adding a tool, versions and releases, Dalamud rules |
| [../SECURITY.md](../SECURITY.md) | everyone | reporting a vulnerability, sensitive surfaces of the plugin (network, local files, actions in the game) |
| [../CHANGELOG.md](../CHANGELOG.md) | everyone | what changed in each version (French, then `### English`) |
| [project/](project/) | contributors and agent | [PROJECT](project/PROJECT.md) (scope), [ROADMAP](project/ROADMAP.md) (the 17 reference plugins and the user requests R-01 to R-14), [ARCHITECTURE](project/ARCHITECTURE.md) (code map), [PUBLISHING](project/PUBLISHING.md) (Dalamud rules kept), and the working documents listed below |
| [evidence/](evidence/) | contributors and agent | probe outputs and log excerpts cited by the code (evidence of the data links), most named `<version>-<subject>`; in English |
| [../Localization/README.md](../Localization/README.md) | translators | adding or completing a language |
| [../community-help/README.md](../community-help/README.md) · [../community-templates/README.md](../community-templates/README.md) · [../community-discoveries/README.md](../community-discoveries/README.md) | community | videos and guides of the Help · gallery of « My tools » · shared discoveries (exchange through GitHub, file format) |
| [../publish/d17/](../publish/d17/) | author | draft of a submission to the official Dalamud repository, not planned (DEC-039): [PULL_REQUEST](../publish/d17/PULL_REQUEST.md), `testing/live/TenshoAssistant/manifest.toml`, icon |
| [../publish/github/](../publish/github/README.md) | maintainer | the 0.323 drafts of the GitHub workflows (the active ones are in `.github/`) |
| [../tools/](../tools/README.md) · `../scripts/` | contributors | read-only data probes (`ShopCurrencyProbe`, `ApiProbe`…) and data scripts (`discoveries-merge.py`: shared discoveries) · `Verify.ps1` (checks), `Release.ps1` (commit and release), `New-Tool.ps1` (new page), `Update-ToolGallery.ps1` (tool gallery) |

Working documents of [project/](project/), changed with almost every version and written in French for now (their English translation is
planned just before 1.0.0): [GLOBAL-PLAN](project/GLOBAL-PLAN.md) (global plan and current state), [TESTING](project/TESTING.md)
(in-game test protocols), [SETTINGS](project/SETTINGS.md) (inventory of the settings: default, where, real effect), [CONTENT-TOOLS](project/CONTENT-TOOLS.md) (content tools study, X-90), [BACKLOG](project/BACKLOG.md), [USER-REQUESTS](project/USER-REQUESTS.md) (user requests),
[DECISIONS](project/DECISIONS.md), [GAPS](project/GAPS.md) (gaps against other Dalamud plugins and choices made),
[REFERENCES](project/REFERENCES.md) (verified APIs and sources), [PROPOSALS-TRIAGE](project/PROPOSALS-TRIAGE.md) (sorting of the
proposals), [MAP-PROPOSALS](project/MAP-PROPOSALS.md) (map), [ITEMS-PROPOSALS](project/ITEMS-PROPOSALS.md) (items, bank, sorting,
market), [CRAFT-GATHER-PROPOSALS](project/CRAFT-GATHER-PROPOSALS.md) (crafting, gathering),
[TOOLS-PROPOSALS](project/TOOLS-PROPOSALS.md) (alarms, free company, quests, gear, mini-games), [G-PROPOSALS](project/G-PROPOSALS.md)
(proposals and provisional choices of step g), [COMPETITIVE-OFF-PLAN](project/COMPETITIVE-OFF-PLAN.md) (Tensho stops in PvP and ranked
content, DEC-038).

Consistency rule: the code is the reference. A document that contradicts the code is fixed; a fact that was not checked is marked as such,
with its date. The current state, what is done and what is validated in game, is in [project/GLOBAL-PLAN.md](project/GLOBAL-PLAN.md).
Some documents of `project/` still name old goal sheets (`goals/`) and agent files (`ai/`): they are kept out of the repository since
0.802, so a link to them does not open on GitHub.

---

# Documentation de Tensho Assistant

| Dossier / fichier | Pour qui | Contenu |
|---|---|---|
| [../README.md](../README.md) | tout le monde | présentation (anglais, puis français), pages et fonctions, installation, compilation, crédits |
| [../CONTRIBUTING.md](../CONTRIBUTING.md) | contributeurs | compiler, tester, conventions, preuves, licences, ajouter un outil, versions et releases, règles Dalamud |
| [../SECURITY.md](../SECURITY.md) | tout le monde | signaler une vulnérabilité, surfaces sensibles du plugin (réseau, fichiers locaux, actions dans le jeu) |
| [../CHANGELOG.md](../CHANGELOG.md) | tout le monde | ce qui a changé à chaque version (français, puis `### English`) |
| [project/](project/) | contributeurs et agent | [PROJECT](project/PROJECT.md) (périmètre), [ROADMAP](project/ROADMAP.md) (les 17 plugins de référence et les demandes R-01 à R-14), [ARCHITECTURE](project/ARCHITECTURE.md) (carte du code), [PUBLISHING](project/PUBLISHING.md) (règles Dalamud tenues), et les documents de travail listés plus bas |
| [evidence/](evidence/) | contributeurs et agent | sorties de sondes et extraits de journaux cités par le code (preuves des liens de données), le plus souvent nommés `<version>-<sujet>` ; en anglais |
| [../Localization/README.md](../Localization/README.md) | traducteurs | ajouter ou compléter une langue |
| [../community-help/README.md](../community-help/README.md) · [../community-templates/README.md](../community-templates/README.md) · [../community-discoveries/README.md](../community-discoveries/README.md) | communauté | vidéos et guides de l'aide · galerie de « Mes outils » · découvertes partagées (échange par GitHub, format du fichier) |
| [../publish/d17/](../publish/d17/) | auteur | brouillon d'une soumission au dépôt officiel Dalamud, non prévue (DEC-039) : [PULL_REQUEST](../publish/d17/PULL_REQUEST.md), `testing/live/TenshoAssistant/manifest.toml`, icône |
| [../publish/github/](../publish/github/README.md) | mainteneur | les brouillons de la 0.323 des workflows GitHub (les actifs sont dans `.github/`) |
| [../tools/](../tools/README.md) · `../scripts/` | contributeurs | sondes de données en lecture seule (`ShopCurrencyProbe`, `ApiProbe`…) et scripts de données (`discoveries-merge.py` : découvertes partagées) · `Verify.ps1` (contrôles), `Release.ps1` (commit et release), `New-Tool.ps1` (nouvelle page), `Update-ToolGallery.ps1` (galerie d'outils) |

Documents de travail de [project/](project/), changés à presque chaque version et écrits en français pour l'instant (leur traduction
anglaise est prévue juste avant la 1.0.0) : [GLOBAL-PLAN](project/GLOBAL-PLAN.md) (plan global et état actuel),
[TESTING](project/TESTING.md) (protocoles d'essai en jeu), [CONTENT-TOOLS](project/CONTENT-TOOLS.md) (étude des outils de contenus, X-90), [SETTINGS](project/SETTINGS.md) (inventaire des réglages : défaut, où,
effet réel), [BACKLOG](project/BACKLOG.md), [USER-REQUESTS](project/USER-REQUESTS.md)
(demandes de l'utilisateur), [DECISIONS](project/DECISIONS.md), [GAPS](project/GAPS.md) (manques face aux autres plugins Dalamud et
choix retenus), [REFERENCES](project/REFERENCES.md) (API et sources vérifiées), [PROPOSALS-TRIAGE](project/PROPOSALS-TRIAGE.md) (tri
des propositions), [MAP-PROPOSALS](project/MAP-PROPOSALS.md) (carte), [ITEMS-PROPOSALS](project/ITEMS-PROPOSALS.md) (objets, banque,
tri, marché), [CRAFT-GATHER-PROPOSALS](project/CRAFT-GATHER-PROPOSALS.md) (artisanat, récolte),
[TOOLS-PROPOSALS](project/TOOLS-PROPOSALS.md) (alarmes, compagnie libre, quêtes, équipement, mini-jeux),
[G-PROPOSALS](project/G-PROPOSALS.md) (propositions et choix provisoires de l'étape g),
[COMPETITIVE-OFF-PLAN](project/COMPETITIVE-OFF-PLAN.md) (Tensho se coupe en JcJ et en contenu classé, DEC-038).

Règle de cohérence : le code fait foi. Un document qui contredit le code est corrigé ; un fait non vérifié est marqué comme tel, avec sa
date. L'état courant, ce qui est fait et ce qui est validé en jeu, est dans [project/GLOBAL-PLAN.md](project/GLOBAL-PLAN.md). Des
documents de `project/` nomment encore d'anciennes fiches d'objectifs (`goals/`) et des fichiers de l'agent (`ai/`) : ils sont gardés
hors du dépôt depuis la 0.802, donc un lien vers eux ne s'ouvre pas sur GitHub.
