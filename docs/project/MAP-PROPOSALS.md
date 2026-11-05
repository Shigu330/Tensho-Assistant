# Carte Tensho : propositions d'amélioration (audit du 2026-11-02, étape e)

Liste établie par un audit multi-agents en lecture seule de la carte (0.327). **Rien n'était implémenté** à l'audit (relu le 2026-11-03 : les propositions faites depuis, à l'étape g, portent une ligne « État ») : vous choisissez ce qu'on
ajoute (règle du 2026-11-02 : les décisions sont gardées pour la fin, voir [ROADMAP](ROADMAP.md) « Décisions reportées à la fin »).
Les défauts trouvés par le même audit sont corrigés en 0.328 ([CHANGELOG](../../CHANGELOG.md)) ; P0 en fait partie.

Valeur et taille sont les estimations de l'audit. « Sonde » : une lecture du jeu ou un relevé en jeu est nécessaire avant de coder
(aucune donnée n'est devinée). Les sources citées sont des fichiers du dépôt.

## P0 — Afficher la carte du jeu quand la carte Tensho ne peut pas s'afficher

- **État (relu le 2026-11-03)** : FAIT 0.328 (défauts de l’audit).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs`.
- **Ce que le joueur obtient** : Aujourd'hui, si l'on appuie sur M pendant que la carte Tensho est masquée, aucune carte ne s'affiche. C'est le cas en combat ou en mission avec « Masquer en combat » ou « Masquer en mission » cochés, ou quand le catalogue des cartes n'est pas encore chargé ou a échoué. Avec le repli, la carte du jeu reste visible chaque fois que la carte Tensho ne peut pas se dessiner.
- **Comment, taille, dépendances** : Passer au remplaçant une fonction « carte Tensho affichable » (catalogue chargé et pas de masquage en cours) à la place de MapViewOpen. Si elle répond non quand AreaMap apparaît, garder la carte du jeu (keepGameMap). Si la carte Tensho se masque pendant l'hébergement (début d'un combat), appeler Restore(unit). Taille S. Aucune sonde. Test en jeu : appuyer sur M en combat avec l'option cochée. Une décision : si l'utilisateur préfère « aucune carte en combat », ce choix devient un réglage (avec infobulle et ↺).
- **Source** : Constaté en lisant le code, pas reproduit en jeu. Plugin.cs:145 : le remplaçant reçoit tenshoOpen = MainWindow.MapViewOpen, qui ne dit que si la carte est ouverte, pas si elle est visible. MainWindow.MapView.cs:76-82 (MapViewVisible) et MapViewWindow.cs:21 (DrawConditions) : la fenêtre n'est pas dessinée si le catalogue est absent ou si l'un des masquages s'applique. NativeMapReplacer.Dalamud.cs:50-62 : la carte du jeu est quand même placée hors écran (hosted). Règle R-14 de docs/ai/RULES.md : échouer proprement.

## P1 — Zoom à l'ouverture, réglable et gardé entre les sessions

- **État (relu le 2026-11-05)** : FAIT 0.542.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : La carte s'ouvre au zoom choisi par le joueur : le dernier zoom utilisé (gardé après une relance du jeu), un pourcentage fixe en suivant le personnage, ou la carte entière. Le zoom appliqué quand on demande une quête ou un lien est réglable lui aussi. Aujourd'hui, la carte démarre toujours à 50 %.
- **Comment, taille, dépendances** : Ajouter à MapViewSettings : OpenZoomMode (dernier, fixe, carte entière), OpenZoomPercent, FocusZoomPercent (100 % par défaut) et LastZoom. LastZoom est enregistré à la fermeture de la fenêtre ou après une pause, jamais à chaque image. Chaque réglage a son infobulle et son ↺. Les classer dans les listes de ConfigurationReset (un test l'exige). Taille S. Aucune sonde. Une décision : la valeur par défaut. Recommandé : « dernier zoom », avec 50 % la première fois, ce qui ne change rien pour l'utilisateur actuel.
- **Source** : Note d'audit « Carte : zoom au lancement » (étape e). MainWindow.MapView.cs:26 : mapZoom=0.5f, un champ jamais enregistré. Lignes 773 et 806 : une quête ou un lien impose un zoom d'au moins 100 % (Math.Max(mapZoom,1f)), valeur figée. Lignes 780 et 816-821 : le choix d'une zone dans les menus passe en « carte entière ». Configuration.cs:57-167 : MapViewSettings n'a aucun champ de zoom.

## P2 — Taille des étiquettes réglée à part de celle des icônes

- **État (relu le 2026-11-05)** : FAIT 0.543.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Les noms de lieux et les étiquettes de niveau des points de récolte restent lisibles à tous les zooms sans se chevaucher. Sur une carte de région très dézoomée, les noms rétrécissent ou se masquent sous un seuil choisi par le joueur. Les étiquettes de récolte et les repères au bord suivent la taille choisie.
- **Comment, taille, dépendances** : Ajouter un « Suivi du zoom des noms » séparé (repli : la valeur actuelle) et un « zoom minimal pour afficher les noms », puis faire suivre la même règle aux étiquettes de récolte (seuil réglable, ou zoom minimal, à la place de 0,6). Ajouter éventuellement une échelle des repères au bord. Option : ne pas dessiner un nom qui en recouvre un autre déjà dessiné (test de rectangles, liste bornée par carte). Infobulle et ↺ partout. Taille S. Aucune sonde. Une décision : les valeurs par défaut (recommandé : celles d'aujourd'hui, rien ne change tant que le joueur ne règle rien).
- **Source** : Note d'audit « zoom des étiquettes ». MainWindow.MapView.cs:837-852 : les noms valent police × 2,4 × zoom^suivi × échelle des noms. Le « Suivi du zoom » est commun avec les icônes (Configuration.cs:113-116, 15 % par défaut), si bien qu'à 15 % de zoom les noms gardent environ 75 % de leur taille et se chevauchent. Lignes 930-931 : l'étiquette de niveau des points de récolte a la taille fixe de la police et n'apparaît qu'au-dessus d'un seuil de zoom codé en dur (0,6). Ligne 1459 : les repères au bord font 26 px fixes.

## P3 — Éthérite la plus proche du drapeau ou d'un point, avec son coût

- **État (relu le 2026-11-05)** : FAIT 0.544.
- **État (relu le 2026-11-03)** : en partie : éthérite la plus proche avec son coût pour les liens du chat (0.373) et « Y aller » des lieux de récolte (0.397) ; pas encore depuis le drapeau ou un point quelconque.
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Clic droit sur la carte ou sur le drapeau : l'entrée « Se téléporter à l'éthérite la plus proche » donne le nom et le coût réel en gils, puis demande la confirmation habituelle. Plus besoin de chercher soi-même l'éthérite à côté d'un drapeau posé (train de chasse, rendez-vous).
- **Comment, taille, dépendances** : Calcul en pixels de texture sur la carte affichée, parmi les marqueurs dont AetheryteId≠0. Coût lu dans TeleportCosts : s'il est absent, la ligne dit « coût inconnu (non harmonisée ?) », jamais 0. Entrée ajoutée au menu général et au menu de l'icône du drapeau. Limite dite à l'écran : seulement les éthérites de cette carte, sans temps de trajet. Les éclats d'éthernet (DataType 4, icône 60430, sens de la clé inconnu dans 0.107.0-aetheryte-probe.log) restent exclus tant qu'ils ne sont pas sondés. Taille S. Aucune sonde pour DataType 3. Pas de nouvelle décision : c'est une partie de M-03, déjà retenu (DEC-025), faisable seule dès maintenant.
- **Source** : GAPS.md:154 (M-03 : « éthérite le plus proche sur clic »). docs/evidence/0.318-plugin-gaps.json : Aetheric Flow et MapLinker, « éthérite la plus proche », valeur haute. Déjà présent dans le code : marqueurs DataType 3 = éthérites (MapMarkerSource.Dalamud.cs:31, preuve docs/evidence/0.107.0-aetheryte-probe.log, lignes RADZ type=3), coût réel lu dans le jeu (MapLiveReader.Dalamud.cs, ReadTeleportCosts), fenêtre de confirmation (MainWindow.MapView.cs:1065-1083).

## P4 — Liens de carte du chat (M-03) : liste, drapeau et éthérite la plus proche

- **État (relu le 2026-11-03)** : FAIT 0.373 (M-03).
- **Valeur** : haute ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Les liens de carte vus dans le chat (trains de chasse, cartes au trésor, rendez-vous) sont gardés dans une liste de la carte Tensho, filtrable par canal. Un clic pose le drapeau et centre la carte. Un bouton propose l'éthérite la plus proche, téléportation toujours sur clic.
- **Comment, taille, dépendances** : Nouveau lecteur Maps/ChatMapLinks.Dalamud.cs : abonnement à ChatMessage avec un propriétaire et un Dispose. Il copie le MapLinkPayload (territoire, carte, X/Y), le canal et l'heure dans un historique borné. Affichage dans la carte : onglet du clic droit ou panneau. Il dépend de la proposition « éthérite la plus proche ». Taille M. Sonde en jeu seulement si l'on lit aussi les coordonnées écrites en texte sans lien : format « X / Y » selon la langue, risque de faux positifs. Décision restante : avancer M-03 dans e) ou le laisser en g) ; taille de l'historique ; lire ou non le texte libre (recommandé : liens seulement).
- **Source** : GAPS.md:154 (M-03), choix de l'utilisateur DEC-025 : « liste dans Tensho (tous les canaux, filtrables), drapeau et éthérite le plus proche sur clic ». Prévu à l'étape g) 3 Monde (ROADMAP.md, étape g). docs/evidence/0.318-plugin-gaps.json : MapLinker, ChatCoordinates et Aetheric Flow, valeur haute. API : IChatGui.ChatMessage (présent dans Dalamud.xml, Hooks/dev).

## P5 — Molette : zoom depuis toute la fenêtre et pas réglable

- **État (relu le 2026-11-05)** : FAIT 0.545.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : La molette zoome partout au-dessus de la carte : sur le curseur de zoom, sur la colonne d'outils et sur les noms posés par-dessus la carte. Le pas de la molette est réglable et son sens peut être inversé.
- **Comment, taille, dépendances** : Lire io.MouseWheel quand la fenêtre de la carte ou l'un de ses enfants est survolé (IsWindowHovered RootAndChildWindows). Au-dessus de la colonne ou du curseur, zoomer autour du centre du canevas. Ajouter MapViewSettings.WheelStep et WheelInvert, avec infobulle et ↺. Taille S. Aucune sonde. Une décision : préciser ce que visait la note, puisque la molette marche déjà sur le canevas (recommandé : les trois points ci-dessus).
- **Source** : Note d'audit « pas de molette ». Le canevas zoome déjà à la molette autour du curseur (MainWindow.MapView.cs:790-796). En revanche, la molette est ignorée dès que la souris est sur un élément posé par-dessus la carte (ligne 786 : hovered && !mapOverlayHovered), et le curseur vertical (ligne 415, VSliderFloat) ne réagit pas à la molette. Le pas est fixe (1,15 à la ligne 794 ; 1,25 pour les boutons, lignes 409 et 418).

## P6 — Coordonnées du personnage toujours visibles et distances

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Un coin de la carte montre en permanence les coordonnées X/Y du personnage, et celles du drapeau s'il y en a un. Les infobulles du curseur, des icônes et des repères au bord donnent la distance depuis le personnage, en ligne droite (le relief n'est pas compté).
- **Comment, taille, dépendances** : Ajouter MapProjection.WorldFromPixel, avec des tests aller-retour. Distance sur X/Z depuis live.Position. L'unité affichée est celle du monde du jeu ; son nom (yalms) est à confirmer en jeu contre une distance connue, sans rien supposer. Un réglage « coordonnées et distances » avec infobulle et ↺. Taille S. Sonde : une vérification en jeu de l'unité. Pas de décision, sauf le coin d'affichage (recommandé : en bas à droite, face aux coordonnées du curseur).
- **Source** : STATUS.md:129 : demande du 2026-11-01 pour les repères au bord, « distance éventuelle », non faite. La version 0.297 n'a gardé que le nom (MainWindow.MapView.cs:1480). Lignes 1039-1046 : seules les coordonnées du curseur s'affichent, et seulement au survol. MapProjection.cs n'a pas de conversion pixel → monde.

## P7 — Éthérites non harmonisées signalées et coût au survol

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Le survol d'une éthérite donne tout de suite son coût de téléportation. Une éthérite non harmonisée est grisée et le dit, au lieu de l'apprendre après le clic.
- **Comment, taille, dépendances** : Au survol, demander RefreshTeleportCosts, qui reste en file et se lit sur le thread du framework, au plus une fois toutes les quelques secondes, puis afficher le coût s'il est connu. L'état harmonisé se lit sur le framework, copié par carte et protégé (R-14) ; s'il est illisible, aucun grisé (inconnu, pas « non harmonisée »). Taille S. Sonde : IsAetheryteUnlocked comparé à la fenêtre Téléportation du jeu, à consigner dans docs/evidence. Pas de décision.
- **Source** : MainWindow.MapView.cs:1206 : au survol, seule l'indication « clic = téléportation » s'affiche ; le coût n'apparaît que dans la fenêtre de confirmation (lignes 1072-1075). MapLiveReader.Dalamud.cs (commentaire de TeleportCosts) : une éthérite absente de la liste est « non harmonisée ou non proposée », ce qui reste ambigu. Nom trouvé dans les DLL locales, sens à sonder : UIState.IsAetheryteUnlocked(uint) (FFXIVClientStructs.xml, Hooks/dev).

## P8 — Favoris dessinés sur la carte, ajoutés au clic droit, avec note (M-08)

- **État (relu le 2026-11-03)** : FAIT 0.378 (M-08).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Maps/MapBookmarks.cs`.
- **Ce que le joueur obtient** : Les favoris s'affichent sur la carte Tensho, avec une icône réglable comme les autres. Un clic droit « Ajouter aux favoris ici » enregistre le point, avec une note et un groupe en option. « Copier les coordonnées » met X/Y dans le presse-papiers. Les favoris peuvent se partager par un code texte.
- **Comment, taille, dépendances** : Nouvelle couche dessinée (clé de glyphe comme GlyphVista, avec taille et priorité dans le tableau des icônes). Champs Note et Group ajoutés au record en nullable, pour que les anciennes configurations se chargent. Entrée de menu avec saisie du nom. Code de partage sur le modèle de TENSHO-GEAR1 (aucun site tiers). Taille S. Aucune sonde. Décisions restantes : format du partage ; portée globale ou par personnage (recommandé : globale, comme aujourd'hui).
- **Source** : GAPS.md:159 (M-08, choix « Ajouter », DEC-025, étape g) 3) : « ni dessinés sur la carte, ni note, ni groupe, ni partage ». MapBookmarks.cs : record sans note ni groupe, 200 favoris au plus. MainWindow.MapView.cs:1099 : le clic droit ne fait qu'envoyer les coordonnées vers la page Cartes (« Utiliser dans l'outil »).

## P9 — ALEA : bonus d'expérience et couleur selon le temps restant

- **État (relu le 2026-11-05)** : FAIT 0.548.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs`.
- **Ce que le joueur obtient** : Sur la carte et dans l'onglet ALEA du clic droit, une ALEA avec bonus est marquée, et la couleur indique celles qui vont bientôt finir.
- **Comment, taille, dépendances** : Copier HasBonus dans MapFateMarker sur le thread du framework, puis afficher le libellé et une couleur selon SecondsLeft. Les seuils sont un choix d'interface, réglable avec infobulle et ↺. Taille S. Relevé en jeu : vérifier que HasBonus correspond à l'icône de bonus du jeu. Pas de décision, sauf les seuils (recommandé : orange sous 5 min).
- **Source** : docs/evidence/0.318-plugin-gaps.json : Mappy, « Liste des ALEA actives : couleur selon l'expiration, « Exp Bonus! » », statut partial. MapLiveReader.Dalamud.cs, ReadFates : le bonus n'est pas copié. MapFateMarker n'a pas de champ bonus. L'API Dalamud expose IFate.HasBonus (Dalamud.xml, Hooks/dev). MainWindow.MapLocalLists.cs:94-98 (FateLabel).

## P10 — Recherche sur la carte (lieux, éthérites, PNJ, quêtes, ressources)

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Un champ de recherche dans la fenêtre de la carte trouve un nom de lieu, une éthérite, un PNJ vendeur ou de Triple Triad, un donneur de quête ou une ressource de récolte, sur toutes les cartes. Un clic ouvre la bonne carte, la centre et pose le drapeau.
- **Comment, taille, dépendances** : Index de recherche construit une seule fois hors Draw (sur un worker), à partir des données déjà en cache : libellés MapMarker de toutes les plages, index local (quêtes, récolte), catalogue des PNJ vendeurs, PNJ Triple Triad. Liste de résultats bornée, avec clipper. Bouton loupe dans la colonne d'outils. Taille M. Sonde seulement pour l'éthernet (sens de la clé DataType 4). Une décision : périmètre des types cherchés (recommandé : tout sauf l'éthernet tant qu'il n'est pas sondé).
- **Source** : docs/evidence/0.318-plugin-gaps.json : Mappy, « Recherche de cartes, d'éthérites, de points d'intérêt et de l'éthernet, avec centrage », statut partial. MapCatalog.cs, Search : région, zone, sous-zone et id seulement, dans la page Cartes (MainWindow.Maps.cs:39-41) et non dans la fenêtre de la carte.

## P11 — Parties non découvertes masquées comme sur la carte du jeu

- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Comme la carte du jeu, la carte Tensho cache ou grise les zones et les régions que le personnage n'a pas encore découvertes. On évite ainsi de dévoiler des lieux.
- **Comment, taille, dépendances** : Sonde d'abord (tools/ApiProbe, puis un relevé en jeu sur une zone à moitié découverte, à consigner dans docs/evidence). Ensuite : lecture sur le framework, copiée par carte, et dessin d'un voile sur les sous-régions non découvertes, ou grisé des entrées de menu. Si la lecture échoue : tout reste visible et le message « état de découverte indisponible » s'affiche (R-14). Taille M. Décision : masquer, griser, ou laisser un réglage (recommandé : réglage, activé par défaut comme le jeu).
- **Source** : docs/goals/MAP-016-PLAN.md:57, 68 et 69 : « cartes non découvertes masquées comme le jeu », toujours parmi les restes. Noms trouvés dans les DLL locales, sens à sonder : colonnes Lumina Map.DiscoveryIndex, DiscoveryArrayByte et DiscoveryFlag (Lumina.Excel.dll) ; FFXIVClientStructs MapDiscoveryManager.IsMapRegionDiscovered(uint, byte) et IsRegionDiscovered (FFXIVClientStructs.xml, Hooks/dev). La texture ou le masque de brouillard du jeu sont inconnus.

## P12 — Noms écrits à côté des icônes, comme la carte du jeu

- **État (relu le 2026-11-05)** : FAIT 0.596.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs`.
- **Ce que le joueur obtient** : Les noms que le jeu écrit à côté de certaines icônes (éthérites, lieux importants) sont écrits sur la carte Tensho avec le même placement, et pas seulement dans l'infobulle.
- **Comment, taille, dépendances** : Sonder les valeurs de SubtextOrientation, comparées à une capture de la carte du jeu, et consigner le résultat dans docs/evidence. Copier la colonne dans MapIconMarker. Écrire le texte du côté indiqué, à la taille des noms (dépend de la proposition sur les étiquettes). Réglage on/off avec infobulle et ↺. Si la colonne est illisible, aucun texte. Taille M. Décision : l'activer par défaut ou non (recommandé : oui, comme le jeu).
- **Source** : MapMarkerSource.Dalamud.cs:30-33 : seul PlaceNameSubtext est copié. MainWindow.MapView.cs:844 : seuls les marqueurs sans icône (Icon==0) sont écrits sur la carte ; le nom d'un marqueur à icône ne sert que d'infobulle (lignes 896-904). Nom trouvé dans Lumina.Excel.dll, sens à sonder : MapMarker.SubtextOrientation.

## P13 — Emplacement de la carte au trésor déchiffrée (M-14)

- **État (relu le 2026-11-03)** : FAIT 0.384 (M-14).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : La carte au trésor déchiffrée que l'on possède est dessinée sur la carte Tensho, avec une icône, un repère au bord et le drapeau en un clic, sans chercher sur internet.
- **Comment, taille, dépendances** : Sonde en jeu d'abord, avec une carte déchiffrée en poche, puis consigner dans docs/evidence. Ensuite : lecture sur le framework, protégée (R-14), copie de la position (carte, X/Z), nouvelle couche avec taille et priorité, et repère au bord. Taille M. Pas de décision (déjà retenu), sauf l'éventuelle avance de g) vers e).
- **Source** : GAPS.md:165 (M-14, choix « Ajouter », étape g) 3), avec sa note : lecture par EventItemManager.GetTreasureHuntRank() et GetTreasureSpotSubKey(), à sonder. Ces deux noms sont présents dans FFXIVClientStructs.dll (Hooks/dev). Données Lumina TreasureSpot (Location vers Level) et TreasureHuntRank. Rappel déjà présent : Configuration.cs:208 (TreasureMapAt).

