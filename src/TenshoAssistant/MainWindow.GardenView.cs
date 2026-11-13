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
            if(forecasts?.FirstOrDefault(forecast=>forecast.Bed==number) is { } forecast)DrawGardenForecast(forecast,planted);
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
}
