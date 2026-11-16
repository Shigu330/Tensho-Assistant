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

## 0.1478.0
- Plan global, X-495 : Relecture du bloc 0.1475 à 0.1477. X-495 : la couleur choisie pour le nom d'un onglet du chat est rendue lisible sur les cinq fonds d'un onglet (inactif, actif, survolé, et les deux d'une fenêtre sans le focus, l'état habituel de la fenêtre de chat) ; le thème de Tensho donne aussi leurs couleurs aux onglets d'une fenêtre sans le focus (avant : les couleurs sombres de Dalamud, même en thème clair). X-496 M1 : un clic sur un lien de Dalamud ou d'un plugin passe à son gestionnaire la chaîne du lien elle-même (du lien à sa fin), comme le fait Dalamud (preuve : docs/evidence/0.1478-dalamud-link-handler.md) ; un échec du gestionnaire est dit dans l'info-bulle du lien. M2 : un clic droit sur ce lien ouvre le menu du message (copier). M4 : la sonde HelpTextProbe (find) nomme à la fin les feuilles qu'elle n'a pas pu lire. M3 (outil de l'agent, hors version) : chiffre « mesuré » seulement avec une marge, sinon un intervalle.
- Pour les joueurs : fenêtre de chat : la couleur du nom d'un onglet reste lisible quand la fenêtre n'a pas le focus ; clic droit sur le lien d'un plugin : le menu du message.
- Tests : ChatReview1478Tests. Verify.ps1 (agent) : 2891/2891, 0 avertissement, 0 erreur ; DLL 0.1478.0.0. Non validé en jeu.
### English
- Review of block 0.1475 to 0.1477. X-495: the colour chosen for a chat tab's name is made legible on the five backgrounds of a tab (inactive, active, hovered, and the two of a window without the focus, the chat window's usual state); Tensho's theme also gives its colours to the tabs of a window without the focus (before: Dalamud's dark ones, also in the light theme). X-496 M1: a click on a link of Dalamud or of a plugin passes the link's own string to its handler (from the link to its end), as Dalamud does (evidence: docs/evidence/0.1478-dalamud-link-handler.md); a failure of the handler is said on the link's tooltip. M2: a right click on that link opens the message's menu (copy). M4: the HelpTextProbe probe (find) names at the end the sheets it could not read.
- For players: Chat window: a tab's name colour stays legible when the window is not focused; right click on a plugin's link opens the message's menu.

## 0.1477.0
- Plan global, X-474 : Fenêtre de chat de Tensho : un lien de Dalamud ou d'un plugin (par exemple « Learn more here » du message « Developer Mode is enabled ») est de nouveau un lien, souligné ; un clic gauche lance le gestionnaire que Dalamud garde pour ce lien, comme un clic dans le chat du jeu (sur le fil du jeu ; le message transmis est le texte du lien). Si plus aucun plugin ne répond à ce lien, il reste du texte simple avec une info-bulle : l'ouvrir depuis le chat du jeu.
- Pour les joueurs : fenêtre de chat : les liens de Dalamud et des plugins (comme « Learn more here ») s'ouvrent comme dans le chat du jeu.
- Tests : ChatDalamudLink1477Tests. Verify.ps1 (agent) : 2885/2885, 0 avertissement, 0 erreur ; DLL 0.1477.0.0. Non validé en jeu.
### English
- Tensho's chat window: a link of Dalamud or of a plugin (for instance « Learn more here » of the « Developer Mode is enabled » message) is a link again, underlined; a left click runs the handler Dalamud keeps for that link, as a click in the game's chat does (on the game's thread; the message passed is the link's text). When no plugin answers that link any more, it stays plain text with a tooltip: open it from the game's chat.
- For players: Chat window: links of Dalamud and plugins (such as « Learn more here ») open as they do in the game's chat.
