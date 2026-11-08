using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;
using TenshoAssistant.Items;
using TenshoAssistant.Leves;

namespace TenshoAssistant;

// g, C-14 (0.363): Guildleves page — every leve of the game data with its level, type, client, zone and gil, completed or accepted
// (read in game, Leves/LeveProgressReader), filters by level, type, client and state; then the accepted crafting leves with the items
// they ask and the copies held. Rows rebuilt when a source or a filter changes.
internal sealed partial class MainWindow
{
    internal Func<IReadOnlyList<LeveDefinition>>? LeveDefinitions { get; set; }
    internal Func<LeveProgressState?>? LeveProgress { get; set; }
    internal Func<bool>? LeveProgressUnavailable { get; set; }
    private string leveQuery="",leveClient="";
    private uint leveType;
    private int leveMinLevel,leveMaxLevel;
    private LeveStateFilter leveState=LeveStateFilter.NotCompleted;
    private (object? Catalog,object? Progress,string Query,string Client,uint Type,int Min,int Max,LeveStateFilter State)? leveKey;
    private LeveDefinition[] leveRows=[];
    private (uint Id,string Name)[] leveTypes=[];
    private string[] leveClients=[];
    private object? leveListsFor;
    private (int Completed,int Known,int Total) leveCount;

    // W-08 (0.698, user request): right click, the nearest aetheryte: the flag on the place, the Tensho map on it, and the game's teleport question as everywhere else.
    private void DrawLeveMenu(LeveDefinition leve)
    {
        if(!ImGui.BeginPopupContextItem($"LeveMenu{leve.Id}"))return;
        try
        {
            void Entry(string key,LevePlace? place)
            {
                var map=place is null ? null:maps.State.Catalog?.Find(place.MapId);
                ImGui.BeginDisabled(map is null || !MapHasAetheryte(map));
                try
                {
                    if(ImGui.Selectable(localization.Get(key)) && map is not null && place is not null)
                        AskNearestAetheryteAt(map,MapProjection.PixelFromWorld(new Vector3(place.X,0,place.Z),map),leve.Name,flag:true);
                }
                finally{ImGui.EndDisabled();}
            }
            Entry("Leves.AetheryteClient",leve.ClientPlace);
            Entry("Leves.AetheryteStart",leve.StartPlace);
        }
        finally{ImGui.EndPopup();}
    }

    // 0.689 (user report): the tooltip gave only the row's id, in developer mode. It now says what the columns say, which a narrow window hides.
    private string LeveTooltip(LeveDefinition leve)
    {
        var lines=new List<string>
        {
            leve.Name,
            $"{localization.Get("Leves.ColumnLevel")} {leve.Level} · {leve.Type}",
            $"{localization.Get("Leves.ColumnClient")} : {leve.Client}",
            $"{localization.Get("Leves.ColumnZone")} : {leve.Zone}",
            $"{localization.Get("Leves.ColumnGil")} : {leve.Gil:N0}",
            localization.Format("Leves.TooltipAllowances",leve.Allowances),
        };
        if(developerMode)lines.Add($"Leve {leve.Id}");
        return string.Join("\n",lines);
    }

