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

## 0.1958.0
- Plan global, X-914 : Images de référence : une nouvelle image « Reminders-tasks » montre Rappels › Tâches avec deux tâches d'essai (un compteur avec ses boutons « - » et « + » en icônes, une tâche quotidienne), données en mémoire par l'application de bureau seulement et reprises après l'image, jamais enregistrées : les boutons de compteur en icônes de 0.1954 sont maintenant vus par la comparaison des images (relecture de 0.1956, M2). Les sous-titres de section de 0.1953 restent hors des images : la police de section n'est chargée que dans le jeu (hors du jeu, le sous-titre se dessine dans la police de base, dans la couleur d'avant) ; constat écarté pour cette raison, un essai en jeu les vérifie.
- Tests : CounterPicture1958Tests. Verify.ps1 (agent) : 4355/4355, 0 avertissement, 0 erreur ; DLL 0.1958.0.0. Non validé en jeu.
### English
- Reference pictures: a new picture « Reminders-tasks » shows Reminders › Tasks with two trial tasks (a counter with its « - » and « + » icon buttons, a daily task), given in memory by the desktop application only and taken back after the picture, never saved: the counters' icon buttons of 0.1954 are now seen by the pictures' comparison (review of 0.1956, M2). The section subtitles of 0.1953 stay out of the pictures: the section font is loaded in the game only (out of the game, the subtitle is drawn in the base font, in the colour of before); finding set aside for that reason, an in-game test checks them.

## 0.1957.0
- Plan global, X-913 : Les gardes de survol des info-bulles riches écrites à la main avec une autre condition (« x is not null && … », « hovered || … ») ou entre accolades passent aussi par RichTipHovered() (même test), et le cliquet les lit toutes : toute condition « if » qui contient le test de survol simple (non nié) devant une instruction ou un bloc qui dessine un Draw…Tooltip est refusée (relecture de 0.1956, M1). Rien ne change à l'écran.
- Tests : UiRichTips1957Tests. Verify.ps1 (agent) : 4352/4352, 0 avertissement, 0 erreur ; DLL 0.1957.0.0. Rien à essayer en jeu.
### English
- The hover guards of rich tooltips written by hand with another condition (« x is not null && … », « hovered || … ») or in braces also go through RichTipHovered() (the same test), and the ratchet reads them all: any « if » condition holding the plain hover test (not negated) in front of a statement or block that draws a Draw…Tooltip is refused (review of 0.1956, M1). Nothing changes on screen.

## 0.1956.0
- Plan global, X-607 : Code plus simple à entretenir (X-607, demande du 18/11) : les info-bulles riches (objet, action, lieu, quête, mission, ALEA, PNJ, recette, récolte…) s'ouvrent derrière une seule garde, `RichTipHovered()` (`UI/Components/Tooltip.cs`), au lieu du test de survol écrit à la main sur chaque page (31 endroits, et `ItemTip`) : même test qu'avant (pas sur un élément grisé), rien ne change à l'écran. CONTRIBUTING la nomme dans ses deux moitiés. Nouveau test `UiRichTips1956Tests` : le test de survol écrit à la main devant une info-bulle riche est refusé.
- Tests : UiRichTips1956Tests. Verify.ps1 (agent) : 4349/4349, 0 avertissement, 0 erreur ; DLL 0.1956.0.0. Rien à essayer en jeu.
### English
- Simpler code to maintain (X-607, request of 18/11): the rich tooltips (item, action, place, quest, duty, FATE, NPC, recipe, gathering…) open behind one guard, `RichTipHovered()` (`UI/Components/Tooltip.cs`), instead of the hover test written by hand on each page (31 places, and `ItemTip`): the same test as before (not on a greyed item), nothing changes on screen. CONTRIBUTING names it in both halves. New test `UiRichTips1956Tests`: the hover test written by hand in front of a rich tooltip is refused.
