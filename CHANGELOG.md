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

## 0.1571.0
- Plan global, X-626 : Listes du chat (relecture de 0.1568, M1 à M3) : le texte en cours dans la case « Ajouter… » est gardé par liste et par onglet (il passait d'un onglet à l'autre) ; après Entrée, le curseur reste dans la case pour la ligne suivante ; « Ne plus mettre X en avant » retire aussi une ligne enregistrée avec le monde (« Nom@Monde ») ; avec « Respecter la casse », les mots d'un onglet s'ajoutent avec leurs majuscules (« Foo » à côté de « foo »).
- Pour les joueurs : listes du chat : ce qu'on tape dans une case « Ajouter… » reste dans son onglet, Entrée garde le curseur dans la case, « Ne plus mettre en avant » marche pour un nom écrit avec son monde, et les mots gardent leurs majuscules avec « Respecter la casse ».
- Tests : ChatLists1571Tests. Verify.ps1 (agent) : 3197/3197, 0 avertissement, 0 erreur ; DLL 0.1571.0.0. Non validé en jeu.
### English
- Chat lists (review of 0.1568, M1 to M3): the text being typed in the « Add » box is kept by list and by tab (it moved from one tab to another); after Enter, the cursor stays in the box for the next line; « No longer put X forward » also removes a line saved with the world (« Name@World »); with « Match case », a tab's words are added with their capitals (« Foo » beside « foo »).
- For players: Chat lists: what you type in an « Add » box stays in that tab, Enter keeps the cursor in the box, « No longer put forward » works for names saved with their world, and words keep their capitals when « Match case » is on.

## 0.1570.0
- Plan global, X-627 : Test de la sortie UTF-8 de l'outil des découvertes partagées (MergeUtf81569Tests) : le second cas lançait l'aide de l'outil, sans « → », avec PYTHONIOENCODING ignoré sous `python -I` ; il ne pouvait pas échouer sur le code d'avant la correction. Il lance maintenant l'outil derrière une sortie cp1252 sur un rapport hors ligne qui contient « → », et vérifie que la même exécution sur une copie de l'outil sans la correction s'arrête sur une UnicodeEncodeError.
- Tests : MergeUtf81569Tests. Verify.ps1 (agent) : 3194/3194, 0 avertissement, 0 erreur ; DLL 0.1570.0.0. Rien à essayer en jeu.
### English
- Test of the shared discoveries tool's UTF-8 output (MergeUtf81569Tests): the second case ran the tool's help, which holds no « → », with PYTHONIOENCODING ignored under `python -I`; it could not fail on the code from before the fix. It now runs the tool behind a cp1252 output on an offline report that holds « → », and checks that the same run on a copy of the tool without the fix stops on a UnicodeEncodeError.

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
