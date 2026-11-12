# Community discoveries

*La traduction française est en bas de ce fichier.*

`v3/` (since Tensho 0.1137) and `discoveries.json` hold the discoveries players shared through World › Discoveries › « Exchange with other players
(GitHub) »: each connected player's own sightings go to a secret gist of theirs, with one « Discoveries: <login> » issue in this repository pointing to
it. Tensho downloads them during an exchange (automatic once a day when turned on, or « Exchange now ») and joins them to the base as imported: the
files of `v3/` whose hash changed since the last exchange, or `discoveries.json` while `v3/` is missing. A discovery holds game rows and places only:
never a player's name or id, nor anything about other players; what leaves the PC carries the day of each sighting, never its time, and the files of
this folder keep the day only too (since Tensho 0.1012). A fish caught or a bite is noted at the character's exact place (the precision chosen in
DEC-045, useful for other players' maps): with its day and the « Discoveries: <login> » issue, it can tell where that GitHub account fished on a given
day. Only the fields a player read themself are sent: dates, levels and places that came from an import are kept apart and never sent (since Tensho
0.1134).

**Licence.** The shared discoveries (`v3/`, `discoveries.json` and `players/`) are published under CC0-1.0 (public domain dedication,
https://creativecommons.org/publicdomain/zero/1.0/): anyone may reuse them for any purpose without asking. A player agrees to it when connecting
GitHub in the plugin (the warning says so) and the issue of each player states it.

The maintainer merges the gists with `python tools/discoveries-merge.py sync` (GitHub CLI needed; `--dry-run` reports only): each player's valid facts kept
in `players/<issue>.json`, facts that do not hold refused, a player whose refusals pass 5 % flagged and left out, then `discoveries.json` rebuilt from the
player files with `rebuild` (each player counts as one import of a fact), with the `v3/` set beside it; then the result is committed.
The files reach players with the next release (they are read from `main`).

**File format (version 2, since Tensho 0.916).** `{"Version":2,"Producer":…,"Written":…,"Facts":[…]}`. A fact is identified by `Kind`, `Subject`, `Detail`,
`Territory`, `Map`, `CellX`, `CellZ`; it holds the exact first place (`X`, `Z`), the widest values seen (`LevelMin`, `LevelMax`), `Seen`, `Imported`,
`First`, `Last`. Version 2 adds two optional fields, left out when empty: `Hq`, how many sightings were HQ (an item got), and `Values`, how many times
each value was seen (`{"value":count}`, at most 32 values; past 32, a new value is counted under the nearest value kept). In `discoveries.json` both are
the sums over the players. Version 1 files (without them) are still read.

**Exchange format v3 (since Tensho 0.1137, DEC-052).** `v3/index.json` lists one `kind-NN.jsonl.gz` per kind present:
`{"Format":"tensho-discoveries","Version":3,"Written":…,"Files":[{"Name":"kind-NN.jsonl.gz","Kind":N,"Count":n,"Bytes":b,"Sha256":"HEX"}]}`, `Bytes` and
`Sha256` being those of the compressed file. A kind file is gzip of UTF-8 lines: line 1 a header
`{"Format":"tensho-discoveries","Version":3,"Kind":N,"Count":n,"Producer":…,"Written":…}`, then one fact per line, with the fields of version 2 above
(optional ones left out when empty, nothing else). Since Tensho 0.1170 the plugin ignores a name it does not know when it reads `v3/` (a field
added later needs no version 4), but refuses a line that holds `Own` or `Key` (a player's own part); the merge tool stays strict on the players' gists
(any other name refuses the line). Readers check the size and the SHA-256 against the index before use and read the lines as a stream
within bounds (32 MB compressed, 256 MB decompressed, 64 KB per line, 200 000 facts per kind); a bad line is refused and counted, a file whose refusals
pass 5 % is refused. A player's gist holds the same set, `index.json` and each kind file in base64 (`kind-NN.jsonl.gz.b64`). The tool writes the same
facts as the same bytes. Version 2 (`discoveries.json`, and the gist's single `tensho-discoveries.b64`) is still written and read for a while, for
plugins before 0.1137, then dropped.

---

# Découvertes de la communauté

`v3/` (depuis Tensho 0.1137) et `discoveries.json` contiennent les découvertes partagées par les joueurs avec Monde › Découvertes › « Échange avec les
autres joueurs (GitHub) » : ce que chaque joueur connecté a vu lui-même part dans un gist secret à son nom, avec un ticket « Discoveries: <login> » dans
ce dépôt qui y renvoie. Tensho les télécharge lors d'un échange (automatique une fois par jour quand il est activé, ou « Échanger maintenant ») et les
joint à la base comme importés : les fichiers de `v3/` dont l'empreinte a changé depuis le dernier échange, ou `discoveries.json` tant que `v3/` manque.
Une découverte ne contient que des lignes et des lieux du jeu : jamais le nom ni l'identifiant d'un joueur, ni rien sur les autres joueurs ; ce qui quitte
le PC porte le jour de chaque observation, jamais son heure, et les fichiers de ce dossier ne gardent eux aussi que le jour (depuis Tensho 0.1012). Un
poisson pris ou une touche est noté à l'endroit exact du personnage (la précision choisie par DEC-045, utile aux cartes des autres joueurs) : avec son
jour et le ticket « Discoveries: <login> », il peut dire où ce compte GitHub a pêché un jour donné. Seuls les champs relevés par le joueur lui-même
partent : les dates, niveaux et lieux venus d'un import sont gardés à part et ne partent jamais (depuis Tensho 0.1134).

**Licence.** Les découvertes partagées (`v3/`, `discoveries.json` et `players/`) sont publiées sous CC0-1.0 (renonciation au droit d'auteur,
https://creativecommons.org/publicdomain/zero/1.0/deed.fr) : chacun peut les reprendre pour tout usage sans demander. Un joueur l'accepte en connectant
GitHub dans le plugin (l'avertissement le dit) et le ticket de chaque joueur l'indique.

Le mainteneur fusionne les gists avec `python tools/discoveries-merge.py sync` (GitHub CLI nécessaire ; `--dry-run` fait seulement le rapport) : les faits
valides de chaque joueur gardés dans `players/<ticket>.json`, les faits invalides refusés, un joueur dont les refus dépassent 5 % signalé et laissé de côté,
puis `discoveries.json` reconstruit à partir des fichiers des joueurs avec `rebuild` (chaque joueur compte comme un import d'un fait), avec l'ensemble
`v3/` à côté ; puis le résultat est enregistré dans le dépôt. Les fichiers arrivent chez les joueurs avec la release suivante (lus sur `main`).

**Format du fichier (version 2, depuis Tensho 0.916).** `{"Version":2,"Producer":…,"Written":…,"Facts":[…]}`. Un fait est identifié par `Kind`, `Subject`,
`Detail`, `Territory`, `Map`, `CellX`, `CellZ` ; il contient le premier lieu exact (`X`, `Z`), les valeurs extrêmes vues (`LevelMin`, `LevelMax`), `Seen`,
`Imported`, `First`, `Last`. La version 2 ajoute deux champs facultatifs, omis quand ils sont vides : `Hq`, le nombre d'observations en HQ (un objet
obtenu), et `Values`, le nombre de fois où chaque valeur a été vue (`{"valeur":nombre}`, 32 valeurs au plus ; au-delà, une nouvelle valeur est comptée
avec la valeur gardée la plus proche). Dans `discoveries.json`, ce sont les sommes sur les joueurs. Les fichiers de version 1 (sans eux) restent lus.

**Format d'échange v3 (depuis Tensho 0.1137, DEC-052).** `v3/index.json` liste un `kind-NN.jsonl.gz` par genre présent :
`{"Format":"tensho-discoveries","Version":3,"Written":…,"Files":[{"Name":"kind-NN.jsonl.gz","Kind":N,"Count":n,"Bytes":b,"Sha256":"HEX"}]}`, `Bytes` et
`Sha256` étant ceux du fichier compressé. Un fichier de genre est un gzip de lignes UTF-8 : ligne 1 un en-tête
`{"Format":"tensho-discoveries","Version":3,"Kind":N,"Count":n,"Producer":…,"Written":…}`, puis un fait par ligne, avec les champs de la version 2
ci-dessus (les facultatifs omis quand ils sont vides, rien d'autre). Depuis Tensho 0.1170, le plugin ignore un nom qu'il ne connaît pas quand il
lit `v3/` (un champ ajouté plus tard ne demande pas de version 4), mais refuse une ligne qui contient `Own` ou `Key` (la part propre d'un joueur) ;
l'outil de fusion reste strict sur les gists des joueurs (tout autre nom fait refuser la ligne). Les lecteurs vérifient la taille et le SHA-256 contre l'index avant tout usage et
lisent les lignes en flux, dans des bornes (32 Mo compressés, 256 Mo décompressés, 64 Ko par ligne, 200 000 faits par genre) ; une ligne fausse est
refusée et comptée, un fichier dont les refus dépassent 5 % est refusé. Le gist d'un joueur contient le même ensemble, `index.json` et chaque fichier de
genre en base64 (`kind-NN.jsonl.gz.b64`). L'outil écrit les mêmes faits avec les mêmes octets. La version 2 (`discoveries.json`, et le fichier unique
`tensho-discoveries.b64` du gist) est encore écrite et lue un temps, pour les plugins d'avant 0.1137, puis sera retirée.
