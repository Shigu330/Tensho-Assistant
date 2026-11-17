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

## 0.1537.0
- Plan global, X-520 : Images de référence de l'application de bureau : les deux images des réglages des onglets du chat montrent désormais les canaux du premier onglet dépliés, avec leur grille de cases (elle était repliée sous « Canaux » dans les images de la version précédente). Le contrôle des images ne prend plus le nom du canal « Erreurs » du chat pour un état d'erreur de la page.
- Tests : ChatSettingsPicture1537Tests. Verify.ps1 (agent) : 3073/3073, 0 avertissement, 0 erreur ; DLL 0.1537.0.0. Rien à essayer en jeu.
### English
- Reference pictures of the desktop application: the two pictures of the chat's tab settings now show the first tab's channels unfolded, with their grid of boxes (it was folded under « Channels » in the previous version's pictures). The pictures' check no longer takes the chat's « Error » channel name for an error state of the page.

## 0.1536.0
- Plan global, X-520 : Images de référence de l'application de bureau : chaque onglet de la page Paramètres a désormais la sienne (Panneaux, Carte, Suivi, Données, Avancé et, en mode développeur, Débogage, chacun dans sa première catégorie ; Général l'avait déjà), ainsi que les réglages des onglets du chat (grilles des canaux, des filtres du jeu et des genres de combat), en largeur normale et étroite. L'application ouvre l'onglet et la catégorie par une nouvelle entrée de son contrat typé avec le plugin (OpenSettings).
- Tests : SettingsPictures1536Tests. Verify.ps1 (agent) : 3071/3071, 0 avertissement, 0 erreur ; DLL 0.1536.0.0. Rien à essayer en jeu.
### English
- Reference pictures of the desktop application: every tab of the Settings page now has its own (Panels, Map, Tracking, Data, Advanced and, in developer mode, Debug, each in its first category; General had one already), as do the chat's tab settings (the grids of channels, of the game's filters and of combat kinds), at the usual width and narrow. The application opens the tab and the category through a new entry of its typed contract with the plugin (OpenSettings).

## 0.1535.0
- Plan global, X-563 : Relecture du bloc 0.1527 à 0.1534, constats mineurs. Fenêtre de chat : quand on remonte un long historique et qu'un paquet de lignes plus anciennes est mis en page au-dessus de la vue, les lignes restent à leur place dès cette image-là (le défilement corrigé ne prend effet qu'à l'image suivante : les lignes de cette image sont dessinées au défilement de la suivante) ; avant, la vue sautait d'une image à chaque paquet. Préchauffage du code : la fin de sa phase précoce (carte, panneaux, chat), qu'attendent la compilation des lecteurs et la fenêtre de chat, est mesurée et écrite au journal. Dédale bestial : le combat tiré de la disposition du plateau du jeu est dit « lecture à confirmer en jeu », comme le numéro du plateau lu dans la même fenêtre.
- Pour les joueurs : fenêtre de chat : remonter un long historique ne fait plus sauter la vue quand des lignes plus anciennes arrivent au-dessus. Dédale bestial : le combat lu dans le plateau du jeu dit qu'il reste à confirmer.
- Tests : Review1534Minor1535Tests. Verify.ps1 (agent) : 3068/3068, 0 avertissement, 0 erreur ; DLL 0.1535.0.0. Non validé en jeu.
### English
- Review of block 0.1527 to 0.1534, minor findings. Chat window: when scrolling back through a long history and a packet of older lines is laid out above the view, the lines stay in place from that frame on (the corrected scroll takes effect at the next frame only: that frame's lines are drawn at the next one's scroll); before, the view jumped for a frame at each packet. Code warm-up: the end of its early phase (map, panels, chat), which the readers' compilation and the chat window wait for, is measured and written in the log. Beast Labyrinth: the fight taken from the game's board window is said to be « reading still to be confirmed in game », like the board's number read in the same window.
- For players: Chat window: scrolling back through a long history no longer jumps when older lines load above. Beast Labyrinth: the fight read from the game's board says it is still to be confirmed.

