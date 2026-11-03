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
                    var pairs=data.ProducersOf(gardenCrossSeed);
                    ImGui.TextUnformatted(localization.Format("Garden.CrossesGivesCount",pairs.Count));
                    foreach(var (first,second) in pairs.Select(pair=>(First:Name(pair.First),Second:Name(pair.Second)))
                        .Select(pair=>string.Compare(pair.First,pair.Second,StringComparison.CurrentCultureIgnoreCase)<=0 ? pair:(First:pair.Second,Second:pair.First))
                        .OrderBy(pair=>pair.First,StringComparer.CurrentCultureIgnoreCase).ThenBy(pair=>pair.Second,StringComparer.CurrentCultureIgnoreCase))
                        ImGui.BulletText(first+" + "+second);
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
}
