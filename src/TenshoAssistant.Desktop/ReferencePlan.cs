namespace TenshoAssistant.Desktop;

/// <summary>
/// X-271 (R37): the reference images to take, in order (pure, tested): the loading state of a few pages before the catalogues (they come
/// first), every page of the application in the default theme, the home page in each theme, two small windows (the navigation folded),
/// the garden's trial patch in its two views, the dashboard with a trial hour. The pages kept in the game are not pictured.
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
        // X-284 M2: the error state of two pages whose sheets cannot be read (as after a game update): one read at once, one off the frame.
        foreach(var page in ErrorPages.Where(shown.Contains))plan.Add(new($"{page}-error",page,0,1280,860,"error"));
        return plan;
    }
}
