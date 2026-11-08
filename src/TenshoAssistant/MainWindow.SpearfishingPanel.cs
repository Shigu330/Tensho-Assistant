using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

/// <summary>
/// R-15 (0.417): while the game's spearfishing window is open (setting FishingPanel, as the fishing help), a panel beside it.
/// X-79 (0.891, user request: « the list should stay all the time and highlight the right size », the old one vanished with the fish): the fish in sight (size,
/// speed), then every fish of the node (else of the zone) always listed in a fixed order, each with its shadow (seen in game first, yours or shared, else GatherBuddy's), the ones
/// whose shadow is in sight highlighted. 0.893: the fish's icons (list and slot lines). 0.892: a fixed layout (three slot lines, rows that keep their place and width), the node found when the window opens.
/// </summary>
internal sealed partial class MainWindow
{
    internal SpearfishingReader? Spearfishing { get; set; }

    /// <summary>X-73.18 (0.890): a shadow's size and speed, in words.</summary>
    private string SpearShadowText(int size,int speed)=>localization.Format("Spearfishing.SizeSpeed",
        localization.Get(size switch{1=>"Spearfishing.Small",2=>"Spearfishing.Average",3=>"Spearfishing.Large",_=>"Spearfishing.UnknownSize"}),
        Gathering.Spearfishing.SpeedName(speed) is { } name ? localization.Get("Spearfishing.Speed"+name):speed.ToString());
    /// <summary>X-73.18 (0.891): the shadow a fish was seen in game with most often (yours or shared); worked out again only when the base changes.</summary>
    private (long Revision,Dictionary<uint,(int Size,int Speed)?> Shadows) seenShadows=(-1,[]);
    private (int Size,int Speed)? SeenShadowOf(uint fish)
    {
        if(DiscoveryStore is not { } store || !DiscoveriesLoaded)return null;
        if(seenShadows.Revision!=store.Revision)seenShadows=(store.Revision,[]);
        if(seenShadows.Shadows.TryGetValue(fish,out var known))return known;
        var shadow=store.MostSeenSpearShadow(fish);
        if(seenShadows.Shadows.Count<1024)seenShadows.Shadows[fish]=shadow;
        return shadow;
    }
    /// <summary>X-79 (0.891, 0.894: by the node's base, as the game's node tooltip lists them): the spearfishing fish of the node, else of the zone (catalogue rows of
    /// type 4 or 5); worked out once per node.</summary>
    private (uint Point,uint Territory,object? Catalog,bool OfPoint,uint[] Fish) spearFishOf=(0,0,null,false,[]);
    private (bool OfPoint,uint[] Fish) SpearfishOf(uint point,uint territory)
    {
        var catalog=gatheringCatalog.State.Catalog;
        if(catalog is null)return (false,[]);
        if(spearFishOf.Point!=point || spearFishOf.Territory!=territory || !ReferenceEquals(spearFishOf.Catalog,catalog))
        {
            var ofPoint=point==0 ? []:catalog.Sources.Where(source=>source.BaseId==point && source.TypeId is 4 or 5).Select(source=>source.ItemId).Distinct().ToArray();
            var fish=ofPoint.Length>0 ? ofPoint:catalog.Sources.Where(source=>source.TerritoryId==territory && source.TypeId is 4 or 5).Select(source=>source.ItemId).Distinct().ToArray();
            spearFishOf=(point,territory,catalog,ofPoint.Length>0,fish);
        }
        return (spearFishOf.OfPoint,spearFishOf.Fish);
    }

