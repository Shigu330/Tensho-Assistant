using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-15 (0.417): while the game's spearfishing window is open (setting FishingPanel, as the fishing help), a panel beside it.
/// X-79 (0.891, user request: « the list should stay all the time and highlight the right size », the old one vanished with the fish): the fish in sight (size,
/// speed), then every fish of the node (else of the zone) always listed in a fixed order, each with its shadow (yours first, else GatherBuddy's), the ones
/// whose shadow is in sight highlighted.
/// </summary>
internal sealed partial class MainWindow
{
    internal SpearfishingReader? Spearfishing { get; set; }

    /// <summary>X-73.18 (0.890): a shadow's size and speed, in words.</summary>
    private string SpearShadowText(int size,int speed)=>localization.Format("Spearfishing.SizeSpeed",
        localization.Get(size switch{1=>"Spearfishing.Small",2=>"Spearfishing.Average",3=>"Spearfishing.Large",_=>"Spearfishing.UnknownSize"}),
        Gathering.Spearfishing.SpeedName(speed) is { } name ? localization.Get("Spearfishing.Speed"+name):speed.ToString());
    /// <summary>X-73.18 (0.891): the shadow you saw a fish have most often; worked out again only when the base changes.</summary>
    private (long Revision,Dictionary<uint,(int Size,int Speed)?> Shadows) seenShadows=(-1,[]);
    private (int Size,int Speed)? SeenShadowOf(uint fish)
    {
        if(DiscoveryStore is not { } store || !DiscoveriesLoaded)return null;
        if(seenShadows.Revision!=store.Revision)seenShadows=(store.Revision,[]);
        if(seenShadows.Shadows.TryGetValue(fish,out var known))return known;
        (int,int)? shadow=store.Find(TenshoAssistant.Discoveries.DiscoveryKind.SpearShadow,subject=>subject==fish,16).OrderByDescending(fact=>fact.Seen+fact.Imported)
            .Select(fact=>((int,int)?)((int)fact.Detail,fact.LevelMin)).FirstOrDefault();
        if(seenShadows.Shadows.Count<1024)seenShadows.Shadows[fish]=shadow;
        return shadow;
    }
    /// <summary>X-79 (0.891): the spearfishing fish of the node, else of the zone (catalogue rows of type 4 or 5); worked out once per node.</summary>
    private (uint Point,uint Territory,object? Catalog,bool OfPoint,uint[] Fish) spearFishOf=(0,0,null,false,[]);
    private (bool OfPoint,uint[] Fish) SpearfishOf(uint point,uint territory)
    {
        var catalog=gatheringCatalog.State.Catalog;
        if(catalog is null)return (false,[]);
        if(spearFishOf.Point!=point || spearFishOf.Territory!=territory || !ReferenceEquals(spearFishOf.Catalog,catalog))
        {
            var ofPoint=point==0 ? []:catalog.Sources.Where(source=>source.PointId==point && source.TypeId is 4 or 5).Select(source=>source.ItemId).Distinct().ToArray();
            var fish=ofPoint.Length>0 ? ofPoint:catalog.Sources.Where(source=>source.TerritoryId==territory && source.TypeId is 4 or 5).Select(source=>source.ItemId).Distinct().ToArray();
            spearFishOf=(point,territory,catalog,ofPoint.Length>0,fish);
        }
        return (spearFishOf.OfPoint,spearFishOf.Fish);
    }

    internal void DrawSpearfishingPanel()
    {
        if(windowDisposed || !configuration.FishingPanel || Spearfishing?.Frame is not { } frame)return;
        Panels?.Show("TenshoSpearfishingPanel",localization.Get("Spearfishing.Title"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(new Vector2(frame.X+frame.Width+6,frame.Y)+WindowMoved("SpearFishing",frame.X,frame.Y),ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(380,0),ImGuiCond.Always);},()=>
        {
            gatherBuddyFish?.Request();
            var data=gatherBuddyFish?.State.Data;
            var items=itemCatalog.State.Catalog;
            (int,int)? Community(uint fish)=>data?.Find(fish) is { SpearSize:{ } size,SpearSpeed:{ } speed } ? (size,speed):null;
            var (ofPoint,fishHere)=SpearfishOf(frame.PointId,frame.TerritoryId);
            var rows=Gathering.Spearfishing.Rows(fishHere,SeenShadowOf,Community,frame.Slots);
            // The fish in sight, one short line each.
            var shown=0;
            foreach(var slot in frame.Slots.Where(slot=>slot.Available))
            {
                shown++;
                var size=localization.Get(slot.Size switch{1=>"Spearfishing.Small",2=>"Spearfishing.Average",3=>"Spearfishing.Large",_=>"Spearfishing.UnknownSize"});
                var speed=Gathering.Spearfishing.SpeedName(slot.Speed) is { } name ? localization.Get("Spearfishing.Speed"+name):slot.Speed.ToString();
                ImGui.TextColored(Muted,localization.Format("Spearfishing.Slot",slot.Index+1,size,speed)+(slot.Large ? " · "+localization.Get("Spearfishing.GuaranteedLarge"):""));
                if(developerMode)ImGui.TextDisabled($"size {slot.Size} · speed {slot.Speed} · reverse {slot.Reverse}");
                // A shadow no fish of the list has: GatherBuddy's fish of other places, three at most.
                if(!rows.Any(row=>row.Size==slot.Size && row.Speed==slot.Speed) && data is not null)
                {
                    var elsewhere=Gathering.Spearfishing.Candidates(data.Spearfish,slot,_=>false).Elsewhere;
                    ImGui.TextDisabled(elsewhere.Count==0 ? localization.Get("Spearfishing.NoMatch")
                        :localization.Format("Spearfishing.Elsewhere",string.Join(", ",elsewhere.Take(3).Select(fish=>items?.Find(fish)?.Name ?? $"ID {fish}"))));
                }
            }
            if(shown==0)ImGui.TextDisabled(localization.Get("Spearfishing.NoFish"));
            ImGui.Separator();
            ImGui.TextUnformatted(localization.Get(ofPoint ? "Spearfishing.PointFish":"Spearfishing.ZoneFish"));
            if(rows.Count==0)ImGui.TextDisabled(localization.Get("Spearfishing.NoList"));
            foreach(var row in rows)
            {
                var text=(items?.Find(row.Fish)?.Name ?? $"ID {row.Fish}")+" — "+(row.Size is { } size && row.Speed is { } speed ? SpearShadowText(size,speed):"?")
                    +(row.Seen ? " · "+localization.Get("Spearfishing.Seen"):"");
                if(row.InSight)ImGui.TextColored(Accent,"» "+text+" · "+localization.Get("Spearfishing.InSight"));
                else if(row.Size is null)ImGui.TextDisabled("   "+text);
                else ImGui.TextUnformatted("   "+text);
            }
            if(data is null)ImGui.TextDisabled(localization.Get("Spearfishing.NoData"));
            ImGui.TextDisabled(localization.Get("Spearfishing.Credit"));
        });
    }
}
