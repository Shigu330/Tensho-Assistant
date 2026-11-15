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
