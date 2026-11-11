# X-126: the desktop application installed from the plugin (0.1113)

User's request of 2026-11-11: install the desktop application from Tensho, never mandatory, only from Settings, section « Desktop
application ». Decision D-137 (2026-11-11): an accepted exception to Dalamud's plugin rules (a plugin that downloads a program), Tensho being
on a custom repository; a build for the official repository leaves it out with `-p:TenshoDesktopInstall=false`.

| Part | What | Code |
|---|---|---|
| Where | Settings › Advanced › Desktop application: state against the plugin (not installed, older, same, newer, gone), install folder, « Install » or « Update », « Open the desktop app », « Open its folder », « Release page », « Automatic update of the desktop app ». | `MainWindow.DesktopInstall.cs` |
| When | Requests only on « Install » / « Update »; nothing at load, nothing in the background. The app is started only by « Open the desktop app ». | `Link/DesktopInstaller.cs` |
| Requests | `GET https://api.github.com/repos/Shigu330/Tensho-Assistant/releases/tags/Stable-x.y.z` (the plugin's version; when it has no release yet, `releases/latest`), then `TenshoAssistant.Desktop.zip` and its `.sha256` at `https://github.com/Shigu330/Tensho-Assistant/releases/download/<tag>/`. Fixed User-Agent, no token, no player data. | `GitHubReleases` in `DesktopInstall.cs` (shared with the app, X-121) |
| Checks | Published Stable release only, newer than the installed copy, both files at this repository's download address, zip at most 300 MB, SHA-256 equal to the published one, unpacked in `<folder>\<version>.partial` (an entry landing outside is refused), the exe and both versions inside checked, then moved to `<folder>\<version>`. | `DesktopUpdate.Refusal`, `DesktopInstall.Install`, `CheckCopy` |
| State | `%LOCALAPPDATA%\TenshoAssistant\Desktop\update.json`: the new copy becomes Current (the app starts there), the copy that was current becomes Previous (the app's « Previous version »). | `DesktopInstallRules.AfterInstall` |
| Limit | The app is not code-signed: the SHA-256 protects against a damaged download, not against a stolen GitHub account (as X-121). | SECURITY.md |

Tests: `DesktopInstall1113Tests` (rules, a fake release installed in a temporary folder, a damaged zip refused, the switch, the wiring).
