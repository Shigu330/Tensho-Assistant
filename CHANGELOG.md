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

## 0.1557.0
- Plan global, X-618 : Relecture du bloc 0.1551 à 0.1554, constats mineurs : M1, le fichier des plans du Dédale abîmé n'est plus dit « mis de côté » avant d'avoir été déplacé (un déplacement qui échouait était annoncé, et de nouveau toutes les 10 s) ; M2, MarketSeen.json tenu par un autre programme au démarrage est relu, puis laissé en place (la sauvegarde suivante le joint), au lieu d'être mis de côté comme abîmé (SeenPriceStore.ReadAtStart) ; M3, une copie expirée des temps de touche de Teamcraft est redemandée après le délai de reprise, la copie restant affichée pendant ce temps ; M4, une copie GatherBuddy fraîche sans poisson (écrite avant 0.1554) est retéléchargée, et une copie expirée sans poisson n'en est plus une ; M5, une ligne illisible de vos découvertes marque vos découvertes incomplètes : rien n'est envoyé à votre gist dans la session (DiscoveryStart.Incomplete).
- Pour les joueurs : pêche : les données de GatherBuddy sont retéléchargées quand la copie gardée est vide ; les prix vus au marché ne sont plus mis de côté quand un autre client du jeu tient leur fichier.
- Tests : AfterError1557Tests. Verify.ps1 (agent) : 3146/3146, 0 avertissement, 0 erreur ; DLL 0.1557.0.0. Rien à essayer en jeu.
### English
- Review of block 0.1551 to 0.1554, minor findings: M1, the labyrinth plans' damaged file is no longer said « set aside » before it is moved (a move that failed was said done, again every 10 s); M2, MarketSeen.json held by another program at the start is read again, then left where it is (the next save joins it), instead of being set aside as damaged (SeenPriceStore.ReadAtStart); M3, an expired copy of Teamcraft's bite times is asked again after the retry delay, the copy staying on show meanwhile; M4, a fresh GatherBuddy copy without a fish (written before 0.1554) is downloaded again, and an expired copy without a fish no longer counts as one; M5, a row of your discoveries that cannot be read marks your discoveries incomplete: nothing is sent to your gist in the session (DiscoveryStart.Incomplete).
- For players: Fishing: GatherBuddy's data downloaded again when the copy kept is empty; market prices seen no longer set aside when another game client holds their file.

## 0.1556.0
- Plan global, X-589 : Sources réseau, suite et fin de l'audit externe du 18/11 : la file des fiches d'objet du wiki change son état « en cours » sous le même verrou que la file (A10 : une fiche demandée juste quand la file se vide n'est plus laissée en chargement) ; PaissaDB ne montre plus les parcelles d'un monde sous un autre pendant le chargement ou après un échec (A11) ; l'alerte immobilière relit la coupure JcJ au moment d'écrire dans le chat (A02, PaissaDb.Alert) ; le lot de CSV de LuminaSupplemental et le second essai du guide du wiki relisent l'interrupteur avant chaque requête (A07) ; la confidentialité de PaissaDB est vérifiée sur un transport HTTP simulé (A24 : une seule requête GET par monde, l'identifiant du monde seul, sans corps).
- Pour les joueurs : parcelles à vendre : celles d'un autre monde ne s'affichent plus pendant le chargement du monde actuel ; aucune alerte en contenu classé.
- Tests : NetworkEnds1556Tests. Verify.ps1 (agent) : 3141/3141, 0 avertissement, 0 erreur ; DLL 0.1556.0.0. Rien à essayer en jeu.
### English
- Network sources, rest and end of the external audit of 18/11: the wiki's item queue changes its « running » state under the same lock as the queue (A10: an item asked just as the queue empties is no longer left loading); PaissaDB no longer shows a world's plots under another while loading or after a failure (A11); the housing alert reads the PvP cut again when it writes in the chat (A02, PaissaDb.Alert); LuminaSupplemental's batch of CSV files and the wiki guide's second try read the switch again before each request (A07); PaissaDB's privacy is checked on a simulated HTTP transport (A24: one GET per world, the world's id only, no body).
- For players: Housing plots: another world's plots are no longer shown while the current world loads; no plot alert in ranked content.

