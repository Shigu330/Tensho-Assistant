using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Fields;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

// W-14 (0.717): Eureka and Bozja, from the game's files: where things happen, the field records, the lost actions. What the character holds there is not read: docs/evidence/0.717-eureka-bozja.md.
internal sealed partial class MainWindow
{
    internal Func<FieldCatalog?>? FieldCatalogLoad { get; set; }
    private Task<FieldCatalog?>? fieldLoad;
    private FieldCatalog? fieldCatalog;
    private bool fieldFailed;
    private string? fieldTab,fieldZoneTab;
    private string fieldQuery="";

    private void DrawFieldContent()
    {
        Heading(localization.Get("FieldContent.Title"),localization.Get("FieldContent.Subtitle"));
        if(fieldCatalog is null && !fieldFailed && fieldLoad is null && FieldCatalogLoad is { } load)fieldLoad=Task.Run(load);
        if(fieldLoad is { IsCompleted:true } done)
        {
            fieldLoad=null;
            fieldCatalog=done.IsCompletedSuccessfully ? done.Result:null;
            fieldFailed=fieldCatalog is null;
        }
        if(fieldFailed){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("FieldContent.Unavailable"));return;}
        if(fieldCatalog is not { } catalog){ImGui.TextDisabled(localization.Get("FieldContent.Loading"));return;}
        ImGui.SetNextItemWidth(240);
        ImGui.InputTextWithHint("###FieldQuery",localization.Get("FieldContent.Search"),ref fieldQuery,100);
        Tip("FieldContent.SearchTip");
        DetailTabs("FieldTabs",
        [
            ("Zones",localization.Get("FieldContent.TabZones"),()=>DrawFieldZones(catalog)),
            ("Records",localization.Get("FieldContent.TabRecords"),()=>DrawFieldRecords(catalog)),
            ("Actions",localization.Get("FieldContent.TabActions"),()=>DrawFieldActions(catalog)),
        ],ref fieldTab);
    }

    private void DrawFieldZones(FieldCatalog catalog)
    {
        var tabs=catalog.Zones.Where(zone=>zone.Events.Count>0).Select(zone=>(zone.Territory.ToString(),$"{zone.Name} ({zone.Events.Count})",(Action)(()=>DrawFieldZone(zone)))).ToList();
        if(tabs.Count==0){ImGui.TextDisabled(localization.Get("FieldContent.NoZones"));return;}
        DetailTabs("FieldZoneTabs",tabs,ref fieldZoneTab);
    }

    private void DrawFieldZone(FieldZone zone)
    {
        var map=maps.State.Catalog?.Find(zone.Map);
        ImGui.TextDisabled(localization.Get("FieldContent.ZoneHint"));
        var rows=zone.Events.Where(row=>FieldBoard.Matches(fieldQuery,row.Name,row.Description)).ToArray();
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("FieldContent.NoMatch"));return;}
        if(!ImGui.BeginTable($"###FieldZone{zone.Territory}",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnName","ColumnKind","ColumnLevel","ColumnPlace"})ImGui.TableSetupColumn(localization.Get("FieldContent."+column));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                var coordinate=map is null ? (Vector2?)null:CoordinateOf(map,new Vector3(row.X,0,row.Z));
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if(ImGui.Selectable($"{row.Name}##FieldEvent{row.Kind}{row.Id}") && map is not null && coordinate is { } target)OpenMapPointFromPage(map,target.X,target.Y);
                if(ImGui.IsItemHovered())WrappedTooltip(FieldEventTip(row));
                if(map is not null && coordinate is { } place)PlaceMenu($"FieldPlace{row.Kind}{row.Id}",map,place,row.Name);
                ImGui.TableNextColumn();ImGui.TextUnformatted(localization.Get(row.Kind==FieldEventKind.Fate ? (zone.Territory>=920 ? "FieldContent.KindSkirmish":"FieldContent.KindFate"):"FieldContent.KindEngagement"));
                ImGui.TableNextColumn();
                if(row.Level is { } level)ImGui.TextUnformatted(level.ToString());else ImGui.TextDisabled("—");
                ImGui.TableNextColumn();
                if(coordinate is { } at)ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture,$"X {at.X:0.0} Y {at.Y:0.0}"));else ImGui.TextDisabled("—");
            }
        }
        finally{ImGui.EndTable();}
    }

    private string FieldEventTip(FieldEvent row)
    {
        var text=row.Description.Length>0 ? row.Description:localization.Get("FieldContent.NoDescription");
        return row.Participants>0 ? text+"\n\n"+localization.Format("FieldContent.Participants",row.Participants):text;
    }

    private void DrawFieldRecords(FieldCatalog catalog)
    {
        ImGui.TextDisabled(localization.Get("FieldContent.RecordsHint"));
        var rows=catalog.Records.Where(row=>FieldBoard.Matches(fieldQuery,row.Name,row.Description,row.Quest)).ToArray();
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("FieldContent.NoMatch"));return;}
        if(!ImGui.BeginTable("###FieldRecords",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnNumber","ColumnName","ColumnRarity","ColumnQuest"})ImGui.TableSetupColumn(localization.Get("FieldContent."+column));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Number.ToString());
                ImGui.TableNextColumn();DrawSmallGameIcon(row.Icon);ImGui.TextUnformatted(row.Name);
                if(ImGui.IsItemHovered() && row.Description.Length>0)WrappedTooltip(row.Description);
                ImGui.TableNextColumn();ImGui.TextUnformatted(new string('★',Math.Clamp(row.Rarity,0,5)));
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Quest);
            }
        }
        finally{ImGui.EndTable();}
    }

    private void DrawFieldActions(FieldCatalog catalog)
    {
        ImGui.TextDisabled(localization.Get("FieldContent.ActionsHint"));
        var rows=catalog.Actions.Where(row=>FieldBoard.Matches(fieldQuery,row.Name,row.Description,row.Category)).ToArray();
        if(rows.Length==0){ImGui.TextDisabled(localization.Get("FieldContent.NoMatch"));return;}
        if(!ImGui.BeginTable("###FieldActions",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerV|ImGuiTableFlags.Resizable))return;
        try
        {
            foreach(var column in new[]{"ColumnName","ColumnCategory","ColumnMost"})ImGui.TableSetupColumn(localization.Get("FieldContent."+column));
            ImGui.TableHeadersRow();
            foreach(var row in rows)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();DrawSmallGameIcon(row.Icon);ImGui.TextUnformatted(row.Name);
                if(ImGui.IsItemHovered() && row.Description.Length>0)WrappedTooltip(row.Description);
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Category);
                ImGui.TableNextColumn();ImGui.TextUnformatted(row.Most.ToString());
            }
        }
        finally{ImGui.EndTable();}
    }
}
