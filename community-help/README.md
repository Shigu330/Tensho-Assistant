# Community videos and guides

The Help of Tensho Assistant lists the videos and written guides here, page by page. The plugin reads `index.json` on the player's click only, from the `main` branch. A link opens in the player's browser, after a confirmation that shows its address. Nothing is played or run inside the game.

## Propose a video or a guide

Add a row to `guides` in `index.json`, run `.\scripts\Verify.ps1 -TestsOnly`, then open a pull request towards `Devloppement`. No version number or CHANGELOG entry is needed.

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
| `address` | https, on an accepted host: YouTube, or a file of this repository for a written guide |
| `forVersion` | Optional: the plugin version it shows |

A row that breaks a rule makes the tests fail, and the plugin refuses the whole file: check it before proposing.

## What is accepted

- It shows Tensho as it works, with no automation and no forbidden tool in the picture.
- You hold the rights to it, or the author agrees.
- No character or account details of anyone else.
- A guide is removed at its author's request, or when it no longer matches the plugin.

Another host is a change of the plugin itself: propose it in an issue.
