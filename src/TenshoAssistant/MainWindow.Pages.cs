namespace TenshoAssistant;

// 0.470 (TEMPLATES-001, DEC-029): every page in one list: the navigation, the drawing and the source shown to contributors all read it. A new tool adds one line here (scripts/New-Tool.ps1 does it).
internal sealed partial class MainWindow
{
    /// <param name="Page">The page; each one appears once, and a test checks it.</param>
    /// <param name="Category">Its group in the tool directory and in the README; in the side bar only « Tracking » is still a category (X-601, PageFamilies.SideBarCategories).</param>
    /// <param name="Label">Its name in the navigation; null for a page reached from the tabs of its parent.</param>
    /// <param name="Draw">Draws the page.</param>
    /// <param name="Source">Where its code starts, shown to contributors.</param>
    /// <param name="Parent">The navigation entry that stays selected on this page.</param>
    /// <param name="Visible">Null: always shown in the navigation.</param>
    /// <param name="Tab">X-600 (0.1573): its name in the tab bar of its family (a navigation entry and the pages that name it as their parent);
    /// the one header (DrawPageHeader) draws the family's title, this page's description and the bar. Null: a page alone, which draws its own heading.</param>
    /// <param name="About">The description under the title while this page of a family is shown.</param>
    /// <param name="TabAction">On a family's navigation entry: a button at the end of the family's tab bar.</param>
    private sealed record PageEntry(Page Page,string? Category,Func<string>? Label,Action Draw,string Source,Page? Parent=null,Func<bool>? Visible=null,
        Func<string>? Tab=null,Func<string>? About=null,(Func<string> Label,Action Run)? TabAction=null);
    private sealed record PageGroup(string? Category,Page[] Pages,PageEntry[] Entries);