    private void DrawLeves()
    {
        Heading(localization.Get("Leves.Title"),localization.Get("Leves.Description"));
        var all=LeveDefinitions?.Invoke() ?? [];
        var progress=LeveProgress?.Invoke();
        if(!ReferenceEquals(leveListsFor,all))
        {
            leveListsFor=all;
            leveTypes=all.Where(leve=>leve.TypeId!=0).Select(leve=>(leve.TypeId,leve.Type)).Distinct().OrderBy(type=>type.TypeId).ToArray();
            leveClients=all.Select(leve=>leve.Client).Where(name=>name.Length>0).Distinct().OrderBy(name=>name,StringComparer.CurrentCulture).ToArray();
        }
        if(progress is null)ImGui.TextDisabled(localization.Get(LeveProgressUnavailable?.Invoke()==true ? "Leves.Unavailable":"Leves.Unknown"));
        else ImGui.TextDisabled(localization.Format("Leves.State",progress.Allowances?.ToString() ?? "?",progress.Accepted.Count));
        ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("##LeveSearch",localization.Get("Leves.Search"),ref leveQuery,64);
        Tip("Leves.SearchTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(200);
        if(ImGui.BeginCombo("##LeveType",leveType==0 ? localization.Get("Leves.AllTypes"):leveTypes.FirstOrDefault(type=>type.Id==leveType).Name ?? ""))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("Leves.AllTypes"),leveType==0))leveType=0;
                foreach(var (id,name) in leveTypes)if(ImGui.Selectable($"{name}##LeveType{id}",leveType==id))leveType=id;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("Leves.TypeTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(200);
        if(ImGui.BeginCombo("##LeveClient",leveClient.Length==0 ? localization.Get("Leves.AllClients"):leveClient))
        {
            try
            {
                if(ImGui.Selectable(localization.Get("Leves.AllClients"),leveClient.Length==0))leveClient="";
                foreach(var name in leveClients)if(ImGui.Selectable(name,leveClient==name))leveClient=name;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("Leves.ClientTip");
        ImGui.SetNextItemWidth(90);
        if(ImGui.InputInt(localization.Get("Leves.MinLevel")+"##LeveMin",ref leveMinLevel))leveMinLevel=Math.Clamp(leveMinLevel,0,100);
        Tip("Leves.MinLevelTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(90);
        if(ImGui.InputInt(localization.Get("Leves.MaxLevel")+"##LeveMax",ref leveMaxLevel))leveMaxLevel=Math.Clamp(leveMaxLevel,0,100);
        Tip("Leves.MaxLevelTip");
        foreach(var filter in Enum.GetValues<LeveStateFilter>())
        {
            ImGui.SameLine();
            if(ImGui.RadioButton(localization.Get("Leves.Filter"+filter)+"##LeveState"+filter,leveState==filter))leveState=filter;
            switch(filter)
            {
                case LeveStateFilter.All: Tip("Leves.FilterAllTip"); break;
                case LeveStateFilter.NotCompleted: Tip("Leves.FilterNotCompletedTip"); break;
                case LeveStateFilter.Completed: Tip("Leves.FilterCompletedTip"); break;
                case LeveStateFilter.Accepted: Tip("Leves.FilterAcceptedTip"); break;
            }
        }
        var key=((object?)all,(object?)progress,leveQuery,leveClient,leveType,leveMinLevel,leveMaxLevel,leveState);
        if(leveKey!=key)
        {
            leveKey=key;
            var query=leveQuery.Trim();
            leveRows=all.Where(leve=>LeveCatalog.Matches(leve,progress,leveState,leveType,leveClient,leveMinLevel,leveMaxLevel,query)).ToArray();
            var shown=all.Where(leve=>LeveCatalog.Matches(leve,null,LeveStateFilter.All,leveType,leveClient,leveMinLevel,leveMaxLevel,query)).ToArray();
            leveCount=(progress?.Completed is { } done ? shown.Count(leve=>done.Contains(leve.Id)):0,progress?.Completed is null ? 0:shown.Length,shown.Length);
        }
        ImGui.TextDisabled(leveCount.Known==0 ? localization.Format("Leves.CountUnknown",leveRows.Length)
            :localization.Format("Leves.Count",leveRows.Length,leveCount.Completed,leveCount.Total));
        DrawAcceptedCraftLeves(progress);
        if(!ImGui.BeginTable("Leves",7,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp|ImGuiTableFlags.ScrollY,new Vector2(0,ImGui.GetContentRegionAvail().Y)))return;
        try
        {
            ImGui.TableSetupScrollFreeze(0,1);
            foreach(var column in new[]{"Name","Level","Type","Client","Zone","Gil","State"})
                ImGui.TableSetupColumn(localization.Get("Leves.Column"+column),column=="Name" ? ImGuiTableColumnFlags.WidthStretch:ImGuiTableColumnFlags.None,column=="Name" ? 3:0);
            ImGui.TableHeadersRow();
            var size=new Vector2(ImGui.GetTextLineHeight());
            var clipper=ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(leveRows.Length);
                while(clipper.Step())
                for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                {
                    var leve=leveRows[index];
                    ImGui.TableNextRow();ImGui.TableNextColumn();
                    DrawGameIcon(leve.TypeIcon,size);ImGui.SameLine();
                    ImGui.TextUnformatted(leve.Name);
                    if(ImGui.IsItemHovered())WrappedTooltip(LeveTooltip(leve));
                    DrawLeveMenu(leve);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(leve.Level.ToString());
                    ImGui.TableNextColumn();ImGui.TextUnformatted(leve.Type);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(leve.Client);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(leve.Zone);
                    ImGui.TableNextColumn();ImGui.TextUnformatted(leve.Gil.ToString("N0"));
                    ImGui.TableNextColumn();
                    if(progress?.Accepted.Contains(leve.Id)==true)ImGui.TextColored(Accent,localization.Get("Leves.Accepted"));
                    else if(progress?.Completed is { } completed)
                    {
                        if(completed.Contains(leve.Id))ImGui.TextColored(Success,localization.Get("Leves.Completed"));
                        else ImGui.TextDisabled(localization.Get("Leves.NotCompleted"));
                    }
                    else ImGui.TextDisabled("—");
                }
            }
            finally{clipper.End();clipper.Destroy();}
        }
        finally{ImGui.EndTable();}
    }

    // Accepted crafting leves: the items they ask (CraftLeve, Items/ItemUseCatalog) and the copies the character holds.
    private void DrawAcceptedCraftLeves(LeveProgressState? progress)
    {
        if(progress is not { Accepted.Count:>0 } || itemUses.State.Catalog is not { } uses)return;
        var crafts=progress.Accepted.Select(uses.ForLeve).OfType<ItemLeveUse>().ToArray();
        if(crafts.Length==0)return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("Leves.CraftQueue",crafts.Length)+"###LeveCraftQueue",ImGuiTreeNodeFlags.DefaultOpen))return;
        RefreshRecipeHoldingsCache();
        var items=itemCatalog.State.Catalog;var size=new Vector2(ImGui.GetTextLineHeight());
        foreach(var leve in crafts)
            foreach(var request in leve.Requests)
            {
                var definition=items?.Find(request.ItemId);
                DrawItemIcon(definition,size);ImGui.SameLine();
                var holdings=GetRecipeHoldings(request.ItemId);
                var held=holdings.HasObservation ? holdings.Total.ToString("N0"):"?";
                ImGui.Selectable(localization.Format("Leves.CraftLine",leve.Name,definition?.Name ?? $"#{request.ItemId}",request.Quantity?.ToString() ?? "?",held)
                    +$"##LeveCraft{leve.RowId}/{request.ItemId}");
                ItemLinksMenu($"LeveCraftMenu{leve.RowId}/{request.ItemId}",definition);
            }
    }
}
