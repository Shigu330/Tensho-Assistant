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

## 0.1585.0
- Plan global, X-556 : Relecture de 0.1516 à 0.1520, constats mineurs. M3 : dans la fenêtre de chat, le « ×n » d'un message replié sortait à droite et se coupait quand la dernière ligne du message remplissait la largeur, et un compte qui montait ne relançait pas la mise en page : un message replié est maintenant mis en page plus étroit de la largeur de son « ×n » (jamais moins de la moitié), et de nouveau quand le compte monte. M2 (R06) : l'explication du filtre Emotes ne dit plus que les emotes standard sont vues dans le périmètre d'un /dire ; elle dit ce que le jeu dit, seulement de /cmdémotion (TextCommand 401). M4 (outil de l'agent) : la reconnaissance d'un essai « pas compris » attrape aussi « pas sûr d'avoir (bien) compris », « compris de travers » et « incompréhensible ». M1 écarté : les infobulles ne s'ouvrent qu'au survol et les images de référence de l'application de bureau ne montrent que des pages au repos ; les explications de X-545 sont couvertes par leur essai en jeu. Nouveau test ChatRepeatWidth1585Tests.
- Pour les joueurs : fenêtre de chat : le « ×n » d'un message répété ne sort plus de la fenêtre.
- Tests : ChatRepeatWidth1585Tests. Verify.ps1 (agent) : 3239/3239, 0 avertissement, 0 erreur ; DLL 0.1585.0.0. Non validé en jeu.
### English
- Review of 0.1516 to 0.1520, minor findings. M3: in the chat window, the « ×n » of a folded message ran out on the right and was cut when the message's last line filled the width, and a growing count did not lay the message out again: a folded message is now laid out narrower by the width of its « ×n » (never below half), and again when the count grows. M2 (R06): the Emotes filter's explanation no longer says the standard emotes are seen within a /say radius; it says what the game says, of /emote only (TextCommand 401). M4 (agent tool): the « not understood » reading of a test note also catches « not sure I understood (well) », « understood it wrong » and « incomprehensible ». M1 set aside: tooltips open on hover only and the desktop application's reference images show pages at rest; the explanations of X-545 are covered by their in-game test. New test ChatRepeatWidth1585Tests.
- For players: Chat window: the « ×n » of a repeated message no longer runs past the window's edge.

## 0.1584.0
- Plan global, X-552 : Dédale bestial, journal : depuis 0.1524, la ligne « Labyrinth reading » n'est écrite que quand son contenu change, mais le journal des 17 et 18/11 en compte encore 321 en deux soirées, dont un tiers dues au survol : les infobulles de détail du Dédale (XBMItemDetail, XBMPetActionDetail, XBMBattleMonsterDetail, XBMBattleDetailAction) qui s'ouvrent sous la souris. Elles ne font plus changer la ligne ; la liste du plateau (XBMStageDetailList), les écrans de chargement, le combat et la coupure la font toujours changer. Relu sur le journal du 18/11 : 321 lignes en auraient fait 192. Nouveau test LabyrinthHover1584Tests.
- Tests : LabyrinthHover1584Tests. Verify.ps1 (agent) : 3237/3237, 0 avertissement, 0 erreur ; DLL 0.1584.0.0. Non validé en jeu.
### English
- Beastmaster labyrinth, log: since 0.1524 the « Labyrinth reading » line is written only when its content changes, but the log of 11/17 and 11/18 still counts 321 of them in two evenings, a third from hovering: the labyrinth's detail tips (XBMItemDetail, XBMPetActionDetail, XBMBattleMonsterDetail, XBMBattleDetailAction) that open under the mouse. They no longer change the line; the board's list (XBMStageDetailList), the loading screens, combat and the cut still do. Replayed on the log of 11/18: 321 lines would have been 192. New test LabyrinthHover1584Tests.

