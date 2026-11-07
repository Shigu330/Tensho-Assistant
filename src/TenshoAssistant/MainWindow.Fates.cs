using System.Globalization;
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

    private void DrawFates()
    {
        Heading(localization.Get("Fates.Title"),localization.Get("Fates.Subtitle"));
        if(fateZones is null && !fatesFailed && fatesTask is null && FatesLoad is { } load)fatesTask=Task.Run(load);
        if(fatesTask is { IsCompleted:true } done)
        {
            fatesTask=null;
            fateZones=done.IsCompletedSuccessfully ? done.Result:null;
            fatesFailed=fateZones is not { Count:>0 };
        }
        if(fatesFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Fates.Unavailable"));return;}
        if(fateZones is not { } zones){ImGui.TextDisabled(localization.Get("Fates.Loading"));return;}
        // Once, on first showing: the zone you are in; afterwards, the one you chose.
        if(!fatesPlaced){fatesPlaced=true;fateZone=FieldBoard.ZoneOf(zones,CurrentTerritory?.Invoke() ?? 0);}
        fateZone=Math.Clamp(fateZone,0,zones.Count-1);
        ImGui.SetNextItemWidth(260);
        if(ImGui.BeginCombo("###FateZone",zones[fateZone].Name))
        {
            try{for(var index=0;index<zones.Count;index++)if(ImGui.Selectable($"{zones[index].Name} ({zones[index].Events.Count})##FateZone{index}",index==fateZone))fateZone=index;}
            finally{ImGui.EndCombo();}
        }
        Tip("Fates.ZoneTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(260);
        ImGui.InputTextWithHint("###FateQuery",localization.Get("Fates.Search"),ref fateQuery,100);
        Tip("Fates.SearchTip");
        ImGui.TextDisabled(localization.Get("Fates.Hint"));
        var rows=FieldBoard.Find(zones,fateZone,fateQuery);
        if(rows.Count==0){ImGui.TextDisabled(localization.Get("Fates.None"));return;}
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

    private void DrawFateRow(FieldZone zone,FieldEvent row)
    {
        var map=maps.State.Catalog?.Find(zone.Map);
        var coordinate=map is null ? (Vector2?)null:CoordinateOf(map,new Vector3(row.X,0,row.Z));
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        if(ImGui.Selectable($"{row.Name}##Fate{zone.Territory}{row.Id}") && map is not null && coordinate is { } target)OpenMapPointFromPage(map,target.X,target.Y);
        if(ImGui.IsItemHovered() && row.Description.Length>0)WrappedTooltip(row.Description);
        if(map is not null && coordinate is { } place)PlaceMenu($"FatePlace{zone.Territory}{row.Id}",map,place,row.Name);
        ImGui.TableNextColumn();
        if(row.Level is { } level)ImGui.TextUnformatted(level.ToString());else ImGui.TextDisabled("—");
        ImGui.TableNextColumn();ImGui.TextUnformatted(zone.Name);
        ImGui.TableNextColumn();
        if(coordinate is { } at)ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture,$"X {at.X:0.0} Y {at.Y:0.0}"));else ImGui.TextDisabled("—");
    }
}
