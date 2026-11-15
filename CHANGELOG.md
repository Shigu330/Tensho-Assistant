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

## 0.1371.0
- Plan global, X-373 : Tests : le test « Settings saver 1164 : le déchargement écrit ce qui reste » ne dépend plus d'un délai réel. L'écriture en cours est retenue par une porte que le test ouvre lui-même, au lieu de 400 ms de vrai temps qu'une machine de la CI lente pouvait dépasser (échec « the wait ran out », CI de 0.1354.0). Même comportement vérifié : l'attente expire, rien n'est écrit tant que la porte est fermée, puis les réglages vivants finissent sur le disque, sans écriture simultanée.
- Tests : SettingsSaver1164Tests. Verify.ps1 (agent) : 2576/2576, 0 avertissement, 0 erreur ; DLL 0.1371.0.0. Rien à essayer en jeu.
### English
- Tests: the test « Settings saver 1164: unload writes what is left » no longer depends on a real delay. The write under way is held by a gate the test opens itself, instead of 400 ms of real time that a slow CI machine could outlast (failure « the wait ran out », CI of 0.1354.0). Same behaviour checked: the wait runs out, nothing is written while the gate is shut, then the live settings end on disk, never two writes at once.

## 0.1370.0
- Plan global, X-367 : Échange des découvertes : quand GitHub ne répond pas, refuse la demande, dépasse son délai ou n'a pas encore publié le fichier commun, la page dit « GitHub injoignable ou fichier commun pas encore publié, réessayez plus tard » au lieu de « Fichier commun illisible » (relecture du bloc 0.1346 à 0.1349, M1 ; décision D-159). Un fichier vraiment abîmé (JSON illisible, empreinte fausse) reste « illisible ». Hors version : l'outil de propreté de l'agent refuse un mode ou une option mal tapés (M2) et l'échec de son auto-test, comme celui du garde-fou, arrête la publication (M3) ; le générateur de la mesure de charge de l'outil de fusion suit les règles de X-183 et son auto-test passe (X-362).
- Pour les joueurs : échange des découvertes : quand GitHub est injoignable, la page dit de réessayer plus tard au lieu de dire le fichier commun illisible.
- Tests : CommonUnreachable1370Tests. Verify.ps1 (agent) : 2576/2576, 0 avertissement, 0 erreur ; DLL 0.1370.0.0. Non validé en jeu.
### English
- Discoveries exchange: when GitHub does not answer, refuses the request, runs past its delay or has not published the common file yet, the page says « GitHub could not be reached or the common file is not published yet; try again later » instead of « Common file unreadable » (review of block 0.1346 to 0.1349, M1; decision D-159). A file that is really damaged (unreadable JSON, wrong hash) still says « unreadable ». Outside the version: the agent's cleanliness tool refuses a mistyped mode or option (M2) and a failed selftest of it, as of the guard, stops the publication (M3); the load-test generator of the merge tool follows the X-183 rules and its selftest passes (X-362).
- For players: Discoveries exchange: when GitHub cannot be reached, the page now says to try again later instead of calling the common file unreadable.

