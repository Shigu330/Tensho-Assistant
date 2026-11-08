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
    // X-75.4 (0.878): the monsters seen in game casting the spell or teaching it (yours or shared), with their levels and places, before the wiki's list.
    private void DrawBlueSpellSeen(uint spellId)
    {
        if(DiscoveryStore is not { } store || !DiscoveriesLoaded)return;
        var seen=store.Find(Discoveries.DiscoveryKind.BlueSpellCaster,subject=>subject==spellId,64).Concat(store.Find(Discoveries.DiscoveryKind.BlueSpellLearned,subject=>subject==spellId,16))
            .Where(fact=>fact.Detail!=0).GroupBy(fact=>fact.Detail).Take(8).ToArray();
        if(seen.Length==0)return;
        ImGui.TextColored(Accent,localization.Get("BlueSpellSources.Seen"));
        foreach(var monster in seen)
        {
            var levels=monster.Where(fact=>fact.Kind==Discoveries.DiscoveryKind.BlueSpellCaster).ToArray();
            var level=levels.Length==0 ? "?":levels.Min(fact=>fact.LevelMin)==levels.Max(fact=>fact.LevelMax) ? levels[0].LevelMin.ToString():$"{levels.Min(fact=>fact.LevelMin)}–{levels.Max(fact=>fact.LevelMax)}";
            ImGui.BulletText(localization.Format("BlueSpellSources.SeenLine",DiscoveryMonsterName(monster.Key),level));
            ImGui.Indent();
            try{DrawSeenPlaces("BlueSeen"+monster.Key,subject=>subject==monster.Key,DiscoveryMonsterName(monster.Key));}
            finally{ImGui.Unindent();}
        }
    }

    // 0.798 (X-38, user request): the monsters of every spell, for the nearby monsters (map now, icons above their heads next); read on a click only.
    private void DrawBlueSpellNearby()
    {
        if(BlueSpellNearby is not { } index)return;
        var spells=collections.State.Entries.Where(entry=>entry.Kind==TenshoAssistant.Collections.CollectionKind.BlueMagic).Select(entry=>entry.Id).ToArray();
        index.LoadCached(spells);
        BoolSetting("MapView.ShowBlueMageMonsters","BlueSpellSources.NearbyTip",()=>configuration.MapView.ShowBlueMageMonsters,value=>configuration.MapView.ShowBlueMageMonsters=value,c=>c.MapView.ShowBlueMageMonsters);
        if(!configuration.MapView.ShowBlueMageMonsters)return;
        ImGui.Indent();
        try
        {
            BoolSetting("MapView.AboveBlueSpell","BlueSpellSources.AboveTip",()=>configuration.MapView.AboveBlueSpell,value=>configuration.MapView.AboveBlueSpell=value,c=>c.MapView.AboveBlueSpell);
            BoolSetting("MapView.BlueMageMonstersAlways","BlueSpellSources.AlwaysTip",()=>configuration.MapView.BlueMageMonstersAlways,value=>configuration.MapView.BlueMageMonstersAlways=value,c=>c.MapView.BlueMageMonstersAlways);
            var missing=spells.Length-index.Known;
            if(index.Running)
            {
                var (done,asked,failed)=index.Progress;
                ImGui.TextDisabled(localization.Format("BlueSpellSources.NearbyReading",done,asked,failed));
            }
            else
            {
                ImGui.TextDisabled(localization.Format("BlueSpellSources.NearbyKnown",index.Known,spells.Length));
                if(missing>0)
                {
                    ImGui.SameLine();
                    if(SourceButton(localization.Format("BlueSpellSources.NearbyFetch",missing),configuration.CommunityWiki,true))index.FetchMissing(spells);
                    Tip("BlueSpellSources.NearbyFetchTip");
                }
                if(index.Error=="disabled")ImGui.TextDisabled(localization.Get("Advanced.SourceOffTip"));
            }
        }
        finally{ImGui.Unindent();}
    }

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
    /// <summary>0.777 (X-22, user report): every place row with this English name; the game has several for some (« Mor Dhona »: 26, 67, 509).</summary>
    internal Func<string,IReadOnlyList<uint>>? EnglishPlaceIds { get; set; }
    private readonly HashSet<string> wikiPlacesNotFound=new(StringComparer.OrdinalIgnoreCase);
    /// <summary>The map of a wiki place: the first of its place rows that has one; null, and noted once in developer mode, when none has.</summary>
    private Maps.MapDefinition? MapOfEnglishPlace(string place)
    {
        foreach(var id in EnglishPlaceIds?.Invoke(place) ?? [])
            if(maps.State.Catalog?.ForPlace(id).FirstOrDefault() is { } map)return map;
        if(developerMode && maps.State.Catalog is not null && wikiPlacesNotFound.Add(place))log.Information("Wiki place without a map: \"{0}\".",place);
        return null;
    }
    // 0.390 (user request « sort by the monster's level »): 0 = wiki order, 1 = lowest level first, 2 = highest first (session only).
    private int blueMonsterOrder=1;
    private static readonly string[] BlueMonsterOrders=["OrderWiki","OrderLevelUp","OrderLevelDown"];

    private void DrawBlueSpellMonsters(uint spellId)
    {
        DrawBlueSpellSeen(spellId);
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
            if(place.X is not { } x || place.Y is not { } y || MapOfEnglishPlace(place.Place) is not { } map)continue;
            PlaceMenu($"BluePlace{index}",map,new(x,y),CommunityMonster(row.Name));
            Tip("EntityMenu.RightClickPlace");
            ImGui.SameLine();
            if(ImGui.SmallButton(localization.Get("BlueSpellSources.Flag")+$"##BlueFlag{index}"))PlaceFlagFromMapView(map,x,y);
        }
        if(document.Monsters.Count>40)ImGui.TextDisabled(localization.Format("Events.More",document.Monsters.Count-40));
        ImGui.TextDisabled(document.Url);
    }
}
