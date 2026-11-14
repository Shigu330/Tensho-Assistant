# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

## 0.1348.0
- Plan global, X-357 : Tri des documents accepté par l'utilisateur : 22 documents de travail périmés retirés (propositions de l'audit du 2026-11-02 et leur tri, étude des outils de contenus, archives, audits du nettoyage X-76 et du réseau de la 0.948, 7 relevés ponctuels de docs/evidence) ; les audits 0.857 et 0.874, cités par le code, rejoignent docs/evidence/ ; chaque lien vers un document retiré devient un lien permanent vers sa dernière version (commit 024c426c), plan compris ; carte des documents mise à jour.
- Tests : AuditsFolder1086Tests. Verify.ps1 (agent) : 2523/2523, 0 avertissement, 0 erreur ; DLL 0.1348.0.0. Rien à essayer en jeu.
### English
- Document sort accepted by the user: 22 outdated working documents removed (the proposals of the 2026-11-02 audit and their triage, the content tools study, archives, the X-76 cleanup and the 0.948 network audits, 7 one-off readings of docs/evidence); audits 0.857 and 0.874, cited by the code, join docs/evidence/; every link to a removed document becomes a permanent link to its last version (commit 024c426c), the plan included; map of the documents updated.

## 0.1347.0
- Plan global, X-357 : Dépôt propre avant l'annonce (inventaire accepté par l'utilisateur) : CONTRIBUTING donne la politique IA de Dalamud pour information seulement (plus suivie, DEC-050) ; tête de GLOBAL-PLAN sans version ni soumission périmées ; ROADMAP, TESTING et docs/README ne nomment plus de chemins de travail hors du dépôt ; les formulaires d'issue renvoient à la version publiée (main). `commit-release.bat` passe dans `scripts/`.
- Tests : FirstPlayerDocs1219Tests. Verify.ps1 (agent) : 2523/2523, 0 avertissement, 0 erreur ; DLL 0.1347.0.0. Rien à essayer en jeu.
### English
- Clean repository before the announcement (inventory accepted by the user): CONTRIBUTING gives the Dalamud AI policy for information only (no longer followed, DEC-050); GLOBAL-PLAN's head without an outdated version or submission; ROADMAP, TESTING and docs/README no longer name working paths outside the repository; the issue forms link the published version (main). `commit-release.bat` moves to `scripts/`.

## 0.1346.0
- Plan global, X-358 : Carte, onglet Quêtes du menu du clic droit : la liste des quêtes que le jeu propose (0.1345) comptait aussi les étapes des quêtes déjà acceptées, de même type de marqueur. Seules les quêtes de la liste du jeu des quêtes pas encore acceptées comptent maintenant comme proposées (`MapOfferedQuests.Tag`, marque `Offered` des marqueurs) ; une carte où ne figurent que des quêtes acceptées ne restreint plus la liste. Test QuestOffered1346 (une quête acceptée et une proposée au même endroit).
- Pour les joueurs : Carte, onglet Quêtes : les quêtes déjà acceptées ne changent plus la liste des quêtes proposées.
- Tests : QuestOffered1346Tests. Verify.ps1 (agent) : 2523/2523, 0 avertissement, 0 erreur ; DLL 0.1346.0.0. Non validé en jeu.
### English
- Map, Quests tab of the right-click menu: the list of the quests the game offers (0.1345) also counted the steps of quests already accepted, which share the marker type. Only the quests of the game's list of quests not accepted yet now count as offered (`MapOfferedQuests.Tag`, `Offered` mark on markers); a map holding only accepted quests no longer restricts the list. Test QuestOffered1346 (an accepted and an offered quest at the same place).
- For players: Map, Quests tab: quests already accepted no longer change which offered quests are listed.

## 0.1345.0
- Plan global, X-355 : Carte, onglet Quêtes, suite (rapport de l'utilisateur du 14/11 au Gold Saucer, avec captures : « Toutes les cartes en main », niveau 1, listée dans l'onglet mais absente de la carte, « la carte a raison ») : une quête peut demander plus que ses quêtes préalables (conditions que Tensho ne lit pas). Interrupteur « Quêtes pas encore disponibles » coupé, là où la liste des quêtes que le jeu propose couvre la carte affichée (ses marqueurs de quêtes pas encore acceptées, ceux que dessine la carte), une quête pas commencée n'est plus listée que si le jeu la propose sur cette carte ; une quête en cours reste ; sans aucune proposition du jeu sur la carte (carte pas couverte, ou rien de proposé : on ne les distingue pas), la règle des quêtes préalables reste seule (R06). Nouveau cas dans QuestLockedMap1343.
- Pour les joueurs : L'onglet Quêtes de la carte, « Quêtes pas encore disponibles » décoché, ne liste plus que les quêtes que le jeu propose sur cette carte quand il les connaît.
- Tests : QuestLockedMap1343Tests. Verify.ps1 (agent) : 2521/2521, 0 avertissement, 0 erreur ; DLL 0.1345.0.0. Non validé en jeu.
### English
- Map, Quests tab, follow-up (user report of 11/14 at the Gold Saucer, with captures: « All cards in hand », level 1, listed in the tab but absent from the map, « the map is right »): a quest can ask more than its previous quests (conditions Tensho does not read). With the « Quests not available yet » box off, where the list of the quests the game offers covers the shown map (its markers of quests not accepted yet, those the map draws), a quest not started is now listed only when the game offers it on that map; a quest under way stays; with no offer of the game on the map (map not covered, or nothing offered: not told apart), the previous-quest rule alone stays (R06). New case in QuestLockedMap1343.
- For players: The map's Quests tab, with « Quests not available yet » off, now lists only the quests the game offers on that map when it knows them.
