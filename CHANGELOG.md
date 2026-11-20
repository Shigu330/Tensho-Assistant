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

## 0.1597.0
- Plan global, X-640 : Découvertes : quand des découvertes à vous mises de côté (abîmées) attendent leur copie sur votre gist et que GitHub refuse durablement l'adresse brute de cette copie (gist.githubusercontent.com : 404, 401, 403…), l'échange le dit (« GitHub a refusé… ») au lieu de « GitHub injoignable », compte les refus d'un échange à l'autre (github.json, gardé au redémarrage) et, au 3e échange de suite, envoie sans cette copie et le dit ; le texte abîmé reste gardé dans la base. « Injoignable » (5xx, 408, 429, réseau) attend toujours sans limite. Avant, tout refus bloquait l'envoi sans fin ni cause dite (X-640, banc X-631, relecture 0.1564 M1 ; décision D-190 a de l'utilisateur).
- Pour les joueurs : échange des découvertes : quand GitHub refuse sans cesse la copie de découvertes abîmées sur votre gist, Tensho le dit et, après 3 échanges, envoie le reste sans elles au lieu d'attendre sans fin.
- Tests : GistRefused1597Tests. Verify.ps1 (agent) : 3274/3274, 0 avertissement, 0 erreur ; DLL 0.1597.0.0. Rien à essayer en jeu.
### English
- Discoveries: when discoveries of yours set aside (damaged) wait for their copy on your gist and GitHub durably refuses that copy's raw address (gist.githubusercontent.com: 404, 401, 403…), the exchange says so (« GitHub refused… ») instead of « GitHub not reached », counts the refusals from one exchange to the next (github.json, kept across restarts) and, at the 3rd exchange in a row, sends without that copy and says it; the damaged text stays kept in the base. « Not reached » (5xx, 408, 429, network) still waits without limit. Before, any refusal blocked the upload with no end and no cause given (X-640, bench X-631, review 0.1564 M1; user decision D-190 a).
- For players: Discoveries exchange: when GitHub keeps refusing the saved copy of damaged discoveries on your gist, Tensho now tells you why and, after 3 exchanges, sends the rest without them instead of waiting forever.

## 0.1596.0
- Plan global, X-652 : Découvertes : une ligne illisible (JSON abîmé) de votre base est mise de côté au démarrage (gardée à part, jamais effacée) et l'envoi à votre gist reprend dans la même session, la copie de ce fait sur votre gist relue d'abord ; avant, l'envoi restait bloqué jusqu'au démarrage suivant. Une mise de côté qui échoue garde le blocage. Le journal dit la reprise (X-652, décision D-188 b de l'utilisateur, avec D-190 a).
- Tests : DamagedRow1596Tests. Verify.ps1 (agent) : 3269/3269, 0 avertissement, 0 erreur ; DLL 0.1596.0.0. Rien à essayer en jeu.
### English
- Discoveries: an unreadable row (damaged JSON) of your base is set aside at start (kept apart, never erased) and the upload to your gist resumes in the same session, the gist's copy of that fact read back first; before, the upload stayed blocked until the next start. A set-aside that fails keeps the block. The log says the resumption (X-652, user decision D-188 b, with D-190 a).

## 0.1595.0
- Plan global, X-651 : Test du tri de l'inventaire d'exemple : le cas de l'ordre de remplissage vérifie de nouveau l'ordre entre les sacs (première pile triée en case 1 du sac 1, 36e en case 1 du sac 3, 71e en case 1 du sac 2, comparées à l'ordre trié de l'aperçu ; les piles du résultat suivent les cases, pas l'ordre de tri), pas seulement le compte des piles par sac (X-651, relecture 0.1594 M1).
- Tests : SortTrial1593Tests. Verify.ps1 (agent) : 3265/3265, 0 avertissement, 0 erreur ; DLL 0.1595.0.0. Rien à essayer en jeu.
### English
- Test of the sample inventory sort: the fill-order case checks the order between the bags again (first sorted stack in bag 1's first slot, 36th in bag 3's first slot, 71st in bag 2's first slot), not only the stack count per bag (X-651, review 0.1594 M1).

## 0.1594.0
- Plan global, X-649 : Inventaire d'exemple du tri (application de bureau) : la recherche de case libre est bornée aux 35 cases d'un sac et échoue avec un message clair au-delà, au lieu de tourner sans fin (X-649, R07). Le test de l'ordre de remplissage trie vraiment l'exemple par InventorySortRules.Build et compte les piles placées dans chaque sac (X-648).
- Tests : SortTrial1594Tests. Verify.ps1 (agent) : 3265/3265, 0 avertissement, 0 erreur ; DLL 0.1594.0.0. Rien à essayer en jeu.
### English
- Sample inventory of the sort (desktop application): the free-slot search is bounded to a bag's 35 slots and fails with a clear message beyond them, instead of looping (X-649, R07). The fill-order test really sorts the sample with InventorySortRules.Build and counts the stacks placed in each bag (X-648).

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
