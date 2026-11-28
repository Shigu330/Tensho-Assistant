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

## 0.1955.0
- Plan global, X-607 : Code plus simple à entretenir (X-607, demande du 18/11) : la barre de pages d'une longue liste (« < Page n / m > »), écrite à la main dans Fabriquer depuis le stock et le Lexique, passe par un seul composant, `Pager` (`UI/Components/Pager.cs`) : mêmes boutons, mêmes identifiants, mêmes info-bulles, même ligne de page ; rien ne change à l'écran. CONTRIBUTING le nomme dans ses deux moitiés. Nouveau test `UiPager1955Tests` : un bouton de page écrit à la main est refusé, le composant garde les bouts grisés et les info-bulles, les deux pages s'en servent.
- Tests : UiPager1955Tests. Verify.ps1 (agent) : 4347/4347, 0 avertissement, 0 erreur ; DLL 0.1955.0.0. Rien à essayer en jeu.
### English
- Simpler code to maintain (X-607, request of 18/11): the page bar of a long list (« < Page n / m > »), written by hand in Craft from stock and the Lexicon, goes through one component, `Pager` (`UI/Components/Pager.cs`): the same buttons, ids, tooltips and page line; nothing changes on screen. CONTRIBUTING names it in both halves. New test `UiPager1955Tests`: a page button written by hand is refused, the component keeps the greyed ends and the tooltips, both pages use it.

## 0.1954.0
- Plan global, X-607 : Pages plus homogènes (X-607, demande du 18/11, accord de l'utilisateur du 28/11) : les boutons « + » et « - » des compteurs, écrits à la main en texte, passent par le bouton à icône commun de Tensho (`ToolButton`, icônes Plus et Moins, centrées, avec leur info-bulle) : compteur d'un outil de Mes outils (grisé dans l'aperçu), compteur d'une tâche perso et son « + » dans la liste des tâches à faire. Info-bulles nouvelles en anglais et en français. CONTRIBUTING le dit dans ses deux moitiés. Nouveau test `UiCounterButtons1954Tests` : un bouton « + » ou « - » en texte est refusé, les compteurs passent par les icônes, les info-bulles existent dans les deux langues.
- Pour les joueurs : les boutons « + » et « - » des compteurs (Mes outils, tâches perso) sont maintenant des icônes avec une info-bulle.
- Tests : UiCounterButtons1954Tests. Verify.ps1 (agent) : 4345/4345, 0 avertissement, 0 erreur ; DLL 0.1954.0.0. Non validé en jeu.
### English
- More consistent pages (X-607, request of 18/11, the user's approval of 28/11): the « + » and « - » buttons of the counters, written by hand as text, go through Tensho's shared icon button (`ToolButton`, Plus and Minus icons, centred, with their tooltip): a tool's counter in My tools (greyed in the preview), a personal task's counter and its « + » in the list of tasks to do. New tooltips in English and French. CONTRIBUTING says so in both halves. New test `UiCounterButtons1954Tests`: a « + » or « - » button as text is refused, the counters go through the icons, the tooltips exist in both languages.
- For players: The « + » and « - » buttons of the counters (My tools, personal tasks) are now icons with a tooltip.

## 0.1953.0
- Plan global, X-607 : Pages plus homogènes (X-607, demande du 18/11, accord de l'utilisateur du 28/11) : les sous-titres des pages écrits à la main (texte doré seul sur sa ligne : Diagnostic, Aide, Objets, Événements, info-bulle du marché…) passent par le composant commun `UiComponents.SubTitle` : la police de section du jeu, dorée, la même partout. Quatre lignes dorées restent : des remarques, pas des titres. CONTRIBUTING nomme le composant dans ses deux moitiés. Nouveau test `UiSubTitles1953Tests` : la forme écrite à la main est refusée, le composant reste en police de section dorée, les pages s'en servent.
- Pour les joueurs : les sous-titres dans les pages ont maintenant tous la police de section du jeu.
- Tests : UiSubTitles1953Tests. Verify.ps1 (agent) : 4343/4343, 0 avertissement, 0 erreur ; DLL 0.1953.0.0. Non validé en jeu.
### English
- More consistent pages (X-607, request of 18/11, the user's approval of 28/11): the sub-titles of the pages written by hand (a gold text alone on its line: Diagnostics, Help, Items, Events, the market tooltip…) go through the shared component `UiComponents.SubTitle`: the game's section font, gold, the same everywhere. Four gold lines stay: remarks, not titles. CONTRIBUTING names the component in both halves. New test `UiSubTitles1953Tests`: the form written by hand is refused, the component stays in the gold section font, the pages use it.
- For players: The sub-titles inside the pages now all use the game's section font.

## 0.1952.0
- Plan global, X-910 : Garde-fou des lignes vides (X-910, relecture de 0.1951, M1) : le contrôle de X-908 voit maintenant les panneaux dont les drapeaux viennent d'une constante (les trois panneaux de listes, `ListPanelFlags`), les fenêtres ouvertes par `Panels?.Show` sans taille ni contrainte de largeur (pêche, voyage en mer, personnage…) et les aides appelées par leur contenu, à toute profondeur ; les menus, info-bulles et fenêtres enfants ouverts dedans sont des fenêtres à part et sont laissés. Un `PushTextWrapPos` n'exempte plus que l'`EmptyState` dessiné après lui et avant son `PopTextWrapPos`. Aucune fenêtre ne change : aucun de ces endroits n'appelait `EmptyState` seul. Test `UiEmptyStateAutoSize1951Tests` élargi (7 cas au lieu de 4).
- Tests : UiEmptyStateAutoSize1951Tests. Verify.ps1 (agent) : 4340/4340, 0 avertissement, 0 erreur ; DLL 0.1952.0.0. Rien à essayer en jeu.
### English
- Empty-line check (X-910, review of 0.1951, M1): the X-908 check now sees panels whose flags come from a constant (the three list panels, `ListPanelFlags`), windows opened by `Panels?.Show` without a size or width constraint (fishing, ocean voyage, character…) and the helpers their contents call, at any depth; popups, menus, tooltips and child windows opened inside are windows of their own and are left out. A `PushTextWrapPos` now exempts only an `EmptyState` drawn after it and before its `PopTextWrapPos`. No window changes: none of these places drew `EmptyState` alone. Test `UiEmptyStateAutoSize1951Tests` widened (7 cases instead of 4).
