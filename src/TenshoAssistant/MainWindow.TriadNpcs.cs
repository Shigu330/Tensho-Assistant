using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

/// <summary>
/// Triple Triad › NPCs and cards (0.168), as TriadBuddy: NPCs not beaten yet or still holding a card you lack (zone, coordinates, click =
/// map flag), and every missing card with where it comes from (NPCs, card pack, then the item's « how to obtain » tooltip).
/// </summary>
internal sealed partial class MainWindow
{
    private TriadNpcService? triadNpcs;
    private bool triadNpcsOnlyOpen=true;
    private string triadMissingQuery=string.Empty;
    internal void ConfigureTriadNpcs(TriadNpcService service)=>triadNpcs=service;

    private void DrawTriadNpcs()
    {
        var state=triadNpcs?.State;
        if(state?.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get(state?.Error is null ? "TriadNpcs.Loading":"TriadNpcs.Error"));return;}
        var entries=collections.State.Entries.Where(entry=>entry.Kind==CollectionKind.TripleTriad).ToDictionary(entry=>entry.Id);
        bool Owned(uint id)=>entries.TryGetValue(id,out var entry) && entry.ConfirmedUnlocked;
        bool? Beaten(uint match)=>state.Beaten is { } beaten ? beaten.Contains(match):null;
        string Card(uint id)=>entries.TryGetValue(id,out var entry) ? entry.Name:id.ToString();
        if(state.Beaten is null)ImGui.TextDisabled(localization.Get("TriadNpcs.BeatenUnknown"));
        if(ImGui.CollapsingHeader(localization.Get("TriadNpcs.NpcTitle"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Checkbox(localization.Get("TriadNpcs.OnlyOpen"),ref triadNpcsOnlyOpen);
            var rows=catalog.ToFace(Beaten,Owned,triadNpcsOnlyOpen);
            ImGui.TextDisabled(localization.Format("TriadNpcs.Count",rows.Count,catalog.Npcs.Count));
            if(ImGui.BeginTable("TriadNpcTable",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.ScrollY|ImGuiTableFlags.SizingStretchProp,new System.Numerics.Vector2(0,280)))
            {
                try
                {
                    ImGui.TableSetupColumn(localization.Get("TriadNpcs.Npc"));ImGui.TableSetupColumn(localization.Get("TriadNpcs.Place"));
                    ImGui.TableSetupColumn(localization.Get("TriadNpcs.Beaten"));ImGui.TableSetupColumn(localization.Get("TriadNpcs.MissingCards"));
                    ImGui.TableSetupScrollFreeze(0,1);ImGui.TableHeadersRow();
                    foreach(var (npc,beaten,missing) in rows)
                    {
                        ImGui.TableNextRow();ImGui.TableNextColumn();
                        var place=npc.MapId!=0 ? $"{npc.Zone} (X {npc.X:0.0}, Y {npc.Y:0.0})":localization.Get("TriadNpcs.NoPlace");
                        if(ImGui.Selectable($"{npc.Name}##TriadNpc{npc.MatchId}",false,ImGuiSelectableFlags.SpanAllColumns) && npc.MapId!=0
                           && maps.State.Catalog?.Find(npc.MapId) is { } map)PlaceFlagFromMapView(map,npc.X,npc.Y);
                        if(ImGui.IsItemHovered())ImGui.SetTooltip(localization.Format("TriadNpcs.Tooltip",npc.Fee,npc.RewardCards.Count==0 ? "—":string.Join(", ",npc.RewardCards.Select(Card)))
                            +(npc.MapId!=0 ? "\n"+localization.Get("TriadNpcs.ClickFlag"):""));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(place);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(beaten==true ? Success:beaten==false ? Accent:Muted,localization.Get(beaten==true ? "TriadNpcs.Yes":beaten==false ? "TriadNpcs.No":"TriadNpcs.Unknown"));
                        ImGui.TableNextColumn();
                        foreach(var card in missing)
                        {
                            if(TriadCard(card) is { } definition){DrawSmallGameIcon(definition.IconId);if(ImGui.IsItemHovered())ImGui.SetTooltip(Card(card));ImGui.SameLine();}
                        }
                        if(missing.Count==0)ImGui.TextDisabled("—");else ImGui.NewLine();
                    }
                }
                finally{ImGui.EndTable();}
            }
        }
        // Card packs to buy first: packs ranked by how many of their cards (sheet's main source of each card) you still lack.
        if(ImGui.CollapsingHeader(localization.Get("TriadNpcs.PacksTitle"),ImGuiTreeNodeFlags.DefaultOpen) && entries.Count>0)
        {
            var packs=catalog.Cards.Values.Where(source=>source.PackItemId!=0).GroupBy(source=>(source.PackItemId,source.PackName))
                .Select(group=>(Pack:group.Key,Total:group.Count(),Missing:group.Where(source=>!Owned(source.CardId)).Select(source=>source.CardId).ToArray()))
                .OrderByDescending(pack=>pack.Missing.Length).ThenByDescending(pack=>(double)pack.Missing.Length/pack.Total).ThenBy(pack=>pack.Pack.PackName).ToArray();
            ImGui.TextDisabled(localization.Get("TriadNpcs.PacksHelp"));
            var rank=0;
            foreach(var (pack,total,lacking) in packs)
            {
                rank++;
                var color=lacking.Length==0 ? Muted:rank==1 ? Success:Accent;
                ImGui.TextColored(color,localization.Format("TriadNpcs.PackLine",rank,pack.PackName,lacking.Length,total,(100.0*lacking.Length/total).ToString("0")));
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(pack.PackItemId);
                foreach(var card in lacking.Take(20))
                    if(TriadCard(card) is { } definition){DrawSmallGameIcon(definition.IconId);if(ImGui.IsItemHovered())ImGui.SetTooltip(Card(card));ImGui.SameLine();}
                if(lacking.Length>0)ImGui.NewLine();
            }
        }
        if(ImGui.CollapsingHeader(localization.Get("TriadNpcs.CardsTitle"),ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(entries.Count==0){ImGui.TextDisabled(localization.Get("TriadRequest.NoOwned"));return;}
            ImGui.SetNextItemWidth(260);ImGui.InputTextWithHint("##TriadMissingQuery",localization.Get("TriadNpcs.Search"),ref triadMissingQuery,64);
            var missing=entries.Values.Where(entry=>!entry.ConfirmedUnlocked && (triadMissingQuery.Length==0 || entry.Name.Contains(triadMissingQuery,StringComparison.CurrentCultureIgnoreCase)))
                .OrderBy(entry=>entry.Number).ToArray();
            ImGui.TextDisabled(localization.Format("TriadNpcs.MissingCount",missing.Length,entries.Count));
            var npcNames=catalog.Npcs.ToDictionary(npc=>npc.MatchId);
            // EndChild must follow BeginChild even when it returns false (0.169: ImGui assertion inner_window == g.CurrentWindow).
            var childVisible=ImGui.BeginChild("TriadMissingCards",new System.Numerics.Vector2(0,320));
            {
                try
                {
                    if(childVisible)
                    foreach(var entry in missing)
                    {
                        if(entry.Card is { } definition)DrawSmallGameIcon(definition.IconId);
                        var source=catalog.Cards.GetValueOrDefault(entry.Id);
                        if(ImGui.IsItemHovered() && source is { ItemId:>0 })DrawItemInfoTooltip(source.ItemId);
                        ImGui.SameLine();
                        ImGui.BeginGroup();
                        ImGui.TextUnformatted(localization.Format("TriadNpcs.CardLine",entry.Number,entry.Name,new string('★',entry.Card?.Stars ?? 0)));
                        var where=new List<string>();
                        foreach(var match in source?.NpcMatchIds ?? [])
                            if(npcNames.TryGetValue(match,out var npc))
                                where.Add(npc.Name+(npc.MapId!=0 ? $" ({npc.Zone})":"")+(Beaten(match)==true ? " ✓":""));
                        if(source is { PackName.Length:>0 })where.Add(localization.Format("TriadNpcs.Pack",source.PackName));
                        ImGui.TextDisabled(where.Count>0 ? string.Join(" · ",where):localization.Get("TriadNpcs.OtherSource"));
                        ImGui.EndGroup();
                    }
                }
                finally{ImGui.EndChild();}
            }
        }
    }
}
