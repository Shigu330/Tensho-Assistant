# X-121 — automatic update of the desktop application from GitHub (2026-11-10)

Request of the user (2026-11-10, R33): the desktop application `src/TenshoAssistant.Desktop` updates itself from this repository's GitHub
releases, with no step for the user; the agent keeps the publishing chain (package, SHA-256, a test of the update in Verify) and never lets
a release go out that would break it. The plugin's code is unchanged (frozen until Dalamud supports 7.58) except its language files
(`Desktop.Update*`, the FAQ entry `DesktopUpdate`) and `Help/Faq.cs`.

## How it works

| Step | What the app does | Code |
|---|---|---|
| When | At start, in the background, when « Automatic update » is on (`%LOCALAPPDATA%\TenshoAssistant\Desktop\update.json`, on by default); on « Check for update » at any time. Never for a diagnostic run (`--headless`, `--inventory`, `--api`, `--il`), a capture, or `--no-update`. | `Program.cs` (`Updates`), `StatusBar.cs` |
| Which version | The installed plugin's version (R33: same number, from the start-up version check); none when they are already the same (no request at all); the latest Stable release when no plugin is found. Never older than the running app (an older plugin is said, not followed), never a version set aside after going back. | `DesktopUpdate.Plan` |
| Request | `GET https://api.github.com/repos/Shigu330/Tensho-Assistant/releases/tags/Stable-x.y.z` (or `releases/latest`), a fixed User-Agent and `Accept`, no token, no player data; answer read up to 1 MB in 30 s. | `GitHubReleases` in `DesktopUpdater.cs` |
| Checks before download | A published `Stable-x.y.z` (not a draft nor a pre-release), the version asked, newer than the app; `TenshoAssistant.Desktop.zip` and `TenshoAssistant.Desktop.zip.sha256` both there, each at exactly `https://github.com/Shigu330/Tensho-Assistant/releases/download/<tag>/<name>` over HTTPS; the zip 300 MB at most. | `DesktopUpdate.Refusal` |
| Download and SHA-256 | The `.sha256` (« hex  name », 4 KB at most), then the zip to `downloads\…partial`; its SHA-256 compared with the published one; a different one deletes it and installs nothing. | `DesktopUpdater.Install`, `DesktopUpdate.ParseHash` |
| Install beside | Unpacked to `versions\<version>.partial` (an entry that would land outside the folder is refused), the exe and both versions inside (app and `plugin\TenshoAssistant.dll`) checked, then moved to `versions\<version>`; `update.json` records it as Current and the running copy as Previous; older copies (neither current, previous nor running) are removed. | `DesktopUpdater.Install`, `CheckCopy`, `DesktopUpdate.AfterInstall`, `Obsolete` |
| Restart | The running exe is never overwritten. The new copy starts by itself only when the catalogues are loaded and the window was not used yet (no click, no field typed in); otherwise it opens at the next start, or on « Restart now ». At start, the copy the user started hands over to the newest installed copy (`ForwardTo`); a restarted copy gets `--no-forward` (no loop); a newer zip taken by hand stays itself. | `DesktopUpdate.MayRestart`, `ForwardTo`, `DesktopUpdater.Forward` |
| Going back | « Previous version (x) » makes the previous copy Current and sets the version left aside (never installed again by itself). A copy started as Current that fails before its window goes back by itself at the next start. | `DesktopUpdate.RollBack`, `DesktopUpdater.RollBackAfterFailure` |

## Publishing side

- `scripts/Verify.ps1` writes `TenshoAssistant.Desktop.zip.sha256` beside the zip, then runs the packaged exe with
  `--update-selftest=src\TenshoAssistant.Desktop\bin\fake-release.json`: a GitHub-shaped release of the version being verified, with this
  repository's download addresses, whose two files are read from that folder (no network). A failure stops Verify, hence publish.sh and
  any release.
- `scripts/Release.ps1` (`Assert-DesktopZip`, run by `Verify.ps1 -Release`) refuses a release without the `.sha256` or with one that is not
  the zip's SHA-256, and attaches it to the GitHub release next to `latest.zip` and `TenshoAssistant.Desktop.zip` (a resumed release
  uploads whichever is missing).
- The network source is a row of `tools/docgen.py` (`NETWORK`, « Desktop app update »), written into `SECURITY.md`; `docgen.py` now
  also checks the desktop application's sources for requests. FAQ: `DesktopUpdate` (`Help/Faq.cs`, `docs/FAQ.md`).

## Limit

The application is not code-signed. The SHA-256 published in the same release protects against a damaged or incomplete download (and
the address check against a file taken elsewhere), **not against a stolen GitHub account**, which could publish both a bad zip and its
matching SHA-256. Said in `SECURITY.md` (both languages) and in the FAQ answer.

## Test runs

Development run (2026-11-10, before the version), the selftest built from the new code against a fake release made of the 0.1032
package: fake release read, plan, download, SHA-256, unpack beside, versions, `update.json`, hand-over, going back, version set aside,
damaged zip refused (SHA-256 `8ba42f76…` against the published `15a09a84…`), foreign address and draft refused: all PASS, except « the
installed exe starts and says its version »: the 0.1032 exe does not know `--print-version` and opened its window. The selftest now stops
a child that does not answer within 60 s (no window can stay open); with the packaged exe of the version being verified the check passes.

`.\scripts\Verify.ps1` (2026-11-10, sources of this version before its number was raised, so the fake tag is `Stable-0.1032.0`): 1681/1681
tests, headless 51 pages, package 196 files 38.2 MB, then the selftest by the packaged exe:

```text
PASS update: fake release read (Stable-0.1032.0)
PASS update: plan asks Stable-0.1032.0, the installed plugin's version
Update: Stable-0.1032.0 installed in …\artifacts\update-selftest\good\versions\0.1032.0 (SHA-256 1e77f0ed…14cb as published); previous copy …\started copy
PASS update: installed beside in …\versions\0.1032.0 (status Desktop.UpdateInstalled)
PASS update: the unpacked app and its plugin are 0.1032.0
PASS update: update.json: current 0.1032.0, previous the started copy
PASS update: the download is removed once unpacked
PASS update: the started copy hands over to the installed one
PASS update: the installed copy does not hand over to itself
PASS update: the installed exe starts and says TenshoAssistant.Desktop 0.1032.0
PASS update: 'Previous version' goes back to the started copy and sets 0.1032.0 aside
PASS update: a version set aside is neither asked again nor started
Update: TenshoAssistant.Desktop.zip of Stable-0.1032.0 has SHA-256 26175f3d…eaa1, the release publishes 1e77f0ed…14cb: refused
PASS update: a zip with one byte changed is refused by its SHA-256 (status Desktop.UpdateErrorHash)
PASS update: a file outside this repository's downloads is refused
PASS update: a draft is refused; the same version is not installed again
PASS update selftest: a fake release installed, handed over, gone back from; a damaged zip and a foreign address refused
```

Real request, read only (`TenshoAssistant.Desktop --update-probe`, 2026-11-10): GitHub answered the latest release `Stable-0.1012.0`
(not a draft nor a pre-release) with one file, `latest.zip`; the update would refuse it (`Desktop.UpdateErrorRelease`: no
`TenshoAssistant.Desktop.zip` nor `.sha256`). The first release made after this version carries both (Release.ps1 refuses it otherwise):
from then on an installed app follows the plugin by itself.

Not observed: a real download from GitHub (no release carries the app yet), and a restart of a real window into a new copy (the
selftest starts the installed exe with `--print-version`; the window's own restart uses the same `Process.Start` with `--no-forward`).