## P14 — Mesurer le temps d'affichage de la carte et l'alléger

- **État (relu le 2026-11-05)** : FAIT pour la mesure, 0.489 (ligne « Map » des diagnostics) ; alléger reste à décider sur mesure (tri du 2026-11-05, [PROPOSALS-TRIAGE](PROPOSALS-TRIAGE.md)).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : La carte, la fenêtre la plus chargée de Tensho, reste fluide, et son coût par image est visible dans Diagnostics, comme celui des autres pages.
- **Comment, taille, dépendances** : Ajouter une ligne « Carte » au relevé de Diagnostics (même anneau de 120 images). Garder le regroupement des vendeurs par (carte, catalogue, ShowFixedIcons), comme mapGatheringCache, et les marqueurs filtrés par référence de live.Events. Taille S. Relevé en jeu par l'utilisateur, avant et après, comme en 0.292. Pas de décision.
- **Source** : docs/evidence/0.292-draw-times.md : seules les pages de la fenêtre principale ont été mesurées (MainWindow.cs:102, pageDrawMs indexé par Page) ; la fenêtre de la carte ne l'a jamais été. Travail refait à chaque image : MainWindow.MapView.cs:856 (marqueurs filtrés avec ToArray), 884-895 (VendorCandidates, puis MapVendorIcons.Nearest pour chaque vendeur, ligne 1162), plus les requêtes LINQ sur les marqueurs. Règle : « rien de lourd dans Draw ».

