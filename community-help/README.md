# Community videos and guides

> *Traduction française plus bas.*

The Help of Tensho Assistant lists the videos and written guides here, page by page. The plugin reads `index.json` on the player's click
only, from the `main` branch. A link opens in the player's browser, after a confirmation that shows its address. Nothing is played or run
inside the game.

## Propose a video or a guide

Add a row to `guides` in `index.json`, run `.\scripts\Verify.ps1 -TestsOnly`, then open a pull request towards `Devloppement`. No version
number or CHANGELOG entry is needed.

```json
{
  "page": "Maps",
  "language": "en",
  "title": "The Tensho map in five minutes",
  "author": "Your name",
  "address": "https://www.youtube.com/watch?v=...",
  "forVersion": "0.506"
}
```

| Field | Rule |
|---|---|
| `page` | A page of the plugin, as named in `MainWindow.Pages.cs` (`Page.Maps` gives `Maps`) |
| `language` | Two letters: `en`, `fr`… The player's language comes first, then English |
| `title` | 120 characters at most |
| `author` | Optional, 60 characters at most |
| `address` | https, on an accepted host: YouTube (`www.youtube.com`, `youtube.com`, `youtu.be`), or a file of this repository on `github.com` for a written guide |
| `forVersion` | Optional: the plugin version it shows |

A row that breaks a rule makes the tests fail, and the plugin refuses the whole file: check it before proposing.

## What is accepted

- It shows Tensho as it works, with no automation and no forbidden tool in the picture.
- You hold the rights to it, or the author agrees.
- No character or account details of anyone else.
- A guide is removed at its author's request, or when it no longer matches the plugin.

Another host is a change of the plugin itself: propose it in an issue.

---

# Vidéos et guides de la communauté

L'Aide de Tensho Assistant liste ici les vidéos et les guides écrits, page par page. Le plugin lit `index.json` seulement sur un clic du
joueur, depuis la branche `main`. Un lien s'ouvre dans le navigateur du joueur, après une confirmation qui montre son adresse. Rien n'est
lu ni exécuté dans le jeu.

## Proposer une vidéo ou un guide

Ajoutez une ligne à `guides` dans `index.json`, lancez `.\scripts\Verify.ps1 -TestsOnly`, puis ouvrez une pull request vers
`Devloppement`. Aucun numéro de version ni entrée dans le CHANGELOG n'est nécessaire.

```json
{
  "page": "Maps",
  "language": "fr",
  "title": "La carte Tensho en cinq minutes",
  "author": "Votre nom",
  "address": "https://www.youtube.com/watch?v=...",
  "forVersion": "0.506"
}
```

| Champ | Règle |
|---|---|
| `page` | Une page du plugin, nommée comme dans `MainWindow.Pages.cs` (`Page.Maps` donne `Maps`) |
| `language` | Deux lettres : `en`, `fr`… La langue du joueur passe en premier, puis l'anglais |
| `title` | 120 caractères au plus |
| `author` | Facultatif, 60 caractères au plus |
| `address` | https, sur un hôte accepté : YouTube (`www.youtube.com`, `youtube.com`, `youtu.be`), ou un fichier de ce dépôt sur `github.com` pour un guide écrit |
| `forVersion` | Facultatif : la version du plugin qu'elle montre |

Une ligne qui enfreint une règle fait échouer les tests, et le plugin refuse alors tout le fichier : vérifiez-la avant de la proposer.

## Ce qui est accepté

- Elle montre Tensho tel qu'il fonctionne, sans automatisation ni outil interdit à l'image.
- Vous en avez les droits, ou son auteur est d'accord.
- Aucun détail de personnage ou de compte de quelqu'un d'autre.
- Un guide est retiré à la demande de son auteur, ou quand il ne correspond plus au plugin.

Un autre hôte est un changement du plugin lui-même : proposez-le dans un ticket.