## 0.1534.0
- Plan global, X-551 : Dédale bestial, panneau d'équipe : il n'est jamais plus large que la fenêtre du jeu à côté de laquelle il se pose (formation de l'équipe, sinon disposition du plateau ; 460 pixels au plus, 300 au moins) ni plus haut que la disposition du plateau, et ses lignes vont à la ligne dans cette largeur. L'équipe conseillée est un tableau comme « Formation de l'équipe » du jeu : une ligne par bête (icône et nom, rang, attaque automatique), séparée du reste par un trait. Les mêmes lignes sur la page Dresseur › Équipe.
- Pour les joueurs : dédale bestial : le panneau d'équipe reste dans la largeur de la fenêtre du jeu à côté et liste l'équipe en lignes comme la formation de l'équipe du jeu.
- Tests : BeastPanelSize1534Tests. Verify.ps1 (agent) : 3065/3065, 0 avertissement, 0 erreur ; DLL 0.1534.0.0. Non validé en jeu.
### English
- Beast Labyrinth, team panel: it is never wider than the game's window it stands beside (the team formation, else the board layout; 460 pixels at most, 300 at least) nor taller than the board layout, and its lines wrap inside that width. The advised team is a table like the game's team formation: one row per beast (icon and name, rank, automatic attack), set apart from the rest by a line. The same rows on the Beastmaster › Team page.
- For players: Beast Labyrinth: the team panel now stays within the width of the game's window beside it and lists the team in rows like the game's team formation.

## 0.1533.0
- Plan global, X-558 : Fenêtre de chat, suite : après un rechargement, le code du chat est compilé d'avance avec celui de la carte et des panneaux, et la fenêtre de chat affiche « Chargement… » une ou deux secondes au lieu de le compiler pendant une image (mesure du 17/11 13 h 05 après 0.1532 : 62 ms pour la première image du chat, dont 5 ms de lecture des lignes et moins de 1 ms de mise en page).
- Pour les joueurs : fenêtre de chat : plus d'à-coup à sa première image après un rechargement ; elle affiche « Chargement… » un instant à la place.
- Tests : ChatWarmup1533Tests. Verify.ps1 (agent) : 3063/3063, 0 avertissement, 0 erreur ; DLL 0.1533.0.0. Non validé en jeu.
### English
- Chat window, continued: after a reload, the chat's code is compiled ahead with the map's and the panels', and the chat window shows « Loading… » for a second or two instead of compiling it during one frame (measured on 17/11 13:05 after 0.1532: 62 ms for the chat's first frame, 5 ms of it reading the lines and under 1 ms laying them out).
- For players: Chat window: no more stall on its first frame after a reload; it shows « Loading… » for a moment instead.

## 0.1532.0
- Plan global, X-558 : Fenêtre de chat : au chargement (ouverture, rechargement avec un long historique) et après un changement de largeur, les lignes les plus récentes sont mises en page d'abord, dans un budget de temps de 3 ms par image (100 lignes au plus) ; seules les 300 plus récentes sont préparées d'avance ; les plus anciennes ne le sont que quand vous remontez, par paquets de 40 juste au-dessus de ce que vous lisez, sans que la vue saute. Rien n'est perdu ni réordonné. En mode développeur, la ligne « Slow frame » distingue « Chat refresh » et « Chat layout ».
- Pour les joueurs : fenêtre de chat : l'ouverture et le rechargement avec un long historique ne font plus d'à-coup ; les anciennes lignes se préparent quand vous remontez.
- Tests : ChatLoading1532Tests. Verify.ps1 (agent) : 3061/3061, 0 avertissement, 0 erreur ; DLL 0.1532.0.0. Non validé en jeu.
### English
- Chat window: on loading (opening, a reload with a long history) and after a change of width, the newest lines are laid out first, within a time budget of 3 ms per frame (100 lines at most); only the 300 newest are prepared ahead; the older ones only when you scroll back, by packets of 40 just above what you read, without the view jumping. Nothing is lost nor reordered. In developer mode, the « Slow frame » line tells « Chat refresh » and « Chat layout » apart.
- For players: Chat window: opening and reloading with a long history no longer stalls the game; older lines are prepared as you scroll back.

