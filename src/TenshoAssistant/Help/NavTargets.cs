namespace TenshoAssistant.Help;

/// <summary>
/// X-809 (the user's report of 22/11: since the new look, a link or an « Open this page » button often opened the wrong tab or section):
/// where a link goes. <see cref="Page"/> is a page's name; <see cref="Tab"/> is the key of a tab of the page's bar (<see cref="NavTargets.Bars"/>),
/// or, on Settings, the Settings tab; <see cref="Category"/> is a Settings category and <see cref="SettingKey"/> a setting shown in it.
/// X-853: a Settings target may name its tab alone; the tab then opens on the category the player chose last there.
/// </summary>
public sealed record NavTarget(string Page,string? Tab=null,string? Category=null,string? SettingKey=null);

/// <summary>
/// X-809: the one table of Tensho's links to a tab or a section (FAQ answers, Home's « Today » lines and next deadline, the voyages link of
/// Reminders). MainWindow.Open(target) follows a target: the page, then its tab (asked of the page's bar until it is open) or its Settings
/// category. NavTargets1809Tests reads every target against the pages, tab bars and Settings categories as they are now, and refuses a key used
/// in the code that is not in the table: a tab renamed or moved fails the test instead of opening the wrong place.
/// X-813 (review 0.1812): the links that chose a tab by a flag (the item sheet's Stocks, Craft, Uses and Gathering; the Storage tabs; the
/// collections' Music, Titles and Achievements) and the Settings categories written in the code (the server bar's details, the tools'
/// « Settings ») are rows of this table too, followed by MainWindow.OpenLink(key); NavTargets1852Tests refuses a flag or a category in the code.
/// X-850 (review 0.1852, I1): Triple Triad's and the crafting page's tabs too (NavTargets1855Tests).
/// X-853 (review 0.1858, I1): X-850 said one jump was left outside; four more were: Settings › Bookmarks to the Maps page's Markers tab, the map's
/// « Use in tool », a monster's place and a chosen bookmark to its Maps tab (a request number of the page's own, dropped after one frame), the
/// map's cog to the Settings tab « Map » in plain text, and the Free Company page's « Plan routes » and « Build » (a request number too). They
/// are rows now; NavTargets1859Tests refuses any tab request field but MainWindow.Open's two, and any assignment of them outside a link followed.
/// One jump stays outside, on purpose: a collection kind (focusCollectionKind) chosen from an item's kind at run time (a mount, a minion, a card
/// of the extra collections…); it is an enum, so a renamed kind breaks the build, and it is kept until its tab is open, as the table's requests are.
/// </summary>
public static class NavTargets
{
    /// <summary>The tab bar (its id in OrderedTabs or DetailTabs) of a page whose tabs a link may ask for.</summary>
    public static readonly IReadOnlyDictionary<string,string> Bars=new Dictionary<string,string>
    {
        ["Alarms"]="AlarmsTabs",
        ["FreeCompany"]="FreeCompanyTabs",
        ["Garden"]="GardenTabs",
        ["Reminders"]="ReminderTabs",
        // X-813: the item sheet, Storage and the collections.
        ["Items"]="ItemSheet",
        ["Storage"]="StorageTabs",
        ["Collections"]="CollectionKinds",
        // X-850 (review 0.1852, I1): Triple Triad and the crafting page.
        ["TripleTriad"]="TriadNotebook",
        ["CraftSimulation"]="CraftPageTabs",
        // X-853 (review 0.1858, I1): the Maps page.
        ["Maps"]="MapsTabs",
    };

