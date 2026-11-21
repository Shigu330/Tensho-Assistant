namespace TenshoAssistant;

// 0.470 (TEMPLATES-001, DEC-029): every page in one list: the navigation, the drawing and the source shown to contributors all read it. A new tool adds one line here (scripts/New-Tool.ps1 does it).
internal sealed partial class MainWindow
{
    /// <param name="Page">The page; each one appears once, and a test checks it.</param>
    /// <param name="Category">Its group in the navigation (null: above the groups); for a tab of a family, only its group in the tool directory.</param>
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
        new(Page.Home,null,PageLabel("Navigation.Home"),DrawHome,"MainWindow.Home.cs"),
        // X-601 (0.1678, D-199): « Items », the item search, the storage, the sort and the markets, each a tab of the one page header (the wishlist stays apart).
        new(Page.Items,"Objects",PageLabel("Navigation.Items"),DrawItems,"MainWindow.Items*.cs, MainWindow.ItemSheet.cs",Tab:PageLabel("Navigation.ItemSearch"),About:PageLabel("ItemsUi.Description")),
        new(Page.Storage,"Objects",null,DrawStorage,"MainWindow.Storage.cs, MainWindow.Bank.cs",Page.Items,Tab:PageLabel("Navigation.Storage"),About:PageLabel("StorageUi.Description")),
        new(Page.SortPreview,"Objects",null,DrawSortPreview,"MainWindow.Sort*.cs, Inventory/",Page.Items,Tab:PageLabel("Navigation.Sort"),About:PageLabel("SortUi.Subtitle")),
        new(Page.Market,"Objects",null,DrawMarket,"MainWindow.Market*.cs, Market/",Page.Items,Tab:PageLabel("Navigation.Market"),About:PageLabel("MarketUi.Description")),
        // X-127 (0.1106): the wishlist, a tool of its own (it was a tab of Collections).
        new(Page.Wishlist,"Objects",PageLabel("Navigation.Wishlist"),DrawWishlistPage,"MainWindow.Wishlist.cs, Items/Wishlist*.cs"),
        // X-600 (0.1573): the tabs of a family are drawn in the order of this list.
        new(Page.CraftSimulation,"Production",PageLabel("Navigation.Crafting"),DrawCraftSimulation,"MainWindow.Craft*.cs, Craft/",Tab:PageLabel("CraftSimulation.Title"),About:PageLabel("CraftSimulation.Description"),TabAction:(PageLabel("CraftSimulation.Notebook"),BackToCraftItem)),
        new(Page.CraftPresets,"Production",null,DrawCraftPresets,"MainWindow.Craft*.cs, Craft/",Page.CraftSimulation,Tab:PageLabel("CraftPresets.Browse"),About:PageLabel("CraftPresets.Description")),
        new(Page.CraftLive,"Production",null,DrawCraftLive,"MainWindow.Craft*.cs, Craft/",Page.CraftSimulation,Tab:PageLabel("CraftSimulation.Live"),About:PageLabel("CraftLiveUi.Description")),
        new(Page.Gathering,"Production",PageLabel("Navigation.Gathering"),DrawGatheringBrowser,"MainWindow.Gathering*.cs",Tab:PageLabel("GatheringBrowser.Title"),About:PageLabel("GatheringBrowser.Description")),
        new(Page.GatheringGp,"Production",null,DrawGatheringGp,"MainWindow.Gathering*.cs",Page.Gathering,Tab:PageLabel("Category.GatheringGp"),About:PageLabel("GpUi.Subtitle")),
        new(Page.GatheringCollectables,"Production",null,DrawCollectablesPage,"MainWindow.CollectableTable.cs, Gathering/Collectables.cs",Page.Gathering,Tab:()=>localization.Format("CollectableTable.Title",Collectables?.Catalog?.Count ?? 0),About:PageLabel("CollectableTable.Help")),
        new(Page.OceanFishing,"Production",PageLabel("Navigation.OceanFishing"),DrawOceanFishing,"MainWindow.OceanFishing.cs"),
        new(Page.IslandSanctuary,"Production",PageLabel("Navigation.IslandSanctuary"),DrawIslandSanctuary,"MainWindow.IslandSanctuary.cs, Island/"),
        new(Page.MacroChain,"Production",PageLabel("Category.MacroChain"),DrawMacroChain,"MainWindow.MacroChain*.cs, Macros/"),
        new(Page.Maps,"World",PageLabel("Navigation.Map"),DrawMaps,"MainWindow.Maps.cs, MainWindow.MapView.cs"),
        new(Page.Weather,"World",PageLabel("Category.Weather"),DrawWeather,"MainWindow.Weather.cs, Gathering/WeatherForecast.cs"),
        new(Page.Fates,"World",PageLabel("Navigation.Fates"),DrawFates,"MainWindow.Fates.cs, Fields/"),
        new(Page.NpcVendors,"World",PageLabel("Category.NpcVendors"),DrawNpcVendors,"MainWindow.NpcVendors.cs, Npcs/"),
        new(Page.DutyLoot,"World",PageLabel("Category.DutyLoot"),DrawDutyLoot,"MainWindow.Duties.cs, Duties/"),
        new(Page.MonsterLoot,"World",PageLabel("Category.MonsterLoot"),DrawMonsterLoot,"MainWindow.Monsters.cs, Monsters/"),
        // X-100 (0.998, DEC-048): « Tracking », the timers and reminders together.
        new(Page.Alarms,"Tracking",()=>localization.Format("Navigation.Alarms",AlarmsToolCount),DrawAlarmsPage,"MainWindow.Alarms.cs, Alarms/"),
        new(Page.Reminders,"Tracking",()=>ReminderAlarmCount is var reminderAlarms and >0 ? localization.Format("Navigation.RemindersCount",reminderAlarms):localization.Get("Navigation.Reminders"),DrawReminders,"MainWindow.Reminders.cs, Reminders/"),
        new(Page.Garden,"Tracking",()=>GardenAlarmCount is var gardenAlarms and >0 ? localization.Format("Navigation.GardenCount",gardenAlarms):localization.Get("Navigation.Garden"),DrawGardenPage,"MainWindow.Garden.cs, MainWindow.GardenCrosses.cs"),
        new(Page.FreeCompany,"Tracking",PageLabel("Navigation.FreeCompany"),DrawFreeCompany,"MainWindow.FreeCompany.cs, MainWindow.RoutePlanner.cs, MainWindow.VesselBuilds.cs, FreeCompany/"),
        // 0.341 (user request): the Character page is gone (the Gear page shows the same real values); the chocobo has its own page.
        new(Page.Quests,"Progression",PageLabel("Navigation.Quests"),DrawQuests,"MainWindow.Quests.cs, Quests/"),
        new(Page.Events,"Progression",PageLabel("Navigation.Events"),DrawEventsPage,"MainWindow.Events.cs, Events/"),
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
        new(Page.AlliedSocieties,"Progression",PageLabel("Navigation.AlliedSocieties"),DrawAlliedSocieties,"MainWindow.AlliedSocieties.cs, Societies/"),
        new(Page.GrandCompanies,"Progression",PageLabel("Navigation.GrandCompanies"),DrawGrandCompanies,"MainWindow.GrandCompanies.cs, GrandCompanies/"),
        new(Page.SpecialDeliveries,"Progression",PageLabel("Navigation.SpecialDeliveries"),DrawSpecialDeliveries,"MainWindow.SpecialDeliveries.cs, Deliveries/SpecialDeliveries.cs"),
        new(Page.CustomDeliveries,"Progression",PageLabel("Navigation.CustomDeliveries"),DrawCustomDeliveries,"MainWindow.CustomDeliveries.cs, Deliveries/"),
        new(Page.Leves,"Progression",PageLabel("Navigation.Leves"),DrawLeves,"MainWindow.Leves.cs"),
        // X-100 (0.998, DEC-048): « Content », one family of content per page (X-90, X-92).
        new(Page.FieldContent,"Content",PageLabel("Navigation.FieldContent"),DrawFieldContent,"MainWindow.FieldContent.cs, Fields/"),
        new(Page.DeepDungeons,"Content",PageLabel("Navigation.DeepDungeons"),DrawDeepDungeons,"MainWindow.DeepDungeons.cs, DeepDungeons/"),
        new(Page.Fashion,"GoldSaucer",PageLabel("Category.Fashion"),DrawFashion,"MainWindow.Fashion*.cs, Fashion/"),
        new(Page.TripleTriad,"GoldSaucer",PageLabel("Category.Triad"),DrawTripleTriad,"MainWindow.TripleTriad.cs, MainWindow.Triad*.cs, MiniGames/"),
        new(Page.MiniCactpot,"GoldSaucer",PageLabel("Category.Cactpot"),DrawMiniCactpot,"MainWindow.MiniCactpot.cs, MiniGames/"),
        // X-100 (0.998, DEC-048): « References », what is looked up.
        new(Page.Lexicon,"References",PageLabel("Navigation.Lexicon"),DrawLexicon,"MainWindow.Lexicon.cs, Lexicon/"),
        new(Page.Help,"References",PageLabel("Navigation.Help"),DrawHelpPage,"MainWindow.Help.cs, Help/"),
        new(Page.Settings,"Preferences",PageLabel("Navigation.Settings"),DrawSettings,"MainWindow.Settings.cs, MainWindow.Advanced.cs"),
        // R-13 (0.320): everything Tensho uses, shown to every player.
        new(Page.Credits,"References",PageLabel("Navigation.Credits"),DrawCredits,"MainWindow.Credits.cs, Credits/"),
        new(Page.Diagnostics,"Preferences",PageLabel("Navigation.Diagnostics"),DrawDiagnostics,"MainWindow.Diagnostics.cs, MainWindow.Advanced.cs",Visible:()=>developerMode),
        new(Page.MyTools,null,PageLabel("Navigation.MyTools"),DrawMyTools,"MainWindow.MyTools.cs, MyTools/"),
        new(Page.Discoveries,"World",PageLabel("Navigation.Discoveries"),DrawDiscoveries,"MainWindow.Discoveries.cs, Discoveries/"),
        // New tools: scripts/New-Tool.ps1 adds their line above this one.
    ];
    /// <summary>The navigation's groups, in the order of the list; a page without a label is not an entry.</summary>
    private PageGroup[] PageGroups=>pageGroups??=PageEntries.Where(entry=>entry.Label is not null).GroupBy(entry=>entry.Category)
        .Select(group=>new PageGroup(group.Key,group.Select(entry=>entry.Page).ToArray(),group.ToArray())).ToArray();
    private PageEntry? PageEntryFor(Page shown)=>Array.Find(PageEntries,entry=>entry.Page==shown);
}
