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

## 0.1593.0
- Plan global, X-647 : Relecture de 0.1592 (X-647, constat mineur M1 sur X-394) : l'inventaire d'exemple de l'image « SortPreview-trial » passe de 30 à 75 piles, plus que deux sacs ; après le tri, Sac 1 et Sac 3 sont pleins et Sac 2 reçoit le reste : l'ordre de remplissage (1, 3, 2, 4) se voit aux piles, plus seulement aux titres. Dans l'application de bureau, l'aperçu d'exemple défile jusqu'au pied des cadres : les quatre sacs sont entiers en 1280x860. SortTrial1593Tests vérifie le débordement, l'ordre et le défilement.
- Tests : SortTrial1593Tests. Verify.ps1 (agent) : 3262/3262, 0 avertissement, 0 erreur ; DLL 0.1593.0.0. Rien à essayer en jeu.
### English
- Review of 0.1592 (X-647, minor finding M1 on X-394): the sample inventory of the « SortPreview-trial » picture goes from 30 to 75 stacks, more than two bags; after the sort, bags 1 and 3 are full and bag 2 takes the rest: the fill order (1, 3, 2, 4) shows by the stacks, not only by the headers. In the desktop application, the sample preview scrolls to the frames' foot: the four bags are whole in 1280x860. SortTrial1593Tests checks the overflow, the order and the scrolling.

## 0.1592.0
- Plan global, X-394 : Suite de 0.1591 (X-394) : l'image « SortPreview-trial » était demandée à 1400 px de haut ; la fenêtre, ramenée à la taille de l'écran (1061 px), décalait le découpage des cadres des sacs (en-têtes « Sac » et premières lignes coupés). L'image est prise à 860 px comme les autres ; SortTrial1591Tests vérifie qu'aucune image ne dépasse 860 px.
- Tests : SortTrial1591Tests. Verify.ps1 (agent) : 3260/3260, 0 avertissement, 0 erreur ; DLL 0.1592.0.0. Rien à essayer en jeu.
### English
- Follow-up of 0.1591 (X-394): the « SortPreview-trial » picture was asked 1400 px tall; the window, cut to the screen's size (1061 px), shifted the clipping of the bags' frames (the « Bag » headers and first rows cut). The picture is taken at 860 px like the others; SortTrial1591Tests checks that no picture is taller than 860 px.
