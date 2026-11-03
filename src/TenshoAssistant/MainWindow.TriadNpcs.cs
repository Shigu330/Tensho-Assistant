using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;
using TenshoAssistant.MiniGames;

namespace TenshoAssistant;

/// <summary>
/// Triple Triad › NPCs and cards (0.168), as TriadBuddy: NPCs not beaten yet or still holding a card you lack (zone, coordinates, click =
/// map flag), and every missing card with where it comes from (NPCs, card pack, then the item's « how to obtain » tooltip).
/// Since 0.276 three sub-tabs, only the open one computed: NPCs, card packs ranked by how many of their cards you lack, missing cards.
/// </summary>
internal sealed partial class MainWindow
{
    private TriadNpcService? triadNpcs;
    private bool triadNpcsOnlyOpen=true;
    private string triadMissingQuery=string.Empty;
    private string? triadNpcsFocus;
    // 0.291 (R-06 step 6, measured 14 ms): the NPC ranking, the pack ranking and the missing-card list are rebuilt only when the
    // catalogue, the collections, the beaten list or a filter change; long lists are drawn through a clipper (visible rows only).
    private (object? Catalog,object? Entries,object? Beaten,bool OnlyOpen) triadFaceKey;
    private IReadOnlyList<(TriadNpc Npc,bool? Beaten,IReadOnlyList<uint> Missing)> triadFaceRows=[];
    private (object? Catalog,object? Entries) triadPacksKey;
    private ((uint PackItemId,string PackName) Pack,int Total,uint[] Missing)[] triadPacks=[];
    private (object? Entries,string Query) triadMissingKey;
    private CollectionEntry[] triadMissingRows=[];
    private object? triadNpcNamesKey;
    private Dictionary<uint,TriadNpc> triadNpcNames=[];
    internal void ConfigureTriadNpcs(TriadNpcService service)=>triadNpcs=service;

