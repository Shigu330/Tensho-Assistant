using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// N-14 (0.450, DEC-027): the crossbreeding planner under the garden journal. For a seed: the pairs that give it, or what it can give
// and with which seed. Pairs from nick75g/FFXIV-Crossbreed-Helper (MIT, English names linked to the game's items), shown in the game's
// language; read once in the background the first time the section is opened.
internal sealed partial class MainWindow
{
    /// <summary>The crossbreeds and the seeds' names in the game's language, loaded once off the frame (set by the plugin).</summary>
    internal Func<Task<(GardenCrossData Data,IReadOnlyDictionary<uint,string> Names)>>? GardenCrossCatalog { get; set; }
    private uint gardenCrossSeed;
    private bool gardenCrossUses;
    private string gardenCrossSearch="";
    private (uint Seed,IReadOnlyList<GardenCrossPair> Pairs)? gardenBestPairs;
    private GardenCrossPair? gardenPair;
    // X-130 (0.1109): the pairs' index, built once per catalogue; the seeds' ids by their journal name (game language).
    private (GardenCrossData? Data,IReadOnlyDictionary<(uint,uint),uint[]> Pairs,IReadOnlySet<uint> Known,IReadOnlyDictionary<string,uint> Ids,IReadOnlyDictionary<uint,string> Names)? gardenForecastIndex;

    /// <summary>X-130: the forecasts of one patch, or null while the crossbreed pairs are not read (asked here the first time).</summary>
    private IReadOnlyList<GardenForecast>? GardenGridForecasts(IReadOnlyList<(int Number,GardenBed Bed)> planted)
    {
        if(GardenCrossCatalog?.Invoke() is not { IsCompletedSuccessfully:true } task)return null;
        var (data,names)=task.Result;
        if(gardenForecastIndex is not { } index || !ReferenceEquals(index.Data,data))
        {
            var ids=new Dictionary<string,uint>(StringComparer.CurrentCultureIgnoreCase);
            foreach(var (seed,name) in GardenSeedCatalog?.Invoke() ?? [])if(name.Length>0)ids.TryAdd(name,seed.SeedId);
            foreach(var (id,name) in names)if(name.Length>0)ids.TryAdd(name,id);
            index=(data,GardenForecasts.Index(data),data.Seeds.ToHashSet(),ids,names);
            // An empty seed catalogue may still be loading: built again until it has names.
            if(ids.Count>0)gardenForecastIndex=index;
        }
        var known=index.Known;
        return GardenForecasts.For(planted.Select(entry=>(entry.Number,index.Ids.TryGetValue(entry.Bed.Seed,out var id) ? id:(uint?)null,entry.Bed.PlantedAt)).ToArray(),
            index.Pairs,seed=>known.Contains(seed));
    }

    private void DrawGardenForecast(GardenForecast forecast,IReadOnlyList<(int Number,GardenBed Bed)> planted)
    {
        string Name(uint id)=>gardenForecastIndex?.Names.TryGetValue(id,out var name)==true && name.Length>0 ? name:"#"+id;
        string BedSeed(int? number)=>planted.FirstOrDefault(entry=>entry.Number==number).Bed?.Seed ?? "?";
        switch(forecast.Kind)
        {
            case GardenForecastKind.Crosses:
                ImGui.TextWrapped(localization.Format("Garden.ForecastGives",string.Join(", ",forecast.Outcomes.Select(Name)),forecast.Partner ?? 0,Name(forecast.PartnerSeed)));
                // The seeds not held (bags and storages read) or on the wishlist, put forward.
                RefreshRecipeHoldingsCache();
                var wanted=forecast.Outcomes.Where(seed=>configuration.Wishlist?.Contains(seed)==true || GetRecipeHoldings(seed) is { HasObservation:true,Total:0 }).Select(Name).ToArray();
                if(wanted.Length>0)ImGui.TextColored(Accent,localization.Format("Garden.ForecastWanted",string.Join(", ",wanted)));
                break;
            case GardenForecastKind.NoPartner:ImGui.TextDisabled(localization.Get("Garden.ForecastNone"));break;
            case GardenForecastKind.UnknownSeed:ImGui.TextDisabled(localization.Get("Garden.ForecastUnknownSeed"));break;
            case GardenForecastKind.UnknownNeighbour:ImGui.TextDisabled(localization.Format("Garden.ForecastUnknownNeighbour",forecast.Partner ?? 0,BedSeed(forecast.Partner)));break;
        }
    }

    // Topsoil that promotes crossbreeding (Item rows, docs/evidence/0.456-garden-grid.md).
    internal const uint GardenTopsoil3=7766,GardenTopsoil2=7765;

