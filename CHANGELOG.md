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

## 0.1539.0
- X-570 (audit du 17/11, partie A, points 6 et 9), partie suivie : Verify.ps1 ne cherche plus le SDK .NET dans l'ancien dossier C:\ia-code (le plus récent ~/.codex/dotnet-10.*, sinon dotnet) ; TESTING le dit. tools/client-snapshot.py prend le SDK par la même règle (plus de version écrite en dur, TENSHO_DOTNET accepté). .github/codeql/codeql-config.yml retiré : aucun workflow ne le lisait. Test ToolPaths1539Tests.
- Tests : ToolPaths1539Tests. Verify.ps1 (agent) : 3079/3079, 0 avertissement, 0 erreur ; DLL 0.1539.0.0. Rien à essayer en jeu.
### English
- X-570 (audit of 17/11, part A, items 6 and 9), tracked part: Verify.ps1 no longer looks for the .NET SDK in the old C:\ia-code folder (the newest ~/.codex/dotnet-10.*, else dotnet); TESTING says so. tools/client-snapshot.py picks the SDK by the same rule (no hard-coded version, TENSHO_DOTNET accepted). .github/codeql/codeql-config.yml removed: no workflow read it. Test ToolPaths1539Tests.

## 0.1538.0
- Plan global, X-567 : X-567 (audit du 17/11, partie B) : quatre textes vus par les joueurs disent ce que fait le code. FAQ de la fenêtre de chat : le chat du jeu n'est plus « jamais caché » ; l'option « Cacher les messages du chat du jeu » (désactivée par défaut, depuis 0.1504) y est nommée. FAQ et README (application de bureau) : les alarmes sonnent depuis l'application (ligne de la barre d'état, son et notification Windows, par défaut quand le jeu n'est pas au premier plan), au lieu de « restent vides ». SECURITY : historique du chat « 2 000 messages et 30 jours par défaut » (7 avant, le code dit 30). FAQ du Jardin : chemin Suivi › Jardinage › onglet Jardin › « Vue détaillée ». Test FaqTruth1538Tests : chaque texte lié au code (libellé de l'option, ChatHistory.DefaultDays et DefaultMessages, groupe et onglet de la page Jardin).
- Pour les joueurs : la FAQ, le README et la page SECURITY disent maintenant ce que fait le plugin : l'option de la fenêtre de chat peut cacher le chat du jeu, l'application de bureau sonne les alarmes, l'historique du chat garde 30 jours par défaut, et la vue détaillée du jardin se trouve dans Suivi › Jardinage.
- Tests : FaqTruth1538Tests. Verify.ps1 (agent) : 3077/3077, 0 avertissement, 0 erreur ; DLL 0.1538.0.0. Rien à essayer en jeu.
### English
- X-567 (audit of 17/11, part B): four texts players read now say what the code does. Chat window FAQ: the game chat is no longer « never hidden »; the « Hide the game chat's messages » option (off by default, since 0.1504) is named. FAQ and README (desktop app): alarms ring from the app (status bar line, Windows sound and notification, by default while the game is not in front) instead of « stay empty ». SECURITY: chat history « 2,000 messages and 30 days by default » (7 before; the code says 30). Garden FAQ: path Tracking › Garden › Garden tab › « Detailed view ». Test FaqTruth1538Tests: each text tied to the code (the option's label, ChatHistory.DefaultDays and DefaultMessages, the Garden page's group and tab).
- For players: The FAQ, README and SECURITY page now match the plugin: the game chat can be hidden by the chat window's option, the desktop app rings alarms, the chat history keeps 30 days by default, and the garden's detailed view is found under Tracking › Garden.

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
