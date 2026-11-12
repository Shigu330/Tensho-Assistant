# Journal des versions / Changelog, 0.1100 à 0.1199

Archive de [CHANGELOG.md](../../CHANGELOG.md) : versions plus anciennes que la dernière release, déplacées par `tools/changelog.py archive`. Même format (français, puis `### English` depuis 0.219.0) ; liens relatifs à la racine réécrits depuis ce dossier.

## 0.1134.0
- Plan global, X-167 : Découvertes envoyées sur GitHub : seuls les champs relevés par vous partent (audit du 12/11, R03). Quand un import élargit les dates ou les niveaux d'un de vos faits, ou quand vous revoyez un fait importé, vos propres dates, niveaux et lieu exact sont gardés à part dans votre fichier (champ « Own », jamais dans un fichier partagé ni dans le gist) et l'envoi n'utilise qu'eux ; un fait qui ne tiendrait pas n'est pas envoyé. Une observation de monstre se compte maintenant depuis votre propre dernière observation, jamais depuis une date importée plus récente. Les faits d'avant cette version à la fois vus et importés restent envoyés comme avant (D-139, choix de l'utilisateur du 12/11, « A » : le fichier commun n'a jamais contenu que son ticket n° 43). Textes corrigés (aide et avertissement avant connexion, README des découvertes, SECURITY.md, deux moitiés) : un poisson pris ou une touche porte l'endroit exact du personnage (DEC-045), donc avec le jour et le ticket il peut dire où ce compte a pêché un jour donné.
- Tests : DiscoveriesProvenance1134Tests. Verify.ps1 (agent) : 1881/1881, 0 avertissement, 0 erreur ; DLL 0.1134.0.0. Non validé en jeu.
### English
- Discoveries sent to GitHub: only the fields you read yourself are sent (audit of 12/11, R03). When an import widens the dates or levels of one of your facts, or when you see an imported fact again, your own dates, levels and exact place are kept apart in your file (field « Own », never in a shared file or the gist) and the upload uses them only; a fact that would not hold is not sent. A monster sighting is now counted from your own last sighting, never from a later imported date. Facts from before this version both seen and imported are still sent as before (D-139, the user's choice of 12/11, « A »: the common file has only ever held their issue 43). Texts fixed (help and warning before connecting, discoveries README, SECURITY.md, both halves): a fish caught or a bite holds the character's exact place (DEC-045), so with the day and the issue it can tell where that account fished on a given day.

## 0.1133.0
- Plan global, X-166 : Découvertes : une découverte importée qui devient la vôtre n'est plus perdue par un arrêt entre les deux écritures d'une sauvegarde (audit du 12/11, R01). Votre fichier s'écrit maintenant avant les fichiers partagés : un arrêt entre les deux laisse le fait dans les deux, jamais dans aucun ; au rechargement, l'ancienne copie partagée est écartée (les imports ne sont pas comptés deux fois) et la sauvegarde suivante la retire. Un import n'est noté fait qu'une fois enregistré : le fichier du dossier Import n'est renommé « .imported », et l'empreinte du fichier commun de GitHub notée, qu'après la sauvegarde qui le contient ; un arrêt avant laisse l'import à reprendre. Une base effacée volontairement puis enregistrée note l'import comme fait (il ne revient pas).
- Tests : DiscoveriesCrash1133Tests. Verify.ps1 (agent) : 1876/1876, 0 avertissement, 0 erreur ; DLL 0.1133.0.0. Non validé en jeu.
### English
- Discoveries: an imported discovery that becomes yours is no longer lost by a stop between the two writes of a save (audit of 12/11, R01). Your file is now written before the shared files: a stop in between leaves the fact in both, never in neither; when read back, the old shared copy is left out (imports are not counted twice) and the next save removes it. An import is marked done only once saved: the Import folder's file is renamed « .imported », and the GitHub common file's hash noted, only after the save that holds it; a stop before leaves the import to be taken again. A base cleared on purpose and then saved marks the import done (it does not come back).

## 0.1132.0
- Plan global, X-163 : Les alarmes sonnent aussi dans l'application de bureau (première partie de X-163) : une fois par seconde, l'application demande au plugin les alarmes à faire sonner, selon la même règle que dans le jeu (avance, météo demandée comprise, jamais sur une météo inconnue) mais avec sa propre mémoire, pour qu'une alarme déjà sonnée dans le jeu sonne quand même dans l'application quand le jeu est derrière ; les alarmes copiées dans le jeu y sonnent seules. Elle joue le son « exclamation » de Windows et montre une ligne pendant une minute dans sa barre d'état. Deux réglages (Paramètres › Alarmes) : son Windows, et « seulement si le jeu n'est pas au premier plan » (activés par défaut). Jamais dans l'essai sans fenêtre de Verify. Restent la notification de Windows et les rappels et le jardinage qui ne sont pas des alarmes.
- Tests : DesktopBell1132Tests. Verify.ps1 (agent) : 1872/1872, 0 avertissement, 0 erreur ; DLL 0.1132.0.0. Non validé en jeu.
### English
- Alarms also ring in the desktop application (first part of X-163): once a second, the application asks the plugin which alarms ring, by the same rule as in the game (lead, required weather included, never on an unknown weather) but with its own memory, so an alarm the game already rang still rings in the application while the game is behind; alarms copied into the game ring there only. It plays Windows' « exclamation » sound and shows a line for a minute in its status bar. Two settings (Settings › Alarms): Windows sound, and « only when the game is not in front » (both on by default). Never in Verify's headless run. Left: the Windows notification, and the reminders and gardening that are not alarms.

## 0.1131.0
- Plan global, X-119 : Application de bureau, données enregistrées du plugin (X-119 étape 2, partie 2, avec X-151) : l'application lit maintenant la banque (Stockage), les listes d'artisanat, Mes outils, l'historique des voyages et les équipes du dresseur, depuis sa propre copie du dossier du plugin (%LOCALAPPDATA%\TenshoAssistant\Desktop\mirror), jamais depuis le dossier du plugin, qui n'est que lu : chaque fichier est lu partagé, borné, écrit dans un fichier temporaire puis mis en place ; un fichier retiré par le plugin l'est de la copie. Quand le lien signale qu'une de ces catégories a changé, la copie est refaite hors de l'image et les données sont reconstruites entre deux images. Les magasins sont construits par une seule entrée du plugin (MainWindow.UseDesktopData), compilée avec lui, au lieu d'être recréés par réflexion. Jamais dans l'essai sans fenêtre de Verify.
- Tests : DesktopData1131Tests. Verify.ps1 (agent) : 1870/1870, 0 avertissement, 0 erreur ; DLL 0.1131.0.0. Non validé en jeu.
### English
- Desktop application, the plugin's saved data (X-119 step 2, part 2, with X-151): the application now reads the bank (Storage), the craft lists, My tools, the voyage history and the beast teams from its own copy of the plugin's folder (%LOCALAPPDATA%\TenshoAssistant\Desktop\mirror), never from the plugin's folder, which is only read: each file is read shared, bounded, written to a temporary file then put in place; a file the plugin removed goes from the copy. When the link says one of these categories changed, the copy is made again off the frame and the data are built again between two frames. The stores are built by one entry of the plugin (MainWindow.UseDesktopData), compiled with it, instead of being rebuilt by reflection. Never in Verify's headless run.

## 0.1130.0
- Plan global, X-164 : Barre d'outils de la carte (demande de l'utilisateur du 12/11, capture) : les icônes sont centrées dans leurs boutons (le dessin de chaque icône est centré, pas sa largeur de caractère), dans toutes les barres d'outils ; le pourcentage de zoom se tape au clavier (Entrée l'applique, de 15 à 400 %) ; un bouton « 100% » dans le style des autres, sous le pourcentage, remet le zoom à 100 %.
- Tests : MapToolbar1130Tests. Verify.ps1 (agent) : 1868/1868, 0 avertissement, 0 erreur ; DLL 0.1130.0.0. Non validé en jeu.
### English
- Map tool column (user request of 12/11, capture): icons are centred in their buttons (each icon's drawing is centred, not its character width), in every tool bar; the zoom percentage is typed with the keyboard (Enter applies it, 15 to 400 %); a « 100% » button in the style of the others, under the percentage, puts the zoom back to 100 %.

## 0.1129.0
- Plan global, X-162 : Infobulles de la barre d'infos du serveur (demande de l'utilisateur du 12/11) : les entrées des monnaies et des gils ont maintenant l'infobulle de Tensho, comme les alarmes et les rappels, accrochée à leur entrée ; toutes s'ouvrent du même côté, en dessous par défaut, au-dessus avec le nouveau réglage « Au-dessus de l'entrée » (Paramètres › Général, sous l'infobulle de Tensho) ; quand ce côté manque de place elles passent de l'autre, et sur la moitié droite de l'écran leur bord droit suit celui de l'entrée : jamais hors de l'écran. Sans l'infobulle de Tensho, celle du jeu reste.
- Tests : BarTooltips1129Tests. Verify.ps1 (agent) : 1866/1866, 0 avertissement, 0 erreur ; DLL 0.1129.0.0. Non validé en jeu.
### English
- Server info bar tooltips (user request of 12/11): the currency and gil entries now have Tensho's tooltip, like the alarms and reminders, attached to their entry; all open on the same side, below by default, above with the new setting « Above the entry » (Settings › General, under Tensho's tooltip); when that side lacks room they open on the other one, and on the right half of the screen their right edge follows the entry's: never off the screen. Without Tensho's tooltip, the game's stays.