    private void DrawGardenCrosses()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("Garden.Crosses")+"###GardenCrosses"))return;
        ImGui.TextDisabled(localization.Get("Garden.CrossesHelp"));
        // X-154 (0.1253): the shared states: failed (not offered here, or its read failed), or still being read.
        if(GardenCrossCatalog?.Invoke() is not { } task){StateLine(UI.UiState.Failed,localization.Get("Garden.CrossesUnavailable"));return;}
        if(task.IsFaulted){StateLine(UI.UiState.Failed,localization.Get("Garden.CrossesUnavailable"));return;}
        if(!task.IsCompletedSuccessfully){StateLine(UI.UiState.Waiting,localization.Get("Garden.CrossesLoading"));return;}
        var (data,names)=task.Result;
        string Name(uint id)=>names.TryGetValue(id,out var name) && name.Length>0 ? name:"#"+id;
        if(ImGui.RadioButton(localization.Get("Garden.CrossesGives"),!gardenCrossUses))gardenCrossUses=false;
        Tip("Garden.CrossesGivesTip");
        ImGui.SameLine();
        if(ImGui.RadioButton(localization.Get("Garden.CrossesUses"),gardenCrossUses))gardenCrossUses=true;
        Tip("Garden.CrossesUsesTip");
        ImGui.SetNextItemWidth(260);
        var open=ImGui.BeginCombo("##GardenCrossSeed",gardenCrossSeed==0 ? localization.Get("Garden.CrossesPick"):Name(gardenCrossSeed));
        Tip("Garden.CrossesPickTip");
        if(open)
        {
            try
            {
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##GardenCrossSearch",localization.Get("Garden.SeedSearch"),ref gardenCrossSearch,40);
                Tip("Garden.SeedSearchTip");
                var seeds=gardenCrossUses ? data.Seeds:data.Crosses.Select(cross=>cross.Target).Distinct();
                foreach(var seed in seeds.OrderBy(Name,StringComparer.CurrentCultureIgnoreCase))
                {
                    var name=Name(seed);
                    if(gardenCrossSearch.Length>0 && !name.Contains(gardenCrossSearch,StringComparison.CurrentCultureIgnoreCase))continue;
                    if(ImGui.Selectable(name+"##GardenCross"+seed,seed==gardenCrossSeed))gardenCrossSeed=seed;
                }
            }
            finally{ImGui.EndCombo();}
        }
        if(gardenCrossSeed!=0)
        {
            if(gardenCrossUses)
            {
                var uses=data.UsesOf(gardenCrossSeed);
                ImGui.TextUnformatted(localization.Format("Garden.CrossesUsesCount",uses.Count));
                foreach(var group in uses.GroupBy(use=>use.Target).OrderBy(group=>Name(group.Key),StringComparer.CurrentCultureIgnoreCase))
                    ImGui.BulletText(localization.Format("Garden.CrossesUsesRow",Name(group.Key),
                        string.Join(", ",group.Select(use=>Name(use.Partner)).Order(StringComparer.CurrentCultureIgnoreCase))));
            }
            else
            {
                DrawGardenPairs(data,Name);
            }
        }
        ImGui.TextDisabled(localization.Format("Garden.CrossesSource",data.Crosses.Count));
        if(data.Unmatched.Count>0)
        {
            ImGui.TextDisabled(localization.Format("Garden.CrossesUnmatched",data.Unmatched.Count));
            if(ImGui.IsItemHovered())WrappedTooltip(string.Join(", ",data.Unmatched));
        }
    }

    // 0.456 (user request, in-game campaign): to get as many of a seed as possible: its pairs, those with the fewest other outcomes first, and the plan of the patch.
    private void DrawGardenPairs(GardenCrossData data,Func<uint,string> name)
    {
        if(gardenBestPairs is not { } best || best.Seed!=gardenCrossSeed){best=(gardenCrossSeed,data.BestPairsFor(gardenCrossSeed));gardenBestPairs=best;gardenPair=null;}
        ImGui.TextUnformatted(localization.Format("Garden.CrossesGivesCount",best.Pairs.Count));
        ImGui.TextDisabled(localization.Get("Garden.PairsHelp"));
        var visible=ImGui.BeginChild("GardenPairs",new Vector2(0,Math.Clamp(best.Pairs.Count*ImGui.GetTextLineHeightWithSpacing()+8,40,180)),true);
        try
        {
            if(visible)
                foreach(var pair in best.Pairs)
                {
                    var others=pair.Outcomes.Where(seed=>seed!=gardenCrossSeed).Select(name).ToArray();
                    var label=name(pair.First)+" + "+name(pair.Second)+"  · "+(others.Length==0 ? localization.Get("Garden.PairOnly"):localization.Format("Garden.PairOthers",others.Length));
                    if(ImGui.Selectable(label+$"##GardenPair{pair.First}/{pair.Second}",gardenPair==pair))gardenPair=pair;
                    if(others.Length>0 && ImGui.IsItemHovered())WrappedTooltip(string.Join(", ",others));
                }
        }
        finally{ImGui.EndChild();}
        if(gardenPair is { } chosen)DrawGardenPlan(chosen,name);
    }

    private void DrawGardenPlan(GardenCrossPair pair,Func<uint,string> name)
    {
        var seeds=GardenSeedCatalog?.Invoke() ?? [];
        var plan=GardenCrosses.Plan(pair,seed=>seeds.FirstOrDefault(row=>row.Seed.SeedId==seed).Seed?.GrowHours);
        if(!ImGui.BeginTable("GardenPlan",3,ImGuiTableFlags.Borders|ImGuiTableFlags.SizingStretchSame))return;
        try
        {
            for(var row=0;row<3;row++)
            {
                ImGui.TableNextRow();
                for(var column=0;column<3;column++)
                {
                    ImGui.TableNextColumn();
                    if(plan.FirstOrDefault(cell=>cell.Row==row && cell.Column==column) is not { } bed){ImGui.TextDisabled("—");continue;}
                    ImGui.PushStyleColor(ImGuiCol.Text,bed.Seed==plan[0].Seed ? Accent:Success);
                    try{ImGui.TextWrapped($"{bed.Order}. {name(bed.Seed)}");}
                    finally{ImGui.PopStyleColor();}
                }
            }
        }
        finally{ImGui.EndTable();}
        ImGui.TextWrapped(localization.Get("Garden.PlanOrder"));
        ImGui.TextWrapped(localization.Format("Garden.PlanSoil",name(GardenTopsoil3),name(GardenTopsoil2)));
        ImGui.TextDisabled(localization.Get("Garden.PlanSource"));
    }
}
