using Dalamud.Bindings.ImGui;
using TenshoAssistant.Help;

namespace TenshoAssistant;

// X-220 (0.1230, user's request of 12/11, K-36): one search at the top of the navigation: pages (title, then help text), items, quests and maps,
// at most five of each; a click opens the page, the item sheet, the quest or the Tensho map. Pages are matched on the frame (about a hundred
// labels, once per query); items, quests and maps off the frame, from the indexes already built. Nothing teleports and no keyboard shortcut.
internal sealed partial class MainWindow
{
    private string quickQuery=string.Empty;
    private string quickShown=string.Empty;
    private string quickRunning=string.Empty;
    private Task<QuickSearchHit[]>? quickTask;
    private QuickSearchHit[] quickHits=[];
    private QuickSearchHit[] quickPages=[];
    private (string Language,(Page Page,string Label,string Purpose)[] Pages)? quickPageIndex;

    private (Page Page,string Label,string Purpose)[] QuickPageIndex()
    {
        if(quickPageIndex is { } index && index.Language==localization.CurrentCode)return index.Pages;
        var pages=PageEntries.Where(entry=>entry.Label is not null).Select(entry=>(entry.Page,entry.Label!(),TopicOf(entry.Page).Purpose)).ToArray();
        quickPageIndex=(localization.CurrentCode,pages);
        return pages;
    }

    private void DrawQuickSearch()
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##QuickSearch",localization.Get("QuickSearch.Hint"),ref quickQuery,QuickSearch.MaxLength);
        Tip("QuickSearch.Tip");
        var query=quickQuery.Trim();
        var tokens=QuickSearch.Tokens(query);
        if(tokens.Length==0){quickHits=[];quickPages=[];quickShown=quickRunning=string.Empty;return;}
        if(quickTask is { IsCompleted:true } done)
        {
            if(done.IsCompletedSuccessfully){quickHits=done.Result;quickShown=quickRunning;}
            quickTask=null;
        }
        // A query typed while one runs starts once that one ends (one search at a time).
        if(quickTask is null && quickRunning!=query)StartQuickSearch(query,tokens);
        foreach(var group in quickPages.Concat(quickShown==query ? quickHits:[]).GroupBy(hit=>hit.Kind))
        {
            ImGui.TextDisabled(localization.Get("QuickSearch.Kind"+group.Key));
            foreach(var hit in group)
            {
                if(ImGui.Selectable($"{hit.Text}###Quick{hit.Kind}{hit.Key}"))OpenQuickHit(hit);
                if(hit.Detail.Length>0)TipText(hit.Detail);
            }
        }
        if(quickShown==query && quickPages.Length==0 && quickHits.Length==0)ImGui.TextDisabled(localization.Get("QuickSearch.None"));
        else if(quickShown!=query)ImGui.TextDisabled(localization.Get("QuickSearch.Searching"));
        ImGui.Separator();
    }

    private void StartQuickSearch(string query,string[] tokens)
    {
        // Pages on the frame: their labels follow the language and the places chosen for each side.
        var pages=QuickPageIndex().Where(entry=>ListedHere(entry.Page)).ToArray();
        var byTitle=QuickSearch.Top(pages,entry=>entry.Label,tokens);
        var byHelp=byTitle.Count<QuickSearch.PerKind ? QuickSearch.Top(pages.Except(byTitle),entry=>entry.Purpose,tokens,QuickSearch.PerKind-byTitle.Count):[];
        quickPages=byTitle.Concat(byHelp).Select(entry=>new QuickSearchHit(QuickSearchKind.Page,0,entry.Page.ToString(),entry.Label,entry.Purpose)).ToArray();
        var items=itemCatalog.State.Catalog;var questList=quests.State.Entries;var mapCatalog=maps.State.Catalog;
        quickRunning=query;
        quickTask=Task.Run(()=>
        {
            var hits=new List<QuickSearchHit>();
            if(items is not null)
                hits.AddRange(items.Search(query,QuickSearch.PerKind).Select(item=>new QuickSearchHit(QuickSearchKind.Item,item.Id,item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),item.Name)));
            hits.AddRange(QuickSearch.Top(questList,quest=>quest.Name,tokens).Select(quest=>new QuickSearchHit(QuickSearchKind.Quest,quest.Id,quest.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),quest.Name,quest.Category)));
            if(mapCatalog is not null)
                hits.AddRange(QuickSearch.Top(mapCatalog.Entries.Where(map=>map.Zone.Length>0),map=>$"{map.Zone} {map.Subzone}",tokens)
                    .Select(map=>new QuickSearchHit(QuickSearchKind.Place,map.MapId,map.MapId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        map.Subzone.Length>0 ? $"{map.Zone} · {map.Subzone}":map.Zone,map.Region)));
            return hits.ToArray();
        });
    }

    private void OpenQuickHit(QuickSearchHit hit)
    {
        switch(hit.Kind)
        {
            case QuickSearchKind.Page when Enum.TryParse<Page>(hit.Key,out var target):page=target;break;
            case QuickSearchKind.Item:SelectItem(hit.Id);OpenItemSheet();break;
            case QuickSearchKind.Quest:SelectQuest(hit.Id);page=Page.Quests;break;
            case QuickSearchKind.Place:ShowMapOnTenshoMap(hit.Id);break;
        }
        quickQuery=string.Empty;
    }
}
