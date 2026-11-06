---
name: tensho-version
description: Procédure d'une version de Tensho Assistant et pièges du code. À lire avant de coder un point du plan global, de changer du code du plugin, ou de pousser une version.
---

# Une version de Tensho Assistant

Règles de fond : `CLAUDE.md` et `docs/ai/RULES.md`. Où reprendre : la ligne « Reprise » de `docs/ai/STATUS.md` et `docs/project/GLOBAL-PLAN.md`. Ici : le déroulé et ce qui fait perdre du temps.

## Déroulé

1. **Vérifier que ça n'existe pas déjà** (consigne de l'utilisateur) : chercher dans le code et dans `Localization/Languages/en.json`. Si ça existe : le noter, ne pas coder de doublon.
2. Coder, avec un test. Écrire les modifications groupées avec un script Python via l'outil Write, jamais dans un heredoc bash (voir plus bas).
3. Écrire `entry.json` (format en tête de `scripts/agent/new-version.py`), puis, depuis la racine du dépôt :

```bash
python -I -X utf8 scripts/agent/new-version.py entry.json
```

```bash
bash scripts/agent/check.sh
```

```bash
bash scripts/agent/publish-version.sh
```

- `check.sh` compile sans recharger le jeu. `publish-version.sh` recharge la DLL si le jeu tourne, remplit les nombres de tests, contrôle les secrets, committe et pousse ; il s'arrête au premier problème.
- Si `check.sh` signale une erreur de compilation des tests, le résultat affiché vient de l'ancien binaire : corriger d'abord.
- Ne pas dire « validé en jeu » sans rapport de l'utilisateur. Release seulement sur sa demande.

## Outil LSP (csharp-ls)

Plugin `csharp-lsp` et `csharp-ls` installés le 2026-11-05 ; .NET 10 lui est donné par `DOTNET_ROOT` dans `.claude/settings.local.json`. À préférer à grep pour trouver une définition, les références ou le type d'un symbole, et pour vérifier un nom avant de compiler.

- `workspaceSymbol` marche partout. `hover`, `findReferences` et `documentSymbol` marchent sur les fichiers du plugin (`MainWindow.*.cs`).
- Essai du 2026-11-05 : ils répondaient vide sur `UI/GameWindow.cs`, un fichier aussi lié au projet de tests. Sur ces fichiers partagés, passer par `workspaceSymbol` ou grep.
- La ligne et la colonne commencent à 1 et doivent tomber sur le nom du symbole.
- Ses diagnostics sont en retard après une édition par script : des « n'existe pas » juste après ne prouvent rien. `check.sh` fait foi.

## Pièges du code

- **Nouveau fichier `.cs` pur** dans un dossier que le projet de tests lie un par un (`Gathering/`, `Maps/`, `Quests/`, `Craft/`) : ajouter son `<Compile Include>` dans `tests/TenshoAssistant.Tests/TenshoAssistant.Tests.csproj`.
- **Nouvelle propriété de `Configuration`** : l'ajouter à une liste de « tout réinitialiser » en tête de `Configuration.cs` (données du joueur : celle de `"GearPlans"` ; réglages : celle de `"HidePanelsBehindGame"`). Un test l'exige.
- **Nouveau champ de `AlarmSettings`** : le recopier dans la reconstruction `configuration.AlarmOptions=new(){…}` de `MainWindow.Alarms.cs`.
- **Tout contrôle** (`Checkbox`, `InputText`, `Combo`…) : `Tip("Section.CléTip")` juste après, ou `WrappedTooltip` si survolé. Un test le vérifie. Les textes vont dans `en.json` et `fr.json`, avec « vous » en français.
- **Clés de langue** : chercher la section au début de ligne, avec deux espaces (`^  "Reminders": {`). Le même nom existe plus profond dans le fichier. Une clé mal placée casse des tests plus loin.
- **Pas de français dans le code**, sauf les mots tapés par les joueurs, listés dans `Languages406Tests.AllowedLiterals`. Le journal est en anglais.
- **Message interne affiché** : l'ajouter à `UI/InternalMessages.cs` et aux deux langues.
- **Natif** : lu sur le thread framework puis copié ; rien de lourd dans Draw ; pas de récursion sans borne sur des données du jeu (un plantage 0xc00000fd a déjà eu lieu), préférer une pile explicite.
- **Fonction du jeu appelée (pas une simple lecture)** : un `try` ne rattrape pas une violation d'accès. Vérifier joueur chargé et hors chargement avant l'appel, surtout si l'appel n'est plus déclenché par un clic (plantage du 2026-11-06 : `UpdateAetheryteList` pendant un changement de zone, `docs/evidence/0.662-teleport-list-crash.md`).
- **Tests sur le texte du code** : ils cherchent des extraits exacts. Après un changement de signature, relire le message du test en échec avant de toucher au code.
- **Scripts Python d'édition** : un script qui échoue a pu s'appliquer en partie : vérifier avec `git status` et ne reprendre que le reste. Fichiers en CRLF : normaliser à la lecture et restaurer à l'écriture. Dans un heredoc, `\\n` devient un vrai saut de ligne : il casse le JSON et le C#.

## Compte rendu

En français, avec COMPILED, TESTED ou VALIDATED_IN_GAME selon ce qui a été fait, le commit, ce qui reste, et `.\scripts\Verify.ps1` depuis `C:\Plugins-ff14`. Le % restant n'est jamais inventé.
