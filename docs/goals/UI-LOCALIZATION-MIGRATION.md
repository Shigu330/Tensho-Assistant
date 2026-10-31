# UI and localization migration

## Clôture — 2026-10-30

**Refonte UI : 100 % du périmètre convenu, acceptée par l’utilisateur.**
L’utilisateur confirme : « les verif en jeu son OK ». La validation en jeu est
donc enregistrée comme rapportée par l’utilisateur, et non exécutée ou mesurée
par l’agent. Elle clôt le bloc de validation restant au checkpoint de 92 %.

Preuves techniques précédentes : Release 0.75.0, zéro erreur/avertissement,
471/471 tests, validateur de langues et vérification du paquet passants.
Aucun code n’a été modifié après ces vérifications pour cette clôture.

English et français : 1763 clés de référence chacun. Allemand et japonais
restent partiels avec fallback anglais, conformément au périmètre initial.
La clôture concerne la refonte, la navigation et la localisation de l’interface ;
elle ne transforme pas les fonctionnalités métier partielles en modules complets.
Les limites de couverture des données et les fonctions expérimentales demeurent.

## Checkpoint précédent — 2026-10-30, quota reserve reached

STOP: real quota check reached 99% used weekly (1% remaining), with 10%
remaining in the five-hour window. Respect the user's reserves of 5% / 1%.
Estimated overall refactor completion: **92%**, not measured UI coverage.
FFXIV was unavailable; the user explicitly asked to continue code/tests only.
No current rendering, plugin loading or FPS claim is validated in game.

Latest Verify.ps1: **471/471 tests**, language validator and package hashes pass.
Release build has no errors/warnings, SDK 10.0.403, assembly version **0.75.0**.
English/French contain 1763 reference UI keys each; de/ja contain 35 keys each
and use English fallback. This does not prove every runtime message is covered.
Static resource-literal audit found no missing keys; all 18 directory routes
have group/name/description resources. Theme scopes balance 32 colors / 6 vars.

Completed after the earlier checkpoint: all listed purchase clipboard, sorting
zone tooltip, quest/triad/fashion fragment and shell text work; compact tool
directory and shared error presentation; descriptive translation keys and
readable JSON; localized developer diagnostics separated from settings; common
window lifetime separated from Mini Cactpot; configuration button opens settings.
Reload/key mode now increments a localization revision to refresh cached labels
and command help. Added-language tests cover metadata code normalization,
selection, fallback and removal without window changes.

Performance work: bank aggregation/categories are cached by managed snapshot,
character, origin and catalogue; bag frames/filter rows also cache by filters,
localization revision and second. Removed unused sorting/location joins.
Craft effects cache by frame/catalogue, projection ordering by result/order,
large-chain estimates by inputs/second, and search signatures by scalar inputs.
These are code changes, not measured in-game performance results.

Final freshness review reuses CacheAge.Fresh in live crafting/stat-copy/GP-copy
views, rejecting future timestamps after a backward PC clock change. Ordinary
dates, saved presets and solver times use the PC-local clock; Eorzea stays distinct.
Native snapshot values and calculation timestamps remain UTC internally.

Resume with [UI-ACCEPTANCE-CHECKLIST.md](UI-ACCEPTANCE-CHECKLIST.md): load the
recent canonical Release DLL, test small/large windows, fonts, English/French,
reload, persisted observation choices, all routes, overlays and actual CPU/FPS.
Review any runtime-only missing/domain messages exposed by those tests. Do not
invent another large module or declare 100% from the managed tests alone.

## Earlier checkpoint — 2026-10-30, continued refactor

Estimated overall completion: 82%, not measured localization coverage. User asks
to continue autonomously until the real quota reserve: stop at 5% remaining in
the five-hour window OR 1% remaining weekly. Latest check: 40% / 6% remaining;
neither reserve has been reached. Do not stop merely because a UI step is done.

Release builds with SDK 10.0.403: no errors or warnings. Verify.ps1 passes 470/470
managed tests, localization metadata/placeholders and canonical package hashes.
The produced DLL remains version 0.75.0. No FFXIV window was available during
the native-window inventory; current rendering is NOT validated in game.

Completed since the earlier checkpoint: collection detail dialogs; quests,
rewards and item sources; maps/bookmarks; market, character and bank views;
Mini Cactpot, Triple Triad notebook/manual simulation/observation; main duty and
monster source views; crafting live/projection and notebook companion; main
recipe/preparation/chain UI; inventory sorting criteria/zones/templates; macro
sequence UI. Main user-facing labels have English/French resources, while some
dynamic strings and domain-generated messages listed below remain unfinished.

Settings now have General, Interface, Modules, Data and developer-only Debug
tabs. Observation preferences persist independently. The main window applies
its theme in PreDraw/PostDraw; item hover, bank import and crafting-log overlays
use a disposable theme scope. Bank frames wrap to the available width. The home
tool directory is now a compact grouped list of all 18 routes, with translated
names/descriptions, direct Open actions and translated details; reference plugin
names remain searchable. Craft projection sorting is cached by result and order.

