# Community discoveries

*La traduction française est en bas de ce fichier.*

`discoveries.json` holds the discoveries players shared through World › Discoveries › « Exchange with other players (GitHub) »: each connected player's
own sightings go to a secret gist of theirs, with one « Discoveries: <login> » issue in this repository pointing to it. Tensho downloads this file during
an exchange (automatic once a day when turned on, or « Exchange now ») and joins it to the base as imported. A discovery holds game rows and places only:
never a player's name, position or id.

The maintainer merges the gists with `python tools/discoveries-merge.py community-discoveries/discoveries.json gist:<id> …` (facts that do not hold are
refused; each gist counts as one import of a fact), then commits the result. The file reaches players with the next release (it is read from `main`).

---

## Français

`discoveries.json` contient les découvertes partagées par les joueurs avec Monde › Découvertes › « Échange avec les autres joueurs (GitHub) » : ce que
chaque joueur connecté a vu lui-même part dans un gist secret à son nom, avec un ticket « Discoveries: <login> » dans ce dépôt qui y renvoie. Tensho
télécharge ce fichier lors d'un échange (automatique une fois par jour quand il est activé, ou « Échanger maintenant ») et le joint à la base comme importé.
Une découverte ne contient que des lignes et des lieux du jeu : jamais le nom, la position ni l'identifiant d'un joueur.

Le mainteneur fusionne les gists avec `python tools/discoveries-merge.py community-discoveries/discoveries.json gist:<id> …` (les faits invalides sont
refusés ; chaque gist compte comme un import d'un fait), puis valide le résultat. Le fichier arrive chez les joueurs avec la release suivante (lu sur `main`).