## 0.1531.0
- Plan global, X-562 : Dédale bestial, constats mineurs de la relecture : une équipe conseillée gardée avant 0.1522 redit combien de ses bêtes ont un rang supposé ; les faiblesses non touchées de chaque combat ne sont plus recalculées à chaque image (gardées tant que l'équipe et les bêtes possédées ne changent pas) ; le mot anglais d'une attaque magique sans élément est celui du jeu, « Magical » (Addon 13101, « Magique » en français) ; les nombres 47 à 62 de la fenêtre de résultat du plateau sont écrits dans le journal, pour relever en jeu s'ils nomment les bonus.
- Tests : BeastMinor1531Tests. Verify.ps1 (agent) : 3058/3058, 0 avertissement, 0 erreur ; DLL 0.1531.0.0. Non validé en jeu.
### English
- Beast Labyrinth, minor review findings: an advised team kept before 0.1522 says again how many of its beasts have an assumed rank; the weaknesses not hit in each fight are no longer worked out at each frame (kept while the team and the beasts held do not change); the English word for a magical attack with no element is the game's, « Magical » (Addon 13101, « Magique » in French); the board result window's numbers 47 to 62 are written in the log, to read in game whether they name the bonuses.

## 0.1530.0
- Plan global, X-136 : Dédale bestial, panneau d'équipe : pour chaque bête conseillée du combat montré, le type de sa technique instinctive (aérien, féroce, robuste, arcanique) et les deux combos instinctifs II à faire avec elle : la technique du dresseur à lancer avant la technique de la bête, ou après, avec le combo obtenu (Assaut vital ou Assaut létal). Tiré du jeu lui-même : la Table des types et le tutoriel du dresseur (le cu sith, féroce : Hache rafale avant, ou Hache mistral après), le type de chacune des cinquante techniques des bêtes et des quatre techniques instinctives du dresseur. Le surlignage en combat reste à faire.
- Pour les joueurs : dédale bestial : le panneau dit maintenant, pour chaque bête conseillée, quelle technique du dresseur lancer avant ou après sa technique pour réussir un combo instinctif II.
- Tests : BeastInstinct1530Tests. Verify.ps1 (agent) : 3056/3056, 0 avertissement, 0 erreur ; DLL 0.1530.0.0. Non validé en jeu.
### English
- Beast Labyrinth, team panel: for each advised beast of the fight shown, its Trick's affinity (Volant, Rampant, Durant, Eldritch) and the two intentional combos to make with it: the beastmaster's skill to use before the beast's Trick, or after it, with the combo it completes (Sunstrider or Moonstalker). From the game itself: the Inner Compass and the beastmaster's tutorial (the Cu Sith, Rampant: Gale Axe before, or Mistral Axe after), the affinity of each of the fifty Tricks and of the beastmaster's four instinctual skills. The highlight in combat is still to do.
- For players: Beast Labyrinth: the panel now says, for each advised beast, which beastmaster skill to use before or after its Trick to complete an intentional combo.

## 0.1529.0
- Plan global, X-486 : Dédale bestial, panneau d'équipe : quand la disposition du plateau du jeu montre certains combats (une étape), le panneau les montre d'abord, un bloc par combat : ses ennemis (icône, faiblesse, étoiles de force, intelligence, résistances et vitalité) et leurs actions (nom, type de dégâts, forme, cible, tirés des feuilles du jeu), puis les trois bêtes pour ce combat, prises dans l'équipe que vous avez emmenée quand elle est lue, sinon dans l'équipe conseillée, chacune avec la raison (« son attaque Contondant touche la faiblesse de Chevalier du plateau »). Le reste (votre équipe pour tout le plateau, l'équipe conseillée) est replié sous « Tout le plateau ». Le panneau suit la fenêtre du jeu (lue quatre fois par seconde, sans rien y envoyer). La page Dresseur montre aussi le type de dégâts des actions des ennemis.
- Pour les joueurs : dédale bestial : devant la formation de l'équipe, le panneau montre d'abord le combat que montre le plateau du jeu, avec ses ennemis, leurs actions et les trois bêtes pour lui avec la raison ; tout le plateau est replié en dessous.
- Tests : BeastFightNow1529Tests. Verify.ps1 (agent) : 3052/3052, 0 avertissement, 0 erreur ; DLL 0.1529.0.0. Non validé en jeu.
### English
- Beast Labyrinth, team panel: when the game's board layout shows some fights (a stage), the panel shows them first, one block per fight: its enemies (icon, weakness, stars of strength, intelligence, resistances and vitality) and their actions (name, type of damage, shape, target, from the game's sheets), then the three beasts for that fight, taken from the team you brought when it is read, else from the advised team, each with the reason (« its Blunt attack hits the weakness of Chevalier du plateau »). The rest (your team for the whole board, the advised team) is folded under « Whole board ». The panel follows the game's window (read four times a second, nothing sent to it). The Beastmaster page also shows the type of damage of the enemies' actions.
- For players: Beast Labyrinth: on the team formation, the panel shows first the fight the game's board shows, with its enemies, their actions and the three beasts for it with why; the whole board is folded below.

## 0.1528.0
- Plan global, X-559 : Fenêtre de chat : les lignes de combat n'entrent plus dans le nombre de messages non lus d'un onglet (ni sur l'étiquette de l'onglet, ni dans « nouveaux messages plus bas ») ; un onglet qui ne montre que le combat reste à zéro, comme le dit DEC-053 (« jamais lues ni comptées »). Réglages du chat : les grilles de cases (canaux, filtres du jeu, genres de lignes de combat) prennent autant de colonnes que la largeur en tient, chacune aussi large que le nom le plus long ; aucun nom n'est coupé ni ne passe sous le bord droit ou sous la barre de défilement, et un nom plus large que la fenêtre elle-même est écrit à côté de sa case, avec retour à la ligne.
- Pour les joueurs : fenêtre de chat : les lignes de combat ne comptent plus dans les non lus d'un onglet, et les cases des réglages du chat tiennent dans la fenêtre à toute largeur, noms en entier.
- Tests : ChatCombatUnread1528Tests. Verify.ps1 (agent) : 3049/3049, 0 avertissement, 0 erreur ; DLL 0.1528.0.0. Non validé en jeu.
### English
- Chat window: combat lines no longer enter a tab's unread count (neither on the tab's label nor in « new messages below »); a tab showing only combat stays at zero, as DEC-053 says (« never read nor counted »). Chat settings: the grids of boxes (channels, the game's filters, kinds of combat line) take as many columns as the width holds, each as wide as the longest name; no name is cut nor goes under the right edge or the scroll bar, and a name wider than the window itself is written beside its box, wrapped.
- For players: Chat window: combat lines no longer add to a tab's unread count, and the chat settings' boxes now fit the window at any width, names in full.

## 0.1527.0
- Plan global, X-561 : Dédale bestial, rangs des bêtes : un rang lu dans la fenêtre « Formation de l'équipe » ne fait plus jamais baisser un rang connu plus haut (les rangs ne baissent pas dans le jeu). Avant, quand le plateau n'était pas encore connu, la fenêtre (qui montre le rang ramené à la limite du plateau, « 5 » pour une bête de rang 7) faisait redescendre le rang gardé sur le disque, et il ne remontait qu'avec un résultat de plateau. Le plateau lu dans la fenêtre est aussi pris avant les rangs (il venait du tour d'avant). Le panneau dit « lu dans la fenêtre du jeu ; son numéro reste à confirmer en jeu » quand le plateau ne vient que du numéro de la fenêtre, pas encore relevé en jeu.
- Pour les joueurs : dédale bestial : le rang d'une bête gardé par Tensho n'est plus baissé par la fenêtre d'équipe, qui montre les rangs ramenés à la limite du plateau.
- Tests : BeastRanksKept1527Tests. Verify.ps1 (agent) : 3046/3046, 0 avertissement, 0 erreur ; DLL 0.1527.0.0. Non validé en jeu.
### English
- Beast Labyrinth, beast ranks: a rank read in the game's team window never lowers a rank known higher any more (ranks never go down in the game). Before, while the board was not yet known, the window (which shows a rank brought down to the board's limit, « 5 » for a rank 7 beast) brought the rank saved on disk down, and it came back only with a board result. The board read in the window is also taken before the ranks (it came from the previous turn). The panel says « read in the game's window; its number is still to be confirmed in game » when the board comes only from the window's number, not yet read in game.
- For players: Beast Labyrinth: a beast's rank kept by Tensho is no longer lowered by the team window, which shows ranks capped at the board's limit.

