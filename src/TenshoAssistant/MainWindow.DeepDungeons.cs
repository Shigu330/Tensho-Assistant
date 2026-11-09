using Dalamud.Bindings.ImGui;
using TenshoAssistant.DeepDungeons;
using TenshoAssistant.Duties;

namespace TenshoAssistant;

// W-15 (0.718): the deep dungeons, to get ready outside: Tensho stops inside them (DEC-038). No fight advice. docs/evidence/0.718-deep-dungeons.md.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<DeepDungeonEntry>?>? DeepDungeonLoad { get; set; }
    private Task<IReadOnlyList<DeepDungeonEntry>?>? deepDungeonTask;
    private IReadOnlyList<DeepDungeonEntry>? deepDungeons;
    private bool deepDungeonFailed;
    private string? deepDungeonTab,deepDungeonPart;
    private string deepDungeonQuery="";

    private void DrawDeepDungeons()
    {
        Heading(localization.Get("DeepDungeon.Title"),localization.Get("DeepDungeon.Subtitle"));
        if(deepDungeons is null && !deepDungeonFailed && deepDungeonTask is null && DeepDungeonLoad is { } load)deepDungeonTask=Task.Run(load);
        if(deepDungeonTask is { IsCompleted:true } done)
        {
            deepDungeonTask=null;
            deepDungeons=done.IsCompletedSuccessfully ? done.Result:null;
            deepDungeonFailed=deepDungeons is null;
        }
        if(deepDungeonFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("DeepDungeon.Unavailable"));return;}
        if(deepDungeons is not { Count:>0 } dungeons){ImGui.TextDisabled(localization.Get("DeepDungeon.Loading"));return;}
        DetailTabs("DeepDungeonTabs",dungeons.Select(dungeon=>(dungeon.Id.ToString(),dungeon.Name,(Action)(()=>DrawDeepDungeon(dungeon)))).ToArray(),ref deepDungeonTab);
    }

    private void DrawDeepDungeon(DeepDungeonEntry dungeon)
    {
        DetailTabs($"DeepDungeonParts{dungeon.Id}",
        [
            ("Floors",localization.Get("DeepDungeon.TabFloors"),()=>DrawDeepDungeonFloors(dungeon)),
            ("Items",localization.Get("DeepDungeon.TabItems"),()=>DrawDeepDungeonItems(dungeon)),
            ("Loot",localization.Get("DeepDungeon.TabLoot"),()=>DrawDeepDungeonLoot(dungeon)),
        ],ref deepDungeonPart);
    }

    private void DrawDeepDungeonFloors(DeepDungeonEntry dungeon)
    {
        if(duties.State.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get("DeepDungeon.Loading"));return;}
        var floors=DeepDungeonBoard.Floors(catalog.Entries,dungeon.Name);
        if(floors.Count==0){ImGui.TextDisabled(localization.Get("DeepDungeon.NoFloors"));return;}
        ImGui.TextDisabled(localization.Get("DeepDungeon.FloorsHint"));
        DrawDutyUnlock(floors[0]);
        var progress=DutyProgressSource?.Invoke();
        if(!ImGui.BeginTable($"###DeepDungeonFloors{dungeon.Id}",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnName","ColumnLevel","ColumnState"})ImGui.TableSetupColumn(localization.Get("DeepDungeon."+column));
            ImGui.TableHeadersRow();
            foreach(var floor in floors)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if(ImGui.Selectable($"{floor.Name}##DeepFloor{floor.DutyId}")){dutyLoot.Select(floor);showDutyDetails=true;dutyItemQuery="";page=Page.DutyLoot;}
                ImGui.TableNextColumn();ImGui.TextUnformatted(floor.Level.ToString());
                ImGui.TableNextColumn();DrawDutyStatus(progress?.Of(floor.InstanceId));
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawDeepDungeonItems(DeepDungeonEntry dungeon)
    {
        ImGui.SetNextItemWidth(240);
        ImGui.InputTextWithHint("###DeepDungeonQuery",localization.Get("DeepDungeon.Search"),ref deepDungeonQuery,100);
        Tip("DeepDungeon.SearchTip");
        DrawDeepDungeonThings(dungeon.Id,"Pomanders",dungeon.Pomanders);
        if(dungeon.Others.Count>0){ImGui.Spacing();DrawDeepDungeonThings(dungeon.Id,"Others",dungeon.Others);}
    }

    private void DrawDeepDungeonThings(uint dungeon,string kind,IReadOnlyList<DeepDungeonThing> things)
    {
        var rows=things.Where(row=>deepDungeonQuery.Length==0 || row.Name.Contains(deepDungeonQuery,StringComparison.CurrentCultureIgnoreCase) || row.Tooltip.Contains(deepDungeonQuery,StringComparison.CurrentCultureIgnoreCase)).ToArray();
        ImGui.TextColored(Accent,localization.Format("DeepDungeon."+kind,rows.Length));
        if(rows.Length==0 || !ImGui.BeginTable($"###DeepDungeon{kind}{dungeon}",2,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnName","ColumnEffect"})ImGui.TableSetupColumn(localization.Get("DeepDungeon."+column));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawSmallGameIcon(row.Icon);ImGui.TextUnformatted(row.Name);
                ImGui.TableNextColumn();ImGui.TextWrapped(row.Tooltip);
            }
        }
        finally{ImGui.EndTable();}
    }

    // What the coffers give comes from the community files, under their own switch.
    private void DrawDeepDungeonLoot(DeepDungeonEntry dungeon)
    {
        // X-75.2 (0.876): what the floors' coffers were seen in game giving (discoveries, yours or shared, by the floors' duties) comes first, and stays with the community files off.
        if(duties.State.Catalog is { } catalog && Seen() is { } seen)
        {
            var found=DeepDungeonBoard.Floors(catalog.Entries,dungeon.Name).SelectMany(floor=>seen.ItemsByDuty.GetValueOrDefault(floor.DutyId) ?? []).Distinct().Take(64).ToArray();
            if(found.Length>0 && UI.UiComponents.Header(localization.Format("DeepDungeon.SeenLoot",found.Length)+"###DeepDungeonSeen",ImGuiTreeNodeFlags.DefaultOpen))
                foreach(var result in found)
                {
                    var got=itemCatalog.State.Catalog?.Find(result);
                    ImGui.PushID((int)result);
                    try{ItemCell(got,result,20,got is null ? result.ToString():null);}
                    finally{ImGui.PopID();}
                }
        }
        if(!configuration.CommunityLumina){ImGui.TextDisabled(localization.Get("DeepDungeon.LootOff"));return;}
        lumina?.Request();
        if(lumina?.State.Data is not { } data){ImGui.TextDisabled(localization.Get("DeepDungeon.LootLoading"));return;}
        var sources=data.SourcesOf(DeepDungeonBoard.LootKind(dungeon.Id));
        if(sources.Count==0){ImGui.TextDisabled(localization.Get("DeepDungeon.NoLoot"));return;}
        ImGui.TextDisabled(localization.Get("DeepDungeon.LootHint"));
        var items=itemCatalog.State.Catalog;
        foreach(var (source,count,list) in sources)
        {
            var coffer=items?.Find(source);
            ImGui.PushID((int)source);
            try
            {
                if(!UI.UiComponents.Header((coffer?.Name ?? source.ToString())+$" ({count})###DeepDungeonSource"))continue;
                foreach(var result in list)
                {
                    var got=items?.Find(result);
                    ItemCell(got,result,20,got is null ? result.ToString():null);
                }
                if(count>list.Count)ImGui.TextDisabled(localization.Format("DeepDungeon.LootMore",count-list.Count));
            }
            finally{ImGui.PopID();}
        }
    }
}
