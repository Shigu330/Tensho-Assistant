namespace TenshoAssistant.Credits;

/// <summary>Kind of a credit, in the order of the Credits page.</summary>
public enum CreditKind { Game, Library, Embedded, Online, Rule, Reference, Tool }

/// <summary>
/// One thing Tensho uses (R-13, 0.320): name, author, licence and links; <see cref="Copied"/> when data, a table or a formula is taken
/// into Tensho (otherwise read or contacted only); the source files that use it (embedded data files included, checked by a test),
/// the network hosts it is reached on (checked against every address in the code) and the configuration setting that turns it off
/// (a dotted path on Configuration, checked by a test), <see cref="SettingPartial"/> when that setting turns only part of it off
/// (« Credits.SettingScope.&lt;Id&gt; » says what). Its sentence is « Credits.Items.&lt;Id&gt; »; « Credits.Names.&lt;Id&gt; » replaces the name when it is
/// not a proper name. Licence « proprietary » = all rights reserved under the owner's published terms; « none » = no licence published.
/// </summary>
public sealed record Credit(string Id, CreditKind Kind, string Name, string Author, string License, string Url, string LicenseUrl,
    IReadOnlyList<string> Files, IReadOnlyList<string> Hosts, string Setting, bool Copied, bool SettingPartial = false);

/// <summary>
/// Everything Tensho uses, from the inventory of 2026-11-02 (code, documentation and every licence read again at its source, three
/// sweeps merged and checked; docs/evidence/0.320-credits.json). A « none » licence means none was published: such a source is read for
/// facts only, never copied. README « Credits » summarizes this list, which is authoritative.
/// </summary>
public static class CreditCatalog
{
    /// <summary>Properties of a dotted setting path (« Discord.Enabled ») from a type, or null when one is missing.</summary>
    public static System.Reflection.PropertyInfo[]? SettingPath(Type root, string setting)
    {
        var path = new List<System.Reflection.PropertyInfo>(); var type = root;
        foreach (var name in setting.Split('.'))
        {
            if (type.GetProperty(name) is not { } property) return null;
            path.Add(property); type = property.PropertyType;
        }
        return path.Count > 0 ? [.. path] : null;
    }

