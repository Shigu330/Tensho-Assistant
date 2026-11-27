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

## 0.1882.0
- Plan global, X-875 : Relecture du bloc 0.1880 à 0.1881 (X-875, I1, I2, M1) : la description du manifeste et la réponse de la FAQ « Tensho agit-il à ma place ? » nomment maintenant les trois réglages qui agissent sans clic : la touche M (activée par défaut), le titre par job (désactivé par défaut) et « Cacher les messages du chat du jeu » (désactivé par défaut), qui cache les panneaux de messages et les onglets du chat du jeu tant que la fenêtre de chat de Tensho est ouverte à leur place. Le test du manifeste lie ce compte aux valeurs par défaut du code et au nom du réglage affiché. La liste des essais en jeu de TESTING.md porte la date et la version de sa production, un compte de la source en français et en anglais, et ses cases suivent la source dans les deux sens ; les cases cochées d'avant la source sont comptées à part ; le paragraphe périmé du 2026-11-02 est retiré.
- Pour les joueurs : la description du plugin et la FAQ citent aussi le réglage qui masque les messages du chat du jeu parmi ceux qui agissent sans clic.
- Tests : Manifest1429Tests. Verify.ps1 (agent) : 4155/4155, 0 avertissement, 0 erreur ; DLL 0.1882.0.0. Rien à essayer en jeu.
### English
- Review of block 0.1880 to 0.1881 (X-875, I1, I2, M1): the manifest's description and the FAQ answer on whether Tensho acts for the player now name the three settings that act without a click: the M key (on by default), the job title (off by default) and « Hide the game chat's messages » (off by default), which hides the game chat's message panels and tabs while Tensho's chat window is open in their place. The manifest test ties this count to the code's defaults and to the setting's shown name. The in-game test list of TESTING.md carries the date and version it was produced for and a count from the test source in French and English, and its boxes follow the source both ways; ticked boxes from before the source are counted apart; the outdated paragraph of 2026-11-02 is removed.
- For players: The plugin's description and the FAQ now also name the setting that hides the game chat's messages among those that act without a click.

## 0.1881.0
- Plan global, X-874 : Relecture de 0.1879 (M1, déjà là avant le bloc) : la vue d'ensemble d'ARCHITECTURE.md disait « une seule DLL, plus la bibliothèque native du solveur Raphael ». Elle nomme maintenant, dans les deux moitiés, ce que livre le paquet à côté de TenshoAssistant.dll : Raphael.Net.dll et raphael_bindings.dll (solveur facultatif), Microsoft.Data.Sqlite.dll, SQLitePCLRaw.batteries_v2, core et provider.e_sqlite3, et la bibliothèque native e_sqlite3.dll (pour TenshoData.db et Discoveries.db), relevés dans le paquet produit. La moitié française reçoit aussi la phrase sur l'application de bureau que l'anglaise avait déjà.
- Tests : ArchitectureOverview1881Tests. Verify.ps1 (agent) : 4155/4155, 0 avertissement, 0 erreur ; DLL 0.1881.0.0. Rien à essayer en jeu.
### English
- Review of 0.1879 (M1, already there before the block): the overview of ARCHITECTURE.md said « a single DLL, plus the native library of the Raphael solver ». It now names, in both halves, what the package ships next to TenshoAssistant.dll: Raphael.Net.dll and raphael_bindings.dll (optional solver), Microsoft.Data.Sqlite.dll, SQLitePCLRaw.batteries_v2, core and provider.e_sqlite3, and the native e_sqlite3.dll (for TenshoData.db and Discoveries.db), as read in the built package. The French half also gets the sentence on the desktop application that the English one already had.
