# QUEST-ITEM-PROGRESS-007 — Progression des quêtes proposant un objet

2026-10-30. TESTED : Verify.ps1 exécuté par l'utilisateur à 18:36 — build Release sans avertissement/erreur, 477/477 tests (dont les 6 nouveaux), langues et paquet verts. Version portée ensuite à 0.76.0.0, recompilée et retestée par l'agent à 18:54 (477/477) ; VALIDATED_IN_GAME: YES (rapport utilisateur 2026-10-30 : version, libellés, état après déconnexion). CLOSE. Plan d'origine : [QUEST-ITEM-PROGRESS-007-PLAN.md](QUEST-ITEM-PROGRESS-007-PLAN.md).

## Comportement

Dans Objets → « Quêtes proposant cet objet », une colonne Progression et un filtre Toutes / En cours / Terminées / Non confirmées s'ajoutent. La progression vient du même `QuestState` que l'index des récompenses, jointe par identifiant de quête complet.

- Relevé hors ligne, non daté, en lecture ou en erreur : toutes les lignes restent « Non confirmée », sans séquence ni portée ; un message l'explique.
- Quête absente du relevé : « Non confirmée », jamais « non commencée » ni « disponible ».
- Quête répétable terminée : reste « Terminée confirmée » ; une infobulle rappelle que cela ne prouve pas une reprise possible.
- Les compteurs (en cours / terminées / non confirmées) portent sur les quêtes correspondant à la recherche, avant le filtre de progression. Les variantes ne sont ni sommées ni choisies.
- Aucune lecture native, aucune requête réseau, aucun second index. La projection est recalculée seulement quand la référence de l'état, l'objet, la recherche ou le filtre change.

## Validation en jeu

PARTIAL — Captures utilisateur 2026-10-30 18:43 (objet « Pièce d’argent allagoise ») : colonne Progression, compteurs 1 en cours · 173 terminées · 127 non confirmées (301/301), filtres Toutes et En cours, séquence affichée ; relevé issu du cache disque daté 29-10-26 21:40. Version de la DLL non visible ; déconnexion non montrée.

Observations : la séquence native 255 est affichée telle quelle (« séquence 255 ») ; le pluriel « 1 variantes » est un défaut de libellé antérieur à cette tranche.

## Fichiers

- `src/TenshoAssistant/Quests/QuestItemProgress.cs` (nouveau) : projection pure.
- `src/TenshoAssistant/MainWindow.QuestItemSources.cs` : filtre, colonne, cache de projection.
- `src/TenshoAssistant/MainWindow.Quests.cs` : libellé de progression partagé.
- `Localization/Languages/en.json`, `fr.json` : 5 nouvelles clés (`QuestSources.Progress`, `ProgressOffline`, `ProgressFiltered`, `Repeatable`, `QuestSourceCounts.Progress`). DE/JA : fallback anglais.
- `tests/TenshoAssistant.Tests/QuestItemProgressTests.cs` (nouveau, 6 cas), `.csproj`, `Program.cs`.

## Vérification attendue

Depuis PowerShell dans `C:\Plugins-ff14` : `.\scripts\Verify.ps1`. Attendu : build Release sans avertissement/erreur, 477/477 tests (471 + 6), langues et paquet verts, DLL 0.75.0.0 (version non incrémentée tant que le build n'est pas confirmé).

Puis en jeu : recharger la DLL canonique, sélectionner un objet récompense de quête (ex. via la recherche d'objets), ouvrir « Quêtes proposant cet objet », vérifier la colonne et les quatre filtres, puis se déconnecter/reconnecter pour voir l'état non confirmé.
