# Journal des versions / Changelog, 0.1100 à 0.1199

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

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

## 0.1110.0
- Jardin : le mode développeur ne recopie plus dans le journal de Dalamud chaque ligne du chat lue dans un quartier résidentiel ; l'entretien et la récolte sont reconnus (0.616, 0.978) et ce qu'une récolte donne reste dans les lignes de gains d'objets (#36).
- Tests : GardenCapture1110Tests. Verify.ps1 (agent) : 1814/1814, 0 avertissement, 0 erreur ; DLL 0.1110.0.0. Rien à essayer en jeu.
### English
- Garden: developer mode no longer copies every chat line read in a housing ward to the Dalamud log; tending and harvesting are recognized (0.616, 0.978) and what a harvest gives stays in the item gain lines (#36). Issues: #36.

## 0.1109.0
- Plan global, X-130 : Jardin : sous chaque parcelle plantée d'un potager du journal, ce qu'elle peut donner (graines possibles avec la voisine avec laquelle elle croise : la première compatible déjà là, à droite, en bas, en haut, à gauche) ; les graines pas possédées ou souhaitées mises en avant ; une graine inconnue des paires est dite, jamais devinée ; source citée (wiki Console Games et paires de nick75g/FFXIV-Crossbreed-Helper).
- Tests : GardenForecast1109Tests. Verify.ps1 (agent) : 1813/1813, 0 avertissement, 0 erreur ; DLL 0.1109.0.0. Non validé en jeu.
### English
- Garden: under each planted bed of a patch in the journal, what it may give (the seeds possible with the neighbour it crosses with: the first compatible one already there, right, down, up, left); the seeds not held or wished put forward; a seed missing from the pairs is said, never guessed; source cited (Console Games Wiki and nick75g/FFXIV-Crossbreed-Helper's pairs).

## 0.1108.0
- Plan global, X-129 : Aide à la récolte : un objet caché révélé après une dépense de PR n'a plus « aucun plan » ; son plan est calculé avec ce qui restait à son apparition (PR et essais), avec ses taux lus à ce moment, et le panneau le dit.
- Tests : LatePlan1108Tests. Verify.ps1 (agent) : 1810/1810, 0 avertissement, 0 erreur ; DLL 0.1108.0.0. Non validé en jeu.
### English
- Gathering help: a hidden item revealed after GP was spent no longer has « no plan »; its plan is computed from what was left when it appeared (GP and attempts), with its rates as read then, and the panel says so.

## 0.1107.0
- Plan global, X-128 : Carte de Tensho : la case « Quêtes pas encore disponibles » est passée du menu du clic droit à l'onglet Quêtes ; décochée, elle retire aussi de la liste de l'onglet les quêtes pas encore disponibles (pas commencées, avec une quête d'avant pas encore faite), en plus de leurs icônes sur la carte.
- Tests : MapLockedQuests1107Tests. Verify.ps1 (agent) : 1808/1808, 0 avertissement, 0 erreur ; DLL 0.1107.0.0. Non validé en jeu.
### English
- Tensho map: the « Quests not available yet » box moved from the right-click menu to the Quests tab; unticked, it also takes the quests not available yet (not started, with an earlier quest not done yet) out of the tab's list, besides their icons on the map.

## 0.1106.0
- Plan global, X-127 : Souhaits : la liste de souhaits devient un outil à part (OBJETS › Souhaits, ce n'est plus un onglet de Collections) ; « ajouté à vos souhaits » s'affiche en bas de la fenêtre avec un bouton qui ouvre la liste ; sous chaque objet récolté, où le récolter (métier, zone, point, heures, ou heures et météo d'un poisson) ; « Ajouter les alarmes » pose en un clic les alarmes des points limités de ce qui manque, sans doublon ; question « Où est ma liste de souhaits ? » dans la FAQ.
- Tests : WishlistTool1106Tests. Verify.ps1 (agent) : 1806/1806, 0 avertissement, 0 erreur ; DLL 0.1106.0.0. Non validé en jeu.
### English
- Wishlist: the wishlist becomes a tool of its own (ITEMS › Wishlist, no longer a Collections tab); « added to your wishlist » shows at the bottom of the window with a button that opens the list; under each gathered item, where to gather it (job, zone, node, hours, or a fish's hours and weather); « Add the alarms » sets in one click the alarms of the timed nodes of what is missing, without duplicates; « Where is my wishlist? » in the FAQ.

## 0.1105.0
- Releases : Tensho écrit sa version dans le journal de Dalamud au chargement, pour que la vérification du lancement la retrouve ; les notes d'une release (hors correctif urgent) disent « Vérifié au lancement, pas encore essayé en détail » (R20 changée le 11/11).
- Tests : LaunchCheck1105Tests. Verify.ps1 (agent) : 1803/1803, 0 avertissement, 0 erreur ; DLL 0.1105.0.0. Rien à essayer en jeu.
### English
- Releases: Tensho writes its version to the Dalamud log at load, so that the launch check can find it; the notes of a release (other than an urgent fix) say « Checked at launch, not tried in detail yet » (R20 changed on 11/11).

## 0.1104.0
- Carte : son code est préparé en premier, une demi-seconde après le chargement de Tensho, pour que la carte ouverte au rechargement ne fige plus l'image (X-01 ; mesure après 0.1103 : marchands 142 → 82 ms, image encore 358 ms, surtout de la compilation).
- Tests : MapWarmup1104Tests. Verify.ps1 (agent) : 1801/1801, 0 avertissement, 0 erreur ; DLL 0.1104.0.0. Rien à essayer en jeu.
### English
- Map: its code is prepared first, half a second after Tensho loads, so that a map open at a reload no longer freezes the frame (X-01; measured after 0.1103: vendors 142 → 82 ms, frame still 358 ms, mostly compiling).

## 0.1103.0
- Carte : la première image après un chargement ne regroupe plus toutes les ventes des marchands du jeu ni ne parcourt tous les marchands : les boutiques et les marchands de chaque carte sont prêts avec le catalogue, hors de l'image (X-01 ; mesure d'avant : 71 à 156 ms pour les marchands à la première image, 2,7 s une fois).
- Tests : MapVendorIndex1103Tests. Verify.ps1 (agent) : 1799/1799, 0 avertissement, 0 erreur ; DLL 0.1103.0.0. Rien à essayer en jeu.
### English
- Map: the first frame after a load no longer groups every NPC sale of the game nor scans every vendor: the shops and the vendors of each map are ready with the catalogue, off the frame (X-01; measured before: 71 to 156 ms for the vendors on the first frame, 2.7 s once).

## 0.1102.0
- Journal des gils : les gils des missions de la pêche en mer ont leur catégorie, « Pêche en mer (missions) », au lieu de « Autre ». Découvertes : un secteur d'aéronef débloqué (message du jeu « Une nouvelle destination à explorer a été découverte ») est noté, comme ceux des sous-marins, seulement si la feuille du jeu connaît ce secteur.
- Tests : GainParsers1102Tests. Verify.ps1 (agent) : 1797/1797, 0 avertissement, 0 erreur ; DLL 0.1102.0.0. Non validé en jeu.
### English
- Gil journal: the gil of the ocean fishing missions have their own category, « Ocean fishing (missions) », instead of « Other ». Discoveries: an airship sector unlocked (the game's message « A new destination to explore has been discovered ») is recorded, as the submarines' are, only when the game sheet knows that sector.

## 0.1101.0
- Découvertes partagées : le ticket #43 est fusionné dans le fichier commun (1967 faits, 0 refusé, 260 nouveaux ou changés) ; ils arrivent chez tous les joueurs avec la prochaine release.
- Tests : CommunityMerge1101Tests. Verify.ps1 (agent) : 1795/1795, 0 avertissement, 0 erreur ; DLL 0.1101.0.0. Rien à essayer en jeu.
### English
- Shared discoveries: ticket #43 merged into the common file (1967 facts, 0 refused, 260 new or changed); they reach every player with the next release.

## 0.1100.0
- Campagne d'essais en jeu triée : 114 essais retirés (rien à voir, doublons, remplacés par un essai plus récent sur le même code, déjà faits, migrations passées, textes que les tests prouvent, arrêts un par un de la visite guidée, testée en entier) ; TESTING et TESTING-ROUTE produits de nouveau avec les essais des versions récentes. Aucun essai déclaré validé.
- Tests : TestingTriage1100Tests. Verify.ps1 (agent) : 1794/1794, 0 avertissement, 0 erreur ; DLL 0.1100.0.0. Rien à essayer en jeu.
### English
- In-game test campaign sorted: 114 tests retired (nothing to see, duplicates, replaced by a later test of the same code, already done, past migrations, texts proven by the tests, single stops of the guided tour, which is tested as a whole); TESTING and TESTING-ROUTE produced again with the tests of the recent versions. No test declared validated.
