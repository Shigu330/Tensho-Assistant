# Evidence · Preuves

Probe outputs and log excerpts cited by the code: each data link the plugin relies on points to one of these files (rule: no data is invented; a test checks the cited paths). The table below is produced by `tools/docgen.py` from the tracked files that name each file; a file cited nowhere stays as history.

Français : sorties de sondes et extraits de journaux cités par le code : chaque lien de données dont le plugin dépend renvoie à l'un de ces fichiers (règle : aucune donnée inventée ; un test vérifie les chemins cités). Le tableau ci-dessous est produit par `tools/docgen.py` depuis les fichiers suivis qui nomment chaque fichier ; un fichier cité nulle part reste comme historique.

<!-- docgen:evidence -->
| Evidence · Preuve | Code and tests · Code et tests | Documents |
|---|---|---|
| [0.1004-discoveries-check.md](0.1004-discoveries-check.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs` | `CHANGELOG.md` |
| [0.1006-submarine-functions.md](0.1006-submarine-functions.md) | `src/TenshoAssistant/FreeCompany/SubmarineVoyageProbe.Dalamud.cs` | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.1008-game-update-inventory.md](0.1008-game-update-inventory.md) | `tests/TenshoAssistant.Tests/GameUpdateInventory1008Tests.cs` | `CHANGELOG.md`, `docs/project/TESTING-ROUTE.md` |
| [0.1009-dalamud-api16-watch.md](0.1009-dalamud-api16-watch.md) | — | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.103.0-maps-probe.log](0.103.0-maps-probe.log) | `src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs` | `CHANGELOG.md` |
| [0.1038-game-font-glyphs.md](0.1038-game-font-glyphs.md) | `tests/TenshoAssistant.Tests/FontGlyphs1038Tests.cs`, `tools/README.md` | — |
| [0.105.0-zones-probe.log](0.105.0-zones-probe.log) | `src/TenshoAssistant/Maps/MapCatalog.cs`, `tests/TenshoAssistant.Tests/MapProjectionTests.cs` | `CHANGELOG.md` |
| [0.1050-first-frame-jit.md](0.1050-first-frame-jit.md) | `src/TenshoAssistant/Diagnostics/JitWarmup.cs` | — |
| [0.107.0-aetheryte-probe.log](0.107.0-aetheryte-probe.log) | `src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs` | `CHANGELOG.md`, `docs/project/MAP-PROPOSALS.md` |
| [0.114.0-local-probe.log](0.114.0-local-probe.log) | `src/TenshoAssistant/Maps/MapLocalIndex.cs`, `tests/TenshoAssistant.Tests/MapLocalIndexTests.cs` | `CHANGELOG.md` |
| [0.126.0-fishing-probe.log](0.126.0-fishing-probe.log) | `src/TenshoAssistant/MainWindow.GatheringGoTo.cs`, `src/TenshoAssistant/Maps/MapLocalIndex.cs` | `CHANGELOG.md` |
| [0.127.0-sightseeing-probe.log](0.127.0-sightseeing-probe.log) | `src/TenshoAssistant/Maps/MapLocalIndex.cs`, `tests/TenshoAssistant.Tests/MapLocalIndexTests.cs` | `CHANGELOG.md` |
| [0.129.0-sightseeing-probe.log](0.129.0-sightseeing-probe.log) | `tests/TenshoAssistant.Tests/WikiVistaTests.cs` | `CHANGELOG.md` |
| [0.133.0-crash-excerpt.log](0.133.0-crash-excerpt.log) | — | `CHANGELOG.md`, `docs/project/ARCHITECTURE.md` |
| [0.135.0-marker-evidence.log](0.135.0-marker-evidence.log) | — | `CHANGELOG.md`, `docs/project/MAP-PROPOSALS.md` |
| [0.136.0-marker-evidence.log](0.136.0-marker-evidence.log) | — | `CHANGELOG.md`, `docs/project/MAP-PROPOSALS.md` |
| [0.139.0-symbols-probe.log](0.139.0-symbols-probe.log) | `src/TenshoAssistant/MainWindow.MapView.cs` | `CHANGELOG.md` |
| [0.141.0-itemaction-probe.log](0.141.0-itemaction-probe.log) | `src/TenshoAssistant/Items/ItemCatalog.cs`, `src/TenshoAssistant/Items/ItemUnlockCopy.Lumina.cs` | `CHANGELOG.md` |
| [0.145.0-cache-before.log](0.145.0-cache-before.log) | — | `CHANGELOG.md` |
| [0.146.0-achievement-probe.log](0.146.0-achievement-probe.log) | — | `CHANGELOG.md` |
| [0.151.0-questicon-probe.log](0.151.0-questicon-probe.log) | `src/TenshoAssistant/Maps/MapLocalIndexLoader.Dalamud.cs` | — |
| [0.155.0-vendor-markers-probe.log](0.155.0-vendor-markers-probe.log) | `src/TenshoAssistant/Maps/MapCatalog.cs`, `tests/TenshoAssistant.Tests/MapProjectionTests.cs` | `CHANGELOG.md` |
| [0.159.0-triad-npc-probe.log](0.159.0-triad-npc-probe.log) | `src/TenshoAssistant/MiniGames/TriadMatchAdvisor.cs`, `src/TenshoAssistant/MiniGames/TriadNpcCatalog.cs`, `src/TenshoAssistant/MiniGames/TriadObservation.cs`, `src/TenshoAssistant/MiniGames/TriadObservationReader.Dalamud.cs`, `tests/TenshoAssistant.Tests/TriadMatchAdvisorTests.cs`, `tests/TenshoAssistant.Tests/TriadMatchSimulatorTests.cs` | `CHANGELOG.md` |
| [0.160.0-triad-match-capture.log](0.160.0-triad-match-capture.log) | `src/TenshoAssistant/MiniGames/TriadObservation.cs`, `src/TenshoAssistant/MiniGames/TriadObservationReader.Dalamud.cs`, `tests/TenshoAssistant.Tests/TriadMatchAdvisorTests.cs` | `CHANGELOG.md` |
| [0.163.0-triad-editdeck-capture.log](0.163.0-triad-editdeck-capture.log) | `src/TenshoAssistant/MiniGames/TriadObservation.cs` | `CHANGELOG.md` |
| [0.168.0-triad-sources-probe.log](0.168.0-triad-sources-probe.log) | `src/TenshoAssistant/MiniGames/TriadNpcCatalog.cs` | `CHANGELOG.md` |
| [0.171.0-cosmic-vendors-probe.log](0.171.0-cosmic-vendors-probe.log) | `tests/TenshoAssistant.Tests/MapProjectionTests.cs` | `CHANGELOG.md` |
| [0.174.0-triad-seldeck-capture.log](0.174.0-triad-seldeck-capture.log) | — | `CHANGELOG.md` |
| [0.175.0-auxesia-maps-probe.log](0.175.0-auxesia-maps-probe.log) | `src/TenshoAssistant/Maps/MapCatalog.cs` | `CHANGELOG.md` |
| [0.176.0-kaede-talk-probe.log](0.176.0-kaede-talk-probe.log) | `src/TenshoAssistant/Npcs/NpcVendorCatalog.cs` | `CHANGELOG.md` |
| [0.180.0-achievement-probe.log](0.180.0-achievement-probe.log) | `src/TenshoAssistant/Collections/CollectionReader.Completion.Dalamud.cs` | `CHANGELOG.md` |
| [0.182.0-paramgrow-probe.log](0.182.0-paramgrow-probe.log) | `src/TenshoAssistant/Character/StatFormulas.cs`, `tests/TenshoAssistant.Tests/StatFormulaTests.cs` | `CHANGELOG.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md` |
| [0.199-adenium-shop.md](0.199-adenium-shop.md) | — | `CHANGELOG.md` |
| [0.201-agent-alarm.md](0.201-agent-alarm.md) | — | `CHANGELOG.md` |
| [0.203-gathering-jobs.md](0.203-gathering-jobs.md) | `src/TenshoAssistant/Alarms/TenshoAlarm.cs` | `CHANGELOG.md`, `docs/project/CRAFT-GATHER-PROPOSALS.md` |
| [0.205-alarm-command.md](0.205-alarm-command.md) | `src/TenshoAssistant/Alarms/GameAlarmCommand.cs` | `CHANGELOG.md`, `docs/project/TOOLS-PROPOSALS.md` |
| [0.211-event-icons.md](0.211-event-icons.md) | `src/TenshoAssistant/Maps/MapMarkerSource.Dalamud.cs` | `CHANGELOG.md`, `docs/project/MAP-PROPOSALS.md`, `docs/project/ROADMAP.md`, `docs/project/archive/DECISIONS-EN-ATTENTE-2026-11-09.md` |
| [0.224-bountiful-base.md](0.224-bountiful-base.md) | `src/TenshoAssistant/Gathering/BountifulEstimate.cs`, `src/TenshoAssistant/MainWindow.GatheringGp.cs`, `src/TenshoAssistant/Plugin.cs`, `tests/TenshoAssistant.Tests/GpOptimizerTests.cs` | `CHANGELOG.md`, `docs/project/BACKLOG.md`, `docs/project/DECISIONS.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md`, `docs/project/archive/DECISIONS-EN-ATTENTE-2026-11-09.md` |
| [0.226-fishing-spot-coordinates.md](0.226-fishing-spot-coordinates.md) | — | `CHANGELOG.md`, `docs/project/ROADMAP.md` |
| [0.231-teamcraft-bite-times.md](0.231-teamcraft-bite-times.md) | — | `CHANGELOG.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md` |
| [0.246-cache-audit.md](0.246-cache-audit.md) | — | `CHANGELOG.md`, `docs/project/ARCHITECTURE.md`, `docs/project/DECISIONS.md`, `docs/project/ITEMS-PROPOSALS.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md`, `docs/project/USER-REQUESTS.md` |
| [0.246-r05-window-order.md](0.246-r05-window-order.md) | — | `docs/project/DECISIONS.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md`, `docs/project/USER-REQUESTS.md` |
| [0.270-universalis-listings.md](0.270-universalis-listings.md) | `src/TenshoAssistant/Market/MarketBoard.cs`, `tests/TenshoAssistant.Tests/Program.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md`, `docs/project/ITEMS-PROPOSALS.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md` |
| [0.278-triad-ex-cards.md](0.278-triad-ex-cards.md) | `src/TenshoAssistant/Collections/CollectionReader.Dalamud.cs`, `tests/TenshoAssistant.Tests/Program.cs` | `CHANGELOG.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md` |
| [0.283-quest-journal-sections.md](0.283-quest-journal-sections.md) | `src/TenshoAssistant/Quests/QuestService.cs` | `CHANGELOG.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md` |
| [0.285-quest-start-cities.md](0.285-quest-start-cities.md) | `src/TenshoAssistant/MainWindow.Quests.cs`, `src/TenshoAssistant/Quests/QuestStartCities.cs` | `CHANGELOG.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md` |
| [0.286-quest-alternatives.md](0.286-quest-alternatives.md) | `src/TenshoAssistant/Quests/QuestAlternatives.cs` | `CHANGELOG.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md` |
| [0.292-draw-times.md](0.292-draw-times.md) | — | `CHANGELOG.md`, `docs/project/ITEMS-PROPOSALS.md`, `docs/project/MAP-PROPOSALS.md`, `docs/project/REFERENCES.md` |
| [0.295-fcc-shops.md](0.295-fcc-shops.md) | `src/TenshoAssistant/Npcs/NpcVendorCatalog.cs`, `src/TenshoAssistant/Npcs/NpcVendorLoader.Dalamud.cs` | `CHANGELOG.md`, `docs/project/BACKLOG.md`, `docs/project/REFERENCES.md` |
| [0.300-calendar-resets.md](0.300-calendar-resets.md) | `src/TenshoAssistant/Reminders/ResetSchedule.cs` | `CHANGELOG.md`, `docs/project/REFERENCES.md`, `docs/project/TOOLS-PROPOSALS.md` |
| [0.307-fc-voyages.md](0.307-fc-voyages.md) | — | `CHANGELOG.md`, `docs/project/REFERENCES.md`, `docs/project/TOOLS-PROPOSALS.md` |
| [0.309-submarine-routes.md](0.309-submarine-routes.md) | `src/TenshoAssistant/FreeCompany/VoyagePlanner.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md`, `docs/project/REFERENCES.md` |
| [0.310-gear-sheets.md](0.310-gear-sheets.md) | `src/TenshoAssistant/Gear/GearCatalog.cs`, `src/TenshoAssistant/Gear/GearLoader.Dalamud.cs` | `CHANGELOG.md`, `docs/project/REFERENCES.md` |
| [0.312-materia.md](0.312-materia.md) | `src/TenshoAssistant/Gear/GearCatalog.cs`, `src/TenshoAssistant/Gear/GearLoader.Dalamud.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md`, `docs/project/archive/DECISIONS-EN-ATTENTE-2026-11-09.md` |
| [0.315-vessel-builds.md](0.315-vessel-builds.md) | `src/TenshoAssistant/FreeCompany/VesselBuilds.cs`, `src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs`, `tests/TenshoAssistant.Tests/Program.cs`, `tests/TenshoAssistant.Tests/Sectors334Tests.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md`, `docs/project/REFERENCES.md`, `docs/project/ROADMAP.md`, `docs/project/archive/DECISIONS-EN-ATTENTE-2026-11-09.md` |
| [0.318-plugin-gaps.json](0.318-plugin-gaps.json) | — | `CHANGELOG.md`, `docs/project/CRAFT-GATHER-PROPOSALS.md`, `docs/project/DECISIONS.md`, `docs/project/GAPS.md`, `docs/project/ITEMS-PROPOSALS.md`, `docs/project/MAP-PROPOSALS.md` |
| [0.320-credits.json](0.320-credits.json) | `src/TenshoAssistant/Credits/CreditCatalog.cs` | `CHANGELOG.md` |
| [0.332-squadron-probe.md](0.332-squadron-probe.md) | `src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs`, `tests/TenshoAssistant.Tests/SquadronTests.cs` | `CHANGELOG.md`, `docs/project/archive/DECISIONS-EN-ATTENTE-2026-11-09.md` |
| [0.332-uld-window-frame.md](0.332-uld-window-frame.md) | `tools/README.md` | `CHANGELOG.md` |
| [0.333-ehcatl-probe.md](0.333-ehcatl-probe.md) | — | `CHANGELOG.md` |
| [0.335-submarine-tiers.md](0.335-submarine-tiers.md) | `tests/TenshoAssistant.Tests/Routes335Tests.cs` | `CHANGELOG.md` |
| [0.336-airship-sectors.md](0.336-airship-sectors.md) | `src/TenshoAssistant/FreeCompany/AirshipPlanner.cs`, `src/TenshoAssistant/FreeCompany/AirshipSectors.cs`, `src/TenshoAssistant/FreeCompany/AirshipVoyageTable.Dalamud.cs` | `CHANGELOG.md` |
| [0.338-overmeld-rates.md](0.338-overmeld-rates.md) | `src/TenshoAssistant/Gear/GearCatalog.cs`, `tests/TenshoAssistant.Tests/Overmeld338Tests.cs` | `CHANGELOG.md` |
| [0.340-chocobo-barding.md](0.340-chocobo-barding.md) | `src/TenshoAssistant/Gear/ChocoboBarding.cs` | `CHANGELOG.md`, `docs/project/archive/DECISIONS-EN-ATTENTE-2026-11-09.md` |
| [0.342-festivals.md](0.342-festivals.md) | `src/TenshoAssistant/Events/Festivals.cs`, `src/TenshoAssistant/MainWindow.Events.cs` | `CHANGELOG.md` |
| [0.344-currencies.md](0.344-currencies.md) | `src/TenshoAssistant/Inventory/Currencies.cs` | `CHANGELOG.md` |
| [0.356-roulettes.md](0.356-roulettes.md) | `src/TenshoAssistant/Plugin.cs`, `src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs`, `tests/TenshoAssistant.Tests/Roulettes356Tests.cs` | `CHANGELOG.md`, `docs/project/TESTING.md` |
| [0.357-armoire.md](0.357-armoire.md) | — | `CHANGELOG.md` |
| [0.363-leves.md](0.363-leves.md) | — | `CHANGELOG.md` |
| [0.364-hunting-log.md](0.364-hunting-log.md) | `src/TenshoAssistant/Hunting/HuntingLog.cs`, `tests/TenshoAssistant.Tests/HuntTests.cs` | `CHANGELOG.md`, `docs/project/TESTING.md` |
| [0.367-blue-spell-wiki.md](0.367-blue-spell-wiki.md) | — | `CHANGELOG.md` |
| [0.368-achievement-progress.md](0.368-achievement-progress.md) | — | `CHANGELOG.md` |
| [0.369-titles.md](0.369-titles.md) | — | `CHANGELOG.md` |
| [0.370-orchestrion-player.md](0.370-orchestrion-player.md) | — | `CHANGELOG.md` |
| [0.372-weekly-counters.md](0.372-weekly-counters.md) | — | `CHANGELOG.md` |
| [0.373-chat-map-links.md](0.373-chat-map-links.md) | — | `CHANGELOG.md` |
| [0.376-gear-wear.md](0.376-gear-wear.md) | `src/TenshoAssistant/Gear/GearWear.cs` | `CHANGELOG.md` |
| [0.382-counters.md](0.382-counters.md) | `src/TenshoAssistant/Plugin.cs` | `CHANGELOG.md` |
| [0.383-gearsets.md](0.383-gearsets.md) | — | `CHANGELOG.md` |
| [0.384-treasure-map.md](0.384-treasure-map.md) | `src/TenshoAssistant/Maps/TreasureMapReader.Dalamud.cs` | `CHANGELOG.md` |
| [0.387-blue-spell-name.md](0.387-blue-spell-name.md) | `tests/TenshoAssistant.Tests/BlueMagicTests.cs` | `CHANGELOG.md` |
| [0.387-collection-items.md](0.387-collection-items.md) | `src/TenshoAssistant/Collections/ExtraCollectionReader.Dalamud.cs` | `CHANGELOG.md` |
| [0.392-ocean-fishing.md](0.392-ocean-fishing.md) | `src/TenshoAssistant/Gathering/OceanFishing.cs` | `CHANGELOG.md` |
| [0.395-blue-spellbook.md](0.395-blue-spellbook.md) | — | `CHANGELOG.md` |
| [0.398-fishing-bait.md](0.398-fishing-bait.md) | — | `CHANGELOG.md` |
| [0.402-notebooks.md](0.402-notebooks.md) | `src/TenshoAssistant/Gathering/NotebookReader.Dalamud.cs` | `CHANGELOG.md` |
| [0.405-game-logs.md](0.405-game-logs.md) | — | `CHANGELOG.md` |
| [0.406-languages.md](0.406-languages.md) | — | `CHANGELOG.md` |
| [0.409-collectables.md](0.409-collectables.md) | `src/TenshoAssistant/Gathering/Collectables.cs`, `src/TenshoAssistant/MainWindow.CollectableTable.cs`, `tests/TenshoAssistant.Tests/CollectablesTests.cs` | `CHANGELOG.md` |
| [0.410-gp-actions.md](0.410-gp-actions.md) | `src/TenshoAssistant/Gathering/CollectablePlan.cs`, `src/TenshoAssistant/Gathering/GpActions.cs`, `src/TenshoAssistant/Plugin.cs` | `CHANGELOG.md` |
| [0.411-gathering-jobs.md](0.411-gathering-jobs.md) | `src/TenshoAssistant/Gear/Gearsets.cs`, `tests/TenshoAssistant.Tests/GatheringGearset411Tests.cs` | `CHANGELOG.md` |
| [0.417-spearfishing.md](0.417-spearfishing.md) | `src/TenshoAssistant/Gathering/Spearfishing.cs`, `tests/TenshoAssistant.Tests/SpearfishingTests.cs`, `tools/DiscoveryCheckProbe/Program.cs` | `CHANGELOG.md` |
| [0.418-ocean-voyage.md](0.418-ocean-voyage.md) | `src/TenshoAssistant/Gathering/OceanVoyage.cs`, `tests/TenshoAssistant.Tests/OceanVoyageTests.cs` | `CHANGELOG.md` |
| [0.420-craft-food.md](0.420-craft-food.md) | `src/TenshoAssistant/Craft/CraftConsumables.cs`, `tests/TenshoAssistant.Tests/CraftConsumables420Tests.cs` | `CHANGELOG.md` |
| [0.434-craft-actions.md](0.434-craft-actions.md) | `src/TenshoAssistant/Craft/CraftActionCatalog.cs`, `src/TenshoAssistant/Craft/CraftSimulation.cs`, `tests/TenshoAssistant.Tests/CraftActions434Tests.cs` | `CHANGELOG.md` |
| [0.440-hunt-bills.md](0.440-hunt-bills.md) | `src/TenshoAssistant/Reminders/HuntBills.cs`, `tests/TenshoAssistant.Tests/HuntTests.cs` | `CHANGELOG.md` |
| [0.441-hunt-marks.md](0.441-hunt-marks.md) | `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs` | `CHANGELOG.md` |
| [0.443-bingo-tasks.md](0.443-bingo-tasks.md) | — | `CHANGELOG.md` |
| [0.444-housing.md](0.444-housing.md) | `src/TenshoAssistant/Maps/PlaceDirectory.cs` | `CHANGELOG.md` |
| [0.448-n06-n14-sources.md](0.448-n06-n14-sources.md) | `src/TenshoAssistant/MainWindow.Squadron.cs`, `src/TenshoAssistant/Reminders/SquadronPlanner.cs`, `tools/garden-data.py` | `CHANGELOG.md`, `docs/project/DECISIONS.md`, `docs/project/G-PROPOSALS.md` |
| [0.451-raphael-study.md](0.451-raphael-study.md) | `src/TenshoAssistant/Craft/RaphaelActions.cs`, `src/TenshoAssistant/Craft/RaphaelCraftSolver.Dalamud.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md` |
| [0.451-xivapi-v2.md](0.451-xivapi-v2.md) | — | `docs/project/PUBLISHING.md` |
| [0.452-craft-buff-rows.md](0.452-craft-buff-rows.md) | `tests/TenshoAssistant.Tests/ResilienceTests.cs` | `CHANGELOG.md` |
| [0.456-garden-grid.md](0.456-garden-grid.md) | `src/TenshoAssistant/Credits/CreditCatalog.cs`, `src/TenshoAssistant/MainWindow.GardenCrosses.cs`, `src/TenshoAssistant/Reminders/GardenCrosses.cs`, `tests/TenshoAssistant.Tests/GardenCrossesTests.cs` | `CHANGELOG.md` |
| [0.459-garden-detection.md](0.459-garden-detection.md) | `src/TenshoAssistant/Reminders/GardenChat.cs`, `tests/TenshoAssistant.Tests/GardenJournalTests.cs` | `CHANGELOG.md` |
| [0.463-squadron-probe.log](0.463-squadron-probe.log) | — | `CHANGELOG.md` |
| [0.478-collectable-truncated-name.md](0.478-collectable-truncated-name.md) | `src/TenshoAssistant/Gathering/Collectables.cs` | `CHANGELOG.md` |
| [0.479-collectable-window-values.md](0.479-collectable-window-values.md) | `src/TenshoAssistant/Gathering/CollectablePlan.cs`, `src/TenshoAssistant/Plugin.cs`, `tests/TenshoAssistant.Tests/CollectablesTests.cs` | `CHANGELOG.md` |
| [0.488-gp-regen-traits.md](0.488-gp-regen-traits.md) | `src/TenshoAssistant/Gathering/RegenTick.cs` | `CHANGELOG.md` |
| [0.493-triad-npc-requirements.md](0.493-triad-npc-requirements.md) | `src/TenshoAssistant/MiniGames/TriadNpcCatalog.cs`, `src/TenshoAssistant/MiniGames/TriadNpcLoader.Dalamud.cs` | `CHANGELOG.md`, `docs/project/TESTING.md` |
| [0.494-selfcheck-first-pass.log](0.494-selfcheck-first-pass.log) | — | `CHANGELOG.md` |
| [0.498-xbm-sheets.md](0.498-xbm-sheets.md) | — | — |
| [0.500-xbm-columns.md](0.500-xbm-columns.md) | `src/TenshoAssistant/Beastmaster/BeastCatalog.cs`, `tests/TenshoAssistant.Tests/BeastCatalogTests.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md` |
| [0.508-retainer-sales.md](0.508-retainer-sales.md) | `src/TenshoAssistant/Market/RetainerSalesReader.Dalamud.cs` | `CHANGELOG.md`, `docs/project/TESTING.md` |
| [0.510-triad-competitive.md](0.510-triad-competitive.md) | `src/TenshoAssistant/MiniGames/TriadMatchAdvisor.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md` |
| [0.512-d17-build.md](0.512-d17-build.md) | `.github/workflows/build.yml` | `CHANGELOG.md`, `CONTRIBUTING.md`, `docs/project/DECISIONS.md`, `docs/project/PUBLISHING.md`, `docs/project/TESTING.md`, `publish/github/README.md`, `publish/github/workflows/build.yml` |
| [0.515-node-effects.md](0.515-node-effects.md) | `src/TenshoAssistant/Gathering/GatherNodeEffects.cs`, `src/TenshoAssistant/Gathering/GatheringObservationReader.Dalamud.cs`, `src/TenshoAssistant/Plugin.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md` |
| [0.517-gp-action-unlocks.md](0.517-gp-action-unlocks.md) | `src/TenshoAssistant/Plugin.cs` | `CHANGELOG.md` |
| [0.518-gather-tomes.md](0.518-gather-tomes.md) | `src/TenshoAssistant/Gathering/GatherKnowledgeReader.Dalamud.cs`, `src/TenshoAssistant/Gathering/GatherPointAccess.cs` | `CHANGELOG.md` |
| [0.519-crystal-actions.md](0.519-crystal-actions.md) | `src/TenshoAssistant/Gathering/GpCrystals.cs` | `CHANGELOG.md` |
| [0.525-load-cost.md](0.525-load-cost.md) | — | `CHANGELOG.md` |
| [0.526-folklore-tomes.md](0.526-folklore-tomes.md) | `src/TenshoAssistant/Collections/ExtraCollectionReader.Dalamud.cs`, `src/TenshoAssistant/Gathering/GatherPointAccess.cs` | `CHANGELOG.md` |
| [0.527-priming-touch.md](0.527-priming-touch.md) | — | `CHANGELOG.md` |
| [0.537-game-windows.md](0.537-game-windows.md) | `src/TenshoAssistant/UI/GameWindow.cs` | `CHANGELOG.md` |
| [0.540-cornflower-seed.md](0.540-cornflower-seed.md) | `tools/garden-data.py` | `CHANGELOG.md` |
| [0.585-classjob-empty-rows.md](0.585-classjob-empty-rows.md) | — | `CHANGELOG.md` |
| [0.586-flight-sets.md](0.586-flight-sets.md) | `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs` | `CHANGELOG.md` |
| [0.596-map-label-orientation.md](0.596-map-label-orientation.md) | `src/TenshoAssistant/Maps/MapLabels.cs`, `tests/TenshoAssistant.Tests/MapLabels596Tests.cs` | `CHANGELOG.md` |
| [0.600-competitive-inventory.md](0.600-competitive-inventory.md) | `src/TenshoAssistant/Competitive/CompetitiveZones.cs` | `docs/project/COMPETITIVE-OFF-PLAN.md`, `docs/project/DECISIONS.md` |
| [0.608-universalis-batch.md](0.608-universalis-batch.md) | `src/TenshoAssistant/Market/MarketService.cs`, `tests/TenshoAssistant.Tests/MarketServiceTests.cs` | `CHANGELOG.md` |
| [0.628-quest-target-on-world-map.md](0.628-quest-target-on-world-map.md) | `src/TenshoAssistant/Maps/MapCatalog.cs` | — |
| [0.631-region-zone-images.md](0.631-region-zone-images.md) | `src/TenshoAssistant/Maps/MapLabels.cs` | — |
| [0.633-quest-route.md](0.633-quest-route.md) | `src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs` | — |
| [0.634-dungeon-entrances.md](0.634-dungeon-entrances.md) | `src/TenshoAssistant/Maps/MapCatalog.cs`, `src/TenshoAssistant/Maps/MapCatalogLoader.Dalamud.cs` | — |
| [0.639-instance-markers.md](0.639-instance-markers.md) | `src/TenshoAssistant/Maps/MapLabels.cs` | — |
| [0.641-zone-menu.md](0.641-zone-menu.md) | `src/TenshoAssistant/Maps/MapCatalog.cs` | — |
| [0.647-gather-point-kinds.md](0.647-gather-point-kinds.md) | `src/TenshoAssistant/Gathering/GatherPointAccess.cs` | — |
| [0.648-duty-unlocks.md](0.648-duty-unlocks.md) | `src/TenshoAssistant/Duties/DutyCatalog.cs` | — |
| [0.649-spearfishing-sources.md](0.649-spearfishing-sources.md) | `src/TenshoAssistant/Gathering/GatheringCatalogLoader.Dalamud.cs` | — |
| [0.650-gp-while-gathering.md](0.650-gp-while-gathering.md) | — | `docs/project/GLOBAL-PLAN.md` |
| [0.651-company-actions.md](0.651-company-actions.md) | `src/TenshoAssistant/Reminders/CompanyBenefits.cs` | — |
| [0.652-leve-markers.md](0.652-leve-markers.md) | `src/TenshoAssistant/Maps/MapLabels.cs` | `docs/project/BACKLOG.md` |
| [0.653-markers-during-journal-request.md](0.653-markers-during-journal-request.md) | `src/TenshoAssistant/Maps/MapLabels.cs` | — |
| [0.654-leve-area-memory.md](0.654-leve-area-memory.md) | `src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.656-gp-alert-teleport.md](0.656-gp-alert-teleport.md) | `src/TenshoAssistant/Gathering/RegenTick.cs`, `src/TenshoAssistant/Gathering/RegenTickReader.Dalamud.cs` | — |
| [0.657-leve-area-from-journal.md](0.657-leve-area-from-journal.md) | `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs` | — |
| [0.658-map-after-teleport.md](0.658-map-after-teleport.md) | `src/TenshoAssistant/Maps/MapResume.cs` | — |
| [0.660-map-name-sizes.md](0.660-map-name-sizes.md) | `src/TenshoAssistant/Maps/MapLabels.cs` | — |
| [0.662-teleport-list-crash.md](0.662-teleport-list-crash.md) | `src/TenshoAssistant/Maps/MapResume.cs` | — |
| [0.663-intuition.md](0.663-intuition.md) | `src/TenshoAssistant/Community/GatherBuddyFish.cs` | — |
| [0.664-workshop-projects.md](0.664-workshop-projects.md) | `src/TenshoAssistant/FreeCompany/WorkshopProjects.cs` | — |
| [0.665-quest-icon-variants.md](0.665-quest-icon-variants.md) | `src/TenshoAssistant/Maps/MapLabels.cs` | — |
| [0.666-adjusted-recipes.md](0.666-adjusted-recipes.md) | `src/TenshoAssistant/Craft/CraftSimulation.cs`, `src/TenshoAssistant/Craft/RecipeCatalogLoader.Dalamud.cs` | — |
| [0.667-island-resources.md](0.667-island-resources.md) | `src/TenshoAssistant/Maps/IslandResources.cs` | — |
| [0.668-hq-chance.md](0.668-hq-chance.md) | `src/TenshoAssistant/Craft/CraftSimulation.cs` | — |
| [0.669-starting-quality.md](0.669-starting-quality.md) | `src/TenshoAssistant/Craft/CraftSimulation.cs` | — |
| [0.670-srank-conditions.md](0.670-srank-conditions.md) | `src/TenshoAssistant/Hunting/SRankConditions.cs`, `src/TenshoAssistant/MainWindow.SRankConditions.cs` | — |
| [0.671-island-workshop.md](0.671-island-workshop.md) | `src/TenshoAssistant/Island/IslandCatalog.cs`, `src/TenshoAssistant/MainWindow.IslandSanctuary.cs` | `docs/project/G-PROPOSALS.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.672-island-agenda.md](0.672-island-agenda.md) | `src/TenshoAssistant/Island/IslandPlanner.cs` | — |
| [0.673-island-materials.md](0.673-island-materials.md) | `src/TenshoAssistant/Island/IslandMaterials.cs` | `docs/project/G-PROPOSALS.md` |
| [0.674-island-groove.md](0.674-island-groove.md) | `src/TenshoAssistant/Island/IslandPlanner.cs` | — |
| [0.675-custom-deliveries.md](0.675-custom-deliveries.md) | `src/TenshoAssistant/Deliveries/CustomDeliveries.cs`, `src/TenshoAssistant/MainWindow.CustomDeliveries.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.678-felicite-terms.md](0.678-felicite-terms.md) | — | — |
| [0.679-island-map.md](0.679-island-map.md) | `src/TenshoAssistant/Npcs/NpcVendorCatalog.cs` | — |
| [0.683-masked-carnivale.md](0.683-masked-carnivale.md) | `src/TenshoAssistant/Carnivale/MaskedCarnivale.cs`, `src/TenshoAssistant/MainWindow.MaskedCarnivale.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.684-island-kinds.md](0.684-island-kinds.md) | `src/TenshoAssistant/Maps/IslandResources.cs` | — |
| [0.685-island-animals.md](0.685-island-animals.md) | `src/TenshoAssistant/Island/IslandAnimals.cs`, `src/TenshoAssistant/MainWindow.IslandPasture.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.686-island-workshops.md](0.686-island-workshops.md) | `src/TenshoAssistant/Island/IslandCatalog.cs` | — |
| [0.688-beast-places.md](0.688-beast-places.md) | `src/TenshoAssistant/Beastmaster/BeastCatalog.cs` | — |
| [0.694-workshop-sets.md](0.694-workshop-sets.md) | `src/TenshoAssistant/FreeCompany/WorkshopProjects.cs` | — |
| [0.704-place-menus.md](0.704-place-menus.md) | — | — |
| [0.708-relic-weapons.md](0.708-relic-weapons.md) | `src/TenshoAssistant/MainWindow.RelicWeapons.cs`, `src/TenshoAssistant/Relics/RelicWeapons.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.710-market-pace.md](0.710-market-pace.md) | `src/TenshoAssistant/Market/MarketService.cs` | — |
| [0.713-allied-societies.md](0.713-allied-societies.md) | `src/TenshoAssistant/MainWindow.AlliedSocieties.cs`, `src/TenshoAssistant/Societies/AlliedSocieties.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.714-grand-company.md](0.714-grand-company.md) | `src/TenshoAssistant/GrandCompanies/GrandCompanies.cs`, `src/TenshoAssistant/MainWindow.GrandCompanies.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.715-special-deliveries.md](0.715-special-deliveries.md) | `src/TenshoAssistant/Deliveries/SpecialDeliveries.cs`, `src/TenshoAssistant/MainWindow.SpecialDeliveries.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.716-relic-families.md](0.716-relic-families.md) | `src/TenshoAssistant/Relics/RelicWeapons.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.717-eureka-bozja.md](0.717-eureka-bozja.md) | `src/TenshoAssistant/Fields/FieldBoard.Dalamud.cs`, `src/TenshoAssistant/Fields/FieldBoard.cs`, `src/TenshoAssistant/MainWindow.FieldContent.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.718-deep-dungeons.md](0.718-deep-dungeons.md) | `src/TenshoAssistant/DeepDungeons/DeepDungeonBoard.cs`, `src/TenshoAssistant/MainWindow.DeepDungeons.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.719-lexicon.md](0.719-lexicon.md) | `src/TenshoAssistant/Lexicon/LexiconBoard.cs`, `src/TenshoAssistant/MainWindow.Lexicon.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.720-relic-armour.md](0.720-relic-armour.md) | `src/TenshoAssistant/MainWindow.RelicWeapons.cs`, `src/TenshoAssistant/Relics/RelicWeapons.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.721-fates.md](0.721-fates.md) | `src/TenshoAssistant/Fields/FieldBoard.cs`, `src/TenshoAssistant/MainWindow.Fates.cs`, `tools/DiscoveryCheckProbe/Program.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.722-selfcheck-pages.md](0.722-selfcheck-pages.md) | `src/TenshoAssistant/MainWindow.SelfCheckPages.cs` | — |
| [0.724-page-sweep.md](0.724-page-sweep.md) | `src/TenshoAssistant/Diagnostics/PageSweepWindow.Dalamud.cs`, `src/TenshoAssistant/MainWindow.GatheringBrowser.cs` | `docs/project/DECISIONS.md` |
| [0.725-sales-speed.md](0.725-sales-speed.md) | `src/TenshoAssistant/Market/MarketQuote.cs` | — |
| [0.728-gather-next.md](0.728-gather-next.md) | `src/TenshoAssistant/Gathering/ActionBarLocator.Dalamud.cs`, `src/TenshoAssistant/Gathering/GpNextAction.cs` | — |
| [0.73.0-native-craft-probe.log](0.73.0-native-craft-probe.log) | — | — |
| [0.730-delivery-rewards.md](0.730-delivery-rewards.md) | `src/TenshoAssistant/Deliveries/CustomDeliveries.cs`, `src/TenshoAssistant/MainWindow.CustomDeliveries.cs`, `tests/TenshoAssistant.Tests/DeliveryRewards730Tests.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.74.0-native-craft-probe.log](0.74.0-native-craft-probe.log) | — | — |
| [0.740-list-blink.md](0.740-list-blink.md) | `src/TenshoAssistant/Inventory/InventoryService.cs`, `src/TenshoAssistant/MainWindow.ListPanels.cs` | — |
| [0.75.0-config-json-probe.log](0.75.0-config-json-probe.log) | — | `CHANGELOG.md` |
| [0.766-map-discovery.md](0.766-map-discovery.md) | `tools/README.md` | `docs/project/GLOBAL-PLAN.md` |
| [0.767-retainer-gil-crystals.md](0.767-retainer-gil-crystals.md) | `src/TenshoAssistant/Inventory/BankModels.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.782-raphael-freeze.md](0.782-raphael-freeze.md) | — | `docs/project/GLOBAL-PLAN.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.787-workshop-delivered.md](0.787-workshop-delivered.md) | `src/TenshoAssistant/FreeCompany/WorkshopDeliveryReader.Dalamud.cs`, `src/TenshoAssistant/FreeCompany/WorkshopProjects.cs`, `tests/TenshoAssistant.Tests/WorkshopDelivered787Tests.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.793-game-leve-markers.md](0.793-game-leve-markers.md) | `src/TenshoAssistant/Maps/MapLabels.cs`, `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs` | `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.803-map-discovery-layout.md](0.803-map-discovery-layout.md) | `src/TenshoAssistant/Maps/MapDiscovery.cs`, `tests/TenshoAssistant.Tests/MapDiscovery803Tests.cs`, `tools/README.md` | `docs/project/GLOBAL-PLAN.md` |
| [0.804-beast-wiki.md](0.804-beast-wiki.md) | `src/TenshoAssistant/Monsters/BeastWikiIndex.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.811-conditional-npcs.md](0.811-conditional-npcs.md) | `src/TenshoAssistant/Npcs/NpcVendorCatalog.cs` | `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.812-alarm-link.md](0.812-alarm-link.md) | `src/TenshoAssistant/Plugin.cs` | `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.833-zone-entries.md](0.833-zone-entries.md) | `src/TenshoAssistant/Maps/ZoneEntries.cs` | `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.834-hidden-gathering-items.md](0.834-hidden-gathering-items.md) | `src/TenshoAssistant/MainWindow.DutyFinder.cs` | `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.848-quest-markers-current.md](0.848-quest-markers-current.md) | — | `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.849-height-chevron.md](0.849-height-chevron.md) | `src/TenshoAssistant/Maps/MapHeight.cs` | `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.857-community-map.md](0.857-community-map.md) | `src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs`, `src/TenshoAssistant/Market/SeenPrices.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.866-credit-references.md](0.866-credit-references.md) | — | `docs/project/GLOBAL-PLAN.md` |
| [0.871-regions.md](0.871-regions.md) | `src/TenshoAssistant/Market/MarketBoardService.cs` | `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.874-probe-coverage.md](0.874-probe-coverage.md) | `src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs`, `tests/TenshoAssistant.Tests/ReadingCaptureTests.cs` | `CHANGELOG.md`, `docs/project/DECISIONS.md`, `docs/project/GLOBAL-PLAN.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.887-fishing-bite.md](0.887-fishing-bite.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs`, `src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs`, `src/TenshoAssistant/Gathering/FishingBaitReader.Dalamud.cs`, `tests/TenshoAssistant.Tests/FishingWiringTests.cs` | `CHANGELOG.md`, `docs/project/CRAFT-GATHER-PROPOSALS.md`, `docs/project/GLOBAL-PLAN.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.888-aetherial-reduction.md](0.888-aetherial-reduction.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs`, `src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs` | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.889-hand-in.md](0.889-hand-in.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs`, `src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs` | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.890-spearfishing.md](0.890-spearfishing.md) | — | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.891-spearfishing-slots.md](0.891-spearfishing-slots.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs`, `src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs`, `src/TenshoAssistant/Maps/MapLocalIndex.cs`, `tests/TenshoAssistant.Tests/SpearfishingTests.cs` | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.894-fishing-spot-area.md](0.894-fishing-spot-area.md) | `src/TenshoAssistant/Maps/MapLocalIndex.cs` | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.896-spearfishing-lag.md](0.896-spearfishing-lag.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs`, `src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs`, `src/TenshoAssistant/Gathering/Spearfishing.cs` | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.899-overmeld-rates.md](0.899-overmeld-rates.md) | — | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.901-island-animals.md](0.901-island-animals.md) | `src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs` | `CHANGELOG.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.903-letters.md](0.903-letters.md) | `src/TenshoAssistant/Inventory/GilJournal.cs` | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [0.904-dead-code.md](0.904-dead-code.md) | — | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.911-discoveries-check.md](0.911-discoveries-check.md) | — | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.916-discoveries-format.md](0.916-discoveries-format.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs`, `tests/TenshoAssistant.Tests/DiscoveriesFormat916Tests.cs`, `tools/discoveries-merge.py` | `CHANGELOG.md` |
| [0.917-ocean-timetable.md](0.917-ocean-timetable.md) | — | `CHANGELOG.md` |
| [0.919-airship-reading-2026-11-08.log](0.919-airship-reading-2026-11-08.log) | `src/TenshoAssistant/FreeCompany/AirshipSectors.cs` | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.92.0-shop-currency-probe.log](0.92.0-shop-currency-probe.log) | `src/TenshoAssistant/Npcs/NpcVendorCatalog.cs` | `CHANGELOG.md` |
| [0.931-srank-check.md](0.931-srank-check.md) | `tests/TenshoAssistant.Tests/SRankCheck931Tests.cs` | `CHANGELOG.md` |
| [0.933-fishing-intuition.md](0.933-fishing-intuition.md) | `src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs` | — |
| [0.938-discoveries-load.md](0.938-discoveries-load.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs` | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.94.0-shop-currency-probe.log](0.94.0-shop-currency-probe.log) | — | `CHANGELOG.md` |
| [0.946-ocean-time.md](0.946-ocean-time.md) | `src/TenshoAssistant/Gathering/OceanFishing.cs` | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.949-network-rules.md](0.949-network-rules.md) | — | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.95.0-shop-currency-probe.log](0.95.0-shop-currency-probe.log) | `src/TenshoAssistant/Npcs/NpcVendorCatalog.cs` | `CHANGELOG.md` |
| [0.950-map-slow-frames.md](0.950-map-slow-frames.md) | — | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.959-individual-weather.md](0.959-individual-weather.md) | `src/TenshoAssistant/Gathering/WeatherForecast.cs` | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [0.96.0-wolf-probe.log](0.96.0-wolf-probe.log) | — | `CHANGELOG.md` |
| [0.961-ocean-missions.md](0.961-ocean-missions.md) | `src/TenshoAssistant/Gathering/OceanMissions.cs` | `CHANGELOG.md` |
| [0.97.0-wolf-npc-probe.log](0.97.0-wolf-npc-probe.log) | `src/TenshoAssistant/Npcs/NpcVendorCatalog.cs` | `CHANGELOG.md` |
| [0.975-field-zones.md](0.975-field-zones.md) | `src/TenshoAssistant/Fields/FieldBoard.cs` | `CHANGELOG.md` |
| [0.978-garden-harvest.md](0.978-garden-harvest.md) | `src/TenshoAssistant/Reminders/GardenChat.cs`, `tests/TenshoAssistant.Tests/GardenJournalTests.cs` | `CHANGELOG.md` |
| [0.979-labyrinth-reading.md](0.979-labyrinth-reading.md) | `src/TenshoAssistant/Competitive/CompetitiveGate.Dalamud.cs` | `docs/project/GLOBAL-PLAN.md` |
| [0.98.0-lgb-probe.log](0.98.0-lgb-probe.log) | `src/TenshoAssistant/MiniGames/TriadNpcLoader.Dalamud.cs`, `src/TenshoAssistant/Npcs/NpcVendorLoader.Dalamud.cs` | `CHANGELOG.md` |
| [0.980-beast-teams.md](0.980-beast-teams.md) | `src/TenshoAssistant/Beastmaster/BeastCatalog.cs`, `src/TenshoAssistant/Beastmaster/BeastTeams.cs`, `src/TenshoAssistant/MainWindow.Beastmaster.cs` | — |
| [0.99.0-consigneur-probe.log](0.99.0-consigneur-probe.log) | `src/TenshoAssistant/Npcs/NpcVendorLoader.Dalamud.cs` | — |
| [0.99.0-crystal-probe.log](0.99.0-crystal-probe.log) | — | `CHANGELOG.md` |
| [0.99.0-maelstrom-probe.log](0.99.0-maelstrom-probe.log) | — | — |
| [craft-log-api-probe.log](craft-log-api-probe.log) | — | — |
| [x115-client-diff-7.57-7.58.md](x115-client-diff-7.57-7.58.md) | — | — |
| [x116-client-placements.md](x116-client-placements.md) | `tools/ClientSnapshot/Program.cs`, `tools/README.md`, `tools/client-snapshot.py` | — |
| [x118-desktop-lexicon.png](x118-desktop-lexicon.png) | — | — |
| [x118-desktop-probe.md](x118-desktop-probe.md) | `src/TenshoAssistant.Desktop/DesktopPages.cs`, `src/TenshoAssistant.Desktop/Program.cs`, `tools/PluginUiTest/src/PluginUiTest.Host/Rewrite.cs`, `tools/README.md` | — |
| [x118-desktop-weather.png](x118-desktop-weather.png) | — | — |
| [x119-desktop-app.md](x119-desktop-app.md) | `src/TenshoAssistant.Desktop/Program.cs`, `src/TenshoAssistant.Desktop/TenshoAssistant.Desktop.csproj`, `tools/README.md` | — |
| [x119-desktop-weather.png](x119-desktop-weather.png) | — | — |
| [x120-plugin-ui-test.md](x120-plugin-ui-test.md) | `tests/TenshoAssistant.Tests/PluginUiTest1032Tests.cs`, `tools/README.md` | — |
| [x121-desktop-update.md](x121-desktop-update.md) | `src/TenshoAssistant.Desktop/DesktopUpdate.cs` | — |
| [x76-0-baseline.md](x76-0-baseline.md) | — | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [x76-2-duplicates.md](x76-2-duplicates.md) | — | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [x76-2bc-plan.md](x76-2bc-plan.md) | — | `CHANGELOG.md`, `docs/project/DECISIONS.md`, `docs/project/GLOBAL-PLAN.md` |
| [x76-9-comparison.md](x76-9-comparison.md) | — | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md` |
| [x85-ocean-sightings-2026-11-08.log](x85-ocean-sightings-2026-11-08.log) | `src/TenshoAssistant/Gathering/OceanFishing.cs` | `docs/project/GLOBAL-PLAN.md` |
| [x91-parsers-check.md](x91-parsers-check.md) | `src/TenshoAssistant/MainWindow.SRankConditions.cs`, `tests/TenshoAssistant.Tests/ParsersCheck971Tests.cs` | `CHANGELOG.md`, `docs/project/GLOBAL-PLAN.md`, `docs/project/archive/GLOBAL-PLAN-X-2026-11-09.md` |
| [x93-load-test.md](x93-load-test.md) | `src/TenshoAssistant/Discoveries/DiscoveriesModel.cs`, `tests/TenshoAssistant.Tests/LoadTest1036Tests.cs`, `tools/DiscoveryLoadProbe/Program.cs`, `tools/README.md`, `tools/discoveries-load.py` | `docs/project/GLOBAL-PLAN.md` |

Counted · compté : 261 files · fichiers ; 194 cited by the code or the tests · citées par le code ou les tests ; 11 cited nowhere · citées nulle part.
<!-- /docgen:evidence -->
