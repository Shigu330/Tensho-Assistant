<!-- Thank you for contributing. Pull requests target the Devloppement branch (main only receives releases).
Traduction française plus bas. -->

## What and why

<!-- What changes, which problem it solves, the issue it closes (« Closes #… »). -->

## Checks

- [ ] `.\scripts\Verify.ps1` is green (paste its last `VERSION …` line), or `.\scripts\Verify.ps1 -TestsOnly` if you do not have the game.
- [ ] New pure logic has tests in `tests/TenshoAssistant.Tests`.
- [ ] A `## x.y.z` entry at the top of `CHANGELOG.md`, with a `### English` section; community changes take the third number (x.y.Z).
- [ ] Every new setting has a tooltip and a « ↺ » default button; texts are in `en.json` and `fr.json` (French uses « vous »); code, comments and log lines are in English.
- [ ] Game data first; community data only when the game lacks it, with its licence and a credit (Credits page, `src/TenshoAssistant/Credits/CreditCatalog.cs`).
- [ ] Nothing copied from code under AGPL or without a licence; evidence of probes or data in `docs/evidence/`.
- [ ] No automation of game actions, nothing sent to the game's servers without a click, no combat or PvP advantage, nothing a player cannot normally see; every framework subscription goes through `CompetitiveGate`.
- [ ] Tested in game: yes / no (say what, or « not validated in game »).

## AI use

<!-- Dalamud's plugin policy asks to declare AI use. Say whether and how an AI tool was used for this change. -->

---

<!-- Traduction française. Vous pouvez remplir le modèle en français ou en anglais ; supprimez la partie que vous n'utilisez pas.
Merci de contribuer. Les pull requests visent la branche Devloppement (main ne reçoit que les releases). -->

## Quoi et pourquoi

<!-- Ce qui change, le problème que cela règle, le ticket que cela ferme (« Closes #… »). -->

## Contrôles

- [ ] `.\scripts\Verify.ps1` est vert (collez sa dernière ligne `VERSION …`), ou `.\scripts\Verify.ps1 -TestsOnly` si vous n'avez pas le jeu.
- [ ] La nouvelle logique pure a des tests dans `tests/TenshoAssistant.Tests`.
- [ ] Une entrée `## x.y.z` en tête de `CHANGELOG.md`, avec une section `### English` ; les changements de la communauté prennent le troisième numéro (x.y.Z).
- [ ] Chaque nouveau réglage a une infobulle et un bouton « ↺ » de valeur par défaut ; les textes sont dans `en.json` et `fr.json` (le français utilise « vous ») ; le code, les commentaires et les lignes du journal sont en anglais.
- [ ] Données du jeu d'abord ; données de la communauté seulement quand le jeu ne les a pas, avec leur licence et un crédit (page Crédits, `src/TenshoAssistant/Credits/CreditCatalog.cs`).
- [ ] Rien de copié depuis du code sous AGPL ou sans licence ; preuves des sondes ou des données dans `docs/evidence/`.
- [ ] Aucune automatisation d'actions du jeu, rien d'envoyé aux serveurs du jeu sans clic, aucun avantage en combat ou en JcJ, rien qu'un joueur ne peut pas voir normalement ; chaque abonnement au framework passe par `CompetitiveGate`.
- [ ] Essayé en jeu : oui / non (dites quoi, ou « non validé en jeu »).

## Usage de l'IA

<!-- La politique des plugins Dalamud demande de déclarer l'usage de l'IA. Dites si et comment un outil d'IA a servi pour ce changement. -->
