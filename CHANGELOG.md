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

## 0.1486.0
- Plan global, X-503 : Relecture de 0.1482 (X-503, M1 et M2). M1 : les marges étroites de la fenêtre de chat restent les siennes : le panneau de la roue dentée, les menus du clic droit et les info-bulles qu'elle ouvre reprennent les marges habituelles (rendues au contenu après Begin, PanelWindow). M2 : les réglages de X-484 (joueurs cachés et mis en avant, mots gardés ou cachés, majuscules, autres messages, filtres du jeu) passent par des composants communs (TextSetting, CheckSetting, SettingButton) : la recherche des réglages et SETTINGS.md les trouvent ; leurs info-bulles disent leur valeur par défaut. M3 (noms d'autres joueurs écrits dans la configuration) attend l'accord de l'utilisateur (R04).
- Pour les joueurs : les menus et le panneau de réglages de la fenêtre de chat retrouvent des marges normales, et ses réglages de joueurs et de mots se trouvent par la recherche des réglages.
- Tests : ChatReview1486Tests. Verify.ps1 (agent) : 2909/2909, 0 avertissement, 0 erreur ; DLL 0.1486.0.0. Non validé en jeu.
### English
- Review of 0.1482 (X-503, M1 and M2). M1: the chat window's narrow margins stay its own: the gear's panel, the right-click menus and the tooltips it opens get the usual margins back (given back to the contents after Begin, PanelWindow). M2: the settings of X-484 (hidden and highlighted players, words kept or hidden, capitals, other messages, the game's filters) go through shared helpers (TextSetting, CheckSetting, SettingButton): the settings' search and SETTINGS.md find them; their tooltips name their default. M3 (other players' names written in the configuration) waits for the user's agreement (R04).
- For players: The chat window's menus and settings panel have normal margins again, and its player and word settings can be found by the settings' search.

## 0.1485.0
- Plan global, X-502 : Relecture de 0.1482 (X-502) : le contour du texte de la fenêtre de chat pâlit avec elle. En mode « S'effacer », le texte pâlissait mais son contour noir restait à environ 85 % d'opacité (les couleurs d'une liste de dessin ne prennent pas l'alpha de la fenêtre) ; le contour prend maintenant cet alpha (ChatLook.Outline).
- Pour les joueurs : quand la fenêtre de chat pâlit derrière une fenêtre du jeu, le contour sombre de son texte pâlit aussi.
- Tests : ChatOutline1485Tests. Verify.ps1 (agent) : 2907/2907, 0 avertissement, 0 erreur ; DLL 0.1485.0.0. Non validé en jeu.
### English
- Review of 0.1482 (X-502): the outline of the chat window's text fades with it. In « Fade » mode the text faded but its black outline stayed about 85 % opaque (a draw list's colours do not take the window's alpha); the outline now takes that alpha (ChatLook.Outline).
- For players: When the chat window fades behind a game window, the dark outline of its text now fades too.

## 0.1484.0
- Plan global, X-501 : Relecture de 0.1482 (X-501) : chaque onglet de la fenêtre de chat garde de nouveau sa propre position de défilement. Depuis 0.1481 les lignes sont dessinées hors de l'onglet, dans une seule zone pour tous : un onglet ouvert reprenait le défilement du précédent, avec le bouton « retour en bas », et ses lignes n'étaient pas marquées lues. La zone des lignes porte maintenant le numéro de son onglet (ChatFormat.LinesId).
- Pour les joueurs : chaque onglet du chat garde de nouveau sa propre position ; ouvrir un autre onglet ne le montre plus remonté.
- Tests : ChatTabScroll1484Tests. Verify.ps1 (agent) : 2906/2906, 0 avertissement, 0 erreur ; DLL 0.1484.0.0. Non validé en jeu.
### English
- Review of 0.1482 (X-501): each tab of the chat window keeps its own scroll position again. Since 0.1481 the lines were drawn outside the tab, in one area for all tabs: a tab opened at the scroll of the one before, with the « back to the bottom » button, and its lines were not marked read. The lines' area is now named after its tab (ChatFormat.LinesId).
- For players: Each chat tab keeps its own scroll position again; opening another tab no longer starts it scrolled up.

## 0.1483.0
- Plan global, X-500 : Relecture de 0.1482 (X-500) : la carte du jeu que Tensho remplace par la sienne n'est plus vue à l'écran par le lecteur des fenêtres du jeu. Le remplacement déplace seulement son nœud racine hors de l'écran (les X et Y de la fenêtre ne changent pas) ; le lecteur saute maintenant une fenêtre dont le nœud racine est hors de l'écran (GameWindows.HostedOffScreen, même valeur GameWindows.OffScreen que le remplacement). La fenêtre de chat ne se cache plus ni ne pâlit quand la carte du jeu est ouverte mais remplacée.
- Pour les joueurs : la fenêtre de chat ne se cache plus quand la carte du jeu s'ouvre alors que celle de Tensho la remplace.
- Tests : ChatReview1483Tests. Verify.ps1 (agent) : 2905/2905, 0 avertissement, 0 erreur ; DLL 0.1483.0.0. Non validé en jeu.
### English
- Review of 0.1482 (X-500): the game map that Tensho replaces with its own is no longer seen on screen by the game windows' reader. The replacement moves only its root node off screen (the window's X and Y do not change); the reader now skips a window whose root node is off screen (GameWindows.HostedOffScreen, the same GameWindows.OffScreen value as the replacement). The chat window no longer hides or fades when the game map is open but replaced.
- For players: The chat window no longer hides when the game map opens while Tensho's map replaces it.

## 0.1482.0
- Plan global, X-484 : Fenêtre de chat de Tensho : chaque onglet peut se régler avec les filtres de la fenêtre de discussion du jeu (feuille LogFilter, noms dans la langue du client) : Discussion (canaux, LS et LSIM 1 à 8, emotes standard et personnalisées), Annonces, et les lignes de combat par sujet (vous, équipiers, alliance, autres joueurs, ennemis combattus ou non, PNJ alliés, compagnons de chacun) et par genre (dégâts, attaques manquées, actions, objets, soins, effets, fin des effets), lus du sujet et de la cible que Dalamud donne pour chaque message. Préréglages « comme Général, Combat, Événement du jeu », tout, rien ; « autres messages » pour les types que le jeu ne liste pas. En plus du jeu : mots à garder ou à cacher (majuscules au choix) par onglet, joueurs cachés ou mis en avant. Reste à faire : montures et téléportations des autres (relevé en jeu d'abord), alertes de X-447.
- Pour les joueurs : fenêtre de chat : chaque onglet peut utiliser les filtres du jeu (combat selon qui fait quoi à qui), plus des mots et des joueurs à cacher ou à mettre en avant.
- Tests : ChatFilters1482Tests. Verify.ps1 (agent) : 2904/2904, 0 avertissement, 0 erreur ; DLL 0.1482.0.0. Non validé en jeu.
### English
- Tensho's chat window: each tab can use the filters of the game's log window (LogFilter sheet, names in the client's language): Discussion (channels, LS and CWLS 1 to 8, standard and custom emotes), Announcements, and the combat lines by subject (you, party, alliance, other players, engaged or other enemies, friendly NPCs, everyone's pets) and by kind (damage, misses, actions, items, healing, effects, effects ending), read from the subject and target Dalamud gives for each message. Presets « as the game's General, Battle, Event », all, none; « other messages » for kinds the game does not list. Beyond the game: words kept or hidden (capitals or not) per tab, players hidden or put forward. Still to do: others' mounts and teleports (in-game reading first), X-447's alerts.
- For players: Chat window: each tab can use the game's own log filters (combat by who does what to whom), plus words and players to hide or put forward.

## 0.1481.0
- Plan global, X-476 : Fenêtre de chat de Tensho plus proche du chat du jeu : heure entre crochets ([22:47]), contour sombre des lettres (réglage, activé par défaut), fond plus léger par défaut (40 %), marges serrées ; onglets au-dessus ou sous les messages (réglage) ; au bout des onglets, un « + » qui ajoute un onglet et une roue dentée qui ouvre les réglages du chat, les mêmes que dans la page Paramètres (un changement d'un côté se voit de l'autre).
- Pour les joueurs : fenêtre de chat : plus proche du chat du jeu (heure entre crochets, texte avec contour, fond plus léger, onglets dessus ou dessous) ; « + » ajoute un onglet et une roue dentée ouvre les réglages du chat.
- Tests : ChatLook1481Tests. Verify.ps1 (agent) : 2899/2899, 0 avertissement, 0 erreur ; DLL 0.1481.0.0. Non validé en jeu.
### English
- Tensho's chat window closer to the game's chat: time in brackets ([22:47]), dark outline around the letters (setting, on by default), lighter background by default (40%), narrow margins; tabs over or under the messages (setting); at the end of the tabs, a « + » that adds a tab and a gear that opens the chat's settings, the same as on the Settings page (a change on one side shows on the other).
- For players: Chat window: closer to the game's chat (time in brackets, outlined text, lighter background, tabs over or under); « + » adds a tab and a gear opens the chat's settings.

## 0.1480.0
- Plan global, X-497 : Relecture de 0.1479 (X-497, X-498). La carte du jeu, à l'écran, compte comme une fenêtre du jeu pour la fenêtre de chat de Tensho : elle la cache (ou la pâlit, selon le réglage « Sous une fenêtre du jeu ») ; elle était ignorée par son nom depuis 0.537. Les panneaux, les barres et la carte de Tensho continuent de l'ignorer ; hébergée hors écran par Tensho, elle ne touche rien. En mode « S'effacer », toute la fenêtre de chat pâlit (barre de titre, cadre et poignée compris), pas seulement son contenu. Lien d'un plugin dans le chat de Tensho : si Tensho n'arrive pas à relire le lien, l'erreur est notée comme celle de Tensho et le plugin reçoit le texte du lien ; seule une erreur du plugin lui-même s'affiche dans l'info-bulle.
- Pour les joueurs : fenêtre de chat : la carte du jeu par-dessus la cache aussi ; pâlie, toute la fenêtre pâlit.
- Tests : ChatReview1480Tests. Verify.ps1 (agent) : 2896/2896, 0 avertissement, 0 erreur ; DLL 0.1480.0.0. Non validé en jeu.
### English
- Review of 0.1479 (X-497, X-498). The game's map, on screen, counts as a game window for Tensho's chat window: it hides it (or fades it, as the « Under a game window » setting says); it was ignored by name since 0.537. Tensho's panels, bars and map keep ignoring it; hosted off screen by Tensho, it touches nothing. In « fade » mode the whole chat window fades (title bar, frame and grip included), not only its content. A plugin's link in Tensho's chat: when Tensho cannot re-read the link, the error is logged as Tensho's and the plugin gets the link's text; only an error of the plugin itself shows on the tooltip.
- For players: Chat window: the game's map over it now hides it too; faded, the whole window fades.

## 0.1479.0
- Plan global, X-485 : Fenêtre de chat de Tensho : elle passe derrière les autres fenêtres de Tensho (jamais ramenée devant par un clic, mise au fond quand elle apparaît). Nouveau réglage « Sous une fenêtre du jeu » (une fenêtre dessinée par Dalamud ne peut pas passer sous celles du jeu) : se cacher (par défaut ; elle revient seule quand la fenêtre du jeu se ferme), s'effacer (sans fond, texte pâle, les clics vont au jeu) ou rester. Ce réglage remplace pour le chat celui des panneaux qui s'écartent devant les fenêtres du jeu.
- Pour les joueurs : fenêtre de chat : reste derrière les fenêtres de Tensho ; nouveau réglage pour ce qu'elle fait sous une fenêtre du jeu (se cacher, s'effacer ou rester).
- Tests : ChatCover1479Tests. Verify.ps1 (agent) : 2893/2893, 0 avertissement, 0 erreur ; DLL 0.1479.0.0. Non validé en jeu.
### English
- Tensho's chat window: it goes behind Tensho's other windows (never brought to the front by a click, put at the back when it appears). New setting « Under a game window » (a window drawn by Dalamud cannot go under the game's): hide (the default; it comes back by itself once the game window closes), fade (no background, faint text, clicks go to the game) or stay. For the chat, this setting replaces the panels' one that steps aside from the game's windows.
- For players: Chat window: stays behind Tensho's windows; new setting for what it does under a game window (hide, fade or stay).
