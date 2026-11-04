namespace TenshoAssistant;

// 0.470 (TEMPLATES-001, DEC-029): every page in one list: the navigation, the drawing and the source shown to contributors all read it. A new tool adds one line here (scripts/New-Tool.ps1 does it).
internal sealed partial class MainWindow
{
    /// <param name="Page">The page; each one appears once, and a test checks it.</param>
    /// <param name="Category">Its group in the navigation (null: above the groups).</param>
    /// <param name="Label">Its name in the navigation; null for a page reached from the tabs of its parent.</param>
    /// <param name="Draw">Draws the page.</param>
    /// <param name="Source">Where its code starts, shown to contributors.</param>
    /// <param name="Parent">The navigation entry that stays selected on this page.</param>
    /// <param name="Visible">Null: always shown in the navigation.</param>
    private sealed record PageEntry(Page Page,string? Category,Func<string>? Label,Action Draw,string Source,Page? Parent=null,Func<bool>? Visible=null);
    private sealed record PageGroup(string? Category,Page[] Pages,PageEntry[] Entries);

    private Func<string> PageLabel(string key)=>()=>localization.Get(key);
    private PageEntry[]? pageEntries;
    private PageGroup[]? pageGroups;
    private PageEntry[] PageEntries=>pageEntries??=
    [
        new(Page.Home,null,PageLabel("Navigation.Home"),DrawHome,"MainWindow.Home.cs"),
        new(Page.Items,"Objects",PageLabel("Navigation.Items"),DrawItems,"MainWindow.Items*.cs, MainWindow.ItemSheet.cs"),
        new(Page.Storage,"Objects",PageLabel("Navigation.Storage"),DrawStorage,"MainWindow.Storage.cs, MainWindow.Bank.cs"),
        new(Page.SortPreview,"Objects",PageLabel("Category.Sorting"),DrawSortPreview,"MainWindow.Sort*.cs, Inventory/"),
        new(Page.Market,"Objects",PageLabel("Navigation.Market"),DrawMarket,"MainWindow.Market*.cs, Market/"),
        new(Page.CraftSimulation,"Production",PageLabel("Navigation.Crafting"),DrawCraftSimulation,"MainWindow.Craft*.cs, Craft/"),
        new(Page.CraftLive,"Production",null,DrawCraftLive,"MainWindow.Craft*.cs, Craft/",Page.CraftSimulation),
        new(Page.CraftPresets,"Production",null,DrawCraftPresets,"MainWindow.Craft*.cs, Craft/",Page.CraftSimulation),
        new(Page.Gathering,"Production",PageLabel("Navigation.Gathering"),DrawGatheringBrowser,"MainWindow.Gathering*.cs"),
        new(Page.GatheringGp,"Production",null,DrawGatheringGp,"MainWindow.Gathering*.cs",Page.Gathering),
        new(Page.GatheringCollectables,"Production",null,DrawCollectablesPage,"MainWindow.CollectableTable.cs, Gathering/Collectables.cs",Page.Gathering),
        new(Page.OceanFishing,"Production",PageLabel("Navigation.OceanFishing"),DrawOceanFishing,"MainWindow.OceanFishing.cs"),
        new(Page.MacroChain,"Production",PageLabel("Category.MacroChain"),DrawMacroChain,"MainWindow.MacroChain*.cs, Macros/"),
        new(Page.Maps,"World",PageLabel("Navigation.Map"),DrawMaps,"MainWindow.Maps.cs, MainWindow.MapView.cs"),
        new(Page.Weather,"World",PageLabel("Category.Weather"),DrawWeather,"MainWindow.Weather.cs, Gathering/WeatherForecast.cs"),
        new(Page.Alarms,"World",()=>localization.Format("Navigation.Alarms",configuration.Alarms?.Count ?? 0),DrawAlarmsPage,"MainWindow.Alarms.cs, Alarms/"),
        new(Page.Reminders,"World",PageLabel("Navigation.Reminders"),DrawReminders,"MainWindow.Reminders.cs, Reminders/"),
        new(Page.Garden,"World",PageLabel("Navigation.Garden"),DrawGardenPage,"MainWindow.Garden.cs, MainWindow.GardenCrosses.cs"),
        new(Page.FreeCompany,"World",PageLabel("Navigation.FreeCompany"),DrawFreeCompany,"MainWindow.FreeCompany.cs, MainWindow.RoutePlanner.cs, MainWindow.VesselBuilds.cs, FreeCompany/"),
        new(Page.NpcVendors,"World",PageLabel("Category.NpcVendors"),DrawNpcVendors,"MainWindow.NpcVendors.cs, Npcs/"),
        new(Page.DutyLoot,"World",PageLabel("Category.DutyLoot"),DrawDutyLoot,"MainWindow.Duties.cs, Duties/"),
        new(Page.MonsterLoot,"World",PageLabel("Category.MonsterLoot"),DrawMonsterLoot,"MainWindow.Monsters.cs, Monsters/"),
        // 0.341 (user request): the Character page is gone (the Gear page shows the same real values); the chocobo has its own page.
        new(Page.Quests,"Progression",PageLabel("Navigation.Quests"),DrawQuests,"MainWindow.Quests.cs, Quests/"),
        new(Page.Events,"Progression",PageLabel("Navigation.Events"),DrawEventsPage,"MainWindow.Events.cs, Events/"),
        new(Page.Collections,"Progression",PageLabel("Navigation.Collection"),()=>DrawCollections(),"MainWindow.Collections.cs, Collections/"),
        new(Page.BlueMagic,"Progression",null,()=>DrawCollections(true),"MainWindow.Collections.cs, Collections/",Page.Collections),
        new(Page.Gear,"Progression",PageLabel("Navigation.Gear"),DrawGearPlanner,"MainWindow.Gear.cs, Gear/"),
        new(Page.Chocobo,"Progression",PageLabel("Navigation.Chocobo"),DrawChocoboPage,"MainWindow.ChocoboBarding.cs, Gear/ChocoboBarding.cs, Gear/ChocoboReader.Dalamud.cs"),
        new(Page.Leves,"Progression",PageLabel("Navigation.Leves"),DrawLeves,"MainWindow.Leves.cs"),
        new(Page.HuntingLog,"Progression",PageLabel("Navigation.HuntingLog"),DrawHuntingLog,"MainWindow.HuntingLog.cs"),
        new(Page.Beastmaster,"Progression",PageLabel("Navigation.Beastmaster"),DrawBeastmaster,"MainWindow.Beastmaster.cs, Beastmaster/"),
        new(Page.Fashion,"GoldSaucer",PageLabel("Category.Fashion"),DrawFashion,"MainWindow.Fashion*.cs, Fashion/"),
        new(Page.TripleTriad,"GoldSaucer",PageLabel("Category.Triad"),DrawTripleTriad,"MainWindow.TripleTriad.cs, MainWindow.Triad*.cs, MiniGames/"),
        new(Page.MiniCactpot,"GoldSaucer",PageLabel("Category.Cactpot"),DrawMiniCactpot,"MainWindow.MiniCactpot.cs, MiniGames/"),
        new(Page.Settings,"Preferences",PageLabel("Navigation.Settings"),DrawSettings,"MainWindow.Settings.cs, MainWindow.Advanced.cs"),
        // R-13 (0.320): everything Tensho uses, shown to every player.
        new(Page.Credits,"Preferences",PageLabel("Navigation.Credits"),DrawCredits,"MainWindow.Credits.cs, Credits/"),
        new(Page.Diagnostics,"Preferences",PageLabel("Navigation.Diagnostics"),DrawDiagnostics,"MainWindow.Diagnostics.cs, MainWindow.Advanced.cs",Visible:()=>developerMode),
        new(Page.MyTools,null,PageLabel("Navigation.MyTools"),DrawMyTools,"MainWindow.MyTools.cs, MyTools/"),
        // New tools: scripts/New-Tool.ps1 adds their line above this one.
    ];
    /// <summary>The navigation's groups, in the order of the list; a page without a label is not an entry.</summary>
    private PageGroup[] PageGroups=>pageGroups??=PageEntries.Where(entry=>entry.Label is not null).GroupBy(entry=>entry.Category)
        .Select(group=>new PageGroup(group.Key,group.Select(entry=>entry.Page).ToArray(),group.ToArray())).ToArray();
    private PageEntry? PageEntryFor(Page shown)=>Array.Find(PageEntries,entry=>entry.Page==shown);
}
