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
