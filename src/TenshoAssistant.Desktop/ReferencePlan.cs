namespace TenshoAssistant.Desktop;

/// <summary>
/// X-271 (R37): the reference images to take, in order (pure, tested): the loading state of a few pages before the catalogues (they come
/// first), every page of the application in the default theme, the home page in each theme, two small windows (the navigation folded),
/// the garden's trial patch in its two views, the dashboard with a trial hour, the retainers' table with trial retainers (X-430, 0.1415).
/// The pages kept in the game are not pictured.
/// </summary>
public static class ReferencePlan
{
    /// <summary>X-284 M2: the pages pictured in their error state (MainWindow.ForgetSheetPages knows how they forget their sheets).</summary>
    public static readonly string[] ErrorPages=["AlliedSocieties","RelicWeapons"];

    public sealed record Shot(string Id,string Page,int Theme,int Width,int Height,string Setup);

    public static IReadOnlyList<Shot> Plan(IEnumerable<string> pages)
    {
        var shown=pages.Where(page=>DesktopPages.Kind(page) is DesktopPageKind.AsIs or DesktopPageKind.NeedsLink).ToArray();
        var plan=new List<Shot>();
        foreach(var page in new[]{"Home","Weather","DutyLoot"}.Where(shown.Contains))plan.Add(new($"{page}-loading",page,0,1280,860,"loading"));
        foreach(var page in shown)plan.Add(new(page,page,0,1280,860,""));
        for(var theme=1;theme<=6;theme++)plan.Add(new($"Home-theme{theme}","Home",theme,1280,860,""));
        plan.Add(new("Home-narrow","Home",0,500,820,""));
        plan.Add(new("Settings-small","Settings",0,900,600,""));
        if(shown.Contains("Garden")){plan.Add(new("Garden-trial-icons","Garden",0,1280,860,"garden-icons"));plan.Add(new("Garden-trial-detailed","Garden",0,1280,860,"garden-detailed"));}
        if(shown.Contains("Diagnostics"))plan.Add(new("Diagnostics-dashboard","Diagnostics",0,1280,860,"dashboard"));
        // X-430 (0.1415): Reminders › Status with two trial characters and four retainers (the table of X-196, every state of a sale).
        if(shown.Contains("Reminders"))plan.Add(new("Reminders-retainers","Reminders",0,1280,860,"retainers"));
        // X-284 M2: the error state of two pages whose sheets cannot be read (as after a game update): one read at once, one off the frame.
        foreach(var page in ErrorPages.Where(shown.Contains))plan.Add(new($"{page}-error",page,0,1280,860,"error"));
        return plan;
    }

    /// <summary>X-430 (0.1415): one trial retainer of the « Reminders-retainers » picture; times in hours from the pictures' clock (null: none).</summary>
    public sealed record TrialRetainer(ulong ContentId,string Character,string Name,string Job,int Level,long Gil,int Items,int MarketItems,double? MarketEndsIn,
        uint Venture,double? VentureDoneIn,string Town,int? Stacks,int? HqStacks,int? Crystals);

    /// <summary>
    /// X-430 (0.1415): the trial retainers of two characters, as the plugin keeps them (Characters/&lt;id&gt;.json): sales going on, ending
    /// within the day (orange), ended (red), none; a venture running, one done, none; a stock known and one unknown; a job unknown.
    /// </summary>
    public static readonly TrialRetainer[] RetainerTrial=
    [
        new(1001,"Trial Alpha","Mira","BTN",100,1_250_000,87,12,120,712,0.5,"Limsa Lominsa",80,4,3200),
        new(1001,"Trial Alpha","Oren","MIN",92,40_200,140,5,6,0,null,"Gridania",null,null,null),
        new(1002,"Trial Beta","Sela","FSH",70,9_800,30,3,-24,401,-1,"Ul'dah",25,0,0),
        new(1002,"Trial Beta","Tamm","",0,0,0,0,null,0,null,"",null,null,null),
    ];
}
