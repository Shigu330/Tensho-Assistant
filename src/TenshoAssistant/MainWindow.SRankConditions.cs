using Dalamud.Bindings.ImGui;
using TenshoAssistant.Hunting;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// N-12 (0.670): Maps › « S rank conditions » (folded). Community knowledge, not game data: said on the page. The names come from the game's
// sheets, in its language. The marks of the player's zone come first (docs/evidence/0.670-srank-conditions.md). X-222 (0.1232): the marks counted by
// kills show the player's own kills (SRankKills), with a button that clears them.
internal sealed partial class MainWindow
{
    internal Func<SRankRef,string?>? SRankRefName { get; set; }
    internal Func<uint,string?>? SRankMarkName { get; set; }
    internal Func<uint,string?>? SRankZoneName { get; set; }
    internal Func<int,string?>? SRankExpansionName { get; set; }
    private string srankQuery="",srankShownQuery="",srankLanguage="";
    private SRankRow[] srankRows=[],srankShown=[];

    private void DrawSRankConditions(MapCatalog catalog)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("SRank.Title")+"###SRankConditions",ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextWrapped(localization.Get("SRank.Help"));
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##SRankQuery",localization.Get("SRank.Search"),ref srankQuery,64);
        Tip("SRank.SearchTip");
        // Built once per language: the rows do not change.
        var language=localization.Get("SRank.Kill");
        if(srankLanguage!=language)
        {
            srankLanguage=language;
            srankRows=SRankConditions.Marks.Select(BuildSRankRow).ToArray();
            srankShownQuery="\0";
        }
        if(srankShownQuery!=srankQuery)
        {
            srankShownQuery=srankQuery;
            srankShown=srankRows.Where(row=>SRankConditions.Matches(row,srankQuery)).ToArray();
        }
        if(srankShown.Length==0){ImGui.TextDisabled(localization.Get("SRank.Empty"));return;}
        var here=mapLive?.State is { } live ? catalog.Find(live.MapId)?.TerritoryId ?? 0:0;
        foreach(var row in srankShown.Where(row=>row.Mark.Territory==here && here!=0))
        {
            ImGui.TextDisabled(localization.Get("SRank.HereZone"));
            DrawSRankRow(row);
            ImGui.Separator();
        }
        foreach(var group in srankShown.GroupBy(row=>row.Mark.Expansion).OrderByDescending(group=>group.Key))
        {
            if(srankQuery.Length==0 && !ImGui.TreeNode(group.First().Expansion+"###SRank"+group.Key))continue;
            try{foreach(var row in group)DrawSRankRow(row);}
            finally{if(srankQuery.Length==0)ImGui.TreePop();}
        }
    }

    // X-222 (0.1232): « Your kills: A 12/100 · B 0/100 » and a button that clears this mark's counters.
    private void DrawSRankKills(SRankMark mark)
    {
        var counts=configuration.SRankKills;
        var parts=mark.Refs.Where(item=>item.Kind==SRankRefKind.Mob).Select(item=>localization.Format("SRank.KillCount",SRankRefName?.Invoke(item) ?? "#"+item.Id,SRankKills.Get(counts,mark.Name,item.Id),item.Count));
        ImGui.TextUnformatted(localization.Format("SRank.YourKills",string.Join(" · ",parts)));
        Tip("SRank.YourKillsTip");
        ImGui.SameLine();
        DesktopEditBegin();
        try
        {
            if(ImGui.SmallButton(localization.Get("SRank.ClearKills")+$"###SRankClear{mark.Name}") && counts is not null && SRankKills.Clear(counts,mark.Name))save();
            Tip("SRank.ClearKillsTip");
        }
        finally{DesktopEditEnd();}
    }

    private SRankRow BuildSRankRow(SRankMark mark)
    {
        string Upper(string? name,string fallback)=>name is { Length:>0 } ? char.ToUpper(name[0],System.Globalization.CultureInfo.CurrentCulture)+name[1..]:fallback;
        var expansion=Upper(SRankExpansionName?.Invoke(mark.Expansion),"#"+mark.Expansion);
        var zone=mark.Territory==0 ? expansion:Upper(SRankZoneName?.Invoke(mark.Territory),"#"+mark.Territory);
        var arguments=SRankConditions.Arguments(mark,item=>Upper(SRankRefName?.Invoke(item),"#"+item.Id));
        return new(mark,Upper(SRankMarkName?.Invoke(mark.Name),"#"+mark.Name),zone,expansion,localization.Format("SRank."+mark.Condition,arguments));
    }

    private void DrawSRankRow(SRankRow row)
    {
        ImGui.TextColored(Accent,$"{row.Name} · {row.Zone}");
        ImGui.TextWrapped(row.Text);
        var mark=row.Mark;
        // X-111 (0.983, user request: wherever a position shows, the flag and the nearest aetheryte): the community's place of the condition (« x, y »),
        // on the mark's zone map, gets the common place menu (right click).
        if(mark.Place is { } said && SRankConditions.PlaceOf(said) is { } placed && mark.Territory!=0
           && maps.State.Catalog?.Entries.FirstOrDefault(entry=>entry.TerritoryId==mark.Territory) is { } markMap)
        {
            Tip("EntityMenu.RightClickPlace");
            PlaceMenu($"SRankPlace{mark.Name}",markMap,placed,row.Name);
        }
        if(SRankKills.Counted(mark))DrawSRankKills(mark);
        var window=localization.Format("SRank.Window",mark.Minimum,mark.Maximum,mark.MaintenanceMinimum,mark.MaintenanceMaximum);
        ImGui.TextDisabled(mark.Odds switch{SRankOdds.Guaranteed=>window+" · "+localization.Get("SRank.Sure"),SRankOdds.Chance=>window+" · "+localization.Get("SRank.Random"),_=>window});
        // X-91 (0.971, planned in X-75.5, docs/evidence/x91-parsers-check.md): where the condition's FATE was seen in game (yours or shared), from the discoveries.
        if(DiscoveryStore is { } seenStore && DiscoveriesLoaded && maps.State.Catalog is { } seenMaps)
            foreach(var fate in mark.Refs.Where(item=>item.Kind==SRankRefKind.Fate))
                foreach(var fact in seenStore.Find(Discoveries.DiscoveryKind.FateSeen,fate.Id,3))
                    if(seenMaps.Find(fact.Map) is { } seenMap)
                    {
                        var at=MapProjection.CoordinateFromWorld(new System.Numerics.Vector3(fact.X,0,fact.Z),seenMap);
                        ImGui.TextDisabled(localization.Format("SRank.FateSeen",DiscoveryFateName(fate.Id),MapProjection.CoordinateNumber(at.X),MapProjection.CoordinateNumber(at.Y),Discoveries.DiscoveryFact.TimesOf(fact)));
                        Tip("EntityMenu.RightClickPlace");
                        PlaceMenu($"SRankFate{fate.Id}{fact.CellX}{fact.CellZ}",seenMap,at,DiscoveryFateName(fate.Id));
                    }
    }
}