## P15 — État des lectures du jeu affiché sur la carte (R-14, partie carte)

- **État (relu le 2026-11-05)** : FAIT 0.569.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs`.
- **Ce que le joueur obtient** : Après une mise à jour de FF14, la carte indique précisément ce qui ne marche plus (« marqueurs de quête indisponibles après une mise à jour du jeu », « téléportation indisponible »), au lieu d'une carte vide sans explication. Le reste continue de fonctionner.
- **Comment, taille, dépendances** : Un statut par partie (position, drapeau, marqueurs, ALEA, objets, groupe, collectés, téléportation, remplacement), publié avec l'état. Chaque lecture est isolée dans son propre try, pour qu'une panne ne coupe pas les autres. Une ligne discrète sur la carte, et le même statut dans Diagnostics. Lié au repli sur la carte du jeu. Taille S à M. Pas de sonde. Décision : faire la partie carte maintenant ou l'intégrer à l'étape g2 R-14 (recommandé : maintenant, puisque e) commence par la carte).
- **Source** : docs/ai/RULES.md (R-14). MapLiveReader.Dalamud.cs:104 : toute erreur efface tout l'état (state=null) et ne laisse qu'un message dans le journal. La flèche, le drapeau, les marqueurs et le groupe disparaissent ensemble, sans rien dire à l'écran. NativeMapReplacer : catch avec journal seulement. Écriture native : ClearFlag (FlagMarkerCount=0).

## P16 — Option : clic gauche sur la carte pour poser le drapeau

- **État (relu le 2026-11-05)** : FAIT 0.546.
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Un clic gauche bref sur un endroit vide de la carte pose le drapeau, sans passer par le menu du clic droit ; un glisser déplace toujours la carte.
- **Comment, taille, dépendances** : Au relâchement du bouton gauche sans glisser (mapDragged faux), sur une zone sans icône et sur une carte de zone (TerritoryId≠0), appeler PlaceFlagFromMapView. Réglage avec infobulle et ↺. Taille S. Relevé en jeu sur le comportement de la carte du jeu. Décision : activé ou non par défaut (recommandé : désactivé, pour ne rien changer).
- **Source** : MainWindow.MapView.cs:788-789 : le clic gauche sert seulement à glisser. Le drapeau se pose par le menu du clic droit (ligne 1091) ou par le clic sur une icône. Que la carte du jeu pose le drapeau d'un simple clic reste à confirmer en jeu, sans le supposer.

## P17 — Relevé du type de marqueur des mandats et de leurs zones

- **Valeur** : moyenne ; fichier principal : `docs/project/BACKLOG.md`.
- **Ce que le joueur obtient** : Les mandats forment une catégorie juste dans les repères au bord et dans les couleurs de zones, au lieu d'être mêlés aux « autres événements ».
- **Comment, taille, dépendances** : Relevé en jeu : mandat accepté, carte ouverte, mode développeur. La ligne « Map event marker icon … type … » est déjà écrite dans le journal (MapLiveReader.Dalamud.cs, ReadEvents). Copier ce relevé dans docs/evidence, puis faire du type une catégorie d'interrupteur EdgeEvents et une couleur de zone. Taille S. Relevé en jeu d'abord. Pas de décision.
- **Source** : BACKLOG.md:14 et ROADMAP.md:82 : type de marqueur des mandats inconnu. TESTING.md:234 : relevé demandé. docs/goals/MAP-016-PLAN.md:57 et 69 : « confirmation des zones de mandats ». Dans docs/evidence/0.135.0-marker-evidence.log et 0.136.0-marker-evidence.log, seuls les types 0, 3 et 4 apparaissent, sans dire lequel désigne un mandat.

## P18 — Cadres et textures du jeu (ULD), en commençant par la carte

- **État (relu le 2026-11-04)** : abandonné : le cadre du jeu de 0.332 est retiré en 0.510 (DEC-033, rendu jugé mauvais).
- **Valeur** : moyenne ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : La fenêtre de la carte (boutons d'outils, cadre, menus) reprend les cadres et textures de l'interface du jeu, dans la même DLL et sans ressource externe. Les autres fenêtres suivent ensuite.
- **Comment, taille, dépendances** : Sonde d'abord : chemins .uld et numéros de parties des boutons et du cadre de la carte du jeu (tools/ApiProbe), avec une capture, consignés dans docs/evidence. Ensuite : chargement unique hors Draw, une partie par bouton, et repli sur le dessin ImGui actuel si une partie manque (R-14). Taille M pour la carte, L pour toute l'interface. Décision : périmètre (carte seule ou tout de suite tout) et degré d'imitation (boutons, cadre, fond).
- **Source** : BACKLOG.md:15 ; ROADMAP.md:46 (étape e) ; ROADMAP.md:53 et 119 (DEC-023 : avant D17) ; docs/goals/R-06.md:137 (priorité aux .tex et .uld du jeu). Aucun ULD n'est lu aujourd'hui (aucune occurrence dans src/). API présente dans Dalamud.xml (Hooks/dev) : UiBuilder.LoadUld(string) et UldWrapper.LoadTexturePart(string, int).

## P19 — Tests en jeu de la carte encore en attente

- **Valeur** : moyenne ; fichier principal : `docs/project/TESTING.md`.
- **Ce que le joueur obtient** : Avant d'ajouter des fonctions à la carte, on confirme que les derniers changements fonctionnent. Sinon, un défaut ancien et un nouveau risquent de se mélanger.
- **Comment, taille, dépendances** : Une courte vérification guidée de ces cinq points (environ 10 minutes en jeu), avant la première version de carte de l'étape e) ou juste après. Taille S. Relevé en jeu. Décision : la faire maintenant ou la garder pour h) (recommandé : maintenant, puisque e) modifie justement la carte).
- **Source** : TESTING.md:146-156 : 0.242–0.243 (R-01, cible du journal), 0.244 (R-03, Triple Triad), 0.245 (R-04, menu propre à chaque icône), 0.259 et 0.262 (infobulles et ↺), 0.308 (fond du niveau de zoom). ROADMAP.md:136 et 144. La consigne de l'utilisateur place les tests en fin de file (étape h).

## P20 — Bouton « Couches » pour tout afficher ou masquer en un clic

- **État (relu le 2026-11-05)** : FAIT 0.547.
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Un bouton de la colonne d'outils ouvre une petite liste de couches (quêtes, ALEA, récolte, vendeurs, exploration, courants, groupe, favoris) qu'on affiche ou masque d'un clic, sans ouvrir les réglages.
- **Comment, taille, dépendances** : Fenêtre surgissante qui reprend les cases existantes (SettingBox, mêmes clés, mêmes ↺), et un 9e bouton dans le calcul de la colonne. Taille S. Aucune sonde. Décision : liste des couches proposées.
- **Source** : Toutes ces couches sont des cases du menu des réglages, en plusieurs sections (MainWindow.MapView.cs:444-561). La colonne d'outils a 8 boutons (ligne 378).

## P21 — Raccourcis clavier dans la carte

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MapViewWindow.cs`.
- **Ce que le joueur obtient** : Quand la carte a le focus : +/- pour zoomer, flèches pour la déplacer, Début pour la carte entière, une touche pour suivre le personnage.
- **Comment, taille, dépendances** : Lire ImGui.IsKeyPressed seulement quand la fenêtre de la carte a le focus et qu'aucun champ texte n'est actif, pour ne pas voler les touches du jeu. Réglage on/off avec infobulle et ↺. Taille S. Aucune sonde. Décision : les touches (recommandé : désactivé par défaut).
- **Source** : MapViewWindow.cs:17 (Échap ne ferme pas la carte). MainWindow.MapView.cs ne lit aucune touche.