PC-local timestamps replace ordinary UTC displays across migrated screens;
calculations and native observation timestamps still use UTC internally. Macro
observation text now correctly describes its saved preference. Invalid external
format overrides are rejected on reload, preserving bundled text or English
fallback; a regression test covers correction/reload and language retention.
A static scan found no missing literal localization.Get/Format reference keys;
that scan does not prove dynamic keys or domain messages are fully translated.

Remaining code work: finish purchase-estimate clipboard text, inventory-sort
zone tooltips, dynamic quest/triad/fashion snippets and shared/domain-generated
failure or model explanation text. Finish the main shell catalog/footer text.
Check labels without accented characters too; an accented-text search is only
a heuristic. Review small-window layouts and remaining per-frame allocations.
Then run Verify.ps1 once more after final changes, update this checkpoint and
validate actual ImGui layout/fonts/navigation/language switching in FFXIV when
available. German/Japanese remain partial with English fallback.

## Resumption — 2026-10-30

Updated user quota reserves: stop at 5% remaining in the five-hour window OR
1% remaining in the weekly window. These replace the earlier common 5% rule.
Collection acquisition labels and blue-spell, orchestrion, title and achievement
detail dialogs are now localized in English/French, with stable popup IDs and
developer-only internal identity tooltips. Runtime rendering remains unverified.
Overall completion estimate after these dialogs: 44%, not measured coverage.

Quota checked: five-hour window reset; weekly remaining 15% at start. Preserve
the user's 5% reserve on whichever window limits first.
Native monster source priority investigated using installed Lumina metadata:
see MONSTER-NATIVE-SOURCES-AUDIT.md. Named fields in the inspected tables do not
establish the missing Item 44106 → monster drop association. Do not invent links.
Overall UI refactor estimate remains 42%; this investigation is not a migrated UI.

## Checkpoint — 2026-10-29

User priority for resumption after quota reset: investigate native Dalamud/Lumina
data for item → monster → location associations. Reuse existing item/map services
and copy native data on the framework thread. Establish which relations actually
exist in the client; never infer drop links from matching names or numeric IDs.
Garland item 44106 (Épaule de rroneek) currently has no `drops` list in either
French or English response. Missing external data is not a parser failure.
Native monster names/locations alone do not prove an item drop association.
Do this investigation on resumption; no background continuation is configured.

Estimated overall completion: 42%, not an automated coverage measurement.
User requires stopping when the limiting quota has 5% remaining.

Since the original checkpoint: concise home with context shortcuts; category
subnavigation including Gold Saucer; English/French crafting presets, search,
condition ranges and macro export; gathering browser, locations, fishing log,
bait chains, fishing forecasts and weather text migrated. Collection browser tabs,
filters and summary migrated; detail popups remain incomplete.

Observation preferences now persist independently in Configuration.Observations;
legacy configs leave readers disabled. Local PC clock is displayed in the hub;
gathering, weather, fishing and collection observation dates use local time.
Internal timestamps and scheduling calculations remain UTC; Eorzea time remains
separate. Other screens still contain UTC display labels to migrate.

Languages are embedded as well as packaged. Startup uses the Dalamud-provided
plugin assembly directory for external files; DLL-only fallback is tested.
First launch selects the client language by metadata; explicit saved choices win.
JSON edits reload manually. Translation validator is callable using the README
command and runs in Verify.ps1. Partial de/ja translations are accepted.
Latest completed verification before this checkpoint: 467/467 tests, package
language hashes and localization validator passing. No in-game visual validation
of the current UI is claimed. Keep version/source changes made by other work.

Next: finish collection detail popups and state labels, then quests and maps.
Remaining gathering GP/scenario UI and crafting live/projection/overlays still
contain untranslated text; do not mark those categories complete. Finish settings
organization, common theme coverage, remaining time displays and runtime QA.

## Verified state

The shared palette and components live in `src/TenshoAssistant/UI`.
`LocalizationManager` loads JSON into memory; rendering uses cached translations,
language metadata and validation reports. Reload is explicit in developer settings.
English is the reference; French is maintained alongside each migrated screen.
German and Japanese are partial and use English fallback.

The navigation, selected settings, saved crafting sequences, simulation controls
and macro export have been migrated progressively. This is not a complete UI refactor.
The solution builds in Release with analyzers enabled. The managed test executable
reported 464/464 passing after the macro export migration. In-game rendering has
not been validated for these changes.

## Historical acceptance plan (superseded by current checkpoint)

- Replace the tool directory home with a concise status and activity view while retaining all tool routes.
- Finish crafting: solver, live projection, condition envelope, notebook, tooltips and model error presentation.
- Migrate gathering, fishing, collections, quests, maps, market, automation and character views in order.
- Keep Gold Saucer, duty loot, monster sources, bank, item uses and sorting reachable.
- Centralize settings into sections and finish separating developer details.
- Complete English/French extraction across remaining views and native overlays.
- Validate placeholders, invalid files, language persistence, fallback and runtime reload with targeted tests.
- Add a standalone contributor validator and document its exact invocation.
- Verify language JSON files are present in the distributed ZIP, not only the build directory.
- Validate fonts, small windows, long translations, all existing routes and performance in FFXIV.
- Remove obsolete UI only after reference checks and successful replacement.

Do not report completion based solely on a successful compilation or the existing
managed test suite. That suite does not exercise ImGui rendering or language files.
