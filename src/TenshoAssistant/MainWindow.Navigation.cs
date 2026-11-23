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
            // X-853: a Settings tab named alone opens on the category chosen last there.
            if(target.Tab is { } tab)
            {
                if(target.Category is { } category)OpenSettingsCategory(tab,category,target.SettingKey);
                else requestedSettingsTab=tab;
            }
            return;
        }
        if(target.Tab is { } key && NavTargets.Bars.TryGetValue(target.Page,out var bar))navTabRequest=(bar,key);
    }

    /// <summary>X-813: follows a link of the table (Help/NavTargets.cs) by its key; a key not in the table is a fault (NavTargets1852Tests
    /// reads every key the code passes here).</summary>
    private void OpenLink(string key)=>Open(NavTargets.Of(key));

    /// <summary>The tab a link asks of this bar (null when none). X-813: kept until that tab is open (<see cref="NavTabReached"/>), as the
    /// focus flags it replaces were: a bar not drawn yet, or drawn in a frame where the tab cannot be selected, still gets it.</summary>
    private string? TakeNavTab(string bar)=>navTabRequest is { } request && request.Bar==bar ? request.Tab:null;

    /// <summary>X-813: the tab a link asked of this bar is open, or the bar has no such tab (gone or hidden for this item): the request ends.</summary>
    private void NavTabReached(string bar){if(navTabRequest is { } request && request.Bar==bar)navTabRequest=null;}
}
