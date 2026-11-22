namespace TenshoAssistant.Help;

/// <summary>
/// X-809 (the user's report of 22/11: since the new look, a link or an « Open this page » button often opened the wrong tab or section):
/// where a link goes. <see cref="Page"/> is a page's name; <see cref="Tab"/> is the key of a tab of the page's bar (<see cref="NavTargets.Bars"/>),
/// or, on Settings, the Settings tab; <see cref="Category"/> is a Settings category and <see cref="SettingKey"/> a setting shown in it.
/// </summary>
public sealed record NavTarget(string Page,string? Tab=null,string? Category=null,string? SettingKey=null);

/// <summary>
/// X-809: the one table of Tensho's links to a tab or a section (FAQ answers, Home's « Today » lines and next deadline, the voyages link of
/// Reminders). MainWindow.Open(target) follows a target: the page, then its tab (asked of the page's bar once) or its Settings category.
/// NavTargets1809Tests reads every target against the pages, tab bars and Settings categories as they are now, and refuses a key used in
/// the code that is not in the table: a tab renamed or moved fails the test instead of opening the wrong place.
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
    };

    /// <summary>A target of the table (a key not in it is a fault, refused by the test that reads the code's keys).</summary>
    public static NavTarget Of(string key)=>Table.TryGetValue(key,out var target) ? target:throw new KeyNotFoundException("No link target "+key);

    /// <summary>Where an FAQ answer's button goes: its row of the table, else its page alone.</summary>
    public static NavTarget? OfFaq(FaqEntry entry)=>Table.TryGetValue("Faq."+entry.Key,out var target) ? target
        :entry.Page is { } page ? new NavTarget(page):null;
}
