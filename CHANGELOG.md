# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

## 0.1118.0
- Plan global, X-141 : Banque d'inventaire à deux clients (audit A02) : deux jeux lancés sur le même profil Dalamud partagent le dossier InventoryBank ; une capture de l'un pouvait effacer les personnages enregistrés par l'autre. Désormais chaque changement ne touche que le personnage concerné : sous un verrou du profil (InventoryBank.lock), son fichier est relu, le stockage lu ou oublié y est mis, puis il est réécrit ; ce que l'autre client a enregistré reste. Un personnage n'est effacé que par « Effacer ce personnage ». Le découpage de l'ancien fichier commun et la lecture au démarrage passent par le même verrou. Si l'autre client garde le verrou trop longtemps, l'enregistrement échoue sans rien écrire et le dit.
- Tests : BankShared1118Tests. Verify.ps1 (agent) : 1835/1835, 0 avertissement, 0 erreur ; DLL 0.1118.0.0. Non validé en jeu.
### English
- Inventory bank with two clients (audit A02): two games started on the same Dalamud profile share the InventoryBank folder; a capture by one could erase the characters saved by the other. Now every change touches only the character concerned: under a lock of the profile (InventoryBank.lock), its file is read again, the storage read or forgotten is put in, then it is written back; what the other client saved stays. A character is erased only by « Erase this character ». The split of the former shared file and the load at start use the same lock. If the other client keeps the lock too long, the save fails without writing anything and says so.

## 0.1117.0
- Plan global, X-140 : Installateur de l'application de bureau (audit A01 et A03) : un dossier qui porte le nom d'une version mais qu'il n'a pas installé n'est plus jamais supprimé ni remplacé ; l'installation s'arrête avec un message qui nomme le dossier, laissé tel quel. Chaque copie installée porte une marque ; une copie valide de Tensho déjà là est gardée telle quelle ; la copie en service, la précédente et celle qui tourne ne sont jamais remplacées. Chaque téléchargement et chaque dépaquetage a son propre emplacement temporaire, retiré après un échec ; le zip est dépaqueté depuis le fichier même dont l'empreinte a été vérifiée, avec un plafond de 2 000 entrées et 1 Go décompressé. update.json et le nettoyage des anciennes copies passent par un verrou partagé entre le plugin et l'application : l'état est relu avant chaque changement, plus aucun réglage perdu entre les deux. Le nettoyage ne touche que les copies de Tensho.
- Tests : DesktopInstall1117Tests. Verify.ps1 (agent) : 1830/1830, 0 avertissement, 0 erreur ; DLL 0.1117.0.0. Non validé en jeu.
### English
- Desktop application installer (audit A01 and A03): a folder named like a version that it did not install is never deleted nor replaced any more; the install stops with a message naming the folder, left as it is. Every installed copy carries a mark; a valid Tensho copy already there is kept as it is; the copy in use, the previous one and the running one are never replaced. Every download and unpacking has its own temporary place, removed after a failure; the zip is unpacked from the very file whose hash was checked, capped at 2,000 entries and 1 GB unpacked. update.json and the clean-up of old copies go through a lock shared by the plugin and the application: the state is read again before each change, no setting is lost between the two. The clean-up touches only Tensho's copies.

