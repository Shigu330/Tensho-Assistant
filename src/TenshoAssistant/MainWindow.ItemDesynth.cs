using Dalamud.Bindings.ImGui;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// X-197 (0.1761, the user's request of 12/11/2026, K-06): under the game's item tooltip (and in the item sheet), the item's desynthesis against
// your level, and what a coffer, a card pack or a container holds that a collection tracks (ItemDesynth).
internal sealed partial class MainWindow
{
    /// <summary>The item's desynthesis from the game's Item sheet (Item.Desynth, ClassJobRepair, LevelItem); null: none, or not read.</summary>
    internal Func<uint,DesynthInfo?>? ItemDesynthOf { get; set; }
    /// <summary>Your desynthesis level of a crafter, read in the game (DesynthesisLevels); null: unknown.</summary>
    internal Func<uint,float?>? DesynthLevelOf { get; set; }
    private readonly Dictionary<uint,DesynthInfo?> desynthCache=[];
    private readonly Dictionary<uint,(CofferStanding? Standing,long At)> cofferCache=[];

    // Once per item: the sheet does not change while the plugin runs.
    private DesynthInfo? DesynthOf(uint id)
    {
        if(id==0 || ItemDesynthOf is not { } read)return null;
        if(desynthCache.TryGetValue(id,out var known))return known;
        DesynthInfo? info;
        try{info=read(id);}catch(Exception){info=null;}
        if(desynthCache.Count>4096)desynthCache.Clear();
        return desynthCache[id]=info;
    }

    // Kept 5 s per item, as the game's unlock states (GameItemUnlocked): a collection may change while the tooltip is shown.
    private CofferStanding? CofferOf(uint id)
    {
        if(id==0 || lumina?.State.Data is not { } data)return null;
        var now=Environment.TickCount64;
        if(cofferCache.TryGetValue(id,out var known) && now-known.At<5000)return known.Standing;
        var items=itemCatalog.State.Catalog;
        var standing=ItemDesynth.Coffer(data.GivesOf(id),given=>items?.Find(given) is { } item ? ItemAcquisition(item) switch
        {
            Collections.CollectionAcquisition.Acquired=>true,
            Collections.CollectionAcquisition.NotAcquired=>false,
            _=>null,
        }:null);
        if(cofferCache.Count>1024)cofferCache.Clear();
        cofferCache[id]=(standing,now);
        return standing;
    }

    private void DrawItemDesynth(uint id)
    {
        if(DesynthOf(id) is not { } info)return;
        var level=DesynthLevelOf?.Invoke(info.Job);
        var standing=ItemDesynth.Standing(info.ItemLevel,level);
        ImGui.TextWrapped(level is { } yours
            ? localization.Format("HoverUi.Desynth",JobName(info.Job),info.ItemLevel,yours.ToString("0.##",Numbers),localization.Get("HoverUi.Desynth"+standing))
            :localization.Format("HoverUi.DesynthUnknown",JobName(info.Job),info.ItemLevel));
    }

    private void DrawItemCoffer(uint id)
    {
        if(CofferOf(id) is not { } coffer)return;
        var partial=coffer.Partial ? localization.Format("HoverUi.CofferPartial",coffer.Listed):"";
        ImGui.TextWrapped(coffer.Missing==0 ? localization.Format("HoverUi.CofferAll",coffer.Collectibles)+partial
            :localization.Format("HoverUi.CofferMissing",coffer.Missing,coffer.Collectibles)+partial);
    }
}
