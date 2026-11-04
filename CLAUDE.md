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
- SDK .NET 10.0 (global.json accepte 10.0.100 et plus, le dépôt Dalamud compile avec 10.0.101 ; local : 10.0.403), Dalamud.NET.Sdk 15 ; build et tests : `.\scripts\Verify.ps1` (compile Release, tests managés,
  validation des langues, paquet). Sondes de données en lecture seule : `tools/` (liste dans `tools/README.md`).
- **Git géré par l'agent (accord de l'utilisateur, 2026-11-03)** : commit et push de chaque version sur `Devloppement` par
  `.\scripts\Verify.ps1 -Commit` (après `-Commit -DryRun`). Release seulement sur demande explicite de l'utilisateur. Jamais de push forcé,
  de réécriture d'historique ni de réglage GitHub sans accord ; issues, milestones et labels GitHub tenus par l'agent (en anglais) ;
  plan détaillé : [docs/ai/STATUS.md](docs/ai/STATUS.md) « Git ».

## À chaque version livrée

1. Incrémenter `<Version>` dans `src/TenshoAssistant/TenshoAssistant.csproj` selon la règle de numérotation (DEC-024) :
   **X**.y.z = grandes étapes, sur décision de l'utilisateur seulement (1.0.0 = première publication sur le dépôt Dalamud) ;
   x.**Y**.0 = nos versions (utilisateur et agent : l'agent monte ce nombre et remet le troisième à 0) ;
   x.y.**Z** = versions de la communauté (contributions extérieures). Seule source de version ; `TenshoAssistant.json` ne contient
   ni version ni InternalName ni DalamudApiLevel : DalamudPackager les remplit, `Verify.ps1` le contrôle).
2. `.\scripts\Verify.ps1` vert (0 avertissement, 0 erreur) — ne jamais demander un essai en jeu avant, et donner la version de DLL produite.
3. Ajouter l'entrée en tête de [CHANGELOG.md](CHANGELOG.md) (demande, cause, correction, preuves, résultat de Verify, « non validé en jeu »),
   en français, suivie d'une sous-section `### English` (résumé en anglais : c'est elle qui sert aux commits et aux releases GitHub).
4. Mettre à jour [docs/ai/STATUS.md](docs/ai/STATUS.md) (version, à valider, prochaine action).
5. Commit : fait par l'agent à la fin de la version, `.\scripts\Verify.ps1 -Commit -DryRun` puis `-Commit` (branche `Devloppement`,
   commit + push ; `$env:TENSHO_COMMIT_TRAILER` ajoute la ligne `Co-Authored-By` de l'agent). Release : `.\scripts\Verify.ps1 -Release`
   (Devloppement fusionnée dans main, tag `Stable-x.y.z`, release GitHub avec `latest.zip`, `repo.json`), seulement sur demande explicite
   de l'utilisateur ; `-DryRun` pour voir les commandes. L'utilisateur peut aussi les lancer lui-même (CONTRIBUTING.md « Versions and releases »).
6. Issues GitHub : commenter celles que la version fait avancer, en ouvrir une pour un travail nouveau, en anglais, et fermer sur preuve (Verify vert pour le code, rapport de l'utilisateur pour le jeu). Quand une règle Dalamud entre en jeu : résumé et accord de l'utilisateur.
7. Toute preuve citée par le code (sonde, extrait de journal) va dans `docs/evidence/` et est référencée par ce chemin.
   `checkpoints/` est un dossier local de travail, ignoré par git. Demande utilisateur (2026-11-02) : avant toute grosse modification,
   un zip de sauvegarde des fichiers du dépôt (sans bin/obj) y est créé (`backup-<horodatage>-<contexte>.zip`).

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
- Hors périmètre : ACT/IINACT et tout domaine Combat (DEC-009), sauf l'exception du dresseur, informations seulement, JcE seulement (DEC-032) ; conseils de jeu en JcE seulement (DEC-033).
- Publication sur le dépôt officiel Dalamud à la 1.0.0 : ce qui reste dans [docs/project/PUBLISHING.md](docs/project/PUBLISHING.md) ; voir aussi [CONTRIBUTING.md](CONTRIBUTING.md#publication-dalamud) (règles D17, politique IA).
