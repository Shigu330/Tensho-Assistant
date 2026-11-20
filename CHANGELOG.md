# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

Summary per version, in French then `### English` (English part required since 0.219.0); the English part is also the body of the
commits and of the release notes on GitHub. Evidence cited: docs/evidence/.
Each version was built and tested by `scripts/Verify.ps1`; in-game validation is stated when the user reported it.
This file keeps the last release and the versions after it; older ones are in [docs/changelog/](docs/changelog/)
(moved by `tools/changelog.py archive`, one file per hundred versions).

## 0.1569.0
- Plan global, X-625 : Outil des découvertes partagées (tools/discoveries-merge.py) : sa sortie passe en UTF-8 avant tout affichage ; lancé avec `python -I` seul sous Windows (console cp1252), `sync --dry-run` s'arrêtait sur la flèche « → » du rapport des faits à revérifier.
- Tests : MergeUtf81569Tests. Verify.ps1 (agent) : 3194/3194, 0 avertissement, 0 erreur ; DLL 0.1569.0.0. Rien à essayer en jeu.
### English
- Shared discoveries tool (tools/discoveries-merge.py): its output is turned to UTF-8 before anything is printed; run with plain `python -I` on Windows (cp1252 console), `sync --dry-run` stopped on the « → » arrow of the recheck report.

## 0.1568.0
- Plan global, X-610 : Fiche d'objet, partie « Comment l'obtenir » : les secteurs d'exploration en aéronef et en sous-marin où l'objet peut être trouvé sont listés (noms des secteurs et des mers tirés des feuilles du jeu ; butin : LuminaSupplemental pour les aéronefs, relevés de FFXIVGachaSpreadsheet pour les sous-marins, déjà embarqués, source citée sous les lignes). Ces explorations comptent comme une source connue : « Aucune source connue » ne s'affiche plus pour ces objets (Fibre alchimique, 12926 : points 10 et 11 en aéronef et des secteurs de sous-marins).
- Pour les joueurs : fiche d'objet : « Comment l'obtenir » liste maintenant les secteurs d'aéronefs et de sous-marins où trouver l'objet.
- Tests : VoyageItemSources1568Tests. Verify.ps1 (agent) : 3192/3192, 0 avertissement, 0 erreur ; DLL 0.1568.0.0. Non validé en jeu.
### English
- Item sheet, « How to get it » part: the airship and submarine exploration sectors where the item may be found are listed (sector and sea names from the game's sheets; loot: LuminaSupplemental for airships, FFXIVGachaSpreadsheet records for submarines, already embedded, source cited under the lines). These voyages count as a known source: « No known source » no longer shows for such items (Alchemic Fibre, 12926: airship points 10 and 11 and submarine sectors).
- For players: Item sheet: « How to get it » now lists the airship and submarine sectors where an item can be found.
