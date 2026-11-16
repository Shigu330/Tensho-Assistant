namespace TenshoAssistant.UI;

/// <summary>
/// X-521 M2 (D-180): the category chosen in each tab of the Settings page, kept in the configuration (tab key → category id). Set only
/// changes the map when the choice is new, so the page saves once per click, never every frame.
/// </summary>
public static class SettingsCategoryChoice
{
    /// <summary>True when <paramref name="category"/> is a new choice for <paramref name="tab"/> (then written); false when it was already chosen.</summary>
    public static bool Set(IDictionary<string,string> chosen,string tab,string category)
    {
        if(string.IsNullOrEmpty(category) || chosen.TryGetValue(tab,out var before) && before==category)return false;
        chosen[tab]=category;
        return true;
    }
}
