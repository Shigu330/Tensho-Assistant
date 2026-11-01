# CLAUDE.md — point d'entrée de l'agent IA

Ce fichier est lu automatiquement par Claude Code. Il dit **où reprendre** et **comment travailler** sur Tensho Assistant.
Les contributeurs humains commencent par [README.md](README.md) et [CONTRIBUTING.md](CONTRIBUTING.md).

## Reprise : ordre de lecture

1. [docs/ai/RULES.md](docs/ai/RULES.md) — règles durables (courtes, obligatoires).
2. [docs/ai/STATUS.md](docs/ai/STATUS.md) — état actuel, version, ce qui reste à valider, prochaine action.
3. [docs/ai/GOAL.md](docs/ai/GOAL.md) — l'objectif actif.
4. Puis seulement le document de l'objectif (`docs/goals/<ID>.md`) et le code ciblé.
5. Le reste ([docs/README.md](docs/README.md) : feuille de route, architecture, tests, références, décisions) par recherche ciblée,
   jamais relu en entier sans raison. Historique des versions : [CHANGELOG.md](CHANGELOG.md) et `git log`.

## Environnement

- Dépôt Windows : `C:\Plugins-ff14` (GitHub : Shigu330/Tensho-Assistant, branches Devloppement (travail) et main (releases) ; analyse CodeQL dans .github/workflows). Session desktop avec PowerShell : l'agent compile et teste.
- SDK .NET 10.0.403 (global.json), Dalamud.NET.Sdk 15 ; build et tests : `.\scripts\Verify.ps1` (compile Release, tests managés,
  validation des langues, paquet). Sondes de données en lecture seule : `tools/ShopCurrencyProbe`, `tools/ApiProbe`.
- **Les commits sont faits par l'application de bureau de l'utilisateur.** L'agent ne commit pas, ne pousse pas, ne réécrit pas l'historique
  sans demande explicite.

## À chaque version livrée

1. Incrémenter `<Version>` dans `src/TenshoAssistant/TenshoAssistant.csproj` (seule source de version ; `TenshoAssistant.json` ne contient
   ni version ni InternalName ni DalamudApiLevel : DalamudPackager les remplit, `Verify.ps1` le contrôle).
2. `.\scripts\Verify.ps1` vert (0 avertissement, 0 erreur) — ne jamais demander un essai en jeu avant, et donner la version de DLL produite.
3. Ajouter l'entrée en tête de [CHANGELOG.md](CHANGELOG.md) (demande, cause, correction, preuves, résultat de Verify, « non validé en jeu »),
   en français, suivie d'une sous-section `### English` (résumé en anglais : c'est elle qui sert aux commits et aux releases GitHub).
4. Mettre à jour [docs/ai/STATUS.md](docs/ai/STATUS.md) (version, à valider, prochaine action).
5. Commit et release : faits par l'utilisateur avec `.\scripts\Verify.ps1 -Commit` (branche `Devloppement`, commit + push) puis
   `.\scripts\Verify.ps1 -Release` (Devloppement fusionnée dans main, tag `Stable-x.y.z`, release GitHub avec `latest.zip`, `repo.json`) ; `-DryRun` pour voir
   les commandes. L'agent ne lance pas ces options sans demande explicite (voir CONTRIBUTING.md « Versions and releases »).
6. Toute preuve citée par le code (sonde, extrait de journal) va dans `docs/evidence/` et est référencée par ce chemin.
   `checkpoints/` est un dossier local de travail, ignoré par git (les anciennes archives `.zip` n'y sont plus utiles).

## Compte rendu (consignes utilisateur)

- Répondre en français. À chaque étape : estimation du % restant du projet global, **signalée comme subjective, non mesurée**,
  et la commande de test exacte : `.\scripts\Verify.ps1` depuis `C:\Plugins-ff14`.
- Distinguer COMPILED / TESTED / VALIDATED_IN_GAME ; n'affirmer que ce qui a été exécuté ou rapporté par l'utilisateur.

## Quota

- L'agent ne voit pas le compteur : ne jamais l'estimer. Réserve fixée par l'utilisateur : arrêt à **1 % restant** sur 5 h ou sur la semaine.
  Quand l'utilisateur transmet un relevé proche de la limite : finir la version en cours (Verify + CHANGELOG + STATUS), noter la reprise, s'arrêter.

## Méthode

- Un seul GOAL actif, petit, vérifiable ; KEEP → EXTEND → REFACTOR → REPLACE → NEW.
- Inconnu ≠ zéro ; absence de preuve ≠ preuve d'absence ; ne jamais inventer une donnée (sonder les données du jeu, citer la source).
- Natif lu sur le thread framework puis copié ; rien de lourd dans Draw ; chaque abonnement a un propriétaire et un nettoyage.
- Références AGPL (Mappy, SortaKinda…) : lues pour comprendre, **jamais copiées** (le projet est sous GPL v3, code original).
- Hors périmètre : ACT/IINACT et tout domaine Combat (décision DEC-009).
- Publication éventuelle sur le dépôt officiel Dalamud : voir [CONTRIBUTING.md](CONTRIBUTING.md#publication-dalamud) (règles D17, politique IA).
