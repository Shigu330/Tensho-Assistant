# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

## 0.1350.0
- Plan global, X-363 : Correction de la ligne « Ajouté au patch 7.x (Garland Tools) » (essai T-0.1317-1 du 14/11) : elle ne lisait que ce que Garland avait déjà rendu pour les sources, et Garland n'est demandé pour les sources que si les données du jeu n'en connaissent aucune ; un objet vendu par un PNJ (49731, Bague d'attaquant de grand champion, patch 7.4 chez Garland) n'avait donc jamais la ligne, ni dans l'en-tête de sa fiche ni dans son infobulle. La fiche demande maintenant le document Garland de son objet (une requête, cache partagé) ; l'infobulle le demande quand la souris reste 0,6 s sur le même objet (HoverDwell), jamais pour chaque ligne balayée ; rien quand Garland ou son usage « sources des objets » est coupé. Preuve complétée : docs/evidence/0.1227-garland-patch.md.
- Pour les joueurs : La ligne « Ajouté au patch » s'affiche de nouveau pour les objets vendus par des PNJ, dans la fiche de l'objet et dans son infobulle (souris gardée sur l'objet).
- Tests : ItemPatchAsk1350Tests. Verify.ps1 (agent) : 2526/2526, 0 avertissement, 0 erreur ; DLL 0.1350.0.0. Non validé en jeu.
### English
- Fix of the « Added in patch 7.x (Garland Tools) » line (test T-0.1317-1 of 14/11): it only read what Garland had already given for the sources, and Garland is asked for the sources only when the game data knows none; an item sold by an NPC (49731, Grand Champion's Ring of Slaying, patch 7.4 on Garland) therefore never had the line, neither in its sheet header nor in its tooltip. The sheet now asks for its item's Garland document (one request, shared cache); the tooltip asks for it once the mouse rests 0.6 s on the same item (HoverDwell), never for every row swept; nothing while Garland or its « item sources » use is off. Evidence extended: docs/evidence/0.1227-garland-patch.md.
- For players: The « Added in patch » line shows again for items sold by NPCs, in the item sheet and in the item tooltip (hold the mouse on the item).

## 0.1349.0
- Plan global, X-357 : Ancien fichier commun des découvertes retiré (version 2, `community-discoveries/discoveries.json`) : le plugin ne lit plus que l'ensemble v3 (`community-discoveries/v3/`) ; sans lui, rien n'est pris, l'échange dit « illisible » et la base locale n'est pas touchée (test de comportement) ; l'outil de fusion n'écrit et ne compare plus que l'ensemble v3 (mêmes 3007 faits que l'ancien fichier, vérifié avant le retrait), client-snapshot.py et discoveries-load.py le lisent ; message « fichier commun pas encore publié » retiré ; README des découvertes, SECURITY, ARCHITECTURE, tools/README et DEC-052 à jour. Hors version (outils de l'agent, X-361) : retour.py accepte --attendre, et optcheck.py vérifie les options de chaque script par la vraie ligne de commande.
- Tests : ExchangeV31137Tests. Verify.ps1 (agent) : 2523/2523, 0 avertissement, 0 erreur ; DLL 0.1349.0.0. Non validé en jeu.
### English
- Old common discoveries file removed (version 2, `community-discoveries/discoveries.json`): the plugin reads the v3 set only (`community-discoveries/v3/`); without it, nothing is taken, the exchange says « unreadable » and the local base is untouched (behaviour test); the merge tool writes and compares the v3 set only (the same 3007 facts as the old file, checked before the removal), client-snapshot.py and discoveries-load.py read it; « common file not published yet » message removed; discoveries README, SECURITY, ARCHITECTURE, tools/README and DEC-052 updated.

## 0.1348.0
- Plan global, X-357 : Tri des documents accepté par l'utilisateur : 22 documents de travail périmés retirés (propositions de l'audit du 2026-11-02 et leur tri, étude des outils de contenus, archives, audits du nettoyage X-76 et du réseau de la 0.948, 7 relevés ponctuels de docs/evidence) ; les audits 0.857 et 0.874, cités par le code, rejoignent docs/evidence/ ; chaque lien vers un document retiré devient un lien permanent vers sa dernière version (commit 024c426c), plan compris ; carte des documents mise à jour.
- Tests : AuditsFolder1086Tests. Verify.ps1 (agent) : 2523/2523, 0 avertissement, 0 erreur ; DLL 0.1348.0.0. Rien à essayer en jeu.
### English
- Document sort accepted by the user: 22 outdated working documents removed (the proposals of the 2026-11-02 audit and their triage, the content tools study, archives, the X-76 cleanup and the 0.948 network audits, 7 one-off readings of docs/evidence); audits 0.857 and 0.874, cited by the code, join docs/evidence/; every link to a removed document becomes a permanent link to its last version (commit 024c426c), the plan included; map of the documents updated.
