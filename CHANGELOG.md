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

## 0.1490.0
- Plan global, X-505 : Fenêtre de chat (X-505, capture de l'utilisateur du 16/11 vers 10 h : fond toujours opaque, même avec l'opacité à 0 %). La zone des lignes recevait du thème un fond de panneau opaque, dessiné par-dessus le fond de la fenêtre : le réglage d'opacité ne se voyait pas. La zone n'a plus de fond propre : seul le fond de la fenêtre, avec son opacité (15 % par défaut), se voit.
- Pour les joueurs : le fond de la fenêtre de chat de Tensho suit maintenant son réglage d'opacité : par défaut il est presque invisible, comme le chat du jeu.
- Tests : ChatBackground1490Tests. Verify.ps1 (agent) : 2924/2924, 0 avertissement, 0 erreur ; DLL 0.1490.0.0. Non validé en jeu.
### English
- Chat window (X-505, the user's capture of 11/16 around 10 am: background still opaque, even with the opacity at 0 %). The lines' area got an opaque panel background from the theme, drawn over the window's background: the opacity setting did not show. The area no longer has a background of its own: only the window's, with its opacity (15 % by default), shows.
- For players: The Tensho chat window's background now follows its opacity setting: by default it is nearly invisible, as the game's chat.

## 0.1489.0
- Plan global, X-504 : Relecture de 0.1486 (X-504, M1 et M2). M1 : le contour du texte est dessiné en quatre copies qui se superposent ; en mode « S'effacer », chaque copie est maintenant pâlie pour que les quatre ensemble soient pâlies comme le texte (le halo n'est plus plus sombre que le texte, 69 % avant là où les quatre se recouvraient). M2 : la zone des lignes porte le numéro propre à la vue de l'onglet, plus sa place : après « Retirer » un onglet, les suivants gardent leur propre défilement.
- Pour les joueurs : dans la fenêtre de chat de Tensho, le contour des lettres pâlit autant que le texte quand la fenêtre s'efface, et retirer un onglet ne change plus le défilement des autres.
- Tests : ChatReview1489Tests. Verify.ps1 (agent) : 2922/2922, 0 avertissement, 0 erreur ; DLL 0.1489.0.0. Non validé en jeu.
### English
- Review of 0.1486 (X-504, M1 and M2). M1: the text's outline is drawn in four overlapping copies; in « Fade » mode each copy is now faded so that the four together fade as the text does (the halo is no longer darker than the text, 69 % before where the four overlapped). M2: the lines' area carries the tab's view's own number, no longer its place: after « Remove » a tab, the next tabs keep their own scroll.
- For players: In the Tensho chat window, the letters' outline fades as much as the text when the window fades, and removing a tab no longer moves the other tabs' scroll.

## 0.1488.0
- Plan global, X-505 : Fenêtre de chat à l'aspect du chat du jeu (X-505, captures de l'utilisateur du 16/11). Chaque écart est un réglage, la valeur du jeu par défaut : onglets sous les messages ; fond presque invisible (15 %, contre 40 %) ; barre de défilement fine et discrète ; heure sans zéro devant ([9:21]), avec un zéro sur demande ; liens dessinés comme dans le jeu (flèche orange devant, texte dans la couleur du message, sans soulignement) ; onglets gris arrondis, celui qui est affiché marqué d'un point ; lignes repliées en retrait. Une configuration enregistrée avant reçoit une fois les onglets en bas et le fond léger (un fond choisi par le joueur est gardé).
- Pour les joueurs : la fenêtre de chat de Tensho ressemble au chat du jeu : onglets en bas, fond presque invisible, barre de défilement fine, heure écrite [9:21], liens avec la flèche orange du jeu, onglets arrondis et lignes repliées en retrait ; chaque point se remet comme avant dans ses réglages.
- Tests : ChatLook1488Tests. Verify.ps1 (agent) : 2920/2920, 0 avertissement, 0 erreur ; DLL 0.1488.0.0. Non validé en jeu.
### English
- Chat window looking like the game's chat (X-505, the user's captures of 11/16). Each difference is a setting, the game's value by default: tabs under the messages; a background nearly invisible (15 %, was 40 %); a thin, discreet scroll bar; the hour without a leading zero ([9:21]), with one on request; links drawn as in the game (an orange arrow before them, their text in the message's colour, not underlined); rounded grey tabs, the one shown marked by a dot; wrapped lines indented. A configuration saved before gets the tabs at the bottom and the light background once (a background chosen by the player is kept).
- For players: The Tensho chat window now looks like the game's chat: tabs at the bottom, a nearly invisible background, a thin scroll bar, the time as [9:21], links with the game's orange arrow, rounded tabs and indented wrapped lines; each can be changed back in its settings.