## 0.1128.0
- Plan global, X-158 : Carnet d'exploration, une seule source (demande de l'utilisateur du 12/11, cas du Viaduc) : « possible maintenant », la couleur de la ligne, le filtre « Possibles maintenant », les lignes de l'infobulle et la prochaine fenêtre viennent d'un même calcul (heure d'Éorzéa du panorama et périodes de météo de la zone, sauts d'un changement à l'autre). La liste dit « possible maintenant, jusqu'à HH:MM » ; l'infobulle dit « Possible maintenant, jusqu'à HH:MM », ou ce qui manque (l'heure, la météo, ou les deux) et quand la prochaine fenêtre s'ouvre, cherchée sur 30 jours hors de l'image, sinon « Aucune fenêtre dans les 30 prochains jours » : jamais de ligne vide. Une météo inconnue n'est jamais dite « possible ».
- Tests : VistaWindow1128Tests. Verify.ps1 (agent) : 1864/1864, 0 avertissement, 0 erreur ; DLL 0.1128.0.0. Non validé en jeu.
### English
- Sightseeing log, one source (user request of 12/11, the Highbridge case): « possible now », the row's colour, the « Possible now » filter, the tooltip's lines and the next window come from one calculation (the vista's Eorzea time and the zone's weather periods, jumping from one change to the next). The list says « possible now, until HH:MM »; the tooltip says « Possible now, until HH:MM », or what is missing (the time, the weather or both) and when the next window opens, searched over 30 days off the frame, otherwise « No window in the next 30 days »: never an empty line. An unknown weather is never called « possible ».

## 0.1127.0
- Plan global, X-144 : Suite de la preuve de lancement : la première preuve écrite en jeu par 0.1126 avait une empreinte de DLL vide (Dalamud charge le plugin depuis la mémoire, l'emplacement de l'assembly est vide) ; le fichier de la DLL vient maintenant de Dalamud (AssemblyLocation), si bien que la note « Vérifié au lancement » peut de nouveau être prouvée.
- Tests : LaunchProof1126Tests. Verify.ps1 (agent) : 1861/1861, 0 avertissement, 0 erreur ; DLL 0.1127.0.0. Rien à essayer en jeu.
### English
- Launch proof follow-up: the first proof written in game by 0.1126 had an empty DLL hash (Dalamud loads the plugin from memory, the assembly's location is empty); the DLL's file now comes from Dalamud (AssemblyLocation), so the « Checked at launch » note can be proven again.

## 0.1126.0
- Plan global, X-144 : Note « Vérifié au lancement » sur preuve (audit A13) : après chaque auto-contrôle fait avec un personnage connecté, le plugin écrit LaunchProof.json dans son dossier de configuration (sa version, l'empreinte SHA-256 de la DLL chargée par le jeu, l'heure et les comptes de l'auto-contrôle ; aucun nom ni chemin). Le script de release (scripts/LaunchProof.ps1) n'écrit « Vérifié au lancement, pas encore essayé en détail » que si cette preuve est celle de la version publiée, de la DLL même de latest.zip, sans échec, personnage connecté, de moins de 14 jours ; sinon la note dit « Pas encore vérifié en jeu », quel que soit le chemin de la release (commit-release.bat compris). Chaque release porte aussi release-proof.json : commit construit et contrôlé, empreintes des paquets livrés, versions de Dalamud et du SDK .NET, CI du commit et preuve de lancement lue.
- Tests : LaunchProof1126Tests. Verify.ps1 (agent) : 1861/1861, 0 avertissement, 0 erreur ; DLL 0.1126.0.0. Non validé en jeu.
### English
- « Checked at launch » note on proof (audit A13): after each self-check made with a character logged in, the plugin writes LaunchProof.json in its configuration folder (its version, the SHA-256 of the DLL the game loaded, the time and the self-check's counts; no name, no path). The release script (scripts/LaunchProof.ps1) writes « Checked at launch, not tried in detail yet » only when that proof is for the version released, for the very DLL of latest.zip, without failure, a character logged in, less than 14 days old; otherwise the note says « Not checked in game yet », whichever way the release is started (commit-release.bat included). Each release also carries release-proof.json: commit built and checked, SHA-256 of the delivered packages, Dalamud and .NET SDK versions, the commit's CI and the launch proof read.

## 0.1125.0
- Plan global, X-143 : Rapport de problème sans fuite (audit A08) : Aide › Signaler un problème montre d'abord un aperçu de ce qui sera envoyé (les textes exacts mis dans l'adresse du formulaire, et dans le presse-papiers quand ils sont trop longs) ; le navigateur ne s'ouvre qu'après « Ouvrir dans mon navigateur », et un champ changé ferme l'aperçu. Le rapport de diagnostic est construit d'une liste fermée de champs, une ligne chacun, bornée. Les chemins sont masqués en entier, dossiers avec espaces et chemins réseau compris, ainsi que les noms d'utilisateur et d'ordinateur Windows ; l'auto-contrôle utilise le même nettoyage.
- Tests : ReportPrivacy1125Tests. Verify.ps1 (agent) : 1859/1859, 0 avertissement, 0 erreur ; DLL 0.1125.0.0. Non validé en jeu.
### English
- Problem report without leaks (audit A08): Help › Report a problem first shows a preview of what will be sent (the exact texts put in the form's address, and in the clipboard when they are too long); the browser opens only after « Open in my browser », and a changed field closes the preview. The diagnosis is built from a closed list of fields, one bounded line each. Paths are masked whole, folders with spaces and network paths included, and so are the Windows user and computer names; the self-check uses the same cleaning.

## 0.1124.0
- Plan global, X-143 : Universalis coupé pour de bon (audit A07) : une requête qui attendait son créneau (la pause de 3 secondes entre deux requêtes) ne part plus si Universalis vient d'être désactivé dans Paramètres › Avancé : le réglage est relu juste avant chaque envoi, pour le tableau du marché, les prix des listes et la vitesse de vente qui les suit ; le tableau dit alors que la source est coupée et montre ce qui était connu.
- Tests : UniversalisOff1124Tests. Verify.ps1 (agent) : 1854/1854, 0 avertissement, 0 erreur ; DLL 0.1124.0.0. Non validé en jeu.
### English
- Universalis off for good (audit A07): a request waiting for its slot (the 3-second pause between two requests) no longer leaves if Universalis was just turned off in Settings › Advanced: the setting is read again right before each send, for the market board, the list prices and the sale speed that follows them; the board then says the source is off and shows what was known.

## 0.1123.0
- Plan global, X-143 : Échéances des téléchargements, première partie (audit A06) : une réponse dont les en-têtes sont arrivés mais dont le corps ne vient plus ne bloque plus une page pour toujours. Les requêtes communautaires (aide communautaire, échange GitHub des découvertes, Garland Tools, wiki, Universalis des ventes de servant, galerie de Mes outils…) ont une échéance qui couvre aussi le corps : le délai du client quand il est plus court, sinon 2 minutes ; le téléchargement de l'application de bureau, 10 minutes. Passé ce délai, l'erreur est une expiration et une nouvelle tentative est possible ; une annulation demandée reste une annulation.
- Tests : HttpDeadline1123Tests. Verify.ps1 (agent) : 1852/1852, 0 avertissement, 0 erreur ; DLL 0.1123.0.0. Rien à essayer en jeu.
### English
- Download deadlines, part one (audit A06): a reply whose headers arrived but whose body stops coming no longer blocks a page for good. Community requests (community help, the GitHub exchange of discoveries, Garland Tools, wikis, Universalis for retainer sales, the My tools gallery…) have a deadline that covers the body too: the client's timeout when shorter, else 2 minutes; the desktop app's download, 10 minutes. Past it, the error is a timeout and a new try is possible; a cancellation asked stays a cancellation.

## 0.1122.0
- Plan global, X-142 : Sauvegardes plus sûres, seconde partie (audit A05) : « Vider le cache » fait la copie de sauvegarde et la purge en une seule opération de la file d'écriture du cache : la copie contient toutes les écritures demandées avant, la purge suit sans qu'aucune lecture ni écriture ne passe entre les deux, et une écriture demandée pendant ce temps attend puis reste après la purge (elle n'est plus perdue ni effacée). Si la copie échoue, rien n'est retiré. Un fichier en cours d'utilisation qui ne peut pas être retiré est compté et le message le dit (« vidé en partie »).
- Tests : CacheClear1122Tests. Verify.ps1 (agent) : 1849/1849, 0 avertissement, 0 erreur ; DLL 0.1122.0.0. Non validé en jeu.
### English
- Safer saves, part two (audit A05): « Empty the cache » takes the backup copy and the purge as one operation of the cache's write queue: the copy holds every write asked before, the purge follows with no read nor write in between, and a write asked meanwhile waits then stays after the purge (it is no longer lost nor erased). If the copy fails, nothing is removed. A file in use that cannot be removed is counted and the message says so (« emptied in part »).

## 0.1121.0
- Plan global, X-142 : Sauvegardes plus sûres, première partie (audit A04 et A09) : les découvertes et les prix vus ne sont plus marqués « enregistrés » avant l'écriture, mais seulement une fois le fichier écrit ; une écriture qui échoue (disque plein, fichier bloqué) les garde à enregistrer et une nouvelle tentative suit une minute plus tard ; ce qui change pendant la copie part dans l'enregistrement suivant. À la fermeture, un enregistrement en cours est attendu (5 s au plus) avant d'écrire le reste. L'enregistreur d'artisanat appelle la fonction du jeu par l'appel sûr aussi pendant la coupure JcJ et contenu classé. La partie 2 (cache : archive et purge en une seule opération) suit.
- Tests : DiscoveriesSave1121Tests. Verify.ps1 (agent) : 1846/1846, 0 avertissement, 0 erreur ; DLL 0.1121.0.0. Rien à essayer en jeu.
### English
- Safer saves, part one (audit A04 and A09): discoveries and seen prices are no longer marked « saved » before the write, only once the file is written; a write that fails (full disk, locked file) keeps them to save and another try follows a minute later; what changes during the copy goes in the next save. At unload, a save already running is waited for (5 s at most) before the rest is written. The crafting recorder calls the game's function through the safe call during the PvP and ranked-content cut too. Part 2 (cache: archive and purge as one operation) follows.

## 0.1120.0
- Plan global, X-150 : Application de bureau, lecture des réglages du plugin (revue de sécurité, audit A15) : à un avis « réglages » du lien, elle les relit hors du dessin, dans un objet neuf, jamais dans celui qui sert : fichier lu en entier avec une borne de 8 Mo pendant que le jeu peut l'écrire, adresses Discord retirées (l'application n'envoie rien), types limités à une liste fermée (ceux de Tensho, nombres, textes, listes et dictionnaires de ceux-ci). Une rafale d'avis donne une seule lecture ; une lecture valide s'applique d'un coup entre deux images ; une lecture ratée est dite et les derniers réglages valides restent. Le démarrage lit de la même façon. SECURITY.md et la preuve x119 le disent.
- Tests : SettingsRead1120Tests. Verify.ps1 (agent) : 1843/1843, 0 avertissement, 0 erreur ; DLL 0.1120.0.0. Non validé en jeu.
### English
- Desktop application, reading the plugin's settings (security review, audit A15): on a « settings » notice of the link it reads them again off the frame, into a new object, never into the one in use: the file read whole with an 8 MB bound while the game may write it, Discord addresses taken out (the app sends nothing), types limited to a closed list (Tensho's own, numbers, texts, lists and dictionaries of those). A burst of notices gives one reading; a valid reading is applied at once between two frames; a failed reading is said and the last valid settings stay. The start reads the same way. SECURITY.md and the x119 evidence say so.

## 0.1119.0
- Plan global, X-149 : Lien local avec l'application de bureau, protocole 3 (revue de sécurité) : les avis de données changées envoient désormais une catégorie d'une liste fermée (réglages, banque, personnages, journal des gils, listes d'artisanat, Mes outils, voyages, hauts faits, équipes du Dédale, missions, découvertes) au lieu du nom du fichier ; les fichiers de la banque et des personnages portaient l'identifiant du personnage, qui ne passe donc plus. Une catégorie hors liste est refusée. Le protocole 2 est refusé ; quand le jeu offre un lien d'un autre protocole, l'application le dit et demande d'installer l'application de la version du plugin. SECURITY.md (deux moitiés) dit exactement ce qui passe par le lien ; info-bulle du réglage et preuve x119 suivent.
- Tests : LinkCategories1119Tests. Verify.ps1 (agent) : 1838/1838, 0 avertissement, 0 erreur ; DLL 0.1119.0.0. Non validé en jeu.
### English
- Local link with the desktop application, protocol 3 (security review): notices of changed data now send a category from a closed list (settings, bank, characters, gil journal, craft lists, My tools, voyages, achievements, Labyrinth teams, duties, discoveries) instead of the file's name; the bank's and the characters' files carried the character's id, which therefore no longer passes. A category outside the list is refused. Protocol 2 is refused; when the game offers a link of another protocol, the application says so and asks to install the application of the plugin's version. SECURITY.md (both halves) states exactly what goes through the link; the setting's tooltip and the x119 evidence follow.

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
