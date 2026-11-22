using TenshoAssistant.Help;

namespace TenshoAssistant;

// X-809 (0.1807, the user's report of 22/11): one way to follow a link to a tab or a Settings section (Help/NavTargets.cs); before, a link set
// the page only and the page reopened the tab or category last shown.
internal sealed partial class MainWindow
{
    // The tab a link asks of a bar (its id, the tab's key): taken once by OrderedTabs when it draws that bar.
    private (string Bar,string Tab)? navTabRequest;

    /// <summary>Opens a link's target: its page, then its Settings category (with the setting shown) or its tab.</summary>
    private void Open(NavTarget target)
    {
        if(!Enum.TryParse<Page>(target.Page,out var next))return;
        page=next;
        navTabRequest=null;
        if(next==Page.Settings)
        {
            if(target.Tab is { } tab && target.Category is { } category)OpenSettingsCategory(tab,category,target.SettingKey);
            return;
        }
        if(target.Tab is { } key && NavTargets.Bars.TryGetValue(target.Page,out var bar))navTabRequest=(bar,key);
    }

    /// <summary>The tab a link asks of this bar, taken once (null when none).</summary>
    private string? TakeNavTab(string bar)
    {
        if(navTabRequest is not { } request || request.Bar!=bar)return null;
        navTabRequest=null;
        return request.Tab;
    }
}
