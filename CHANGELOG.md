# Journal des versions / Changelog

Résumé par version, en français puis `### English` (partie anglaise obligatoire depuis 0.219.0) ; la partie anglaise sert aussi de corps
aux commits et aux notes de release sur GitHub. Preuves citées : docs/evidence/.
Chaque version a été compilée et testée par `scripts/Verify.ps1` ; la validation en jeu est indiquée quand l'utilisateur l'a rapportée.
Ce fichier garde la dernière release et les versions qui suivent ; les plus anciennes sont dans [docs/changelog/](docs/changelog/)
(déplacées par `tools/changelog.py archive`, un fichier par centaine de versions).

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
