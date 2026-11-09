# Community discoveries

*La traduction française est en bas de ce fichier.*

`discoveries.json` holds the discoveries players shared through World › Discoveries › « Exchange with other players (GitHub) »: each connected player's
own sightings go to a secret gist of theirs, with one « Discoveries: <login> » issue in this repository pointing to it. Tensho downloads this file during
an exchange (automatic once a day when turned on, or « Exchange now ») and joins it to the base as imported. A discovery holds game rows and places only:
never a player's name, position or id; what leaves the PC carries the day of each sighting, never its time, and the files of this folder keep the day
only too (since Tensho 0.1012).

**Licence.** The shared discoveries (`discoveries.json` and `players/`) are published under CC0-1.0 (public domain dedication,
https://creativecommons.org/publicdomain/zero/1.0/): anyone may reuse them for any purpose without asking. A player agrees to it when connecting
GitHub in the plugin (the warning says so) and the issue of each player states it.

The maintainer merges the gists with `python tools/discoveries-merge.py sync` (GitHub CLI needed; `--dry-run` reports only): each player's valid facts kept
in `players/<issue>.json`, facts that do not hold refused, a player whose refusals pass 5 % flagged and left out, then `discoveries.json` rebuilt from the
player files with `rebuild` (each player counts as one import of a fact); then the result is committed.
The file reaches players with the next release (it is read from `main`).

**File format (version 2, since Tensho 0.916).** `{"Version":2,"Producer":…,"Written":…,"Facts":[…]}`. A fact is identified by `Kind`, `Subject`, `Detail`,
`Territory`, `Map`, `CellX`, `CellZ`; it holds the exact first place (`X`, `Z`), the widest values seen (`LevelMin`, `LevelMax`), `Seen`, `Imported`,
`First`, `Last`. Version 2 adds two optional fields, left out when empty: `Hq`, how many sightings were HQ (an item got), and `Values`, how many times
each value was seen (`{"value":count}`, at most 32 values; past 32, a new value is counted under the nearest value kept). In `discoveries.json` both are
the sums over the players. Version 1 files (without them) are still read.

---

# Découvertes de la communauté

`discoveries.json` contient les découvertes partagées par les joueurs avec Monde › Découvertes › « Échange avec les autres joueurs (GitHub) » : ce que
chaque joueur connecté a vu lui-même part dans un gist secret à son nom, avec un ticket « Discoveries: <login> » dans ce dépôt qui y renvoie. Tensho
télécharge ce fichier lors d'un échange (automatique une fois par jour quand il est activé, ou « Échanger maintenant ») et le joint à la base comme importé.
Une découverte ne contient que des lignes et des lieux du jeu : jamais le nom, la position ni l'identifiant d'un joueur ; ce qui quitte le PC porte le jour
de chaque observation, jamais son heure, et les fichiers de ce dossier ne gardent eux aussi que le jour (depuis Tensho 0.1012).

**Licence.** Les découvertes partagées (`discoveries.json` et `players/`) sont publiées sous CC0-1.0 (renonciation au droit d'auteur,
https://creativecommons.org/publicdomain/zero/1.0/deed.fr) : chacun peut les reprendre pour tout usage sans demander. Un joueur l'accepte en connectant
GitHub dans le plugin (l'avertissement le dit) et le ticket de chaque joueur l'indique.

Le mainteneur fusionne les gists avec `python tools/discoveries-merge.py sync` (GitHub CLI nécessaire ; `--dry-run` fait seulement le rapport) : les faits
valides de chaque joueur gardés dans `players/<ticket>.json`, les faits invalides refusés, un joueur dont les refus dépassent 5 % signalé et laissé de côté,
puis `discoveries.json` reconstruit à partir des fichiers des joueurs avec `rebuild` (chaque joueur compte comme un import d'un fait) ; puis le résultat est
enregistré dans le dépôt. Le fichier arrive chez les joueurs avec la release suivante (lu sur `main`).

**Format du fichier (version 2, depuis Tensho 0.916).** `{"Version":2,"Producer":…,"Written":…,"Facts":[…]}`. Un fait est identifié par `Kind`, `Subject`,
`Detail`, `Territory`, `Map`, `CellX`, `CellZ` ; il contient le premier lieu exact (`X`, `Z`), les valeurs extrêmes vues (`LevelMin`, `LevelMax`), `Seen`,
`Imported`, `First`, `Last`. La version 2 ajoute deux champs facultatifs, omis quand ils sont vides : `Hq`, le nombre d'observations en HQ (un objet
obtenu), et `Values`, le nombre de fois où chaque valeur a été vue (`{"valeur":nombre}`, 32 valeurs au plus ; au-delà, une nouvelle valeur est comptée
avec la valeur gardée la plus proche). Dans `discoveries.json`, ce sont les sommes sur les joueurs. Les fichiers de version 1 (sans eux) restent lus.
