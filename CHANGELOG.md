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

## 0.1564.0
- Plan global, X-624 : Relecture de 0.1562, constats mineurs : M2, deux clients du jeu sur un même profil : le second, qui trouve les lignes mises de côté déjà réglées par le premier, reçoit quand même dans sa mémoire la copie relue sur le gist (sans réécrire la base) quand le premier l'a gardée ; avant, son envoi suivant remplaçait le gist sans ce fait ; M3, une copie relue pendant l'écriture d'une sauvegarde qui échoue ensuite n'est plus ajoutée une deuxième fois par la sauvegarde suivante (la base d'avant reprend la copie que le fichier a reçue) ; M1 fait en 0.1563 ; M4 (lecture de gist.githubusercontent.com sous le consentement « Connecter GitHub ») laissé à la décision D-190.
- Pour les joueurs : découvertes : avec deux clients du jeu sur un même PC, vos découvertes mises de côté ne disparaissent plus de votre copie GitHub, ni ne sont comptées deux fois après une sauvegarde échouée.
- Tests : AsideToken1563Tests. Verify.ps1 (agent) : 3177/3177, 0 avertissement, 0 erreur ; DLL 0.1564.0.0. Rien à essayer en jeu.
### English
- Review of 0.1562, minor findings: M2, two game clients on one profile: the second, which finds the rows set aside already settled by the first, still gets in its memory the copy read back from the gist (the database not written again) when the first kept it; before, its next send replaced the gist without that fact; M3, a copy read back while a save is being written, that save then failing, is no longer added a second time by the next save (the base of before takes the copy the file received); M1 done in 0.1563; M4 (reading gist.githubusercontent.com under the « Connect GitHub » consent) left to decision D-190.
- For players: Discoveries: with two game clients on one PC, your discoveries kept aside are no longer lost from your GitHub copy, nor counted twice after a failed save.

## 0.1563.0
- Plan global, X-623 : Relecture de 0.1562, I1 : les découvertes à vous mises de côté (lignes abîmées) sont relues sur votre gist avec le jeton renouvelé d'abord, comme l'envoi le fait (avant, un jeton de l'application GitHub échu après 8 h recevait un refus 401 pris pour « GitHub injoignable » : l'envoi restait bloqué à chaque échange) ; un 401 avec un jeton de renouvellement en main renouvelle une fois et relit ; un 401 sans lui, ou un renouvellement refusé, finit comme l'envoi (« reconnectez-vous »), rien n'est envoyé ni perdu. X-624 M1 : seul un 404 de la liste du gist veut dire « gist disparu » ; une lecture refusée de l'adresse brute d'un fichier coupé (404, 401, 5xx) compte comme « injoignable », les lignes restent en attente (avant, elles étaient réglées sans rien garder et l'envoi suivant retirait leurs copies du gist), et ne déconnecte jamais.
- Pour les joueurs : partage GitHub : après quelques heures sans échange, vos découvertes mises de côté sont de nouveau relues au lieu d'attendre sans fin avec « GitHub injoignable ».
- Tests : AsideToken1563Tests. Verify.ps1 (agent) : 3174/3174, 0 avertissement, 0 erreur ; DLL 0.1563.0.0. Rien à essayer en jeu.
### English
- Review of 0.1562, I1: your discoveries set aside (damaged rows) are read back from your gist with the token renewed first, as the send does (before, a GitHub App token past its 8 h got a 401 taken for « GitHub not reached »: the send stayed blocked at every exchange); a 401 with a refresh token in hand renews once and reads again; a 401 without one, or a renewal refused, ends as the send does (« connect again »), nothing sent nor lost. X-624 M1: only a 404 of the gist's listing means « gist gone »; a refused read of a cut file's raw address (404, 401, 5xx) counts as « not reached », the rows stay pending (before, they were settled with nothing kept and the next send removed their copies from the gist), and never disconnects.
- For players: GitHub sharing: after a few hours without exchanging, your discoveries kept aside are read back again instead of waiting forever with « GitHub not reached ».

## 0.1562.0
- Plan global, X-622 : Relecture de 0.1560, constats mineurs du Dédale : M1, l'effet d'une action ennemie n'est dit « inflige : » que pour un effet néfaste (colonne 6 de Status = 2 : Poison, Glas…) ; un effet favorable (Bonus de dégâts, Couvert, Pointes de glace : colonne 6 = 1) est dit « confère : », un effet de catégorie inconnue « effet : » (avant, « inflige : Bonus de dégâts » disait le contraire de ce qui se passe, R06 ; preuve docs/evidence/0.1562-beast-effect-category.md) ; M2, pour les 10 bêtes d'une équipe, l'ennemi le plus fort d'un combat compté est le plus fort de ceux qui ont une faiblesse : un ennemi le plus fort sans faiblesse ne fait plus passer la faiblesse du deuxième plus fort après celle de deux ennemis faibles du même combat quand les places manquent (comme le fait déjà l'ordre d'appel du combat) ; règles des plans en version 6 (plans gardés refaits).
- Pour les joueurs : dédale bestial : un bonus ou une barrière que l'ennemi se donne n'est plus dit infligé à vos bêtes ; l'équipe de 10 garde mieux la faiblesse de l'ennemi le plus fort d'un combat qui en a une.
- Tests : BeastReview1562Tests. Verify.ps1 (agent) : 3168/3168, 0 avertissement, 0 erreur ; DLL 0.1562.0.0. Non validé en jeu.
### English
- Review of 0.1560, minor findings of the beast labyrinth: M1, an enemy action's effect is said « inflicts: » only for a harmful status (Status column 6 = 2: Poison, Doom…); a helpful one (a damage bonus, Cover, ice spikes: column 6 = 1) is said « grants: », one of an unknown category « effect: » (before, « inflicts: Damage Up » said the opposite of what happens, R06; evidence docs/evidence/0.1562-beast-effect-category.md); M2, for the 10 beasts of a team, a fight's strongest enemy counted is the strongest of those that have a weakness: a strongest enemy without one no longer lets the second strongest's weakness go after that of two weak enemies of the same fight when places lack (as the fight's calling order already does); plan rules at version 6 (plans kept made again).
- For players: Beast labyrinth: an enemy's bonus or barrier is no longer said to be inflicted on your beasts; the team of 10 better keeps the weakness of a fight's strongest enemy that has one.