## P22 — Noms des variantes d'icônes de quête (71141 / 71151)

- **Valeur** : basse ; fichier principal : `docs/project/BACKLOG.md`.
- **Ce que le joueur obtient** : Les réglages d'icônes affichent le vrai sens de chaque variante d'icône de quête, au lieu de « variante n ».
- **Comment, taille, dépendances** : Relevé en jeu : quêtes de chaque variante visibles sur la carte, avec capture et ligne de journal, consignées dans docs/evidence. Ensuite seulement, renommer les libellés MapView.QuestKind. Sans relevé, garder « variante n ». Taille S. Relevé en jeu d'abord. Pas de décision.
- **Source** : BACKLOG.md:12-13 ; ROADMAP.md:83 ; TESTING.md:235 ; docs/evidence/0.211-event-icons.md. Ce sens n'existe pas dans les données du jeu ; seul un relevé en jeu le donne. C'est le seul point « avant D17 » encore ouvert.

## P23 — Amis proches sur la carte

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs`.
- **Ce que le joueur obtient** : Les amis présents autour du personnage, donc déjà chargés par le jeu, sont dessinés sur la carte Tensho comme les membres du groupe.
- **Comment, taille, dépendances** : Dans ReadObjects, copier les joueurs avec StatusFlags.Friend (nom, position), au plus N. Nouvelle couche avec taille et priorité, désactivée par défaut. Membres de la compagnie libre : aucune source fiable connue (le tag n'identifie pas sûrement la compagnie), donc laissés de côté sans sonde. Taille S. Décision : ajouter ou non cette couche (vie privée).
- **Source** : docs/evidence/0.318-plugin-gaps.json et GAPS.md:434 : Mini-Mappingway (MIT), « amis et membres de la compagnie libre », statut partial : seul le groupe est montré (ReadParty). ReadObjects parcourt déjà la table des objets. Dalamud expose StatusFlags.Friend (Dalamud.xml). Règle notée dans GAPS (MinimapPlugin) : ne montrer que ce que le jeu affiche déjà, sans en faire un radar.

## P24 — Commande de téléportation par nom (/tensho tp)

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/Plugin.cs`.
- **Ce que le joueur obtient** : Taper « /tensho tp Ul'dah » (ou un alias) lance la même demande de téléportation que le clic sur une éthérite de la carte, avec son coût.
- **Comment, taille, dépendances** : Sous-commande qui cherche dans la liste des éthérites (feuille Aetheryte, en cache) puis ouvre la confirmation, ou téléporte directement si l'option le permet. C'est toujours une action explicite du joueur. Taille S. Aucune sonde. Décision : nom de la commande et alias.
- **Source** : docs/evidence/0.318-plugin-gaps.json et GAPS.md:443 : Teleporter, statut partial, téléportation au clic seulement. MapLiveReader.Teleport et ReadTeleportCosts existent déjà.

