using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Items;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// X-178 (0.1257, user request): a garden patch read at a glance. Each planted bed is its seed's game icon with a growth bar under it, the
// time left beside it, a round mark on its corner (tending later, tending due, ripe) and two icon buttons (tended, remove in two clicks);
// the dates are in the tooltip, a click puts the seed in context, a right click opens its menu. An empty bed is an outlined square. The
// state is always told by a glyph or a word, never by the colour alone (warning and accent are almost the same colour in the dark theme).
// « Detailed view » (Garden.DetailedView) brings the text of before back. Computations: Reminders/GardenView.cs.
internal sealed partial class MainWindow
{
    // The seeds' items by the name noted in the journal, for the beds without a noted item: found once per catalogue and name.
    private readonly Dictionary<string,uint?> gardenSeedByName=new(StringComparer.CurrentCultureIgnoreCase);
    private ItemCatalog? gardenSeedByNameCatalog;

    private uint? GardenSeedByName(string name)
    {
        if(gardenForecastIndex is { } index && index.Ids.TryGetValue(name,out var known))return known;
        // Without the seed list (the desktop application, or before it is read): the item of exactly that name, if any.
        if(itemCatalog.State.Catalog is not { } catalog)return null;
        if(!ReferenceEquals(catalog,gardenSeedByNameCatalog)){gardenSeedByName.Clear();gardenSeedByNameCatalog=catalog;}
        if(gardenSeedByName.TryGetValue(name,out var found))return found;
        if(gardenSeedByName.Count>=GardenJournal.Capacity*2)gardenSeedByName.Clear();
        found=null;
        foreach(var item in catalog.Search(name,10))
            if(string.Equals(item.Name,name,StringComparison.CurrentCultureIgnoreCase)){found=item.Id;break;}
        return gardenSeedByName[name]=found;
    }

    private ItemDefinition? GardenSeedItem(GardenBed bed)
        =>GardenView.SeedIdOf(bed,GardenSeedByName) is { } id ? itemCatalog.State.Catalog?.Find(id):null;

    private string GardenLeftText((int Days,int Hours,int Minutes) left)
    {
        var (key,first,second)=GardenView.LeftText(left);
        return localization.Format(key,first,second);
    }