## 0.1369.0
- Plan global, X-381 : Révision des textes X-356, second regard sur les constats écartés faute de preuve (D-160) : chacun sondé de nouveau, puis un vérificateur sceptique par constat. Tranchés avec une preuve et corrigés : « Chroniques du zodiaque » / « Trials of the Braves » au lieu de « livre du zodiaque », « Zodiac book » et « relic book » (Addon#8330.0, Quest#66972.0 ; 7 clés par langue) ; « cible d'élite » aux 7 clés françaises qui disaient encore « marque (de chasse) » (Achievement#979.2 ; l'anglais « hunt mark », que le client écrit aussi, est gardé) ; « Client » au lieu de « Mandateur » pour le client d'un mandat (feuille LeveClient sondée : 282 noms sur 282 commencent par « Client : » ; l'aide ne dit plus que le client donne le mandat) ; « vaincus » au lieu de « victimes » et « victoires » dans le bestiaire (LogMessage#1002.5, Addon#10441.0 ; 6 clés) ; les libellés que Dalamud affiche dans la FAQ (« Mettre à jour les plugins », « Dalamud › Paramètres › Expérimental › Dépôts de plugins personnalisés », d'après la traduction française installée avec Dalamud). Déjà tranché : « Armes antiques » (D-161). Restent deux questions de vocabulaire pour l'utilisateur : « contenu » ou « mission » pour « duty », « vendeur » ou « marchand » pour un PNJ qui vend. Preuve : docs/evidence/0.1369-second-look-terms.md.
- Pour les joueurs : les mots du jeu à quelques endroits de plus : « Chroniques du zodiaque » pour le livre du zodiaque, « cible d'élite », « Client » pour le client d'un mandat, « vaincus » dans le bestiaire ; la FAQ nomme les boutons de Dalamud comme Dalamud les écrit.
- Tests : SecondLook1369Tests. Verify.ps1 (agent) : 2573/2573, 0 avertissement, 0 erreur ; DLL 0.1369.0.0. Non validé en jeu.
### English
- X-356 text review, second look at the findings set aside for lack of proof (D-160): each one probed again, then a sceptical checker per finding. Settled with a proof and fixed: « Chroniques du zodiaque » / « Trials of the Braves » instead of « livre du zodiaque », « Zodiac book » and « relic book » (Addon#8330.0, Quest#66972.0; 7 keys per language); « cible d'élite » in the 7 French keys that still said « marque (de chasse) » (Achievement#979.2; English « hunt mark », which the client writes too, is kept); « Client » instead of « Mandateur » for the client of a leve (LeveClient sheet probed: 282 names out of 282 start with « Client : »; the help no longer says the client gives the leve); « vaincus » instead of « victimes » and « victoires » in the hunting log (LogMessage#1002.5, Addon#10441.0; 6 keys); the labels Dalamud shows, in the FAQ (« Update plugins », and in French « Dalamud › Paramètres › Expérimental › Dépôts de plugins personnalisés », from the French translation installed with Dalamud). Already settled: « Armes antiques » (D-161). Two vocabulary questions are left for the user: « contenu » or « mission » for « duty », « vendeur » or « marchand » for an NPC who sells. Evidence: docs/evidence/0.1369-second-look-terms.md.
- For players: The game's own words in a few more places: Trials of the Braves for the Zodiac book, and in French « cible d'élite », « Client » for a leve's client and « vaincus » in the hunting log; the FAQ names Dalamud's buttons as Dalamud writes them.

## 0.1368.0
- Plan global, X-397 : Relecture du bloc 0.1362 à 0.1364, constat I1 : les listes d'artisanat portaient encore deux noms en français (« Liste(s) de fabrication » dans le titre de la page, l'onglet du simulateur, le panneau et les lignes de l'aide à la récolte, à côté de « liste d'artisanat » dans leurs infobulles). Un seul nom maintenant, « liste d'artisanat », titres compris ; l'infobulle d'ajout cite le vrai titre de la page. Test CraftListName1368Tests (insensible à la casse, titre cité égal au titre de la page).
- Pour les joueurs : les listes d'artisanat ont un seul nom partout en français.
- Tests : CraftListName1368Tests. Verify.ps1 (agent) : 2570/2570, 0 avertissement, 0 erreur ; DLL 0.1368.0.0. Non validé en jeu.
### English
- Review of block 0.1362 to 0.1364, finding I1: the craft lists still had two French names (« Liste(s) de fabrication » in the page title, the simulator tab, the panel and the gathering help lines, beside « liste d'artisanat » in their tooltips). One name now, « liste d'artisanat », titles included; the add tooltip quotes the page's real title. Test CraftListName1368Tests (case-insensitive, quoted title equal to the page title).
- For players: French: craft lists have a single name everywhere.