## 0.1583.0
- Plan global, X-541 : Carte › Repères › Repères favoris, import de coordonnées collées (« Lieu ( x , y ) », listes de chasse) : le champ d'import est maintenant sur plusieurs lignes, si bien qu'un collage de plusieurs lignes garde ses retours à la ligne et donne un favori par ligne ; et le lieu est cherché sous le nom de zone du client puis sous ses noms dans les autres langues du jeu (feuille PlaceName en anglais, français, allemand et japonais, lue au premier import), si bien qu'une liste en anglais est reconnue sur un client français. L'info-bulle du champ le dit. Nouveau test PastedZones1583Tests (comportement : trois lignes dont une en anglais et une inconnue donnent deux favoris et un inconnu).
- Pour les joueurs : repères de la carte : une liste de coordonnées collée donne un favori par ligne, et les noms de zone d'une autre langue du jeu sont reconnus.
- Tests : PastedZones1583Tests. Verify.ps1 (agent) : 3235/3235, 0 avertissement, 0 erreur ; DLL 0.1583.0.0. Non validé en jeu.
### English
- Map › Bookmarks › Favourite bookmarks, import of pasted coordinates (« Place ( x , y ) », hunt lists): the import field is now multi-line, so a paste of several lines keeps its line breaks and gives one bookmark per line; and the place is looked up under the client's zone name, then under its names in the game's other languages (PlaceName sheet in English, French, German and Japanese, read on the first import), so an English list is recognised on a French client. The field's tooltip says so. New test PastedZones1583Tests (behaviour: three lines, one in English and one unknown, give two bookmarks and one unknown).
- For players: Map bookmarks: pasted coordinate lists keep one bookmark per line, and zone names in another game language are recognised.

## 0.1582.0
- Plan global, X-642 : Relecture de 0.1579 à 0.1581, constats mineurs. M1 : le fichier de langue français avait un retour chariot seul après une clé ajoutée en 0.1580 ; Git le voyait comme un fichier binaire et l'enregistrait entier en CRLF, si bien qu'une seule clé ajoutée réécrivait ses 8 536 lignes dans le diff. Les fichiers de langue anglais et français sont repassés en fins de ligne LF, et un nouveau test refuse tout retour chariot seul dans les fichiers de langue (LanguageLineEnds1582Tests). M2 : les raisons des constats non retenus du banc du relecteur sont écrites dans le point X-631 (déjà corrigés, déjà au plan, faux ou écartés).
- Tests : LanguageLineEnds1582Tests. Verify.ps1 (agent) : 3232/3232, 0 avertissement, 0 erreur ; DLL 0.1582.0.0. Rien à essayer en jeu.
### English
- Review of 0.1579 to 0.1581, minor findings. M1: the French language file had a lone carriage return after a key added in 0.1580; Git saw it as a binary file and stored it whole in CRLF, so one added key rewrote its 8,536 lines in the diff. The English and French language files are back to LF line ends, and a new test refuses any lone carriage return in the language files (LanguageLineEnds1582Tests). M2: the reasons of the reviewer bench findings not kept are written in point X-631 (already fixed, already planned, wrong or set aside).

## 0.1581.0
- Plan global, X-639 : Constat du banc du relecteur (X-631, relecture de 0.1560, M3) : au démarrage, un fichier des prix vus abîmé qui n'a pas pu être mis de côté n'est plus pris pour un fichier tenu par un autre programme : il n'est plus relu toutes les 30 s pour rien, et le journal dit qu'il est abîmé (la sauvegarde suivante le met de côté). Nouveau test SeenStuck1581Tests (comportement : fichier abîmé ouvert par un autre lecteur).
- Tests : SeenStuck1581Tests. Verify.ps1 (agent) : 3230/3230, 0 avertissement, 0 erreur ; DLL 0.1581.0.0. Rien à essayer en jeu.
### English
- Finding of the reviewer bench (X-631, review of 0.1560, M3): at start, a damaged prices-seen file that could not be set aside is no longer taken for a file held by another program: it is no longer read again every 30 s for nothing, and the log says it is damaged (the next save sets it aside). New test SeenStuck1581Tests (behaviour: a damaged file opened by another reader).

