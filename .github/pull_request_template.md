<!-- Thank you for contributing. Pull requests target the Devloppement branch (main only receives releases). -->

## What and why

<!-- What changes, which problem it solves, the issue it closes (« Closes #… »). -->

## Checks

- [ ] `.\scripts\Verify.ps1` is green (paste its last `VERSION …` line), or `.\scripts\Verify.ps1 -TestsOnly` if you do not have the game.
- [ ] New pure logic has tests in `tests/TenshoAssistant.Tests`.
- [ ] A `## x.y.z` entry at the top of `CHANGELOG.md`, with a `### English` section; community changes take the third number (x.y.Z).
- [ ] Every new setting has a tooltip and a « ↺ » default button; texts are in `en.json` and `fr.json` (French uses « vous »).
- [ ] Game data first; community data only when the game lacks it, with its licence and a credit (Credits page, `src/TenshoAssistant/Credits/CreditCatalog.cs`).
- [ ] Nothing copied from code under AGPL or without a licence; evidence of probes or data in `docs/evidence/`.
- [ ] No automation of game actions, nothing sent to the game's servers without a click, no combat or PvP advantage, nothing a player cannot normally see.
- [ ] Tested in game: yes / no (say what, or « not validated in game »).

## AI use

<!-- Dalamud's plugin policy asks to declare AI use. Say whether and how an AI tool was used for this change. -->