## 0.1555.0
- Plan global, X-617 : Dédale bestial : ce qui est lu pendant que BeastTeams.json est tenu par un autre programme (rangs, résultat d'un plateau, pages du bestiaire, plans calculés) n'est plus perdu à la relecture du fichier : BeastTeams.Join le joint au fichier (rang jamais revu à la baisse, rang exact avant un plancher, résultat le plus récent, page du bestiaire du rang le plus haut, plans calculés entre-temps), puis l'écrit (BeastPlansFile.Load). Avant, le fichier relu remplaçait la mémoire sans un mot. Relecture du bloc 0.1551 à 0.1554, constat I1.
- Pour les joueurs : dédale bestial : les rangs et le résultat d'un plateau lus pendant que le fichier des plans était tenu par un autre programme ne sont plus perdus.
- Tests : BeastHeld1555Tests. Verify.ps1 (agent) : 3136/3136, 0 avertissement, 0 erreur ; DLL 0.1555.0.0. Rien à essayer en jeu.
### English
- Beastmaster's labyrinth: what is read while BeastTeams.json is held by another program (ranks, a board's result, bestiary pages, plans made) is no longer lost when the file is read again: BeastTeams.Join joins it to the file (a rank never lowered, an exact rank over a floor, the most recent result, the bestiary page of the higher rank, the plans made meanwhile), then writes it (BeastPlansFile.Load). Before, the file read again replaced the memory without a word. Review of block 0.1551 to 0.1554, finding I1.
- For players: Beastmaster's labyrinth: ranks and a board's result read while the plans' file was held by another program are no longer lost.

## 0.1554.0
- Plan global, X-589 : Audit externe du 18/11, famille « sources réseau », première partie (X-589) : chaque source finit dans un état terminal (données, copie expirée ou erreur retentée). A08 : LuminaSupplemental, une requête au-delà de ses 30 s sans copie finit en erreur retentée après 5 min (avant : « chargement » toute la session, jamais retenté). A09 : temps de touche de Teamcraft, une erreur est retentée après 5 min et la copie expirée est prise d'abord (avant : erreur gardée sans échéance). A12 : Universalis (comparaison des servants), une heure d'envoi qui n'est pas un nombre ou hors calendrier est inconnue, et toute autre erreur de la réponse termine la requête (avant : « chargement » gardé). A26 : GatherBuddy, le texte est vérifié avant d'être gardé : un index sans fichier ou sans poisson est un échec, jamais une copie vide gardée fraîche. Reste pour la partie suivante : A10, A11, A02, A07, A24.
- Pour les joueurs : les sources en ligne (sources d'objets, temps de touche des poissons, comparaison des prix des servants, poissons de GatherBuddy) ne restent plus « en chargement » après une erreur de réseau ou de données : elles disent l'erreur et réessaient plus tard.
- Livré : X-589 (critères restant à valider).
- Tests : NetworkEnds1554Tests. Verify.ps1 (agent) : 3133/3133, 0 avertissement, 0 erreur ; DLL 0.1554.0.0. Rien à essayer en jeu.
### English
- External audit of 18/11, family « network sources », first part (X-589): each source ends in a terminal state (data, an expired copy, or an error tried again). A08: LuminaSupplemental, a request past its 30 s without a copy ends in an error tried again after 5 min (before: « loading » for the session, never tried again). A09: Teamcraft's bite times, an error is tried again after 5 min and the expired copy is taken first (before: error kept with no deadline). A12: Universalis (retainer comparison), an upload time that is not a number or past the calendar is unknown, and any other error of the answer ends the request (before: « loading » kept). A26: GatherBuddy, the text is checked before it is kept: an index without files or without a fish is a failure, never an empty copy kept fresh. Left for the next part: A10, A11, A02, A07, A24.
- For players: Online sources (item sources, fish bite times, retainer price comparison, GatherBuddy fish) no longer stay « loading » after a network or data error: they show the error and try again later.
- Delivered: X-589 (criteria left to validate).