    /// <summary>0.897: one icon-font symbol as text.</summary>
    private static void SpearGlyph(FontAwesomeIcon icon,Vector4 colour)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.TextColored(colour,icon.ToIconString());}
        finally{ImGui.PopFont();}
    }

    internal void DrawSpearfishingPanel()
    {
        if(windowDisposed || !configuration.FishingPanel || Spearfishing?.Frame is not { } frame)return;
        Panels?.Show("TenshoSpearfishingPanel",localization.Get("Spearfishing.Title"),ImGuiWindowFlags.NoSavedSettings|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoCollapse|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.AlwaysAutoResize,()=>{ImGui.SetNextWindowPos(new Vector2(frame.X+frame.Width+6,frame.Y)+WindowMoved("SpearFishing",frame.X,frame.Y),ImGuiCond.Always);ImGui.SetNextWindowSize(new Vector2(320,0),ImGuiCond.Always);},()=>
        {
            gatherBuddyFish?.Request();
            var data=gatherBuddyFish?.State.Data;
            var items=itemCatalog.State.Catalog;
            (int,int)? Community(uint fish)=>data?.Find(fish) is { SpearSize:{ } size,SpearSpeed:{ } speed } ? (size,speed):null;
            var (ofPoint,fishHere)=SpearfishOf(frame.BaseId,frame.TerritoryId);
            var rows=Gathering.Spearfishing.Rows(fishHere,SeenShadowOf,Community,frame.Slots);
            // 0.892 (user report: « it changes all the time »): a fixed layout. Three slot lines always; the list keeps its rows, the fish in sight are framed and coloured.
            // 0.897 (user request: too much text to read in real time): sizes S/M/L, speeds as chevrons (one per step of 50), a star for a guaranteed large fish, an
            // eye for the shadows seen in game (yours or shared); the words on hover.
            var disabled=Vector4.Lerp(Muted,UI.FfxivTheme.Panel,0.35f);
            var widest=rows.Select(row=>Gathering.Spearfishing.SpeedStep(row.Speed ?? 0)).Concat(frame.Slots.Select(slot=>Gathering.Spearfishing.SpeedStep(slot.Speed))).DefaultIfEmpty(1).Max();
            var shadowWidth=ImGui.CalcTextSize("L "+new string('>',Math.Max(1,widest))).X;
            for(var index=0;index<3;index++)
            {
                ImGui.TextDisabled($"{index+1}");
                ImGui.SameLine(ImGui.GetFontSize()*1.4f);
                if(frame.Slots.FirstOrDefault(slot=>slot.Index==index && slot.Available) is not { } slot){ImGui.TextDisabled("—");continue;}
                ImGui.TextColored(Accent,Gathering.Spearfishing.ShortShadow(slot.Size,slot.Speed));
                if(ImGui.IsItemHovered())WrappedTooltip(SpearShadowText(slot.Size,slot.Speed));
                if(slot.Large)
                {
                    ImGui.SameLine();
                    SpearGlyph(FontAwesomeIcon.Star,new Vector4(1f,0.82f,0.3f,1));
                    Tip("Spearfishing.GuaranteedLarge");
                }
                // 0.893 (user request: the fish's icon, to help the eye): the icons of the fish of the list with this shadow, four at most, at text height.
                foreach(var match in rows.Where(row=>row.Size is { } size && row.Speed is { } speed && Gathering.Spearfishing.Shadows(slot).Contains((size,speed))).Take(4))
                {
                    ImGui.SameLine();
                    DrawItemIcon(items?.Find(match.Fish),new Vector2(ImGui.GetTextLineHeight()));
                }
            }
            ImGui.Separator();
            // Without a node, the zone's list would be too long: only the fish seen in game there (yours or shared) and those whose shadow is in sight.
            var listed=ofPoint ? rows:rows.Where(row=>row.Seen || row.InSight).ToArray();
            if(!ofPoint)ImGui.TextDisabled(localization.Get("Spearfishing.ZoneFish"));
            if(listed.Count==0)ImGui.TextDisabled(localization.Get("Spearfishing.NoList"));
            // 0.893 (user request): each fish with its icon; a fish in sight gets a frame around its icon and the accent colour (same height and width either way).
            var side=ImGui.GetTextLineHeight()*1.6f;
            foreach(var row in listed)
            {
                var name=items?.Find(row.Fish)?.Name ?? $"ID {row.Fish}";
                var colour=row.InSight ? Accent:row.Size is null ? disabled:Muted;
                DrawItemIcon(items?.Find(row.Fish),new Vector2(side));
                if(row.InSight)ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin()-new Vector2(1),ImGui.GetItemRectMax()+new Vector2(1),ImGui.GetColorU32(Accent),3,ImDrawFlags.None,2);
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY()+(side-ImGui.GetTextLineHeight())/2);
                var column=ImGui.GetCursorPosX();
                ImGui.TextColored(colour,row.Size is { } size && row.Speed is { } speed ? Gathering.Spearfishing.ShortShadow(size,speed):"?");
                ImGui.SameLine(column+shadowWidth+ImGui.GetStyle().ItemSpacing.X);
                ImGui.TextColored(colour,name);
                if(ImGui.IsItemHovered())WrappedTooltip(name+" — "+(row.Size is { } wordsSize && row.Speed is { } wordsSpeed ? SpearShadowText(wordsSize,wordsSpeed):"?")
                    +(row.Seen ? " · "+localization.Get("Spearfishing.Seen"):""));
                if(row.Seen)
                {
                    ImGui.SameLine();
                    SpearGlyph(FontAwesomeIcon.Eye,colour);
                    Tip("Spearfishing.Seen");
                }
            }
            if(developerMode)ImGui.TextDisabled($"node base {frame.BaseId} · {fishHere.Length} fish · slots "+string.Join(" ",frame.Slots.Select(slot=>$"{(slot.Available ? 1:0)}/{slot.Size}/{slot.Speed}")));
            if(data is null)ImGui.TextDisabled(localization.Get("Spearfishing.NoData"));
            ImGui.TextDisabled(localization.Get("Spearfishing.Credit"));
            Tip("Spearfishing.Legend");
        });
    }
}