    public static readonly IReadOnlyDictionary<string,NavTarget> Table=new Dictionary<string,NavTarget>
    {
        // FAQ answers (Help/Faq.cs): a Settings answer opens the category it names, not the one last shown.
        ["Faq.Language"]=new("Settings","General","Appearance"),
        ["Faq.Internet"]=new("Settings","Data","Sources"),
        ["Faq.Offline"]=new("Settings","Data","Sources"),
        ["Faq.Map"]=new("Settings","Map","MapWindow","MapView.ReplaceGameMap"),
        ["Faq.Panels"]=new("Settings","Panels","GamePanels"),
        ["Faq.ChatWindow"]=new("Settings","Panels","ChatWindow"),
        ["Faq.ChatHistory"]=new("Settings","Panels","ChatHistory","ChatWindow.KeepHistory"),
        ["Faq.Reset"]=new("Settings","Advanced","Restore"),
        ["Faq.DesktopLink"]=new("Settings","Advanced","Desktop","Advanced.DesktopLink"),
        ["Faq.DesktopInstall"]=new("Settings","Advanced","Desktop"),
        ["Faq.GardenText"]=new("Garden","Garden"),
        // Home › Today (MainWindow.HomeToday.cs): each line and the next deadline open the tab that shows them.
        ["Home.Reminders"]=new("Reminders","Timers"),
        ["Home.Finished"]=new("Reminders","Timers"),
        ["Home.Vessels"]=new("FreeCompany","Vessels"),
        ["Home.Quests"]=new("Quests"),
        ["Home.AlarmsJobs"]=new("Alarms","Jobs"),
        ["Home.AlarmsOther"]=new("Alarms","Other"),
        ["Home.Garden"]=new("Garden","Garden"),
        // Reminders › Timers › voyages: « Details ».
        ["Reminders.Voyages"]=new("FreeCompany","Vessels"),
        // X-813: the item sheet's tabs (an item's links from the menus, the tools and the other pages); « Sheet » keeps the tab last shown.
        ["Item.Sheet"]=new("Items"),
        ["Item.Overview"]=new("Items","Overview"),
        ["Item.Stocks"]=new("Items","Stocks"),
        ["Item.Craft"]=new("Items","Craft"),
        ["Item.Uses"]=new("Items","Uses"),
        ["Item.Gathering"]=new("Items","Gathering"),
        // X-813: Storage's tabs (a character's storages, the currencies and what a currency buys).
        ["Storage.Bank"]=new("Storage","Bank"),
        ["Storage.Currencies"]=new("Storage","Currencies"),
        ["Storage.CurrencyValue"]=new("Storage","CurrencyValue"),
        // X-813: the collections' tabs opened by a link (a roll, a title, an achievement).
        ["Collections.Music"]=new("Collections","Music"),
        ["Collections.Titles"]=new("Collections","Titles"),
        ["Collections.Achievements"]=new("Collections","Achievements"),
        // X-813: the server bar's « Details » and its family's click (X-724: its category).
        ["Dashboard.ServerBar"]=new("Settings","Tracking","ServerBar"),
        // X-813: the tools' « Settings » button (DrawToolSettingsLink): « Panels. » and the category of the Panels tab.
        ["Panels.Crafting"]=new("Settings","Panels","Crafting"),
        ["Panels.CraftMacros"]=new("Settings","Panels","CraftMacros"),
        ["Panels.Gathering"]=new("Settings","Panels","Gathering"),
        ["Panels.Macros"]=new("Settings","Panels","Macros"),
        ["Panels.Games"]=new("Settings","Panels","Games"),
        // X-850 (review 0.1852, I1): Triple Triad's tabs (an NPC's « Triple Triad tool », the Deck button of the settings, « Show in Triple
        // Triad » from the menus) and the crafting page's (a recipe opened in the simulation or its preparation).
        ["Triad.Notebook"]=new("TripleTriad","Notebook"),
        ["Triad.Deck"]=new("TripleTriad","Deck"),
        ["Triad.Npcs"]=new("TripleTriad","Npcs"),
        ["Craft.Simulation"]=new("CraftSimulation","Simulation"),
        ["Craft.Preparation"]=new("CraftSimulation","Preparation"),
        // X-853 (review 0.1858, I1): the Maps page's tabs (Settings › Bookmarks; the map's « Use in tool », a monster's place, a chosen
        // bookmark), the Free Company page's (« Plan routes » of the airship sectors, a route's « Build ») and the map's cog (the Map tab of
        // Settings, on the category chosen last there).
        ["Maps.Maps"]=new("Maps","Maps"),
        ["Maps.Markers"]=new("Maps","Markers"),
        ["FreeCompany.Routes"]=new("FreeCompany","Routes"),
        ["FreeCompany.Builds"]=new("FreeCompany","Builds"),
        ["MapView.Settings"]=new("Settings","Map"),
    };

    /// <summary>A target of the table (a key not in it is a fault, refused by the test that reads the code's keys).</summary>
    public static NavTarget Of(string key)=>Table.TryGetValue(key,out var target) ? target:throw new KeyNotFoundException("No link target "+key);

    /// <summary>Where an FAQ answer's button goes: its row of the table, else its page alone.</summary>
    public static NavTarget? OfFaq(FaqEntry entry)=>Table.TryGetValue("Faq."+entry.Key,out var target) ? target
        :entry.Page is { } page ? new NavTarget(page):null;
}
