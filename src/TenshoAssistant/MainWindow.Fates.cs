using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Fields;

namespace TenshoAssistant;

// W-14b (0.721): where every FATE of the open zones takes place, from the game's zone layouts. docs/evidence/0.721-fates.md.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<FieldZone>?>? FatesLoad { get; set; }
    internal Func<uint>? CurrentTerritory { get; set; }
    private Task<IReadOnlyList<FieldZone>?>? fatesTask;
    private IReadOnlyList<FieldZone>? fateZones;
    private bool fatesFailed,fatesPlaced;
    private int fateZone;
    private string fateQuery="";

    /// <summary>From elsewhere: this FATE's lines, wherever they are.</summary>
    private void OpenFate(string name){fateQuery=name;page=Page.Fates;}

    // X-213 (0.1781): the shared FATE ranks (the zones from the sheets; the character's ranks as the game's window showed them).
    internal Func<IReadOnlyList<SharedFateZone>?>? SharedFatesLoad { get; set; }
    internal Func<IReadOnlyDictionary<uint,SharedFateRead>?>? SharedFatesNow { get; set; }
    private Task<IReadOnlyList<SharedFateZone>?>? sharedFatesTask;
    private IReadOnlyList<SharedFateZone>? sharedFateZones;
    private bool sharedFatesFailed;

    private void DrawFates()
    {
        DrawSharedFates();
        if(fateZones is null && !fatesFailed && fatesTask is null && FatesLoad is { } load)fatesTask=Task.Run(load);
        if(fatesTask is { IsCompleted:true } done)
        {
            fatesTask=null;
            fateZones=done.IsCompletedSuccessfully ? done.Result:null;
            fatesFailed=fateZones is not { Count:>0 };
        }
        if(fatesFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Fates.Unavailable"));return;}
        if(fateZones is not { } zones){StateLine(UI.UiState.Waiting,localization.Get("Fates.Loading"));return;}
        // Once, on first showing: the zone you are in; afterwards, the one you chose.
        if(!fatesPlaced){fatesPlaced=true;fateZone=FieldBoard.ZoneOf(zones,CurrentTerritory?.Invoke() ?? 0);}
        fateZone=Math.Clamp(fateZone,0,zones.Count-1);
        ImGui.SetNextItemWidth(Widths.Long);
        if(ImGui.BeginCombo("###FateZone",zones[fateZone].Name))
        {
            try{for(var index=0;index<zones.Count;index++)if(ImGui.Selectable($"{zones[index].Name} ({zones[index].Events.Count})##FateZone{index}",index==fateZone))fateZone=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("Fates.ZoneTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(Widths.Long);
        InputWithHint("###FateQuery","Fates.Search",ref fateQuery,100);
        Tip("Fates.SearchTip");
        ImGui.TextDisabled(localization.Get("Fates.Hint"));
        var rows=FieldBoard.Find(zones,fateZone,fateQuery);
        if(rows.Count==0){EmptyState("Fates.None");return;}
        if(!ImGui.BeginTable("###FateRows",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable|ImGuiTableFlags.ScrollY,new Vector2(0,Math.Max(160,ImGui.GetContentRegionAvail().Y-8))))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var column in new[]{"ColumnName","ColumnLevel","ColumnZone","ColumnPlace"})ImGui.TableSetupColumn(localization.Get("Fates."+column));
            ImGui.TableHeadersRow();
            foreach(var (zone,row) in rows)DrawFateRow(zone,row);
        }
        finally{ImGui.EndTable();}
    }

    /// <summary>
    /// X-213 (the user's request of 12/11, K-23): each shared FATE zone, the character's rank, the FATEs toward the next one and the
    /// gemstone trader's items still to come, as the game's Shared FATE window showed them when last opened; « unknown » until then (R06).
    /// </summary>
    private void DrawSharedFates()
    {
        if(sharedFateZones is null && !sharedFatesFailed && sharedFatesTask is null && SharedFatesLoad is { } load)sharedFatesTask=Task.Run(load);
        if(sharedFatesTask is { IsCompleted:true } done)
        {
            sharedFatesTask=null;
            sharedFateZones=done.IsCompletedSuccessfully ? done.Result:null;
            sharedFatesFailed=sharedFateZones is not { Count:>0 };
        }
        var kept=SharedFatesNow?.Invoke();
        var zones=sharedFateZones;
        var header=zones is null ? localization.Get("Fates.SharedTitle"):localization.Format("Fates.SharedHeader",zones.Count(zone=>kept?.ContainsKey(zone.Territory)==true),zones.Count);
        if(!ImGui.CollapsingHeader(header+"###SharedFates"))return;
        Tip("Fates.SharedHelp");
        if(sharedFatesFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Fates.SharedUnavailable"));return;}
        if(zones is null){StateLine(UI.UiState.Waiting,localization.Get("Fates.SharedLoading"));return;}
        ImGui.TextDisabled(SharedFates.LastRead(kept) is { } at ? localization.Format("Fates.SharedReadAt",at.ToLocalTime().ToString("g")):localization.Get("Fates.SharedNeverRead"));
        if(!UI.UiComponents.BeginDataTable("###SharedFateRows",4,scrollHeight:Math.Min(260,ImGui.GetTextLineHeightWithSpacing()*(zones.Count+1)+8)))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            UI.UiComponents.HeadersRow([..new[]{"SharedColumnZone","SharedColumnRank","SharedColumnNext","SharedColumnRewards"}.Select(column=>localization.Get("Fates."+column))],_=>false);
            foreach(var zone in zones)DrawSharedFateRow(zone,kept?.GetValueOrDefault(zone.Territory));
        }
        finally{ImGui.EndTable();}
    }

    private void DrawSharedFateRow(SharedFateZone zone,SharedFateRead? read)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();ImGui.TextUnformatted(zone.Name);
        ImGui.TableNextColumn();
        if(read is null){ImGui.TextDisabled(localization.Get("Fates.SharedUnknown"));Tip("Fates.SharedUnknownTip");}
        else ImGui.TextUnformatted(localization.Format("Fates.SharedRank",read.Rank,read.MaxRank));
        ImGui.TableNextColumn();
        if(read is null){ImGui.TextDisabled(localization.Get("Fates.SharedUnknown"));Tip("Fates.SharedUnknownTip");}
        else if(read.Rank>=read.MaxRank)ImGui.TextColored(Success,localization.Get("Fates.SharedMaxRank"));
        else
        {
            ImGui.TextUnformatted(localization.Format("Fates.SharedNext",read.Progress,read.Needed));
            if(TipHovered())TipText(localization.Format("Fates.SharedNextTip",read.Rank+1));
        }
        ImGui.TableNextColumn();
        if(!zone.TraderKnown){ImGui.TextDisabled(localization.Get("Fates.SharedRewardsNoTrader"));Tip("Fates.SharedRewardsNoTraderTip");return;}
        if(SharedFates.Remaining(zone,read) is not { } left){ImGui.TextDisabled(localization.Get("Fates.SharedUnknown"));Tip("Fates.SharedUnknownTip");return;}
        var achievement=read is not null && read.Rank<zone.MaxRank ? zone.Achievement:null;
        if(left.Count==0 && achievement is null){ImGui.TextColored(Success,localization.Get("Fates.SharedRewardsNone"));return;}
        ImGui.TextUnformatted(left.Count==1 ? localization.Get("Fates.SharedRewardsOne"):localization.Format("Fates.SharedRewards",left.Count));
        if(!ImGui.IsItemHovered())return;
        using(TooltipScope.Begin())
        {
            TooltipTitle(zone.Name);
            foreach(var reward in left)
            {
                DrawGameIcon(reward.Icon,new Vector2(ImGui.GetTextLineHeight()));
                ImGui.SameLine();
                ImGui.TextUnformatted(reward.Name);
                ImGui.SameLine();
                ImGui.TextDisabled(localization.Format("Fates.SharedRewardLine",reward.Rank,reward.Cost,reward.CostName));
            }
            if(achievement is not null)ImGui.TextUnformatted(localization.Format("Fates.SharedAchievement",zone.MaxRank,achievement));
        }
    }

    private void DrawFateRow(FieldZone zone,FieldEvent row)
    {
        var map=maps.State.Catalog?.Find(zone.Map);
        var coordinate=map is null ? (Vector2?)null:CoordinateOf(map,new Vector3(row.X,0,row.Z));
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if(ImGui.Selectable($"{row.Name}##Fate{zone.Territory}{row.Id}") && map is not null && coordinate is { } target)OpenMapPointFromPage(map,target.X,target.Y);
        // X-684 (0.1676): the one FATE tooltip.
        if(ImGui.IsItemHovered())DrawFateTooltip(FateTips.Build(row.Name,0,row.Level,localization.Get("FieldContent.KindFate"),
            coordinate is { } fatePlace ? $"{zone.Name} · {Maps.MapProjection.CoordinateText(fatePlace)}":zone.Name,null,row.Description,row.Participants));
        if(map is not null && coordinate is { } place)PlaceMenu($"FatePlace{zone.Territory}{row.Id}",map,place,row.Name);
        ImGui.TableNextColumn();
        if(row.Level is { } level)ImGui.TextUnformatted(level.ToString());else ImGui.TextDisabled("—");
        ImGui.TableNextColumn();ImGui.TextUnformatted(zone.Name);
        ImGui.TableNextColumn();
        if(coordinate is { } at)ImGui.TextUnformatted(Maps.MapProjection.CoordinateText(at));else ImGui.TextDisabled("—");
    }
}