    public static readonly IReadOnlyList<Credit> All =
    [
        new("FinalFantasyXIV", CreditKind.Game, "FINAL FANTASY XIV", "SQUARE ENIX CO., LTD.", "proprietary", "https://www.finalfantasyxiv.com", "https://support.na.square-enix.com/rule.php?id=5382",
            ["MiniGames/MiniCactpotSolver.cs", "Plugin.cs", "Items/ItemCatalogLoader.Dalamud.cs", "Maps/MapProjection.cs", "Maps/MapCatalogLoader.Dalamud.cs", "UI/Components/ItemCell.cs", "UI/UiComponents.cs", "MainWindow.MapView.cs", "Items/ExternalLinks.cs"],
            // 0.349 (g, X-07): the Lodestone database pages, opened in the player's browser on a click (nothing fetched by Tensho).
            ["fr.finalfantasyxiv.com", "de.finalfantasyxiv.com", "jp.finalfantasyxiv.com", "na.finalfantasyxiv.com"], "", false),
        new("Dalamud", CreditKind.Library, "Dalamud", "goatcorp", "AGPL-3.0", "https://github.com/goatcorp/Dalamud", "https://github.com/goatcorp/Dalamud/blob/master/LICENSE",
            ["Plugin.cs", "Configuration.Dalamud.cs", "MapViewWindow.cs", "Macros/MacroChainBridge.Dalamud.cs", "Items/ItemContextMenuBridge.Dalamud.cs", "Collections/CollectionReader.Dalamud.cs"], [], "", false),
        new("DalamudBindingsImGui", CreditKind.Library, "Dalamud.Bindings.ImGui", "Juna Meinhold", "MIT", "https://github.com/goatcorp/Dalamud/tree/master/imgui/Dalamud.Bindings.ImGui", "https://github.com/goatcorp/Dalamud/blob/master/imgui/Dalamud.Bindings.ImGui/LICENSE.txt",
            ["MainWindow.cs", "MapViewWindow.cs", "BankImportOverlay.cs", "Alarms/AlarmDtrEntry.Dalamud.cs", "UI/UiComponents.cs", "UI/FfxivTheme.cs", "UI/Components/SettingRow.cs", "UI/Navigation/MainWindow.CategoryNavigation.cs"], [], "", false),
        new("DearImGui", CreditKind.Library, "Dear ImGui", "Omar Cornut", "MIT", "https://github.com/ocornut/imgui", "https://github.com/ocornut/imgui/blob/master/LICENSE.txt",
            ["MainWindow.cs", "MapViewWindow.cs", "UI/UiComponents.cs"], [], "", false),
        new("FFXIVClientStructs", CreditKind.Library, "FFXIVClientStructs, InteropGenerator.Runtime", "aers", "MIT", "https://github.com/aers/FFXIVClientStructs", "https://github.com/aers/FFXIVClientStructs/blob/main/LICENSE",
            ["MiniGames/TriadObservationReader.Dalamud.cs", "Alarms/AlarmService.Dalamud.cs", "Character/CharacterStatsReader.Dalamud.cs", "Craft/CraftLiveReader.Dalamud.cs", "Fashion/FashionCheckReader.Dalamud.cs", "FreeCompany/VoyageWatcher.Dalamud.cs", "Gathering/GatheringObservationReader.Dalamud.cs", "Inventory/BankInventoryBridge.Dalamud.cs"], [], "", false),
        new("Lumina", CreditKind.Library, "Lumina", "NotAdam", "WTFPL", "https://github.com/NotAdam/Lumina", "https://github.com/NotAdam/Lumina/blob/master/LICENSE",
            ["MiniGames/TriadNpcLoader.Dalamud.cs", "Npcs/NpcVendorLoader.Dalamud.cs", "Caching/GameCacheIdentity.Dalamud.cs", "Maps/MapLocalIndexLoader.Dalamud.cs"], [], "", false),
        new("LuminaExcel", CreditKind.Library, "Lumina.Excel", "WorkingRobot, NotAdam", "WTFPL", "https://github.com/NotAdam/Lumina.Excel", "https://github.com/NotAdam/Lumina.Excel/blob/master/LICENSE",
            ["Plugin.cs", "Items/ItemCatalogLoader.Dalamud.cs", "Craft/RecipeCatalogLoader.Dalamud.cs", "Gathering/GatheringCatalogLoader.Dalamud.cs", "Maps/MapCatalogLoader.Dalamud.cs", "Quests/QuestReader.Dalamud.cs", "Collections/CompletionCopy.Lumina.cs"], [], "", false),
        new("NewtonsoftJson", CreditKind.Library, "Newtonsoft.Json", "James Newton-King", "MIT", "https://github.com/JamesNK/Newtonsoft.Json", "https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md",
            ["Configuration.cs", "Plugin.cs"], [], "", false),
        new("DotNetRuntime", CreditKind.Library, ".NET 10", ".NET Foundation, Microsoft", "MIT", "https://github.com/dotnet/runtime", "https://github.com/dotnet/runtime/blob/main/LICENSE.TXT",
            ["Community/BoundedHttp.cs", "Caching/DataCache.cs", "Localization/LocalizationManager.cs", "Market/MarketService.cs", "Alarms/DiscordNotifier.cs", "Inventory/BankFileStore.cs"], [], "", false),
        new("FontAwesome", CreditKind.Library, "Font Awesome Free", "Fonticons, Inc.", "CC-BY-4.0 AND OFL-1.1", "https://fontawesome.com/license/free", "https://github.com/FortAwesome/Font-Awesome/blob/7.x/LICENSE.txt",
            ["MainWindow.Tools.cs", "MainWindow.Home.cs", "MainWindow.MapView.cs", "MainWindow.Alarms.cs", "UI/Components/SettingRow.cs", "UI/Navigation/MainWindow.CategoryNavigation.cs"], [], "", false),
        new("SubmarineTracker", CreditKind.Embedded, "SubmarineTracker", "Infi (Infiziert90)", "MIT", "https://github.com/Infiziert90/SubmarineTracker", "https://github.com/Infiziert90/SubmarineTracker/blob/master/LICENSE",
            ["FreeCompany/Data/submarine-breakpoints.csv", "FreeCompany/VoyageWatcher.Dalamud.cs", "FreeCompany/VesselBuilds.cs", "FreeCompany/VoyagePlanner.cs", "MainWindow.VesselBuilds.cs", "MainWindow.RoutePlanner.cs", "TenshoAssistant.csproj"], [], "", true),
        new("SubmersibleLootSpreadsheet", CreditKind.Embedded, "Submersible Loot Spreadsheet", "Elena (saerielle)", "none", "https://docs.google.com/spreadsheets/d/1eiuGRrQTjJ5n4ogybKOBxJRT7EiJLlX93fge-Z8MmD4", "",
            ["FreeCompany/Data/submarine-breakpoints.csv"], [], "", true),
        new("FFXIVGachaSpreadsheet", CreditKind.Embedded, "FFXIVGachaSpreadsheet (XIVStats)", "Infi (Infiziert90)", "MIT", "https://github.com/Infiziert90/FFXIVGachaSpreadsheet", "https://github.com/Infiziert90/FFXIVGachaSpreadsheet/blob/master/LICENSE",
            ["FreeCompany/Data/submarine-loot.json", "FreeCompany/VoyageWatcher.Dalamud.cs", "FreeCompany/VoyagePlanner.cs", "MainWindow.RoutePlanner.cs", "Fashion/FashionDataService.cs", "Fashion/FashionData.cs", "MainWindow.Fashion.cs", "MainWindow.FashionWeek.cs"], ["raw.githubusercontent.com"], "CommunityFashion", true),
        // 0.336 (DEC-023, point 3): two Generated CSVs copied as published, with the GPL-3.0 notice in their header.
        new("LuminaSupplementalAirships", CreditKind.Embedded, "LuminaSupplemental", "Critical-Impact", "GPL-3.0", "https://github.com/Critical-Impact/LuminaSupplemental", "https://github.com/Critical-Impact/LuminaSupplemental/blob/main/LICENSE",
            ["FreeCompany/Data/airship-unlock.csv", "FreeCompany/Data/airship-drop.csv", "FreeCompany/AirshipSectors.cs", "FreeCompany/VoyageWatcher.Dalamud.cs", "MainWindow.AirshipSectors.cs", "TenshoAssistant.csproj",
             "Events/Data/festival-names.csv", "Events/Festivals.cs", "Events/FestivalReader.Dalamud.cs", "MainWindow.Events.cs"], [], "", true),
        // 0.449 (DEC-027, point 3): seed growing and wilting hours, built by tools/garden-data.py with the GPL-3.0 notice in the header.
        new("LotlabGardening", CreditKind.Embedded, "FFXIV Gardening Tracker", "Lotlab", "GPL-3.0", "https://github.com/Lotlab/FFXIV-Gardening-Tracker", "https://github.com/Lotlab/FFXIV-Gardening-Tracker/blob/master/LICENSE.txt",
            ["Reminders/Data/garden-seeds.csv", "Reminders/GardenSeeds.cs", "MainWindow.Garden.cs", "TenshoAssistant.csproj"], [], "", true),
        // 0.450 (DEC-027, point 2): crossbreeding pairs, built by tools/garden-data.py with the MIT notice in the header.
        new("CrossbreedHelper", CreditKind.Embedded, "FFXIV Crossbreed Helper", "Nick75 (nick75g)", "MIT", "https://github.com/nick75g/FFXIV-Crossbreed-Helper", "https://github.com/nick75g/FFXIV-Crossbreed-Helper/blob/main/LICENSE",
            ["Reminders/Data/garden-crosses.csv", "Reminders/GardenCrosses.cs", "MainWindow.GardenCrosses.cs", "TenshoAssistant.csproj"], [], "", true),
        // A-03 (0.465, DEC-028): the optional crafting solver and its native library, shipped next to the DLL.
        new("RaphaelNet", CreditKind.Library, "Raphael.Net 5.0.0", "Asriel Camora (WorkingRobot)", "MIT", "https://github.com/WorkingRobot/Raphael.Net", "https://github.com/WorkingRobot/Raphael.Net/blob/main/LICENSE",
            ["Craft/RaphaelCraftSolver.Dalamud.cs", "Craft/RaphaelActions.cs", "TenshoAssistant.csproj"], [], "", false),
        new("RaphaelRs", CreditKind.Library, "raphael-rs", "KonaeAkira", "Apache-2.0", "https://github.com/KonaeAkira/raphael-rs", "https://github.com/KonaeAkira/raphael-rs/blob/main/LICENSE",
            ["Craft/RaphaelCraftSolver.Dalamud.cs", "TenshoAssistant.csproj"], [], "", false),
        new("CharacterPanelRefined", CreditKind.Embedded, "Character Panel Refined", "Kouzukii", "MIT", "https://github.com/Kouzukii/ffxiv-characterstatus-refined", "https://github.com/Kouzukii/ffxiv-characterstatus-refined/blob/master/LICENSE",
            ["Character/StatFormulas.cs", "Character/StatFormulas.cs", "Character/CharacterStatsReader.Dalamud.cs", "MainWindow.Character.cs", "MainWindow.Gear.cs", "MainWindow.Tools.cs"], [], "", true),
        new("Universalis", CreditKind.Online, "Universalis", "karashiiro, Universalis-FFXIV", "MIT", "https://universalis.app", "https://github.com/Universalis-FFXIV/Universalis/blob/v2/LICENSE",
            ["Market/MarketService.cs", "Market/MarketBoardService.cs", "Market/MarketBoard.cs", "Items/ItemHoverReader.Dalamud.cs", "MainWindow.Market.cs", "MainWindow.MarketBoard.cs", "MainWindow.MarketHover.cs", "MainWindow.PreparationCost.cs"], ["universalis.app"], "CommunityUniversalis", false),
        new("GarlandTools", CreditKind.Online, "Garland Tools", "Chris Faulhaber (ufx)", "MIT", "https://www.garlandtools.org", "https://github.com/ufx/GarlandTools/blob/master/LICENSE",
            ["Community/GarlandClient.cs", "Community/GarlandItemSources.cs", "Duties/DutyLootService.cs", "Duties/DutyLootDocument.cs", "Monsters/MonsterLootService.cs", "Plugin.cs", "MainWindow.Duties.cs", "MainWindow.DutyFinder.cs"], ["www.garlandtools.org"], "CommunityGarland", false),
        new("ConsoleGamesWiki", CreditKind.Online, "FFXIV Console Games Wiki", "", "GFDL-1.2-or-later", "https://ffxiv.consolegameswiki.com", "https://ffxiv.consolegameswiki.com/wiki/FFXIV_Wiki:About",
            ["Community/WikiItemAcquisition.cs", "Monsters/WikiMonsterSources.cs", "Maps/WikiVistas.cs", "Plugin.cs", "MainWindow.Monsters.cs", "MainWindow.MarketHover.cs", "MainWindow.MapView.cs"], ["ffxiv.consolegameswiki.com"], "CommunityWiki", false),
        new("LuminaSupplemental", CreditKind.Online, "LuminaSupplemental", "Critical-Impact", "GPL-3.0", "https://github.com/Critical-Impact/LuminaSupplemental", "https://github.com/Critical-Impact/LuminaSupplemental/blob/main/LICENSE",
            ["Community/LuminaSupplemental.cs", "Npcs/NpcVendorCatalog.cs", "Plugin.cs", "MainWindow.Lumina.cs", "MainWindow.ItemUses.cs", "MainWindow.SourceLinks.cs", "MainWindow.Monsters.cs", "MainWindow.Duties.cs"], ["raw.githubusercontent.com"], "CommunityLumina", false),
        new("GatherBuddy", CreditKind.Online, "GatherBuddy", "Ottermandias", "Apache-2.0", "https://github.com/Ottermandias/GatherBuddy", "https://github.com/Ottermandias/GatherBuddy/blob/main/LICENSE",
            ["Community/GatherBuddyFish.cs", "Plugin.cs", "MainWindow.FishingPanel.cs", "MainWindow.GatheringFish.cs", "MainWindow.FishingSources.cs", "MainWindow.Gathering.cs", "MainWindow.Alarms.cs", "MainWindow.Tools.cs"], ["api.github.com", "raw.githubusercontent.com"], "CommunityGatherBuddy", false),
        new("FFXIVTeamcraft", CreditKind.Online, "FFXIV Teamcraft", "Flavien Normand, ffxiv-teamcraft", "MIT", "https://github.com/ffxiv-teamcraft/ffxiv-teamcraft", "https://github.com/ffxiv-teamcraft/ffxiv-teamcraft/blob/staging/LICENSE",
            ["Community/FishBiteTimes.cs", "Plugin.cs", "MainWindow.FishingPanel.cs", "MainWindow.GatheringFish.cs", "MainWindow.Advanced.cs", "Gear/GearCatalog.cs", "MainWindow.Gear.cs", "Craft/TeamcraftExport.cs", "MainWindow.CraftPlan.cs"], ["gubal.ffxivteamcraft.com", "ffxivteamcraft.com"], "CommunityTeamcraftBites", false),
        // 0.445 (N-05): open plots, read only, the world id only is sent; no licence stated by the project.
        new("PaissaDB", CreditKind.Online, "PaissaDB", "zhudotexe", "none", "https://github.com/zhudotexe/FFXIV_PaissaDB", "",
            ["Community/PaissaDb.cs", "MainWindow.Plots.cs", "Plugin.cs"], ["paissadb.zhu.codes"], "PlotWatch", false),
        // 0.447 (X-04, decided 2026-11-03): player events, opened in the player's browser on a click; nothing is fetched by Tensho.
        new("ToolGallery", CreditKind.Online, "Tensho Assistant community tools", "Tensho Assistant contributors", "CC0-1.0", "https://github.com/Shigu330/Tensho-Assistant/tree/main/community-templates", "https://creativecommons.org/publicdomain/zero/1.0/",
            ["MyTools/ToolGallery.cs", "MainWindow.MyTools.cs"], ["raw.githubusercontent.com"], "", false),
        new("Partake", CreditKind.Online, "Partake", "Partake", "proprietary", "https://www.partake.gg/", "",
            ["MainWindow.Events.cs"], ["www.partake.gg"], "", false),
        new("Discord", CreditKind.Online, "Discord", "Discord Inc.", "proprietary", "https://discord.com/developers/docs/resources/webhook", "",
            ["Alarms/DiscordNotifier.cs", "Alarms/DiscordWebhook.cs", "MainWindow.Discord.cs", "MainWindow.Alarms.cs", "Plugin.cs"], ["discord.com", "discordapp.com", "ptb.discord.com", "canary.discord.com"], "Discord.Enabled", false),
        // 0.456: the planting rules of crossbreeding (facts, in our own words): docs/evidence/0.456-garden-grid.md.
        // 0.671 (N-10): how a craft's value moves on the island, in Tensho's own code; the game's sheets give the ratios.
        new("WorkshopOptimizer", CreditKind.Rule, "Workshop Optimizer Plugin", "belzaru17", "MIT", "https://github.com/belzaru17/WorkshopOptimizerPlugin", "https://github.com/belzaru17/WorkshopOptimizerPlugin/blob/main/LICENSE",
            ["Island/IslandCatalog.cs", "MainWindow.IslandSanctuary.cs"], [], "", false),
        // 0.670 (N-12): the spawn conditions of the S ranks, crossed and in Tensho's own words.
        new("EliteMarks", CreditKind.Rule, "Elite Marks (FFXIV Console Games Wiki)", "", "GFDL-1.2-or-later", "https://ffxiv.consolegameswiki.com/wiki/Elite_Marks", "https://ffxiv.consolegameswiki.com/wiki/FFXIV_Wiki:About",
            ["Hunting/SRankConditions.cs", "MainWindow.SRankConditions.cs"], [], "", false),
        new("SRankGuide", CreditKind.Rule, "Final Fantasy XIV: The Hunt (S rank guide)", "", "none", "https://srankguide.carrd.co", "",
            ["Hunting/SRankConditions.cs"], [], "", false),
        new("SRankAssistant", CreditKind.Reference, "S Rank Assistant", "TheRedheadedWitch", "none", "https://github.com/TheRedheadedWitch/SRankAssistant", "",
            ["Hunting/SRankConditions.cs"], [], "", false),
        new("GardeningGuide", CreditKind.Rule, "Gardening Guide (FFXIV Console Games Wiki)", "", "GFDL-1.2-or-later", "https://ffxiv.consolegameswiki.com/wiki/Gardening_Guide", "https://ffxiv.consolegameswiki.com/wiki/FFXIV_Wiki:About",
            ["Reminders/GardenCrosses.cs", "MainWindow.GardenCrosses.cs"], [], "", false),
        new("GatherBuddyReborn", CreditKind.Rule, "GatherBuddyReborn", "FFXIV-CombatReborn", "Apache-2.0", "https://github.com/FFXIV-CombatReborn/GatherBuddyReborn", "https://raw.githubusercontent.com/FFXIV-CombatReborn/GatherBuddyReborn/main/LICENSE",
            ["Gathering/WeatherForecast.cs", "MainWindow.Weather.cs"], [], "", true),
        new("FrozenRabbitTome", CreditKind.Rule, "frozen_rabbit_tome", "", "none", "", "",
            ["Gathering/BountifulEstimate.cs", "MainWindow.GatheringGp.cs", "MainWindow.GatheringPanel.cs"], [], "", true),
        new("SubmersiblesDurabilitySheet", CreditKind.Rule, "Submersibles durability damage", "", "unknown", "", "",
            ["FreeCompany/VesselBuilds.cs", "MainWindow.VesselBuilds.cs"], [], "", true),
        new("VesselCostCapacity", CreditKind.Rule, "Parts cost <= capacity", "SubmarineTracker, Argus", "unknown", "", "",
            ["FreeCompany/VesselBuilds.cs", "MainWindow.VesselBuilds.cs"], [], "", true),
        new("VesselBuildNaming", CreditKind.Rule, "SSUC++ notation", "", "unknown", "", "",
            ["FreeCompany/VesselBuilds.cs", "MainWindow.VesselBuilds.cs"], [], "", true),
        new("AllaganStudiesMathWizard", CreditKind.Rule, "Allagan Studies: How to be a math wizard", "Allagan Studies", "unknown", "", "",
            ["Character/StatFormulas.cs", "Character/CharacterStatsReader.Dalamud.cs", "MainWindow.Character.cs", "MainWindow.Gear.cs"], [], "", true),
        new("AkhMorning", CreditKind.Rule, "akhmorning.com", "", "unknown", "https://www.akhmorning.com", "",
            ["Character/StatFormulas.cs"], [], "", true),
        new("FashionReportWeek", CreditKind.Rule, "Fashion Report week", "", "unknown", "", "",
            ["Fashion/FashionCheck.cs", "MainWindow.FashionWeek.cs"], [], "", true),
        new("GlamourAllJobs74", CreditKind.Rule, "Glamour for every job (7.4)", "SQUARE ENIX", "proprietary", "https://www.finalfantasyxiv.com", "",
            ["Gear/GearCatalog.cs", "MainWindow.Gear.cs", "Configuration.cs"], [], "GearAllJobs", true),
        new("Craftimizer", CreditKind.Reference, "Craftimizer", "Asriel Camora (WorkingRobot)", "MIT", "https://github.com/WorkingRobot/Craftimizer", "https://github.com/WorkingRobot/Craftimizer/blob/main/LICENSE",
            ["Macros/MacroWriter.Dalamud.cs", "MainWindow.CraftSimulation.cs", "Craft/CraftSimulation.cs", "Craft/CraftActionCatalogLoader.Dalamud.cs", "MainWindow.Tools.cs"], [], "", false),
        new("TeamcraftSimulator", CreditKind.Reference, "ffxiv-teamcraft/simulator", "Flavien Normand, ffxiv-teamcraft", "MIT", "https://github.com/ffxiv-teamcraft/simulator", "https://github.com/ffxiv-teamcraft/simulator/blob/master/LICENSE",
            ["Craft/CraftSimulation.cs"], [], "", false),
        new("Artisan", CreditKind.Reference, "Artisan", "PunishXIV", "BSD-3-Clause", "https://github.com/PunishXIV/Artisan", "https://github.com/PunishXIV/Artisan/blob/main/LICENCE.md",
            [], [], "", false),
        new("Mappy", CreditKind.Reference, "Mappy", "MidoriKami, harbingerftw", "AGPL-3.0-or-later", "https://github.com/harbingerftw/Mappy", "https://github.com/harbingerftw/Mappy/blob/master/LICENSE",
            ["Configuration.cs", "MainWindow.MapView.cs", "Maps/MapCatalog.cs", "Maps/NativeMapReplacer.Dalamud.cs", "MainWindow.Tools.cs"], [], "", false),
        new("SortaKinda", CreditKind.Reference, "SortaKinda", "MidoriKami", "AGPL-3.0-or-later", "https://github.com/MidoriKami/SortaKinda", "https://github.com/MidoriKami/SortaKinda/blob/master/LICENSE",
            ["MainWindow.SortPreview.cs", "MainWindow.Tools.cs"], [], "", false),
        new("AvantGarde", CreditKind.Reference, "Avant-Garde", "NeNeppie", "AGPL-3.0", "https://github.com/NeNeppie/AvantGarde", "https://github.com/NeNeppie/AvantGarde/blob/main/LICENSE",
            ["Fashion/FashionCheckReader.Dalamud.cs", "MainWindow.Fashion.cs", "MainWindow.FashionWeek.cs", "MainWindow.Tools.cs"], [], "", false),
        new("TriadBuddy", CreditKind.Reference, "TriadBuddy", "MgAl2O4", "MIT", "https://github.com/MgAl2O4/FFTriadBuddyDalamud", "https://github.com/MgAl2O4/FFTriadBuddyDalamud/blob/main/LICENSE",
            ["MainWindow.TripleTriad.cs", "MainWindow.TriadAdvice.cs", "MainWindow.TriadDecks.cs", "MainWindow.TriadNpcs.cs", "MainWindow.TriadRequest.cs", "MiniGames/TriadMatchSimulator.cs", "MainWindow.Tools.cs"], [], "", false),
        new("FFTriadBuddy", CreditKind.Reference, "FFTriadBuddy", "MgAl2O4", "MIT", "https://github.com/MgAl2O4/FFTriadBuddy", "https://github.com/MgAl2O4/FFTriadBuddy/blob/master/LICENSE",
            ["MiniGames/TripleTriadSolver.cs"], [], "", false),
        new("LodestoneTripleTriadGuide", CreditKind.Reference, "Lodestone Triple Triad guide", "SQUARE ENIX", "proprietary", "https://na.finalfantasyxiv.com/lodestone/playguide/contentsguide/goldsaucer/tripletriad/", "",
            ["MiniGames/TripleTriadSolver.cs"], [], "", false),
        new("EzMiniCactpot", CreditKind.Reference, "ezMiniCactpot", "daemitus, Minmoose, MidoriKami", "GPL-3.0", "https://github.com/Etheirys/EzMiniCactpot", "https://github.com/Etheirys/EzMiniCactpot/blob/main/LICENSE",
            ["MainWindow.MiniCactpot.cs", "MainWindow.Tools.cs"], [], "", false),
        new("LazyGatherer", CreditKind.Reference, "LazyGatherer", "Hiroa", "AGPL-3.0", "https://github.com/Hiroa/LazyGatherer", "https://github.com/Hiroa/LazyGatherer/blob/main/LICENSE",
            ["MainWindow.GatheringGp.cs", "Gathering/GatheringObservationReader.Dalamud.cs", "MainWindow.Tools.cs"], [], "", false),
        new("ItemUse", CreditKind.Reference, "Item Use", "PunishedPineapple", "AGPL-3.0", "https://github.com/PunishedPineapple/ItemUse", "https://github.com/PunishedPineapple/ItemUse/blob/main/LICENSE",
            ["MainWindow.ItemUses.cs", "Items/ItemUseCatalog.cs", "MainWindow.ItemContextMenu.cs", "MainWindow.Tools.cs"], [], "", false),
        new("DutyLootPreview", CreditKind.Reference, "Duty Loot Preview", "grittyfrog", "none", "https://github.com/grittyfrog/DutyLootPreview", "",
            ["MainWindow.Duties.cs", "MainWindow.DutyFinder.cs", "MainWindow.Tools.cs"], [], "", false),
        new("MonsterLootHunter", CreditKind.Reference, "Monster Loot Hunter", "Sylf (danielbrenom)", "MIT", "https://github.com/danielbrenom/MonsterLootHunter", "https://github.com/danielbrenom/MonsterLootHunter/blob/main/LICENSE",
            ["MainWindow.Monsters.cs", "MainWindow.ItemContextMenu.cs", "MainWindow.Tools.cs"], [], "", false),
        new("QuestTracker", CreditKind.Reference, "QuestTracker", "isaiahcat, keifufu", "AGPL-3.0", "https://github.com/keifufu/QuestTracker", "https://github.com/keifufu/QuestTracker/blob/master/LICENSE",
            ["MainWindow.Quests.cs", "MainWindow.Tools.cs"], [], "", false),
        new("PriceInsight", CreditKind.Reference, "Price Insight", "Kouzukii", "MIT", "https://github.com/Kouzukii/ffxiv-priceinsight", "https://github.com/Kouzukii/ffxiv-priceinsight/blob/main/LICENSE",
            ["MainWindow.Tools.cs", "MainWindow.ItemContextMenu.cs", "MainWindow.Items.cs"], [], "", false),
        new("BlueMageHelper", CreditKind.Reference, "Blue Mage Helper", "Infi, Sl0nderman, harbingerftw", "none", "https://github.com/harbingerftw/BlueMageHelper", "",
            ["MainWindow.Tools.cs"], [], "", false),
        new("MacroChain", CreditKind.Reference, "Macro Chain", "Caraxi", "none", "https://github.com/Caraxi/MacroChain", "",
            ["Macros/MacroChainBridge.Dalamud.cs", "Macros/MacroChainService.cs", "MainWindow.MacroChain.cs", "MainWindow.Tools.cs"], [], "", false),
        new("CollectionsSeventh", CreditKind.Reference, "Collections", "Seventh", "none", "https://github.com/Seventhxiv/Collections", "",
            ["MainWindow.Tools.cs"], [], "", false),
        new("HimbeertoniRaidTool", CreditKind.Reference, "HimbeertoniRaidTool", "Koenari", "MIT", "https://github.com/Koenari/HimbeertoniRaidTool", "https://github.com/Koenari/HimbeertoniRaidTool/blob/main/LICENSE.md",
            [], [], "", false),
        new("Glamourer", CreditKind.Reference, "Glamourer", "Ottermandias", "Apache-2.0", "https://github.com/Ottermandias/Glamourer", "https://github.com/Ottermandias/Glamourer/blob/main/LICENSE",
            [], [], "", false),
        new("XivGear", CreditKind.Reference, "xivgear.app", "", "none", "https://xivgear.app", "",
            [], [], "", false),
        new("Argus", CreditKind.Reference, "Argus", "", "unknown", "https://github.com/ofnature/Argus", "",
            ["FreeCompany/VesselBuilds.cs"], [], "", false),
        new("GatheringOptimizer", CreditKind.Reference, "GatheringOptimizer", "belzaru17", "MIT", "https://github.com/belzaru17/GatheringOptimizer", "https://github.com/belzaru17/GatheringOptimizer/blob/main/LICENSE",
            [], [], "", false),
        new("SaintCoinach", CreditKind.Reference, "SaintCoinach", "xivapi", "WTFPL", "https://github.com/xivapi/SaintCoinach", "https://github.com/xivapi/SaintCoinach/blob/master/LICENSE",
            [], [], "", false),
        new("AllaganTools", CreditKind.Reference, "Allagan Tools (InventoryTools)", "Critical-Impact", "GPL-3.0", "https://github.com/Critical-Impact/InventoryTools", "https://github.com/Critical-Impact/InventoryTools/blob/main/LICENSE",
            [], [], "", false),
        new("Questionable", CreditKind.Reference, "Questionable", "PunishXIV", "AGPL-3.0", "https://github.com/PunishXIV/Questionable", "https://github.com/PunishXIV/Questionable/blob/master/LICENSE",
            [], [], "", false),
        new("SamplePlugin", CreditKind.Reference, "Dalamud SamplePlugin", "goatcorp", "AGPL-3.0", "https://github.com/goatcorp/SamplePlugin", "https://github.com/goatcorp/SamplePlugin/blob/master/LICENSE.md",
            ["Plugin.cs"], [], "", false),
        new("XIVShinies", CreditKind.Reference, "XIV-Shinies", "XIV-Shinies", "MIT", "https://github.com/XIV-Shinies/xiv-shinies-plugin", "https://github.com/XIV-Shinies/xiv-shinies-plugin/blob/HEAD/LICENSE",
            [], [], "", false),
        new("Tomestone", CreditKind.Reference, "Dalamud.Tomestone", "TomestoneGG", "AGPL-3.0", "https://github.com/TomestoneGG/Dalamud.Tomestone", "https://github.com/TomestoneGG/Dalamud.Tomestone/blob/HEAD/LICENSE.md",
            [], [], "", false),
        new("FfxivTodo", CreditKind.Reference, "ffxiv-todo", "datamachine", "MIT", "https://github.com/datamachine/ffxiv-todo", "https://github.com/datamachine/ffxiv-todo/blob/main/LICENSE",
            [], [], "", false),
        new("Tsukimichi", CreditKind.Reference, "Tsukimichi", "xenofei", "MIT", "https://github.com/xenofei/Tsukimichi", "https://github.com/xenofei/Tsukimichi/blob/main/LICENSE",
            [], [], "", false),
        new("AutoRetainer", CreditKind.Reference, "AutoRetainer", "PunishXIV", "unknown", "https://github.com/PunishXIV/AutoRetainer", "",
            [], [], "", false),
        new("DalamudNetSdk", CreditKind.Tool, "Dalamud.NET.Sdk 15.0.0", "goatcorp", "MIT", "https://github.com/goatcorp/Dalamud.NET.Sdk", "https://licenses.nuget.org/MIT",
            ["TenshoAssistant.csproj"], [], "", false),
        new("DalamudPackager", CreditKind.Tool, "DalamudPackager 15.0.0", "Anna Clemens, goatcorp", "EUPL-1.2", "https://github.com/goatcorp/DalamudPackager", "https://licenses.nuget.org/EUPL-1.2",
            ["packages.lock.json", "TenshoAssistant.json", "TenshoAssistant.csproj"], [], "", false),
        new("ReproducibleBuilds", CreditKind.Tool, "DotNet.ReproducibleBuilds 1.2.39", ".NET Foundation", "MIT", "https://github.com/dotnet/reproducible-builds", "https://licenses.nuget.org/MIT",
            ["packages.lock.json"], [], "", false),
        new("DotNetSdk", CreditKind.Tool, ".NET SDK 10.0.403", ".NET Foundation, Microsoft", "MIT", "https://github.com/dotnet/sdk", "https://github.com/dotnet/sdk/blob/main/LICENSE.TXT",
            ["TenshoAssistant.csproj"], [], "", false),
        new("PowerShell", CreditKind.Tool, "PowerShell 7", "Microsoft", "MIT", "https://github.com/PowerShell/PowerShell", "https://github.com/PowerShell/PowerShell/blob/master/LICENSE.txt",
            ["MainWindow.Advanced.cs"], [], "", false),
        new("Python", CreditKind.Tool, "Python", "Python Software Foundation", "PSF-2.0", "https://www.python.org", "https://github.com/python/cpython/blob/main/LICENSE",
            ["FreeCompany/Data/submarine-loot.json"], [], "", false),
        new("GitHubCli", CreditKind.Tool, "GitHub CLI", "GitHub Inc.", "MIT", "https://cli.github.com", "https://github.com/cli/cli/blob/trunk/LICENSE",
            [], [], "", false),
        new("CodeQL", CreditKind.Tool, "CodeQL", "GitHub", "MIT", "https://github.com/github/codeql-action", "https://github.com/github/codeql-action/blob/main/LICENSE",
            [], [], "", false),
        new("ClaudeCode", CreditKind.Tool, "Claude Code", "Anthropic PBC", "proprietary", "https://claude.com/claude-code", "https://github.com/anthropics/claude-code/blob/main/LICENSE.md",
            ["TenshoAssistant.json", "TenshoAssistant.csproj", "images/icon.png"], [], "", false),
    ];
}
