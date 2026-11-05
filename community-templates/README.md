# Community tools

Tools for the « My tools » page of Tensho Assistant. A tool is a data file: it holds no code and sends nothing to the game. The gallery in the plugin reads this folder from the `main` branch, only when a player clicks.

## Propose a tool

1. Make it in the game: « My tools » › « Create a tool ». Tick « Free to share (CC0) », then « Copy the JSON ».
2. Save it as `tools/<id>.json`. `<id>` is the tool's identifier; you may change it to a readable one (lowercase letters, digits and dashes).
3. From the repository root, run:

   ```powershell
   .\scripts\Update-ToolGallery.ps1
   ```

4. Run `.\scripts\Verify.ps1 -TestsOnly`, then open a pull request towards `Devloppement`. No version number or CHANGELOG entry is needed for a tool alone.

## What is accepted

- **Licence:** `CC0-1.0`, so anyone may use and change the tool.
- **Format:** the tests check each file with the plugin's own rules. An unknown field or block, or a link that is not https, refuses it.
- **Content:** useful for players, without advertising or anything unpleasant. No real-money trading, no automation or exploit instructions, nothing a player cannot normally see, and no personal information.
- **Items:** by their game identifier, so names follow each player's language. Add `en` and `fr` texts if you can.

Every tool is read by a person before it enters the gallery. Validation does not prove a link is safe: the reviewer also looks at where each link goes.
