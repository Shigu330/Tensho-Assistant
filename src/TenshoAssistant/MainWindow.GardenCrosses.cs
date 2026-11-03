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
    // Topsoil that promotes crossbreeding (Item rows, docs/evidence/0.456-garden-grid.md).
    internal const uint GardenTopsoil3=7766,GardenTopsoil2=7765;

    private void DrawGardenCrosses()
    {
        if(!ImGui.TreeNode(localization.Get("Garden.Crosses")+"###GardenCrosses"))return;
        try
        {
            ImGui.TextDisabled(localization.Get("Garden.CrossesHelp"));
            if(GardenCrossCatalog?.Invoke() is not { } task){ImGui.TextDisabled(localization.Get("Garden.CrossesUnavailable"));return;}
            if(task.IsFaulted){ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("Garden.CrossesUnavailable"));return;}
            if(!task.IsCompletedSuccessfully){ImGui.TextDisabled(localization.Get("Garden.CrossesLoading"));return;}
            var (data,names)=task.Result;
            string Name(uint id)=>names.TryGetValue(id,out var name) && name.Length>0 ? name:"#"+id;
            if(ImGui.RadioButton(localization.Get("Garden.CrossesGives"),!gardenCrossUses))gardenCrossUses=false;
            Tip("Garden.CrossesGivesTip");
            ImGui.SameLine();
            if(ImGui.RadioButton(localization.Get("Garden.CrossesUses"),gardenCrossUses))gardenCrossUses=true;
            Tip("Garden.CrossesUsesTip");
            ImGui.SetNextItemWidth(260);
            var open=ImGui.BeginCombo("##GardenCrossSeed",gardenCrossSeed==0 ? localization.Get("Garden.CrossesPick"):Name(gardenCrossSeed));
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Garden.CrossesPickTip"));
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
        finally{ImGui.TreePop();}
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