    private Func<string> PageLabel(string key)=>()=>localization.Get(key);
    private PageEntry[]? pageEntries;
    private PageGroup[]? pageGroups;
    private PageEntry[] PageEntries=>pageEntries??=
    [
        // X-601 (0.1681, D-199): the list is in the order of the side bar: the two pages above everything, the pages that group, « Tracking »
        // (the one category left, with its four pages), then the rest; the tabs of a family follow its entry.
        new(Page.Home,null,PageLabel("Navigation.Home"),DrawHome,"MainWindow.Home.cs"),
        // R-13 (0.320): everything Tensho uses, shown to every player.
        new(Page.MyTools,null,PageLabel("Navigation.MyTools"),DrawMyTools,"MainWindow.MyTools.cs, MyTools/"),
        // X-601 (0.1677, D-199): « Character », the first page that groups: the collections, blue magic and its carnivale, the beastmaster,
        // the chocobo, the hunting log, the gear and the relic weapons, each a tab of the one page header.
        new(Page.Collections,"Progression",PageLabel("Navigation.Character"),()=>DrawCollections(),"MainWindow.Collections.cs, Collections/",Tab:PageLabel("Navigation.Collection"),About:PageLabel("CollectionsBrowser.Description")),
        new(Page.BlueMagic,"Progression",null,()=>DrawCollections(true),"MainWindow.Collections.cs, Collections/",Page.Collections,Tab:PageLabel("Navigation.BlueMagic"),About:PageLabel("CollectionsBrowser.BlueDescription")),
        new(Page.MaskedCarnivale,"Content",null,DrawMaskedCarnivale,"MainWindow.MaskedCarnivale.cs, Carnivale/",Page.Collections,Tab:PageLabel("Navigation.MaskedCarnivale"),About:PageLabel("MaskedCarnivale.Subtitle")),
        new(Page.Beastmaster,"Content",null,DrawBeastmaster,"MainWindow.Beastmaster.cs, Beastmaster/",Page.Collections,Tab:PageLabel("Navigation.Beastmaster"),About:PageLabel("BeastUi.Help")),
        new(Page.Chocobo,"Progression",null,DrawChocoboPage,"MainWindow.ChocoboBarding.cs, Gear/ChocoboBarding.cs, Gear/ChocoboReader.Dalamud.cs",Page.Collections,Tab:PageLabel("Navigation.Chocobo"),About:PageLabel("ChocoboBarding.Help")),
        new(Page.HuntingLog,"Progression",null,DrawHuntingLog,"MainWindow.HuntingLog.cs",Page.Collections,Tab:PageLabel("Navigation.HuntingLog"),About:PageLabel("HuntingLog.Description")),
        new(Page.Gear,"Progression",null,DrawGearPlanner,"MainWindow.Gear.cs, Gear/",Page.Collections,Tab:PageLabel("Navigation.Gear"),About:PageLabel("Gear.Description")),
        new(Page.RelicWeapons,"Content",null,DrawRelicWeapons,"MainWindow.RelicWeapons.cs, Relics/",Page.Collections,Tab:PageLabel("Navigation.RelicWeapons"),About:PageLabel("Relics.Subtitle")),
        // X-601 (0.1678, D-199): « Items », the item search, the storage, the sort and the markets, each a tab of the one page header (the wishlist stays apart).
        new(Page.Items,"Objects",PageLabel("Navigation.Items"),DrawItems,"MainWindow.Items*.cs, MainWindow.ItemSheet.cs",Tab:PageLabel("Navigation.ItemSearch"),About:PageLabel("ItemsUi.Description")),
        new(Page.Storage,"Objects",null,DrawStorage,"MainWindow.Storage.cs, MainWindow.Bank.cs",Page.Items,Tab:PageLabel("Navigation.Storage"),About:PageLabel("StorageUi.Description")),
        new(Page.SortPreview,"Objects",null,DrawSortPreview,"MainWindow.Sort*.cs, Inventory/",Page.Items,Tab:PageLabel("Navigation.Sort"),About:PageLabel("SortUi.Subtitle")),
        new(Page.Market,"Objects",null,DrawMarket,"MainWindow.Market*.cs, Market/",Page.Items,Tab:PageLabel("Navigation.Market"),About:PageLabel("MarketUi.Description")),
        // X-127 (0.1106): the wishlist, a tool of its own (it was a tab of Collections).
        new(Page.Wishlist,"Objects",PageLabel("Navigation.Wishlist"),DrawWishlistPage,"MainWindow.Wishlist.cs, Items/Wishlist*.cs"),
        // X-600 (0.1573): the tabs of a family are drawn in the order of this list.
        // X-601 (0.1679, D-199): « Crafting » also holds the macro chains.
        new(Page.CraftSimulation,"Production",PageLabel("Navigation.Crafting"),DrawCraftSimulation,"MainWindow.Craft*.cs, Craft/",Tab:PageLabel("CraftSimulation.Title"),About:PageLabel("CraftSimulation.Description"),TabAction:(PageLabel("CraftSimulation.Notebook"),BackToCraftItem)),
        new(Page.CraftPresets,"Production",null,DrawCraftPresets,"MainWindow.Craft*.cs, Craft/",Page.CraftSimulation,Tab:PageLabel("CraftPresets.Browse"),About:PageLabel("CraftPresets.Description")),
        new(Page.CraftLive,"Production",null,DrawCraftLive,"MainWindow.Craft*.cs, Craft/",Page.CraftSimulation,Tab:PageLabel("CraftSimulation.Live"),About:PageLabel("CraftLiveUi.Description")),
        new(Page.MacroChain,"Production",null,DrawMacroChain,"MainWindow.MacroChain*.cs, Macros/",Page.CraftSimulation,Tab:PageLabel("Navigation.Macros"),About:PageLabel("MacroUi.Description")),
        // X-601 (0.1679, D-199): « Gathering » also holds ocean fishing.
        new(Page.Gathering,"Production",PageLabel("Navigation.Gathering"),DrawGatheringBrowser,"MainWindow.Gathering*.cs",Tab:PageLabel("GatheringBrowser.Title"),About:PageLabel("GatheringBrowser.Description")),
        new(Page.GatheringGp,"Production",null,DrawGatheringGp,"MainWindow.Gathering*.cs",Page.Gathering,Tab:PageLabel("Category.GatheringGp"),About:PageLabel("GpUi.Subtitle")),
        new(Page.GatheringCollectables,"Production",null,DrawCollectablesPage,"MainWindow.CollectableTable.cs, Gathering/Collectables.cs",Page.Gathering,Tab:()=>localization.Format("CollectableTable.Title",Collectables?.Catalog?.Count ?? 0),About:PageLabel("CollectableTable.Help")),
        new(Page.OceanFishing,"Production",null,DrawOceanFishing,"MainWindow.OceanFishing.cs",Page.Gathering,Tab:PageLabel("Navigation.OceanFishing"),About:PageLabel("OceanFishing.Description")),
        new(Page.IslandSanctuary,"Production",PageLabel("Navigation.IslandSanctuary"),DrawIslandSanctuary,"MainWindow.IslandSanctuary.cs, Island/"),
        // X-601 (0.1679, D-199): « World », the map, the weather, the FATEs, the shops, the loot of duties and monsters and the discoveries, each a tab of the one page header.
        new(Page.Maps,"World",PageLabel("Navigation.World"),DrawMaps,"MainWindow.Maps.cs, MainWindow.MapView.cs",Tab:PageLabel("Navigation.Map"),About:PageLabel("MapsUi.Description")),
        new(Page.Weather,"World",null,DrawWeather,"MainWindow.Weather.cs, Gathering/WeatherForecast.cs",Page.Maps,Tab:PageLabel("Category.Weather"),About:()=>weatherZone),
        new(Page.Fates,"World",null,DrawFates,"MainWindow.Fates.cs, Fields/",Page.Maps,Tab:PageLabel("Navigation.Fates"),About:PageLabel("Fates.Subtitle")),
        new(Page.NpcVendors,"World",null,DrawNpcVendors,"MainWindow.NpcVendors.cs, Npcs/",Page.Maps,Tab:PageLabel("Navigation.Shops"),About:PageLabel("NpcUi.Subtitle")),
        new(Page.DutyLoot,"World",null,DrawDutyLoot,"MainWindow.Duties.cs, Duties/",Page.Maps,Tab:PageLabel("Category.DutyLoot"),About:PageLabel("DutyUi.Description")),
        new(Page.MonsterLoot,"World",null,DrawMonsterLoot,"MainWindow.Monsters.cs, Monsters/",Page.Maps,Tab:PageLabel("Category.MonsterLoot"),About:PageLabel("MonsterUi.Subtitle")),
        new(Page.Discoveries,"World",null,DrawDiscoveries,"MainWindow.Discoveries.cs, Discoveries/",Page.Maps,Tab:PageLabel("Navigation.Discoveries"),About:PageLabel("DiscoveriesUi.Help")),
        // X-100 (0.998, DEC-048): « Tracking », the timers and reminders together.
        new(Page.Alarms,"Tracking",()=>localization.Format("Navigation.Alarms",AlarmsToolCount),DrawAlarmsPage,"MainWindow.Alarms.cs, Alarms/"),
        new(Page.Reminders,"Tracking",()=>ReminderAlarmCount is var reminderAlarms and >0 ? localization.Format("Navigation.RemindersCount",reminderAlarms):localization.Get("Navigation.Reminders"),DrawReminders,"MainWindow.Reminders.cs, Reminders/"),
        new(Page.Garden,"Tracking",()=>GardenAlarmCount is var gardenAlarms and >0 ? localization.Format("Navigation.GardenCount",gardenAlarms):localization.Get("Navigation.Garden"),DrawGardenPage,"MainWindow.Garden.cs, MainWindow.GardenCrosses.cs"),
        new(Page.FreeCompany,"Tracking",PageLabel("Navigation.FreeCompany"),DrawFreeCompany,"MainWindow.FreeCompany.cs, MainWindow.RoutePlanner.cs, MainWindow.VesselBuilds.cs, FreeCompany/"),
        // 0.341 (user request): the Character page is gone (the Gear page shows the same real values); the chocobo has its own page.
        // X-601 (0.1680, D-199): « Quests », the journal, the levequests, the seasonal events, the allied societies and the grand companies.
        new(Page.Quests,"Progression",PageLabel("Navigation.Quests"),DrawQuests,"MainWindow.Quests.cs, Quests/",Tab:PageLabel("Navigation.Journal"),About:PageLabel("QuestsBrowser.Description")),
        new(Page.Leves,"Progression",null,DrawLeves,"MainWindow.Leves.cs",Page.Quests,Tab:PageLabel("Navigation.Leves"),About:PageLabel("Leves.Description")),
        new(Page.Events,"Progression",null,DrawEventsPage,"MainWindow.Events.cs, Events/",Page.Quests,Tab:PageLabel("Navigation.Events"),About:PageLabel("Events.Description")),
        new(Page.AlliedSocieties,"Progression",null,DrawAlliedSocieties,"MainWindow.AlliedSocieties.cs, Societies/",Page.Quests,Tab:PageLabel("Navigation.AlliedSocieties"),About:PageLabel("Allied.Subtitle")),
        new(Page.GrandCompanies,"Progression",null,DrawGrandCompanies,"MainWindow.GrandCompanies.cs, GrandCompanies/",Page.Quests,Tab:PageLabel("Navigation.GrandCompanies"),About:PageLabel("GrandCompany.Subtitle")),
        // X-601 (0.1680, D-199): « Deliveries », the special and the custom deliveries.
        new(Page.SpecialDeliveries,"Progression",PageLabel("Navigation.Deliveries"),DrawSpecialDeliveries,"MainWindow.SpecialDeliveries.cs, Deliveries/SpecialDeliveries.cs",Tab:PageLabel("Navigation.SpecialDeliveries"),About:PageLabel("SpecialDeliveries.Subtitle")),
        new(Page.CustomDeliveries,"Progression",null,DrawCustomDeliveries,"MainWindow.CustomDeliveries.cs, Deliveries/",Page.SpecialDeliveries,Tab:PageLabel("Navigation.CustomDeliveries"),About:PageLabel("CustomDeliveries.Subtitle")),
        // X-100 (0.998, DEC-048): « Content », one family of content per page (X-90, X-92).
        // X-601 (0.1680, D-199): « Duty » (the game's name for its contents), the field operations and the deep dungeons.
        new(Page.FieldContent,"Content",PageLabel("Navigation.Duty"),DrawFieldContent,"MainWindow.FieldContent.cs, Fields/",Tab:PageLabel("Navigation.FieldContent"),About:PageLabel("FieldContent.Subtitle")),
        new(Page.DeepDungeons,"Content",null,DrawDeepDungeons,"MainWindow.DeepDungeons.cs, DeepDungeons/",Page.FieldContent,Tab:PageLabel("Navigation.DeepDungeons"),About:PageLabel("DeepDungeon.Subtitle")),
        // X-601 (0.1680, D-199): « Gold Saucer », the fashion report, Triple Triad and the Mini Cactpot.
        new(Page.Fashion,"GoldSaucer",PageLabel("Category.GoldSaucer"),DrawFashion,"MainWindow.Fashion*.cs, Fashion/",Tab:PageLabel("Category.Fashion"),About:PageLabel("FashionUi.Description")),
        new(Page.TripleTriad,"GoldSaucer",null,DrawTripleTriad,"MainWindow.TripleTriad.cs, MainWindow.Triad*.cs, MiniGames/",Page.Fashion,Tab:PageLabel("Category.Triad"),About:PageLabel("TriadUi.Description")),
        new(Page.MiniCactpot,"GoldSaucer",null,DrawMiniCactpot,"MainWindow.MiniCactpot.cs, MiniGames/",Page.Fashion,Tab:PageLabel("Category.Cactpot"),About:PageLabel("CactpotUi.Description")),
        // X-100 (0.998, DEC-048): « References », what is looked up.
        // X-601 (0.1681, D-199): « Help », the help, the lexicon and the credits.
        new(Page.Help,"References",PageLabel("Navigation.Help"),DrawHelpPage,"MainWindow.Help.cs, Help/",Tab:PageLabel("Navigation.Help"),About:PageLabel("PageHelp.HelpPurpose")),
        new(Page.Lexicon,"References",null,DrawLexicon,"MainWindow.Lexicon.cs, Lexicon/",Page.Help,Tab:PageLabel("Navigation.Lexicon"),About:PageLabel("Lexicon.Subtitle")),
        new(Page.Credits,"References",null,DrawCredits,"MainWindow.Credits.cs, Credits/",Page.Help,Tab:PageLabel("Navigation.Credits"),About:()=>localization.Format("Credits.Count",TenshoAssistant.Credits.CreditCatalog.All.Count)),
        // X-601 (0.1681, D-199): « Settings », the settings and, in developer mode, the diagnostics.
        new(Page.Settings,"Preferences",PageLabel("Navigation.Settings"),DrawSettings,"MainWindow.Settings.cs, MainWindow.Advanced.cs",Tab:PageLabel("Navigation.Settings"),About:PageLabel("Settings.Description")),
        new(Page.Diagnostics,"Preferences",null,DrawDiagnostics,"MainWindow.Diagnostics.cs, MainWindow.Advanced.cs",Page.Settings,Tab:PageLabel("Navigation.Diagnostics"),About:PageLabel("DiagnosticsUi.Help"),Visible:()=>developerMode),
        // New tools: scripts/New-Tool.ps1 adds their line above this one.
    ];
    /// <summary>The navigation's groups, in the order of the list; a page without a label is not an entry.</summary>
    // X-601 (0.1681): the side bar is flat but for the categories of PageFamilies.SideBarCategories (« Tracking »): entries that follow each other
    // in the same category make one group; the entries around it are drawn without a header.
    private PageGroup[] PageGroups=>pageGroups??=[.. TenshoAssistant.UI.PageFamilies.Runs(PageEntries.Where(entry=>entry.Label is not null).ToArray(),
        entry=>TenshoAssistant.UI.PageFamilies.SideBarCategory(entry.Category)).Select(run=>new PageGroup(run.Key,run.Items.Select(entry=>entry.Page).ToArray(),run.Items))];
    private PageEntry? PageEntryFor(Page shown)=>Array.Find(PageEntries,entry=>entry.Page==shown);
}
