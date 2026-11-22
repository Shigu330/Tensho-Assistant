# Community discoveries

*La traduction française est en bas de ce fichier.*

`v3/` (since Tensho 0.1137) holds the discoveries players shared through World › Discoveries › « Exchange with other players
(GitHub) »: each connected player's own sightings go to a secret gist of theirs, with one « Discoveries: <login> » issue in this repository pointing to
it. Tensho downloads them during an exchange (automatic once a day when turned on, or « Exchange now ») and joins them to the base as imported: the
files of `v3/` whose hash changed since the last exchange (the single `discoveries.json` of before is gone since Tensho 0.1349). A discovery holds game rows and places only:
never a player's name or id, nor anything about other players; what leaves the PC carries the day of each sighting, never its time, and the files of
this folder keep the day only too (since Tensho 0.1012). A fish caught or a bite is noted at the character's exact place (the precision chosen in
DEC-045, useful for other players' maps): with its day and the « Discoveries: <login> » issue, it can tell where that GitHub account fished on a given
day. Only the fields a player read themself are sent: dates, levels and places that came from an import are kept apart and never sent (since Tensho
0.1134).

**Licence.** The shared discoveries (`v3/` and `players/`) are published under CC0-1.0 (public domain dedication,
https://creativecommons.org/publicdomain/zero/1.0/): anyone may reuse them for any purpose without asking. A player agrees to it when connecting
GitHub in the plugin (the warning says so) and the issue of each player states it.

The maintainer merges the gists with `python tools/discoveries-merge.py sync` (GitHub CLI needed; `--dry-run` reports only): each player's valid facts kept
in `players/<issue>.jsonl.gz`, facts that do not hold refused, a player whose refusals pass 5 % flagged and left out, then the `v3/` set rebuilt from the
player files with `rebuild` (each player counts as one import of a fact); then the result is committed (since Tensho 0.1823, the share of an account on
the maintainer's own list of trusted accounts, kept outside the repository, is committed without waiting when every check of `tools/discoveries-trust.py`
passes: since Tensho 0.1824, among them a pull request from a branch of this repository opened by the worker's account, never a fork, whose
`players/` and `v3/` files are exactly what a sync of that share alone writes; any other share, or any doubt, waits for the maintainer). Since Tensho 0.1803, `rebuild` (and `sync`,
`withdraw`) stops before writing anything when `v3/` is missing or cut short (no `index.json`, an index listing fewer files than the folder holds,
a file that does not read whole, or a fact a player holds that the committed `v3/` has but the one read now lacks): the places and days live there
only, so `v3/` is restored from git first. The places and days a player left out brings (flagged, or an older issue of the same account) never count.
Since Tensho 0.1821, a player flagged after a rebuild counted them (the header of their player file says `"Counted":true` or `false`, as the last
rebuild left it) no longer leaves their place in `v3/` either: the place `v3/` holds for their facts falls back to the middle of its 50-yalm cell,
unless a counted player's gist read in the same run gives it (the fact and its days stay). A player never counted brought no place; an older
issue replaced by a newer one of the same account is the same player, whose place stays.
**Player files (since Tensho 0.1801).** A file of `players/` keeps no exact place (`X`, `Z`) nor day (`First`, `Last`): beside the issue, they told
where that GitHub account was on that day. It keeps what the merge needs (the fact's identity, its levels, its HQ sightings and values), compressed:
`<issue>.jsonl.gz`, gzip of UTF-8 lines, a header `{"Format":"tensho-discoveries-player","Version":1,"Issue":n,"Login":…,"Count":n}` then one fact
per line. `v3/` keeps, without any name, each fact's earliest exact place and its days (from `v3/` as it is, the gists read by `sync`, and the
`players/<issue>.json` files of before, which `rebuild` writes again in the new format). The gist an issue links holds the player's own sightings
with their day and place, as the player shared them, until they are merged: since Tensho 0.1817 the header of `players/<issue>.jsonl.gz` notes the gist
and its revision merged last (`"Gist"`, `"Revision"`); the merge tool never reads that revision again, and the plugin, reading the note from `main`,
deletes that gist whole (GitHub keeps no old revision of a deleted gist, but a copy or fork someone made of it before survives) and makes a new one
with only what is new, the issue edited to link it (its history keeps the old link, now dead). The tool keeps a player's facts by key, so a fact
missing from a newer gist is never taken out (a gist holds news only). A fact found wrong later (a plugin bug, a rule tightened) is taken out
of every player file by a rule of the tool: `rebuild` applies the current rules to each file and writes it again without the facts they refuse;
`withdraw` takes out a whole player.
The files reach players with the next release (they are read from `main`).
Since Tensho 0.1210: one issue per GitHub account counts (the newest; an older one by the same author is left out); each field must have the
plugin's type; a player counts for at most 1,000 sightings of a fact in the common histograms (the shape of theirs kept); the exact place kept is the one
of the earliest day; a player alone far outside the other players' levels on over 5 % of the facts they share is left out and checked by hand.
**Withdrawing.** What you share stays in the common base even if you stop sharing. Close your « Discoveries » issue (or
ask in it): the maintainer removes `players/<issue>.jsonl.gz`, the link with your GitHub account, with `python tools/discoveries-merge.py withdraw <issue>`;
since Tensho 0.1816 your facts stay in `v3/`, held by an anonymous file `players/withdrawn-<random>.jsonl.gz` (no issue, no login) that counts like a
player file (before, the facts no other player had left the common files). Until then, and after: the closed issue keeps its public title
« Discoveries: <login> », and the git history keeps the files as they were, the login in them; if you share again later,
your facts kept this way count once more.

**File format (version 2, since Tensho 0.916).** `{"Version":2,"Producer":…,"Written":…,"Facts":[…]}`. A fact is identified by `Kind`, `Subject`, `Detail`,
`Territory`, `Map`, `CellX`, `CellZ`; it holds the exact first place (`X`, `Z`), the widest values seen (`LevelMin`, `LevelMax`), `Seen`, `Imported`,
`First`, `Last`. Version 2 adds two optional fields, left out when empty: `Hq`, how many sightings were HQ (an item obtained), and `Values`, how many times
each value was seen (`{"value":count}`, at most 32 values; past 32, a new value is counted under the nearest value kept). In `v3/` both are
the sums over the players. Version 1 files (without them) are still read.

**Exchange format v3 (since Tensho 0.1137, DEC-052).** `v3/index.json` lists one `kind-NN.jsonl.gz` per kind present:
`{"Format":"tensho-discoveries","Version":3,"Written":…,"Files":[{"Name":"kind-NN.jsonl.gz","Kind":N,"Count":n,"Bytes":b,"Sha256":"HEX"}]}`, `Bytes` and
`Sha256` being those of the compressed file. A kind file is gzip of UTF-8 lines: line 1 a header
`{"Format":"tensho-discoveries","Version":3,"Kind":N,"Count":n,"Producer":…,"Written":…}`, then one fact per line, with the fields of version 2 above
(optional ones left out when empty, nothing else). Since Tensho 0.1170 the plugin ignores a name it does not know when it reads `v3/` (a field
added later needs no version 4), but refuses a line that holds `Own` or `Key` (a player's own part); the merge tool stays strict on the players' gists
(any other name refuses the line). Readers check the size and the SHA-256 against the index before use and read the lines as a stream
within bounds (32 MB compressed, 256 MB decompressed, 64 KB per line, 2,000,000 facts per kind since Tensho 0.1805; Tensho 0.1804 and older read at
most 200,000 facts per kind and refuse the whole set, every kind, once one kind counts more: the merge tool warns when a kind passes it); a bad line is refused and counted, a file whose refusals
pass 5 % is refused. A player's gist holds the same set, `index.json` and each kind file in base64 (`kind-NN.jsonl.gz.b64`). The tool writes the same
facts as the same bytes. Since Tensho 0.1349 the common facts are in `v3/` only: the version 2 common file `discoveries.json` is no longer written
nor read (the plugin takes nothing without `v3/`); the merge tool still reads a player's older gist holding the single `tensho-discoveries.b64`.

---

# Découvertes de la communauté

`v3/` (depuis Tensho 0.1137) contient les découvertes partagées par les joueurs avec Monde › Découvertes › « Échange avec les
autres joueurs (GitHub) » : ce que chaque joueur connecté a vu lui-même part dans un gist secret à son nom, avec un ticket « Discoveries: <login> » dans
ce dépôt qui y renvoie. Tensho les télécharge lors d'un échange (automatique une fois par jour quand il est activé, ou « Échanger maintenant ») et les
joint à la base comme importées : les fichiers de `v3/` dont l'empreinte a changé depuis le dernier échange (l'ancien fichier unique `discoveries.json` est retiré depuis Tensho 0.1349).
Une découverte ne contient que des lignes et des lieux du jeu : jamais le nom ni l'identifiant d'un joueur, ni rien sur les autres joueurs ; ce qui quitte
le PC porte le jour de chaque observation, jamais son heure, et les fichiers de ce dossier ne gardent eux aussi que le jour (depuis Tensho 0.1012). Un
poisson pris ou une touche est noté à l'endroit exact du personnage (la précision choisie par DEC-045, utile aux cartes des autres joueurs) : avec son
jour et le ticket « Discoveries: <login> », il peut dire où ce compte GitHub a pêché un jour donné. Seuls les champs relevés par le joueur lui-même
partent : les dates, niveaux et lieux venus d'un import sont gardés à part et ne partent jamais (depuis Tensho 0.1134).

**Licence.** Les découvertes partagées (`v3/` et `players/`) sont publiées sous CC0-1.0 (renonciation au droit d'auteur,
https://creativecommons.org/publicdomain/zero/1.0/deed.fr) : chacun peut les reprendre pour tout usage sans demander. Un joueur l'accepte en connectant
GitHub dans le plugin (l'avertissement le dit) et le ticket de chaque joueur l'indique.

Le mainteneur fusionne les gists avec `python tools/discoveries-merge.py sync` (GitHub CLI nécessaire ; `--dry-run` fait seulement le rapport) : les faits
valides de chaque joueur gardés dans `players/<ticket>.jsonl.gz`, les faits invalides refusés, un joueur dont les refus dépassent 5 % signalé et laissé de côté,
puis l'ensemble `v3/` reconstruit à partir des fichiers des joueurs avec `rebuild` (chaque joueur compte comme un import d'un fait) ;
puis le résultat est enregistré dans le dépôt (depuis Tensho 0.1823, le partage d'un compte de la liste de confiance du mainteneur, tenue hors du
dépôt, est enregistré sans attendre quand toutes les vérifications de `tools/discoveries-trust.py` passent : depuis Tensho 0.1824, parmi elles
une demande venue d'une branche de ce dépôt ouverte par le compte du travailleur, jamais d'un fork, dont les fichiers `players/` et `v3/` sont
exactement ce qu'écrit un sync de ce seul partage ; tout autre partage, ou le moindre doute,
attend le mainteneur). Depuis Tensho 0.1803, `rebuild` (et `sync`, `withdraw`) s'arrête avant de rien écrire quand `v3/`
manque ou est tronqué (pas d'`index.json`, un index qui liste moins de fichiers que le dossier n'en a, un fichier qui ne se lit pas en entier, ou un
fait qu'un joueur a, présent dans le `v3/` enregistré mais absent de celui lu) : les lieux et les jours ne sont que là, donc `v3/` se remet d'abord
depuis git. Les lieux et les jours qu'apporte un joueur laissé de côté (signalé, ou un ticket plus ancien du même compte) ne comptent jamais.
Depuis Tensho 0.1821, un joueur signalé après avoir été compté par une reconstruction (l'en-tête de son fichier de joueur dit `"Counted":true` ou
`false`, comme la dernière reconstruction l'a laissé) ne laisse plus non plus son lieu dans `v3/` : le lieu que `v3/` garde pour ses faits retombe au
centre de sa case de 50 yalms, sauf si le gist d'un joueur compté lu dans le même passage le donne (le fait et ses jours restent). Un joueur jamais
compté n'a apporté aucun lieu ; un ticket plus ancien remplacé par un plus récent du même compte est le même joueur, son lieu reste.
Les fichiers arrivent chez les joueurs avec la release suivante (lus sur `main`).
**Fichiers de joueur (depuis Tensho 0.1801).** Un fichier de `players/` ne garde aucun lieu exact (`X`, `Z`) ni aucun jour (`First`, `Last`) : à côté
du ticket, ils disaient où était ce compte GitHub ce jour-là. Il garde ce que la fusion demande (l'identité du fait, ses niveaux, ses observations HQ
et ses valeurs), compressé : `<ticket>.jsonl.gz`, gzip de lignes UTF-8, un en-tête
`{"Format":"tensho-discoveries-player","Version":1,"Issue":n,"Login":…,"Count":n}` puis un fait par ligne. `v3/` garde, sans aucun nom, le premier
lieu exact de chaque fait et ses jours (venus de `v3/` tel qu'il est, des gists lus par `sync` et des fichiers `players/<ticket>.json` d'avant, que
`rebuild` réécrit au nouveau format). Le gist lié par un ticket contient les observations du joueur avec leur jour et leur lieu, telles qu'il les a
partagées, jusqu'à leur fusion : depuis Tensho 0.1817, l'en-tête de `players/<ticket>.jsonl.gz` note le gist et la révision fusionnés en dernier
(`"Gist"`, `"Revision"`) ; l'outil de fusion ne relit jamais cette révision, et le plugin, qui lit cette note sur `main`, supprime ce gist en entier
(GitHub ne garde aucune ancienne révision d'un gist supprimé, mais une copie ou un fork qu'une personne en aurait fait avant reste) et en crée un
nouveau avec seulement les nouveautés, le ticket modifié pour y renvoyer (son historique garde l'ancien lien, devenu mort). L'outil garde les faits
d'un joueur par clé : un fait absent d'un gist plus récent n'est jamais retiré (un gist ne garde que les nouveautés). Un fait reconnu faux plus
tard (bogue du plugin, règle resserrée) sort de chaque fichier de joueur par une règle de l'outil : `rebuild` applique les règles du moment à chaque
fichier et le réécrit sans les faits qu'elles refusent ; `withdraw` retire un joueur entier.
Depuis Tensho 0.1210 : un seul ticket par compte GitHub compte (le plus récent ; un plus ancien du même auteur est laissé de côté) ; chaque champ
doit avoir le type du plugin ; un joueur compte pour 1 000 observations d'un fait au plus dans les histogrammes communs (la forme des siens gardée) ; le
lieu exact gardé est celui du jour le plus ancien ; un joueur seul très loin des niveaux des autres joueurs sur plus de 5 % des faits qu'il partage avec
eux est laissé de côté et regardé à la main.
**Se retirer.** Ce que vous partagez reste dans la base commune même si vous arrêtez de partager. Fermez votre ticket
« Discoveries » (ou demandez-le dedans) : le mainteneur retire `players/<ticket>.jsonl.gz`, le lien avec votre compte GitHub, avec
`python tools/discoveries-merge.py withdraw <ticket>` ; depuis Tensho 0.1816, vos faits restent dans `v3/`, portés par un fichier anonyme
`players/withdrawn-<aléatoire>.jsonl.gz` (sans ticket ni identifiant) qui compte comme un fichier de joueur (avant, les faits qu'aucun autre joueur
n'avait quittaient les fichiers communs). Jusque-là, et après : le ticket fermé garde son titre public « Discoveries: <identifiant> », et
l'historique git garde les fichiers tels qu'ils étaient, avec l'identifiant ; si vous partagez de nouveau plus tard, vos faits
gardés ainsi comptent une fois de plus.

**Format du fichier (version 2, depuis Tensho 0.916).** `{"Version":2,"Producer":…,"Written":…,"Facts":[…]}`. Un fait est identifié par `Kind`, `Subject`,
`Detail`, `Territory`, `Map`, `CellX`, `CellZ` ; il contient le premier lieu exact (`X`, `Z`), les valeurs extrêmes vues (`LevelMin`, `LevelMax`), `Seen`,
`Imported`, `First`, `Last`. La version 2 ajoute deux champs facultatifs, omis quand ils sont vides : `Hq`, le nombre d'observations en HQ (un objet
obtenu), et `Values`, le nombre de fois où chaque valeur a été vue (`{"valeur":nombre}`, 32 valeurs au plus ; au-delà, une nouvelle valeur est comptée
avec la valeur gardée la plus proche). Dans `v3/`, ce sont les sommes sur les joueurs. Les fichiers de version 1 (sans eux) restent lus.

**Format d'échange v3 (depuis Tensho 0.1137, DEC-052).** `v3/index.json` liste un `kind-NN.jsonl.gz` par genre présent :
`{"Format":"tensho-discoveries","Version":3,"Written":…,"Files":[{"Name":"kind-NN.jsonl.gz","Kind":N,"Count":n,"Bytes":b,"Sha256":"HEX"}]}`, `Bytes` et
`Sha256` étant ceux du fichier compressé. Un fichier de genre est un gzip de lignes UTF-8 : ligne 1 un en-tête
`{"Format":"tensho-discoveries","Version":3,"Kind":N,"Count":n,"Producer":…,"Written":…}`, puis un fait par ligne, avec les champs de la version 2
ci-dessus (les facultatifs omis quand ils sont vides, rien d'autre). Depuis Tensho 0.1170, le plugin ignore un nom qu'il ne connaît pas quand il
lit `v3/` (un champ ajouté plus tard ne demande pas de version 4), mais refuse une ligne qui contient `Own` ou `Key` (la part propre d'un joueur) ;
l'outil de fusion reste strict sur les gists des joueurs (tout autre nom fait refuser la ligne). Les lecteurs vérifient la taille et le SHA-256 contre l'index avant tout usage et
lisent les lignes en flux, dans des bornes (32 Mo compressés, 256 Mo décompressés, 64 Ko par ligne, 2 000 000 de faits par genre depuis Tensho 0.1805 ;
Tensho 0.1804 et avant lisent au plus 200 000 faits par genre et refusent tout l'ensemble, tous les genres, dès qu'un genre en compte plus : l'outil de
fusion prévient quand un genre dépasse ce nombre) ; une ligne fausse est
refusée et comptée, un fichier dont les refus dépassent 5 % est refusé. Le gist d'un joueur contient le même ensemble, `index.json` et chaque fichier de
genre en base64 (`kind-NN.jsonl.gz.b64`). L'outil écrit les mêmes faits avec les mêmes octets. Depuis Tensho 0.1349, les faits communs sont dans `v3/`
seulement : le fichier commun de version 2 `discoveries.json` n'est plus écrit ni lu (sans `v3/`, le plugin ne prend rien) ; l'outil de fusion lit encore
l'ancien gist d'un joueur qui contient le fichier unique `tensho-discoveries.b64`.
