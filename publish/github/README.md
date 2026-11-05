# GitHub automation drafts

**Activated in 0.447 (2026-11-03, DEC-026):** `CodeQL.yml` (fixed in 0.446), `build.yml` and `dependabot.yml` now live in `.github/`;
main and Devloppement are protected against force pushes and deletion, and the `Stable-*` tags are protected (repository rulesets).
Since 0.512, `build.yml` also has a job that builds like the Dalamud repository (docs/evidence/0.512-d17-build.md); the copy here is kept in step.
The text below is the original note of 0.323.

Prepared in 0.323 (queue step d). GitHub runs workflows only from `.github/workflows/` and reads Dependabot only from
`.github/dependabot.yml`, so nothing here runs until the maintainer moves it. Nothing was pushed or changed on GitHub by the agent.

| Draft | Goes to | What it does |
|---|---|---|
| `workflows/CodeQL.yml` | replaces `.github/workflows/CodeQL.yml` | Security analysis of the C# code on `main` and `Devloppement` (the current file watches a `Stable` branch that only exists locally, and its weekly build fails without Dalamud). `build-mode: none` needs no Dalamud. |
| `workflows/build.yml` | `.github/workflows/build.yml` (new) | On every push and pull request: Windows runner, .NET from `global.json`, Dalamud from goatcorp's public `dalamud-distrib`, then `./scripts/Verify.ps1 -Force`; keeps `latest.zip` 7 days. Read-only token. |
| `dependabot.yml` | `.github/dependabot.yml` (new) | One weekly pull request on `Devloppement` updating the GitHub Actions versions. |

To activate (maintainer only), from the repository root:

```powershell
Copy-Item publish/github/workflows/CodeQL.yml .github/workflows/CodeQL.yml -Force
Copy-Item publish/github/workflows/build.yml .github/workflows/build.yml
Copy-Item publish/github/dependabot.yml .github/dependabot.yml
```

Then `.\scripts\Verify.ps1 -Commit`. Actions minutes on a public repository are free; a failing run shows in the pull request.

Suggested repository settings (also yours only, on GitHub › Settings): on `main` and `Devloppement`, block force pushes and deletion
(compatible with `Verify.ps1 -Release`); do not require pull requests or a linear history, which would block `-Commit` and `-Release`;
protect the `Stable-*` tags; enable secret scanning push protection; make `Devloppement` the default branch (if it is not yet) so pull
requests target it. The Dalamud download address used by `build.yml` is the one other open-source Dalamud plugins use in their CI; check
it still serves API 15 before activating.

Line endings: the repository stores text files with LF (`.gitattributes`, `* text=auto`). Switching to `* text=auto eol=lf` needs a
`git add --renormalize .` right after a clean commit, because it rewrites the working copy; left to the maintainer.
