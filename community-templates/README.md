# Community tools

> *Traduction française plus bas.*

Tools for the « My tools » page of Tensho Assistant. A tool is a data file: it holds no code and sends nothing to the game. The gallery in
the plugin reads this folder from the `main` branch, only when a player clicks.

## Propose a tool

1. Make it in the game: « My tools » › « Create a tool ». Tick « Free to share (CC0) », then « Copy the JSON ».
2. Save it as `tools/<id>.json`. `<id>` is the tool's identifier; you may change it to a readable one (lowercase letters, digits and
   dashes).
3. From the repository root, run:

   ```powershell
   .\scripts\Update-ToolGallery.ps1
   ```

   It rebuilds `index.json` from the files in `tools/`: identifier, name, author, and the fingerprint the plugin checks.

4. Run `.\scripts\Verify.ps1 -TestsOnly`, then open a pull request towards `Devloppement`. No version number or CHANGELOG entry is needed
   for a tool alone.

## What is accepted

- **Licence:** `CC0-1.0`, so anyone may use and change the tool.
- **Format:** the tests check each file with the plugin's own rules. An unknown field or block, or a link that is not https, gets it refused.
- **Content:** useful for players, without advertising or anything unpleasant. No real-money trading, no automation or exploit
  instructions, nothing a player cannot normally see, and no personal information.
- **Items:** by their game identifier, so names follow each player's language. Add `en` and `fr` texts if you can.

Every tool is read by a person before it enters the gallery. Validation does not prove a link is safe: the reviewer also looks at where
each link goes.

---

# Outils de la communauté

Des outils pour la page « Mes outils » de Tensho Assistant. Un outil est un fichier de données : il ne contient aucun code et n'envoie
rien au jeu. La galerie du plugin lit ce dossier depuis la branche `main`, seulement quand un joueur clique.

## Proposer un outil

1. Créez-le dans le jeu : « Mes outils » › « Créer un outil ». Cochez « Libre de partage (CC0) », puis « Copier le JSON ».
2. Enregistrez-le sous `tools/<id>.json`. `<id>` est l'identifiant de l'outil ; vous pouvez le changer pour un identifiant lisible
   (lettres minuscules, chiffres et tirets).
3. Depuis la racine du dépôt, lancez :

   ```powershell
   .\scripts\Update-ToolGallery.ps1
   ```

   Il refait `index.json` à partir des fichiers de `tools/` : identifiant, nom, auteur, et l'empreinte que le plugin vérifie.

4. Lancez `.\scripts\Verify.ps1 -TestsOnly`, puis ouvrez une pull request vers `Devloppement`. Aucun numéro de version ni entrée dans le
   CHANGELOG n'est nécessaire pour un outil seul.

## Ce qui est accepté

- **Licence :** `CC0-1.0`, pour que chacun puisse utiliser et modifier l'outil.
- **Format :** les tests contrôlent chaque fichier avec les règles du plugin lui-même. Un champ ou un bloc inconnu, ou un lien qui n'est
  pas en https, le fait refuser.
- **Contenu :** utile aux joueurs, sans publicité ni rien de désagréable. Pas de commerce en argent réel, pas d'instructions
  d'automatisation ou d'exploitation de failles, rien qu'un joueur ne peut pas voir normalement, et aucune information personnelle.
- **Objets :** par leur identifiant du jeu, pour que les noms suivent la langue de chaque joueur. Ajoutez des textes `en` et `fr` si vous
  le pouvez.

Chaque outil est lu par une personne avant d'entrer dans la galerie. La validation ne prouve pas qu'un lien est sûr : la personne qui
relit regarde aussi où mène chaque lien.
