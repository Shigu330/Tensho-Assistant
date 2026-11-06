using Dalamud.Bindings.ImGui;
using TenshoAssistant.Monsters;

namespace TenshoAssistant;

// g, C-13 (0.367): in the blue magic spell window, the monsters that teach it, from the wiki's spell page (on click only, facts only:
// name, level, place); a place with coordinates the game knows gets a « Flag » button (Tensho map flag).
internal sealed partial class MainWindow
{
    internal WikiMonsterSources? BlueSpellWiki { get; set; }
    // 0.395 (user request): the wiki's spellbook table (every spell, one page) read when the Blue magic page opens, to sort the whole list.
    internal BlueSpellWikiIndex? BlueSpellIndex { get; set; }
    private int blueSpellOrder;
    private int? BlueSpellLevel(int number)=>BlueSpellIndex?.Get(number)?.MonsterLevel;

    // Above the spell list: the order (number, or the lowest teaching monster's level) and how far the wiki reading is.
    private void DrawBlueSpellOrder()
    {
        if(BlueSpellIndex is not { } index)return;
        index.Ensure();
        ImGui.SetNextItemWidth(260);
        if(ImGui.BeginCombo("##BlueSpellOrder",localization.Get("BlueSpellSources.SpellOrder"+blueSpellOrder)))
        {
            try
            {
                for(var order=0;order<3;order++)
                    if(ImGui.Selectable(localization.Get("BlueSpellSources.SpellOrder"+order),order==blueSpellOrder))blueSpellOrder=order;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("BlueSpellSources.SpellOrderTip");
        ImGui.SameLine();
        if(index.Book is { } book)ImGui.TextDisabled(localization.Format("BlueSpellSources.IndexDone",book.Rows.Count,book.FetchedAt.ToLocalTime().ToString("g")));
        else if(index.Running)ImGui.TextDisabled(localization.Get("BlueSpellSources.IndexReading"));
        else if(!configuration.CommunityWiki || index.Error=="disabled")ImGui.TextDisabled(localization.Get("BlueSpellSources.IndexOff"));
        else if(index.Error is not null)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("BlueSpellSources.IndexFailed"));
    }
    internal Func<string,uint>? EnglishPlaceId { get; set; }
    // 0.390 (user request « sort by the monster's level »): 0 = wiki order, 1 = lowest level first, 2 = highest first (session only).
    private int blueMonsterOrder=1;
    private static readonly string[] BlueMonsterOrders=["OrderWiki","OrderLevelUp","OrderLevelDown"];

    private void DrawBlueSpellMonsters(uint spellId)
    {
        if(BlueSpellWiki is not { } wiki)return;
        wiki.Select(spellId);
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Get("BlueSpellSources.Title"));
        var view=wiki.State;
        ImGui.BeginDisabled(view.Loading);
        try
        {
            if(SourceButton(localization.Get("BlueSpellSources.Fetch"),configuration.CommunityWiki,true))_=wiki.RequestAsync();
        }
        finally{ImGui.EndDisabled();}
        if(view.Loading)ImGui.TextDisabled(localization.Get("WikiMonsters.Loading"));
        if(view.Error is { } error){ImGui.TextWrapped(localization.Get(SourceOff(error) ? "Advanced.SourceOffTip":"WikiMonsters.Error"));if(developerMode)ImGui.TextWrapped(RawMessage(error));}
        if(view.Document is not { } document){ImGui.TextDisabled(localization.Get("BlueSpellSources.Help"));return;}
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Sources,document.FetchedAt);
        // 0.387: a page the wiki does not have (revision 0) is said as such, with the title asked, instead of « no monster ».
        if(document.Revision==0){ImGui.TextWrapped(localization.Format("BlueSpellSources.Missing",document.Title));ImGui.TextDisabled(document.Url);return;}
        if(document.Monsters.Count==0){ImGui.TextWrapped(localization.Get("BlueSpellSources.Empty"));ImGui.TextDisabled(document.Url);return;}
        ImGui.SetNextItemWidth(200);
        if(ImGui.BeginCombo("##BlueMonsterOrder",localization.Get("BlueSpellSources."+BlueMonsterOrders[Math.Clamp(blueMonsterOrder,0,2)])))
        {
            try
            {
                for(var order=0;order<BlueMonsterOrders.Length;order++)
                    if(ImGui.Selectable(localization.Get("BlueSpellSources."+BlueMonsterOrders[order]),order==blueMonsterOrder))blueMonsterOrder=order;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("BlueSpellSources.OrderTip");
        var index=0;
        foreach(var row in BlueSpellSources.SortByLevel(document.Monsters,blueMonsterOrder).Take(40))
        {
            index++;
            ImGui.TextWrapped($"{CommunityMonster(row.Name)} · {row.Level} · {CommunityPlace(row.Location)}");
            var place=BlueSpellSources.Parse(row.Location);
            if(place.X is not { } x || place.Y is not { } y || EnglishPlaceId?.Invoke(place.Place) is not (> 0 and var placeId)
               || maps.State.Catalog?.ForPlace(placeId).FirstOrDefault() is not { } map)continue;
            PlaceMenu($"BluePlace{index}",map,new(x,y),CommunityMonster(row.Name));
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("EntityMenu.RightClickPlace"));
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("BlueSpellSources.Flag")+$"##BlueFlag{index}"))PlaceFlagFromMapView(map,x,y);
        }
        if(document.Monsters.Count>40)ImGui.TextDisabled(localization.Format("Events.More",document.Monsters.Count-40));
        ImGui.TextDisabled(document.Url);
    }
}
