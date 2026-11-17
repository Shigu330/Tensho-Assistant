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

## 0.1516.0
- Plan global, X-547 : Relecture de 0.1515, constats mineurs : le commentaire du test d'écriture du journal des versions ne nomme plus un outil interne et dit que la liste des noms qui gardent leur majuscule est contrôlée des deux côtés (la même règle : un nom suivi d'une fin de mot, « Tensho-Assistant » compris). Preuve de 0.1470 (fenêtre de résultat du Dédale bestial) : la section « encore ouvert » donne l'index 11 sur les trois plateaux (361, 780, 912), sans plus contredire le troisième plateau.
- Tests : Writing1358Tests. Verify.ps1 (agent) : 3009/3009, 0 avertissement, 0 erreur ; DLL 0.1516.0.0. Rien à essayer en jeu.
### English
- Review of 0.1515, minor findings: the comment of the changelog's writing test no longer names an internal tool and says that the list of names keeping their capital is checked on both sides (the same rule: a name followed by a word end, « Tensho-Assistant » included). Evidence of 0.1470 (the beast labyrinth's result window): the « still open » section gives index 11 on the three boards (361, 780, 912), no longer contradicting the third board.

## 0.1515.0
- Journal des versions : dans la ligne « Pour les joueurs » de 0.1511, le nom du plugin reprend sa majuscule (« Tensho n'affiche plus ses fenêtres… »). L'outil qui écrit cette ligne mettait en minuscule tout premier mot après les deux-points, un nom propre compris ; il garde maintenant la majuscule d'une liste de noms (Tensho, Dalamud, Discord…), et le test d'écriture du journal accepte la même liste (relecture de 0.1514, M4). Preuve de 0.1470 (fenêtre de résultat du Dédale bestial) : un troisième plateau, du 16/11, confirme la somme des parts du score, le bonus et le total ; le sens de chaque part reste à établir par un relevé avec capture.
- Tests : Writing1358Tests. Verify.ps1 (agent) : 3009/3009, 0 avertissement, 0 erreur ; DLL 0.1515.0.0. Rien à essayer en jeu.
### English
- Changelog: in the « Pour les joueurs » line of 0.1511, the plugin's name gets its capital back (« Tensho n'affiche plus ses fenêtres… »). The tool writing that line lowered any first word after the colon, a proper name too; it now keeps the capital of a list of names (Tensho, Dalamud, Discord…), and the changelog's writing test accepts the same list (review of 0.1514, M4). Evidence of 0.1470 (the beast labyrinth's result window): a third board, of 11/16, confirms the sum of the score parts, the bonus and the total; the meaning of each part is still to be settled by a reading with a capture.

## 0.1514.0
- Plan global, X-535 : Carte : retour en arrière de 0.1513 (X-535), sur le choix de l'utilisateur (17/11 vers 1 h 10 : « Laisser désactivé »). Le réglage « Clic gauche pour poser le drapeau » redevient désactivé par défaut, sous son nom d'avant (FlagOnClick), comme depuis 0.546 à sa demande ; un réglage enregistré avant 0.1513 est relu tel quel. Le code de la carte, l'info-bulle (« Par défaut : désactivé ») et le test de 0.546 sont ceux de 0.1512 ; la classe Maps.MapClickFlag et son test de 0.1513 sont retirés. Le clic pose bien le drapeau une fois le réglage activé ; l'essai T-0.546-1 dit maintenant où l'activer.
- Pour les joueurs : sur la carte Tensho, poser le drapeau d'un clic gauche est de nouveau désactivé par défaut, comme avant : à activer dans les réglages de la carte (« Clic gauche pour poser le drapeau »).
- Tests : MapZoom542Tests. Verify.ps1 (agent) : 3009/3009, 0 avertissement, 0 erreur ; DLL 0.1514.0.0. Rien à essayer en jeu.
### English
- Map: 0.1513 undone (X-535), on the user's choice (17/11 around 1:10: « keep it off »). The setting « Left click places the flag » is off by default again, under its former name (FlagOnClick), as since 0.546 at the user's request; a setting saved before 0.1513 is read back as it was. The map's code, the tooltip (« Default: off ») and the test of 0.546 are those of 0.1512; the class Maps.MapClickFlag and its test of 0.1513 are removed. The click does place the flag once the setting is on; test T-0.546-1 now says where to turn it on.
- For players: On Tensho's map, placing the flag with a left click is off by default again, as before: turn it on in the map settings (« Left click places the flag »).