## 0.1526.0
- Plan global, X-550 : Dédale bestial, panneau d'équipe : les trois bêtes de chaque combat sont prises parmi l'équipe que vous avez vraiment choisie, lue dans la fenêtre « Formation de l'équipe » du jeu (nom, rang et PV de chaque bête, relus à chaque réouverture, donc après chaque combat). Cette partie vient en premier, avant l'équipe conseillée : une bête à terre (0 PV) n'est pas appelée et c'est dit, une bête sous 30 % de ses PV est signalée, et quand une bête conseillée pour un combat manque (pas emmenée ou à terre), le panneau dit laquelle et quelle bête prend sa place. Mêmes règles que l'équipe conseillée (faiblesses d'abord) ; rien n'est choisi ni envoyé au jeu à votre place.
- Pour les joueurs : dédale bestial : le panneau d'équipe conseille maintenant depuis l'équipe que vous avez vraiment prise, dit quelles bêtes sont à terre ou à PV bas, et quelle bête remplace une bête conseillée laissée de côté.
- Tests : BeastTeamRead1526Tests. Verify.ps1 (agent) : 3043/3043, 0 avertissement, 0 erreur ; DLL 0.1526.0.0. Non validé en jeu.
### English
- Beast Labyrinth, team panel: the three beasts of each fight are taken among the team you actually chose, read in the game's team window (each beast's name, rank and HP, read again each time it opens, so after each fight). This part comes first, before the advised team: a beast down (0 HP) is not called and this is said, a beast under 30 % of its HP is flagged, and when a beast advised for a fight is missing (not taken or down), the panel says which one and which beast takes its place. Same rules as the advised team (weaknesses first); nothing is chosen nor sent to the game for you.
- For players: Beast Labyrinth: the team panel now advises from the team you actually took, says which beasts are down or low on HP, and which beast replaces an advised one you left behind.