    /// <summary>One cell of a patch in icons: the bed's number, then the seed (or the empty bed).</summary>
    private void DrawGardenGridIcons(int number,GardenBed? bed,IReadOnlyList<(int Number,GardenBed Bed)> planted,IReadOnlyList<GardenForecast>? forecasts,DateTimeOffset now,ref bool changed,ref GardenBed? removed)
    {
        var frame=ImGui.GetFrameHeight();
        var side=MathF.Round(frame*2);
        var stacked=GardenView.Stacked(ImGui.GetContentRegionAvail().X,frame,ImGui.GetStyle().ItemSpacing.X);
        ImGui.TextDisabled(number.ToString(Numbers));
        if(bed is null)
        {
            // An empty bed: an outlined square (the theme's border does not show in the dark theme), reached by the keyboard and the gamepad.
            ImGui.InvisibleButton($"##GardenEmpty{number}",new Vector2(side));
            TipText(localization.Format("Garden.EmptyBed",number));
            ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(),ImGui.GetItemRectMax(),ImGui.GetColorU32(Muted with{W=Muted.W*0.5f}));
            return;
        }
        ImGui.PushID(bed.Id.ToString());
        try
        {
            var state=GardenJournal.State(bed,now);
            var badge=GardenView.Badge(bed,now);
            var item=GardenSeedItem(bed);
            ImGui.BeginGroup();
            bool asIcon;
            try
            {
                var clicked=DrawItemIconOrText(item,new Vector2(side),bed.Seed,GardenColor(state),out asIcon);
                var hovered=ImGui.IsItemHovered();
                var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
                if(item is not null)ItemLinksMenu(UniqueMenuId($"Garden{bed.Id}"),item);
                if(clicked && item is not null)SelectItem(item.Id);
                if(hovered)DrawGardenBedTooltip(bed,item,state,now);
                if(asIcon)DrawGardenBadge(badge,min,max,frame);
                DrawGardenGrowth(bed,state,now,asIcon ? side:MathF.Min(side*2,ImGui.GetContentRegionAvail().X),frame);
            }
            finally{ImGui.EndGroup();}
            if(!stacked)ImGui.SameLine();
            ImGui.BeginGroup();
            try
            {
                if(GardenView.Left(bed,now) is { } left)ImGui.TextUnformatted(GardenLeftText(left));
                else ImGui.TextColored(Success,localization.Get("Garden.Ripe"));
                // Without the icon there is no mark: the word says a tending is due.
                if(!asIcon && badge==GardenBadge.Due)ImGui.TextColored(Warning,localization.Get("Garden.StateToTend"));
                DrawGardenIconButtons(bed,now,ref changed,ref removed);
            }
            finally{ImGui.EndGroup();}
            if(forecasts?.FirstOrDefault(forecast=>forecast.Bed==number) is { } forecast)DrawGardenCrossIcons(forecast,planted);
        }
        finally{ImGui.PopID();}
    }

    /// <summary>The growth bar: as wide as the icon, under it; full and in the success colour once ripe.</summary>
    private static void DrawGardenGrowth(GardenBed bed,GardenState state,DateTimeOffset now,float width,float frame)
    {
        var height=MathF.Max(3,MathF.Round(frame/6));
        ImGui.Dummy(new Vector2(width,height));
        var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
        var draw=ImGui.GetWindowDrawList();
        var border=UI.FfxivTheme.Border;
        draw.AddRectFilled(min,max,ImGui.GetColorU32(border with{W=border.W*0.6f}));
        var growth=GardenView.Growth(bed,now);
        if(growth>0)draw.AddRectFilled(min,new Vector2(min.X+MathF.Max(1,MathF.Round(width*growth)),max.Y),ImGui.GetColorU32(state==GardenState.Ripe ? Success:Accent));
    }

    /// <summary>
    /// The round mark on the icon's top right corner (the bottom right one is the collection mark's): an outline and a drop while the tending
    /// is to come, a full disc with « ! » once it is due, a full disc with a check once ripe. The glyph tells the state.
    /// </summary>
    private void DrawGardenBadge(GardenBadge badge,Vector2 min,Vector2 max,float frame)
    {
        if(badge==GardenBadge.None)return;
        // 0.7 of a frame height: at 0.55 the glyph was too small to read (capture of the desktop application).
        var diameter=MathF.Round(frame*0.7f);
        var centre=new Vector2(max.X-diameter/2,min.Y+diameter/2);
        var draw=ImGui.GetWindowDrawList();
        var background=UI.FfxivTheme.Background;
        var (fill,glyphColour,icon)=badge switch
        {
            GardenBadge.Due=>(Warning,background,FontAwesomeIcon.Exclamation),
            GardenBadge.Ripe=>(Success,background,FontAwesomeIcon.Check),
            _=>(background,Muted,FontAwesomeIcon.Tint),
        };
        draw.AddCircleFilled(centre,diameter/2,ImGui.GetColorU32(fill));
        draw.AddCircle(centre,diameter/2,ImGui.GetColorU32(badge==GardenBadge.Later ? Muted:background));
        DrawGlyphIn(icon,centre-new Vector2(diameter/2),centre+new Vector2(diameter/2),glyphColour,diameter*0.62f);
    }

    private void DrawGardenIconButtons(GardenBed bed,DateTimeOffset now,ref bool changed,ref GardenBed? removed)
    {
        // X-180 (0.1168): greyed in the desktop application (nothing is saved there).
        DesktopEditBegin();
        try
        {
            if(ToolButton(FontAwesomeIcon.HandHoldingDroplet,"Garden.TendTip",GardenJournal.TendAt(bed) is not null)){GardenJournal.Tend(bed,now);changed=true;}
            ImGui.SameLine();
            if(IconDeleteButton(FontAwesomeIcon.TrashAlt,"Garden.Remove","Garden.RemoveTip"))removed=bed;
        }
        finally{DesktopEditEnd();}
    }

    /// <summary>The tooltip of a planted seed: icon and name, place, state in a word, planting, harvest (and the time left), tending.</summary>
    private void DrawGardenBedTooltip(GardenBed bed,ItemDefinition? item,GardenState state,DateTimeOffset now)
    {
        using(TooltipScope.Begin())
        {
            if(item is not null){DrawItemIcon(item,new Vector2(32),false);ImGui.SameLine();}
            ImGui.TextColored(GardenColor(state),item?.Name ?? bed.Seed);
            ImGui.TextDisabled(bed.Place);
            ImGui.TextColored(GardenColor(state),localization.Get(state switch{GardenState.Ripe=>"Garden.Ripe",GardenState.ToTend=>"Garden.StateToTend",_=>"Garden.StateGrowing"}));
            ImGui.TextUnformatted(localization.Format("Garden.TipPlanted",bed.PlantedAt.ToLocalTime().ToString("g")));
            var ripe=GardenJournal.RipeAt(bed).ToLocalTime().ToString("g");
            ImGui.TextUnformatted(GardenView.Left(bed,now) is { } left ? localization.Format("Garden.TipRipeIn",ripe,GardenLeftText(left)):localization.Format("Garden.RipeAt",ripe));
            if(GardenJournal.TendAt(bed) is { } due)
            {
                var when=due.ToLocalTime().ToString("g");
                if(state==GardenState.ToTend)ImGui.TextColored(Warning,localization.Format("Garden.TipTendNow",when));
                else ImGui.TextUnformatted(localization.Format("Garden.TipTendAt",when));
            }
            if(item is not null)ImGui.TextDisabled(localization.Get("Garden.TipClick"));
        }
    }

    /// <summary>The most crossbreed icons shown under a bed; the others are counted (« +N », named in its tooltip).</summary>
    private const int GardenCrossIconsShown=4;

    /// <summary>
    /// X-178 (0.1259): what a planted bed may give, under it: one small icon per possible seed (a star: on the wishlist; a disc: not held in
    /// the bags and storages read), each with its tooltip and menu; a crossed circle when no neighbour was compatible, a question mark when
    /// a seed is not in the pairs. A forecast, never a result read from the game.
    /// </summary>
    private void DrawGardenCrossIcons(GardenForecast forecast,IReadOnlyList<(int Number,GardenBed Bed)> planted)
    {
        var side=ImGui.GetFrameHeight();
        string Name(uint id)=>gardenForecastIndex?.Names.TryGetValue(id,out var name)==true && name.Length>0 ? name:"#"+id;
        string BedSeed(int? number)=>planted.FirstOrDefault(entry=>entry.Number==number).Bed?.Seed ?? "?";
        switch(forecast.Kind)
        {
            case GardenForecastKind.Crosses:
                RefreshRecipeHoldingsCache();
                var shown=Math.Min(forecast.Outcomes.Count,GardenCrossIconsShown);
                for(var index=0;index<shown;index++)
                {
                    var seed=forecast.Outcomes[index];
                    if(index>0)ImGui.SameLine();
                    ImGui.PushID((int)seed);
                    try
                    {
                        var item=itemCatalog.State.Catalog?.Find(seed);
                        var holdings=GetRecipeHoldings(seed);
                        var wished=configuration.Wishlist?.Contains(seed)==true;
                        var clicked=DrawItemIconOrText(item,new Vector2(side),Name(seed),Text,out var asIcon);
                        var hovered=ImGui.IsItemHovered();
                        var min=ImGui.GetItemRectMin();var max=ImGui.GetItemRectMax();
                        if(item is not null)ItemLinksMenu(UniqueMenuId($"GardenCross{seed}"),item);
                        if(clicked)SelectItem(seed);
                        if(hovered)DrawGardenCrossTooltip(item,Name(seed),forecast.Partner ?? 0,BedSeed(forecast.Partner),wished,holdings.HasObservation,holdings.Total);
                        if(asIcon)DrawGardenCrossMark(GardenView.Mark(wished,holdings.HasObservation,holdings.Total),min,max);
                    }
                    finally{ImGui.PopID();}
                }
                if(forecast.Outcomes.Count>shown)
                {
                    ImGui.SameLine();
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextDisabled("+"+(forecast.Outcomes.Count-shown).ToString(Numbers));
                    TipText(string.Join(", ",forecast.Outcomes.Skip(shown).Select(Name)));
                }
                break;
            case GardenForecastKind.NoPartner:DrawGardenCrossNote(FontAwesomeIcon.Ban,localization.Get("Garden.ForecastNone"));break;
            case GardenForecastKind.UnknownSeed:DrawGardenCrossNote(FontAwesomeIcon.Question,localization.Get("Garden.ForecastUnknownSeed"));break;
            case GardenForecastKind.UnknownNeighbour:
                DrawGardenCrossNote(FontAwesomeIcon.Question,localization.Format("Garden.ForecastUnknownNeighbour",forecast.Partner ?? 0,BedSeed(forecast.Partner)));
                break;
        }
    }

    /// <summary>« No crossbreed » or « unknown »: a muted glyph the keyboard and the gamepad reach, the sentence in its tooltip.</summary>
    private void DrawGardenCrossNote(FontAwesomeIcon icon,string text)
    {
        ImGui.InvisibleButton("##GardenCrossNote",new Vector2(ImGui.GetFrameHeight()));
        TipText(text);
        DrawGlyphIn(icon,ImGui.GetItemRectMin(),ImGui.GetItemRectMax(),Muted,ImGui.GetFontSize()*0.85f);
    }

    /// <summary>The marks of a possible crossbreed: a star on the top right (wished), a disc on the top left (not held in what was read).</summary>
    private void DrawGardenCrossMark(GardenCrossMark mark,Vector2 min,Vector2 max)
    {
        var side=max.X-min.X;
        var draw=ImGui.GetWindowDrawList();
        var background=ImGui.GetColorU32(UI.FfxivTheme.Background);
        if(mark is GardenCrossMark.Wished or GardenCrossMark.WishedNotOwned)
        {
            var radius=MathF.Round(side*0.3f);
            var centre=new Vector2(max.X-radius,min.Y+radius);
            draw.AddCircleFilled(centre,radius,background);
            DrawGlyphIn(FontAwesomeIcon.Star,centre-new Vector2(radius),centre+new Vector2(radius),Accent,radius*1.5f);
        }
        if(mark is GardenCrossMark.NotOwned or GardenCrossMark.WishedNotOwned)
        {
            var radius=MathF.Max(3,MathF.Round(side*0.2f));
            var centre=new Vector2(min.X+radius,min.Y+radius);
            draw.AddCircleFilled(centre,radius,ImGui.GetColorU32(Warning));
            draw.AddCircle(centre,radius,background);
        }
    }

    private void DrawGardenCrossTooltip(ItemDefinition? item,string name,int partner,string partnerSeed,bool wished,bool observed,long total)
    {
        using(TooltipScope.Begin())
        {
            if(item is not null){DrawItemIcon(item,new Vector2(32),false);ImGui.SameLine();}
            ImGui.TextUnformatted(item?.Name ?? name);
            ImGui.TextUnformatted(localization.Format("Garden.CrossWith",partner,partnerSeed));
            if(wished)ImGui.TextUnformatted(localization.Get("Garden.CrossWished"));
            // Held or not only in what was read (bags and storages); nothing read: said, never « not owned » (R06).
            if(!observed)ImGui.TextDisabled(localization.Get("Garden.CrossUnknown"));
            else if(total>0)ImGui.TextUnformatted(localization.Format("Garden.CrossOwned",total.ToString("N0",Numbers)));
            else ImGui.TextColored(Warning,localization.Get("Garden.CrossNotOwned"));
            ImGui.TextDisabled(localization.Get("Garden.CrossForecast"));
        }
    }
}
