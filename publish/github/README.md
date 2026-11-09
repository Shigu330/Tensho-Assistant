# GitHub automation drafts

> *Traduction française plus bas.*

**Active since 0.447 (2026-11-03, DEC-026):** the workflows that run are the ones in `.github/` (`workflows/build.yml`,
`workflows/CodeQL.yml`, `dependabot.yml`); `main` and `Devloppement` are protected against force pushes and deletion, and the `Stable-*`
tags are protected (repository rulesets). `build.yml` also has a job that builds like the Dalamud repository (since 0.512,
`docs/evidence/0.512-d17-build.md`).

The files in this folder are the drafts of 0.323 and are **no longer kept in step** (checked on 2026-11-09): in `.github/`,
Dependabot has raised the action versions, `CodeQL.yml` now builds the plugin with Dalamud's reference assemblies (`build-mode: manual`)
instead of `build-mode: none`, and its pull request trigger watches `main` only. Read `.github/` for what runs; this folder is history.

The original note of 0.323 follows.

Prepared in 0.323 (queue step d). GitHub runs workflows only from `.github/workflows/` and reads Dependabot only from
`.github/dependabot.yml`, so nothing here runs until the maintainer moves it. Nothing was pushed or changed on GitHub by the agent.

| Draft | Goes to | What it does |
|---|---|---|
| `workflows/CodeQL.yml` | replaces `.github/workflows/CodeQL.yml` | Security analysis of the C# code on `main` and `Devloppement` (the file of that time watched a `Stable` branch that only existed locally, and its weekly build failed without Dalamud). `build-mode: none` needs no Dalamud. |
| `workflows/build.yml` | `.github/workflows/build.yml` (new) | On every push and pull request: Windows runner, .NET from `global.json`, Dalamud from goatcorp's public `dalamud-distrib`, then `./scripts/Verify.ps1 -Force`; keeps `latest.zip` 7 days. Read-only token. |
| `dependabot.yml` | `.github/dependabot.yml` (new) | One weekly pull request on `Devloppement` updating the GitHub Actions versions. |

To activate (maintainer only), from the repository root:

```powershell
Copy-Item publish/github/workflows/CodeQL.yml .github/workflows/CodeQL.yml -Force
Copy-Item publish/github/workflows/build.yml .github/workflows/build.yml
Copy-Item publish/github/dependabot.yml .github/dependabot.yml
```

Then `.\scripts\Verify.ps1 -Commit`. Actions minutes on a public repository are free; a failing run shows in the pull request.

Suggested repository settings (also the maintainer's only, on GitHub › Settings): on `main` and `Devloppement`, block force pushes and
deletion (compatible with `Verify.ps1 -Release`); do not require pull requests or a linear history, which would block `-Commit` and
`-Release`; protect the `Stable-*` tags; enable secret scanning push protection; make `Devloppement` the default branch (if it is not yet)
so pull requests target it. The Dalamud download address used by `build.yml` is the one other open-source Dalamud plugins use in their
CI; check it still serves API 15 before activating.

Line endings: the repository stores text files with LF (`.gitattributes`, `* text=auto`). Switching to `* text=auto eol=lf` needs a
`git add --renormalize .` right after a clean commit, because it rewrites the working copy; left to the maintainer (DEC-026: nothing to
normalize).

---

# Brouillons d'automatisation GitHub

**Actifs depuis la 0.447 (2026-11-03, DEC-026) :** les workflows qui tournent sont ceux de `.github/` (`workflows/build.yml`,
`workflows/CodeQL.yml`, `dependabot.yml`) ; `main` et `Devloppement` sont protégées contre les push forcés et la suppression, et les tags
`Stable-*` sont protégés (rulesets du dépôt). `build.yml` a aussi un job qui compile comme le dépôt Dalamud (depuis la 0.512,
`docs/evidence/0.512-d17-build.md`).

Les fichiers de ce dossier sont les brouillons de la 0.323 et **ne sont plus tenus à jour** (vérifié le 2026-11-09) : dans
`.github/`, Dependabot a monté les versions des actions, `CodeQL.yml` compile maintenant le plugin avec les assemblys de référence de
Dalamud (`build-mode: manual`) au lieu de `build-mode: none`, et son déclencheur de pull request ne surveille que `main`. Lisez
`.github/` pour ce qui tourne ; ce dossier est de l'historique.

La note d'origine de la 0.323 suit.

Préparé en 0.323 (étape d de la file). GitHub ne lance les workflows que depuis `.github/workflows/` et ne lit Dependabot que depuis
`.github/dependabot.yml` : rien ici ne tourne tant que le mainteneur ne l'y déplace pas. L'agent n'a rien poussé ni changé sur GitHub.

| Brouillon | Va dans | Ce qu'il fait |
|---|---|---|
| `workflows/CodeQL.yml` | remplace `.github/workflows/CodeQL.yml` | Analyse de sécurité du code C# sur `main` et `Devloppement` (le fichier de l'époque surveillait une branche `Stable` qui n'existait qu'en local, et sa compilation hebdomadaire échouait sans Dalamud). `build-mode: none` n'a pas besoin de Dalamud. |
| `workflows/build.yml` | `.github/workflows/build.yml` (nouveau) | À chaque push et pull request : machine Windows, .NET selon `global.json`, Dalamud depuis le `dalamud-distrib` public de goatcorp, puis `./scripts/Verify.ps1 -Force` ; garde `latest.zip` 7 jours. Jeton en lecture seule. |
| `dependabot.yml` | `.github/dependabot.yml` (nouveau) | Une pull request par semaine sur `Devloppement` qui met à jour les versions des GitHub Actions. |

Pour les activer (mainteneur seulement), depuis la racine du dépôt :

```powershell
Copy-Item publish/github/workflows/CodeQL.yml .github/workflows/CodeQL.yml -Force
Copy-Item publish/github/workflows/build.yml .github/workflows/build.yml
Copy-Item publish/github/dependabot.yml .github/dependabot.yml
```

Puis `.\scripts\Verify.ps1 -Commit`. Les minutes d'Actions sont gratuites sur un dépôt public ; un passage en échec se voit dans la pull
request.

Réglages du dépôt suggérés (eux aussi au mainteneur seulement, dans GitHub › Settings) : sur `main` et `Devloppement`, bloquer les push
forcés et la suppression (compatible avec `Verify.ps1 -Release`) ; ne pas exiger de pull request ni d'historique linéaire, ce qui
bloquerait `-Commit` et `-Release` ; protéger les tags `Stable-*` ; activer la protection des secrets au push ; faire de `Devloppement`
la branche par défaut (si elle ne l'est pas encore) pour que les pull requests la visent. L'adresse de téléchargement de Dalamud utilisée
par `build.yml` est celle qu'utilisent d'autres plugins Dalamud open source dans leur CI ; vérifier qu'elle sert toujours l'API 15 avant
d'activer.

Fins de ligne : le dépôt garde les fichiers texte en LF (`.gitattributes`, `* text=auto`). Passer à `* text=auto eol=lf` demande un
`git add --renormalize .` juste après un commit propre, car cela réécrit la copie de travail ; laissé au mainteneur (DEC-026 : rien à
normaliser).
