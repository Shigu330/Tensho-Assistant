# Community discoveries

*La traduction française est en bas de ce fichier.*

`discoveries.json` holds the discoveries players shared (World › Discoveries › « Share mine »). Tensho downloads it only when a player clicks
« Take the common discoveries », and joins it to their base as imported. A discovery holds game rows and places only: never a player's name,
position or id.

How a shared file gets here: the player attaches the zip to a « Discoveries » issue; the maintainer checks it and merges it with
`python tools/discoveries-merge.py community-discoveries/discoveries.json <file.zip or .json>…` (facts that do not hold are refused; each file counts
as one import of a fact), then commits the result. The file reaches players with the next release (it is read from the `main` branch).

---

## Français

`discoveries.json` contient les découvertes partagées par les joueurs (Monde › Découvertes › « Partager les miennes »). Tensho ne le télécharge
que quand un joueur clique « Récupérer les découvertes communes », et le joint à sa base comme importé. Une découverte ne contient que des lignes et
des lieux du jeu : jamais le nom, la position ni l'identifiant d'un joueur.

Comment un fichier arrive ici : le joueur joint le zip à un ticket « Discoveries » ; le mainteneur le vérifie et le fusionne avec
`python tools/discoveries-merge.py community-discoveries/discoveries.json <fichier.zip ou .json>…` (les faits invalides sont refusés ; chaque fichier
compte comme un import d'un fait), puis valide le résultat. Le fichier arrive chez les joueurs avec la release suivante (il est lu sur la branche `main`).