## 0.1580.0
- Plan global, X-633 : Constats du banc du relecteur (X-631) sur la fenêtre de chat, corrigés. X-633 : une répétition repliée (« ×N ») compte comme non lue et comme alerte, comme un message neuf : un mot d'alerte redit dans la minute remet « ! » sur un onglet déjà vu. X-634 : l'alerte d'un message est trouvée une fois par message (et de nouveau seulement quand les mots changent) au lieu de chaque image, et le regroupement des lignes de combat par genre est fait une fois. X-635 : un lien de carte illisible ferme le lien encore ouvert : son texte reste simple. X-636 : un lien du chat agit au relâché du clic gauche, sans glissement : appuyer sur un lien pour déplacer la fenêtre ne pose plus le drapeau ni n'ouvre l'objet. X-637 : la bulle d'un lien n'est formée qu'au survol. X-638 : la bulle d'un lien d'objet de haute qualité le dit (HQ). Tests existants adaptés au nouvel appel (raison écrite). Nouveau test ChatBench1580Tests.
- Pour les joueurs : fenêtre de chat : un message répété replié avec un mot d'alerte marque de nouveau son onglet, un lien n'agit plus quand on déplace la fenêtre depuis lui, et un lien d'objet de haute qualité dit HQ.
- Tests : ChatBench1580Tests. Verify.ps1 (agent) : 3228/3228, 0 avertissement, 0 erreur ; DLL 0.1580.0.0. Non validé en jeu.
### English
- Findings of the reviewer bench (X-631) on the chat window, fixed. X-633: a folded repeat (« ×N ») counts as unread and as an alert, like a new message: an alert word said again within the minute puts « ! » back on a tab already seen. X-634: a message's alert is found once per message (again only when the words change) instead of each frame, and the combat lines' grouping by kind is made once. X-635: an unreadable map link ends the link still open: its text stays plain. X-636: a chat link acts on the left button's release, without a drag: pressing a link to move the window no longer places the flag nor opens the item. X-637: a link's tooltip is built only on hover. X-638: the tooltip of a high-quality item link says so (HQ). Existing tests adapted to the new call (reason written). New test ChatBench1580Tests.
- For players: Chat window: a repeated message folded with an alert word marks its tab again, a link no longer acts when you drag the window from it, and a high-quality item link says HQ.

## 0.1579.0
- Plan global, X-632 : Relecture de 0.1576 à 0.1578, M1 : le refus d'une alerte d'incantation de boss ou d'un minuteur d'incantation (DEC-055) s'étend aux fenêtres de barre d'incantation du jeu (_TargetInfoCastBar, _CastBar, FocusTargetInfo, CastBarEnemy et leurs types AddonCastBar, AddonTargetInfo…). Leur nom n'apparaît qu'à un seul endroit permis, la liste de ce qui n'est pas une fenêtre devant les panneaux (GameWindow), qui les ignore ; cliquet sur ce compte, application de bureau fouillée aussi. Nouveau test DalamudRulesCastBar1579Tests, avec la preuve que le refus trouve une lecture de ces fenêtres.
- Tests : DalamudRulesCastBar1579Tests. Verify.ps1 (agent) : 3223/3223, 0 avertissement, 0 erreur ; DLL 0.1579.0.0. Rien à essayer en jeu.
### English
- Review of 0.1576 to 0.1578, M1: the refusal of a boss cast alert or a cast timer (DEC-055) is widened to the game's cast bar windows (_TargetInfoCastBar, _CastBar, FocusTargetInfo, CastBarEnemy and their types AddonCastBar, AddonTargetInfo…). Their name appears in a single allowed place, the list of what is not a window in front of the panels (GameWindow), which skips them; ratchet on this count, desktop application searched too. New test DalamudRulesCastBar1579Tests, with the proof that the refusal finds a read of these windows.

## 0.1578.0
- Plan global, X-629 : Ménage après l'en-tête unique des pages à onglets (X-600, 0.1573) : quatre clés de langue sans usage retirées (CraftPresets.Back, CraftPresets.Title, CraftLiveUi.Title, GpUi.Title), en anglais et en français ; l'allemand et le japonais ne les avaient pas. Aucune source ni aucun test ne les demandait (recherche dans src, tests et tools). Nouveau test UnusedKeys1578Tests.
- Tests : UnusedKeys1578Tests. Verify.ps1 (agent) : 3221/3221, 0 avertissement, 0 erreur ; DLL 0.1578.0.0. Rien à essayer en jeu.
### English
- Clean-up after the single header of the tabbed pages (X-600, 0.1573): four language keys without use removed (CraftPresets.Back, CraftPresets.Title, CraftLiveUi.Title, GpUi.Title), in English and in French; German and Japanese did not have them. No source nor test asked for them (search in src, tests and tools). New test UnusedKeys1578Tests.