## 0.1487.0
- Plan global, X-506 : Liens de la fenêtre de chat (X-506, X-479). X-506 : un lien dont la seule partie visible est la flèche du jeu (le lien de lieu des messages d'alarme de Tensho) s'affiche comme une flèche, sans nom : le nom de la zone n'est plus collé au nom de l'objet lié qui suit (« Thanalan occidentalTerreau thanalanais III ») ; le nom de la cible n'est écrit que pour un lien qui sinon ne montrerait rien. X-479 : la charge utile d'un message dit par le joueur (« test <objet> test », 16/11) porte le nom de l'objet comme texte du lien ; relevés du 16/11 gardés dans docs/evidence/0.1487-chat-link-said.md et rejoués par les tests.
- Pour les joueurs : dans la fenêtre de chat de Tensho, les messages d'alarme montrent le lien de lieu comme une flèche, comme le chat du jeu, sans le nom de la zone collé au nom de l'objet.
- Tests : ChatLinks1487Tests. Verify.ps1 (agent) : 2913/2913, 0 avertissement, 0 erreur ; DLL 0.1487.0.0. Non validé en jeu.
### English
- Chat window links (X-506, X-479). X-506: a link whose only visible piece is the game's arrow (the place link of Tensho's alarm messages) shows as an arrow, with no name: the zone's name is no longer glued to the next linked item's name; the target's name is written only for a link that would otherwise show nothing. X-479: the payload of a message said by the player (« test <item> test », 11/16) carries the item's name as the link's text; the records of 11/16 are kept in docs/evidence/0.1487-chat-link-said.md and replayed by the tests.
- For players: In the Tensho chat window, alarm messages show the place link as an arrow like the game's chat, without the zone's name glued to the item's name.

## 0.1486.0
- Plan global, X-503 : Relecture de 0.1482 (X-503, M1 et M2). M1 : les marges étroites de la fenêtre de chat restent les siennes : le panneau de la roue dentée, les menus du clic droit et les info-bulles qu'elle ouvre reprennent les marges habituelles (rendues au contenu après Begin, PanelWindow). M2 : les réglages de X-484 (joueurs cachés et mis en avant, mots gardés ou cachés, majuscules, autres messages, filtres du jeu) passent par des composants communs (TextSetting, CheckSetting, SettingButton) : la recherche des réglages et SETTINGS.md les trouvent ; leurs info-bulles disent leur valeur par défaut. M3 (noms d'autres joueurs écrits dans la configuration) attend l'accord de l'utilisateur (R04).
- Pour les joueurs : les menus et le panneau de réglages de la fenêtre de chat retrouvent des marges normales, et ses réglages de joueurs et de mots se trouvent par la recherche des réglages.
- Tests : ChatReview1486Tests. Verify.ps1 (agent) : 2909/2909, 0 avertissement, 0 erreur ; DLL 0.1486.0.0. Non validé en jeu.
### English
- Review of 0.1482 (X-503, M1 and M2). M1: the chat window's narrow margins stay its own: the gear's panel, the right-click menus and the tooltips it opens get the usual margins back (given back to the contents after Begin, PanelWindow). M2: the settings of X-484 (hidden and highlighted players, words kept or hidden, capitals, other messages, the game's filters) go through shared helpers (TextSetting, CheckSetting, SettingButton): the settings' search and SETTINGS.md find them; their tooltips name their default. M3 (other players' names written in the configuration) waits for the user's agreement (R04).
- For players: The chat window's menus and settings panel have normal margins again, and its player and word settings can be found by the settings' search.