## 0.1116.0
- Dédale bestial, première étape de la lecture du résultat du plateau (X-135, R06 : valeurs pas encore connues) : en mode développeur et dans le Dédale seulement, les nombres des fenêtres du Dédale (noms « XBM… », celle du résultat comprise) sont écrits dans le journal quand ils changent, 20 lignes par fenêtre et par visite au plus ; jamais un texte (une fenêtre de résultat peut nommer le personnage).
- Tests : LabyrinthValues1116Tests. Verify.ps1 (agent) : 1824/1824, 0 avertissement, 0 erreur ; DLL 0.1116.0.0. Non validé en jeu.
### English
- Beast labyrinth, first step of reading the board's result (X-135, R06: values not known yet): in developer mode and in the labyrinth only, the numbers of the labyrinth's windows (names « XBM… », the result's included) are written to the log when they change, 20 lines per window and visit at most; never a text (a result window may name the character).

## 0.1115.0
- Plan global, X-137 : Dédale bestial : Tensho n'y est plus coupé (choix de l'utilisateur du 11/11, aucun classement vu) ; ses territoires restent reconnus pour le panneau d'équipe. La coupure reste en JcJ, aux tournois de Triple Triade, au Mahjong et dans les donjons profonds. DEC-038 complétée (deux langues), ARCHITECTURE suit.
- Tests : LabyrinthOpen1115Tests. Verify.ps1 (agent) : 1823/1823, 0 avertissement, 0 erreur ; DLL 0.1115.0.0. Non validé en jeu.
### English
- Beast labyrinth: Tensho is no longer cut there (the user's choice of 11/11, no ranking seen); its territories stay known for the team panel. The cut stays in PvP, Triple Triad tournaments, Mahjong and deep dungeons. DEC-038 completed (both languages), ARCHITECTURE follows.

## 0.1114.0
- Lien local avec l'application de bureau, protocole 2 (X-119 étape 2 suite, partie 1) : en plus de qui et où, le plugin envoie sa classe et son niveau, ses PR et les places libres des sacs, et les noms des fichiers enregistrés de Tensho que le jeu vient d'écrire (jamais leur contenu ; bornes : 16 noms de 160 caractères, ligne de 8 Ko). L'application relit elle-même le fichier de réglages du plugin quand le jeu l'enregistre (ses pages suivent les réglages du jeu) et montre ces états dans sa barre d'état. Info-bulle du réglage et FAQ du lien mises à jour.
- Tests : LinkLive1114Tests. Verify.ps1 (agent) : 1822/1822, 0 avertissement, 0 erreur ; DLL 0.1114.0.0. Non validé en jeu.
### English
- Local link with the desktop application, protocol 2 (X-119 step 2 suite, part 1): besides who and where, the plugin sends its job and level, GP and the bags' free slots, and the names of Tensho's saved files the game just wrote (never their content; bounds: 16 names of 160 characters, 8 KB line). The application reads the plugin's settings file again itself when the game saves it (its pages follow the game's settings) and shows those states in its status bar. The setting's tooltip and the link's FAQ are updated.

## 0.1113.0
- Plan global, X-126 : Application de bureau installable depuis Paramètres › Avancé › Application de bureau (facultatif, décision D-137) : état face au plugin (pas installée, plus ancienne, à jour, plus récente), dossier d'installation, « Installer » ou « Mettre à jour » (release GitHub de la version du plugin, sinon la dernière Stable ; SHA-256 vérifié avec celui de la release ; dépaquetée à côté), « Ouvrir l'application de bureau » (jamais lancée sans ce clic), « Ouvrir son dossier », « Page de la release », « Mise à jour automatique de l'application de bureau ». Aucune requête sans clic. L'installation est un seul code partagé avec la mise à jour de l'application (DesktopInstall.cs). Une compilation avec TenshoDesktopInstall=false la retire. FAQ « Tensho peut-il installer l'application de bureau pour moi ? », source réseau dans SECURITY.
- Tests : DesktopInstall1113Tests. Verify.ps1 (agent) : 1819/1819, 0 avertissement, 0 erreur ; DLL 0.1113.0.0. Non validé en jeu.
### English
- Desktop application installable from Settings › Advanced › Desktop application (optional, decision D-137): state against the plugin (not installed, older, up to date, newer), install folder, « Install » or « Update » (GitHub release of the plugin's version, else the latest Stable one; SHA-256 checked against the release's; unpacked beside), « Open the desktop app » (never started without that click), « Open its folder », « Release page », « Automatic update of the desktop app ». No request without a click. The install is one code shared with the app's own update (DesktopInstall.cs). A build with TenshoDesktopInstall=false leaves it out. FAQ « Can Tensho install the desktop app for me? », network source in SECURITY.

## 0.1112.0
- Vérification : quand seuls des documents suivis (CHANGELOG, README, docs/) ont changé depuis la dernière vérification verte, Verify.ps1 (et donc -Commit et le hook avant push) relance les tests managés au lieu de tout sauter ; la compilation reste sautée (plan système 4.21 : 096963e était parti avec 3 tests de documents en échec).
- Tests : VerifyDocuments1112Tests. Verify.ps1 (agent) : 1816/1816, 0 avertissement, 0 erreur ; DLL 0.1112.0.0. Rien à essayer en jeu.
### English
- Verification: when only tracked documents (CHANGELOG, README, docs/) changed since the last green run, Verify.ps1 (so -Commit and the pre-push hook) runs the managed tests again instead of skipping every check; the build is still skipped (system plan 4.21: 096963e had left with 3 failing document tests).

## 0.1111.0
- Carte : le code de ses propres types (catalogues, projection, calques, lecteurs) est préparé en premier lui aussi, une demi-seconde après le chargement de Tensho (X-01, suite de 0.1104 : première image de la carte encore à 293 ms, surtout de la compilation).
- Tests : MapWarmup1111Tests. Verify.ps1 (agent) : 1815/1815, 0 avertissement, 0 erreur ; DLL 0.1111.0.0. Rien à essayer en jeu.
### English
- Map: the code of its own types (catalogues, projection, layers, readers) is prepared first too, half a second after Tensho loads (X-01, after 0.1104: the map's first frame still at 293 ms, mostly compiling).
