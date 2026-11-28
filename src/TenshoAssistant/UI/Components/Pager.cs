using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-607 (0.1955, user request of 18/11: « code as easy as possible to maintain, above all for a human »): the page bar of a long list,
// « < Page n / m > », in one place: the previous button greyed on the first page, the next one on the last, each with its tooltip, the page
// line between them. The keys are given whole (a page's texts stay findable by their name in the code). UiPager1955Tests.
internal sealed partial class MainWindow
{
    /// <summary>The page bar: <paramref name="page"/> counts from 0; <paramref name="pageKey"/> is formatted with (page + 1, pages).</summary>
    private void Pager(string id,ref int page,int pages,string pageKey,string previousTipKey,string nextTipKey)
    {
        ImGui.BeginDisabled(page<=0);
        try{if(ImGui.SmallButton("<###"+id+"Previous"))page--;}
        finally{ImGui.EndDisabled();}
        Tip(previousTipKey);
        ImGui.SameLine();ImGui.TextUnformatted(localization.Format(pageKey,page+1,pages));
        ImGui.SameLine();
        ImGui.BeginDisabled(page>=pages-1);
        try{if(ImGui.SmallButton(">###"+id+"Next"))page++;}
        finally{ImGui.EndDisabled();}
        Tip(nextTipKey);
    }
}