    private void DrawTriadNpcs()
    {
        var state=triadNpcs?.State;
        if(state?.Catalog is not { } catalog){ImGui.TextDisabled(localization.Get(state?.Error is null ? "TriadNpcs.Loading":"TriadNpcs.Error"));return;}
        // 0.290: cached per collection state (was rebuilt from every collection row on each frame).
        var entries=TriadEntriesById();
        bool Owned(uint id)=>entries.TryGetValue(id,out var entry) && entry.ConfirmedUnlocked;
        bool? Beaten(uint match)=>state.Beaten is { } beaten ? beaten.Contains(match):null;
        string Card(uint id)=>entries.TryGetValue(id,out var entry) ? entry.Name:id.ToString();
        if(state.Beaten is null)ImGui.TextDisabled(localization.Get(state.BeatenUnavailable ? "TriadNpcs.BeatenUnavailable":"TriadNpcs.BeatenUnknown"));
        // 0.276 (user request): the three sections are sub-tabs (only the open one is computed) instead of stacked headers.
        DetailTabs("TriadNpcTabs",
        [
            ("Npcs",localization.Get("TriadNpcs.NpcTitle"),DrawNpcList),
            ("Packs",localization.Get("TriadNpcs.PacksTitle"),DrawPacks),
            ("Missing",localization.Get("TriadNpcs.CardsTitle"),DrawMissing),
        ],ref triadNpcsFocus);

        void DrawNpcList()
        {
            ImGui.Checkbox(localization.Get("TriadNpcs.OnlyOpen"),ref triadNpcsOnlyOpen);Tip("TriadNpcs.OnlyOpenTip");
            var faceKey=((object?)catalog,(object?)entries,(object?)state.Beaten,triadNpcsOnlyOpen);
            if(faceKey!=triadFaceKey){triadFaceKey=faceKey;triadFaceRows=catalog.ToFace(Beaten,Owned,triadNpcsOnlyOpen);}
            var rows=triadFaceRows;
            ImGui.TextDisabled(localization.Format("TriadNpcs.Count",rows.Count,catalog.Npcs.Count));
            if(ImGui.BeginTable("TriadNpcTable",4,ImGuiTableFlags.RowBg|ImGuiTableFlags.ScrollY|ImGuiTableFlags.SizingStretchProp,new System.Numerics.Vector2(0,Math.Max(200,ImGui.GetContentRegionAvail().Y))))
            {
                try
                {
                    ImGui.TableSetupColumn(localization.Get("TriadNpcs.Npc"));ImGui.TableSetupColumn(localization.Get("TriadNpcs.Place"));
                    ImGui.TableSetupColumn(localization.Get("TriadNpcs.Beaten"));ImGui.TableSetupColumn(localization.Get("TriadNpcs.MissingCards"));
                    ImGui.TableSetupScrollFreeze(0,1);ImGui.TableHeadersRow();
                    var clipper=ImGui.ImGuiListClipper();
                    try
                    {
                    clipper.Begin(rows.Count);
                    while(clipper.Step())
                    for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                    {
                        var (npc,beaten,missing)=rows[index];
                        ImGui.TableNextRow();ImGui.TableNextColumn();
                        var place=npc.MapId!=0 ? $"{npc.Zone} (X {npc.X:0.0}, Y {npc.Y:0.0})":localization.Get("TriadNpcs.NoPlace");
                        if(ImGui.Selectable($"{npc.Name}##TriadNpc{npc.MatchId}",false,ImGuiSelectableFlags.SpanAllColumns) && npc.MapId!=0
                           && maps.State.Catalog?.Find(npc.MapId) is { } map)PlaceFlagFromMapView(map,npc.X,npc.Y);
                        NpcMenu($"TriadNpcMenu{npc.MatchId}",npc.Name,0,maps.State.Catalog?.Find(npc.MapId) is { } npcMap ? [new(npcMap.TerritoryId,npcMap.MapId,npcMap.Zone,npc.X,npc.Y,0,0,0)]:[]);
                        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("TriadNpcs.Tooltip",npc.Fee,npc.RewardCards.Count==0 ? "—":string.Join(", ",npc.RewardCards.Select(Card)))
                            +(npc.MapId!=0 ? "\n"+localization.Get("TriadNpcs.ClickFlag"):""));
                        ImGui.TableNextColumn();ImGui.TextUnformatted(place);
                        ImGui.TableNextColumn();
                        ImGui.TextColored(beaten==true ? Success:beaten==false ? Accent:Muted,localization.Get(beaten==true ? "TriadNpcs.Yes":beaten==false ? "TriadNpcs.No":"TriadNpcs.Unknown"));
                        ImGui.TableNextColumn();
                        foreach(var card in missing)
                        {
                            if(TriadCard(card) is { } definition){DrawSmallGameIcon(definition.IconId);TriadCardTooltip(card);ImGui.SameLine();}
                        }
                        if(missing.Count==0)ImGui.TextDisabled("—");else ImGui.NewLine();
                    }
                    }
                    finally{clipper.End();clipper.Destroy();}
                }
                finally{ImGui.EndTable();}
            }
        }
        // Card packs to buy first: packs ranked by how many of their cards (sheet's main source of each card) you still lack.
        void DrawPacks()
        {
            if(entries.Count==0){ImGui.TextDisabled(localization.Get(collections.State.Loading ? "TriadRequest.NoOwned":"ServiceErrors.Collection"));return;}
            var packsKey=((object?)catalog,(object?)entries);
            if(packsKey!=triadPacksKey)
            {
                triadPacksKey=packsKey;
                triadPacks=catalog.Cards.Values.Where(source=>source.PackItemId!=0).GroupBy(source=>(source.PackItemId,source.PackName))
                    .Select(group=>(Pack:group.Key,Total:group.Count(),Missing:group.Where(source=>!Owned(source.CardId)).Select(source=>source.CardId).ToArray()))
                    .OrderByDescending(pack=>pack.Missing.Length).ThenByDescending(pack=>(double)pack.Missing.Length/pack.Total).ThenBy(pack=>pack.Pack.PackName).ToArray();
            }
            var packs=triadPacks;
            ImGui.TextDisabled(localization.Get("TriadNpcs.PacksHelp"));
            var rank=0;
            foreach(var (pack,total,lacking) in packs)
            {
                rank++;
                var color=lacking.Length==0 ? Muted:rank==1 ? Success:Accent;
                ImGui.TextColored(color,localization.Format("TriadNpcs.PackLine",rank,pack.PackName,lacking.Length,total,(100.0*lacking.Length/total).ToString("0")));
                if(ImGui.IsItemHovered())DrawItemInfoTooltip(pack.PackItemId);
                foreach(var card in lacking.Take(20))
                    if(TriadCard(card) is { } definition){DrawSmallGameIcon(definition.IconId);TriadCardTooltip(card);ImGui.SameLine();}
                if(lacking.Length>0)ImGui.NewLine();
            }
        }
        void DrawMissing()
        {
            if(entries.Count==0){ImGui.TextDisabled(localization.Get(collections.State.Loading ? "TriadRequest.NoOwned":"ServiceErrors.Collection"));return;}
            ImGui.SetNextItemWidth(260);ImGui.InputTextWithHint("##TriadMissingQuery",localization.Get("TriadNpcs.Search"),ref triadMissingQuery,64);Tip("TriadNpcs.SearchTip");
            var missingKey=((object?)entries,triadMissingQuery);
            if(missingKey!=triadMissingKey)
            {
                triadMissingKey=missingKey;
                triadMissingRows=entries.Values.Where(entry=>!entry.ConfirmedUnlocked && (triadMissingQuery.Length==0 || entry.Name.Contains(triadMissingQuery,StringComparison.CurrentCultureIgnoreCase)))
                    .OrderBy(entry=>entry.Card?.Extra==true).ThenBy(entry=>entry.Number).ToArray();
            }
            var missing=triadMissingRows;
            ImGui.TextDisabled(localization.Format("TriadNpcs.MissingCount",missing.Length,entries.Count));
            if(!ReferenceEquals(catalog,triadNpcNamesKey)){triadNpcNamesKey=catalog;triadNpcNames=catalog.Npcs.ToDictionary(npc=>npc.MatchId);}
            var npcNames=triadNpcNames;
            // EndChild must follow BeginChild even when it returns false (0.169: ImGui assertion inner_window == g.CurrentWindow).
            var childVisible=ImGui.BeginChild("TriadMissingCards",new System.Numerics.Vector2(0,Math.Max(200,ImGui.GetContentRegionAvail().Y)));
            {
                try
                {
                    var clipper=ImGui.ImGuiListClipper();
                    try
                    {
                    if(childVisible)clipper.Begin(missing.Length);
                    while(childVisible && clipper.Step())
                    for(var index=clipper.DisplayStart;index<clipper.DisplayEnd;index++)
                    {
                        var entry=missing[index];
                        if(entry.Card is { } definition)DrawSmallGameIcon(definition.IconId);
                        var source=catalog.Cards.GetValueOrDefault(entry.Id);
                        TriadCardTooltip(entry.Id);
                        ImGui.SameLine();
                        ImGui.BeginGroup();
                        ImGui.TextUnformatted(localization.Format("TriadNpcs.CardLine",entry.Card?.Label ?? entry.Number.ToString(),entry.Name,new string('★',entry.Card?.Stars ?? 0)));
                        var where=new List<string>();
                        foreach(var match in source?.NpcMatchIds ?? [])
                            if(npcNames.TryGetValue(match,out var npc))
                                where.Add(npc.Name+(npc.MapId!=0 ? $" ({npc.Zone})":"")+(Beaten(match)==true ? " ✓":""));
                        if(source is { PackName.Length:>0 })where.Add(localization.Format("TriadNpcs.Pack",source.PackName));
                        ImGui.TextDisabled(where.Count>0 ? string.Join(" · ",where):localization.Get("TriadNpcs.OtherSource"));
                        ImGui.EndGroup();
                    }
                    }
                    finally{clipper.End();clipper.Destroy();}
                }
                finally{ImGui.EndChild();}
            }
        }
    }
}