## P25 — Mini-carte Tensho

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Une petite fenêtre compacte qui suit le personnage (éthérites, groupe, ALEA, drapeau), redimensionnable et zoomable, à côté de la mini-carte du jeu ou à sa place.
- **Comment, taille, dépendances** : Seconde fenêtre WindowSystem qui réutilise DrawMapCanvas en mode compact (sans menus). La rotation avec la caméra demande de lire l'orientation de la caméra (sonde). Taille L. Décision : la faire ou non, et rotation ou nord en haut.
- **Source** : GAPS.md:467 et docs/evidence/0.318-plugin-gaps.json : MinimapPlugin, statut partial (la carte Tensho suit le joueur mais n'est pas une mini-carte et ne tourne pas). Règle de conformité notée : ne montrer que ce que le jeu affiche.

## P26 — Position mise en évidence au survol des listes du clic droit

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.MapLocalLists.cs`.
- **Ce que le joueur obtient** : En survolant une ligne des onglets du clic droit (quête, PNJ, point de récolte), l'endroit correspondant clignote sur la carte en dessous.
- **Comment, taille, dépendances** : Garder la position survolée de l'image en cours ; le canevas dessine un anneau pulsé à l'image suivante. Taille S. Aucune sonde. Pas de décision.
- **Source** : MainWindow.MapLocalLists.cs:90-91 et lignes suivantes : au survol, seules les coordonnées s'affichent en texte (CoordinateText).

## P27 — Couleur réglable par icône

- **État (relu le 2026-11-05)** : ÉCARTÉE à la revue avec l'utilisateur ([plan global](GLOBAL-PLAN.md)).
- **Valeur** : basse ; fichier principal : `src/TenshoAssistant/MainWindow.MapView.cs`.
- **Ce que le joueur obtient** : Chaque type d'icône peut recevoir une teinte, en plus de sa taille et de sa priorité, pour repérer d'un coup d'œil ce qui compte.
- **Comment, taille, dépendances** : Table IconTints (globale et par carte, comme les tailles), appliquée par le paramètre de couleur d'AddImage. Pastille de couleur avec infobulle et ↺ sur chaque ligne. Taille S. Aucune sonde. Décision : utile ou non (faible valeur).
- **Source** : docs/evidence/0.318-plugin-gaps.json : Mappy, « Réglages par icône : visibilité, taille, couleur », statut partial, valeur basse. MainWindow.MapView.cs:646-702 : visible, taille, priorité et menu, sans couleur.
