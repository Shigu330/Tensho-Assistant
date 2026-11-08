using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.Collections;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// Collections page, also drawn as the Blue magic page: mounts, minions, emotes, orchestrion rolls (with categories), titles and achievements
// (categories and detail popup: MainWindow.Completion.cs), blue magic spells; obtained / not obtained / unknown filter with counts, the
// ✓ / ✗ badge (0.275), how-to-obtain tooltips and the right-click menu (an entry unlocked by one item opens that item's menu, 0.274).
// Lists come from the game sheets and unlock states from Collections/CollectionReader (framework thread), cached for the Collections
// duration; titles and achievements are known only once their game window was opened this session.
internal sealed partial class MainWindow
{
    private CollectionState? collectionViewState;
    private CollectionKind collectionViewKind;
    private string collectionViewQuery = "";
    private int collectionViewFilter = -1;
    private CollectionEntry[] collectionViewEntries = [];
    private CollectionEntry[] collectionViewMatches = [];
    // X-76 (0.956): the summary's counts, worked out with collectionViewEntries instead of on every frame.
    private int collectionViewAcquired, collectionViewNotAcquired, collectionViewUnknown;
    private uint? collectionViewMusicCategory;
    private uint? collectionViewAchievementCategory;
    // 0.365 (g, C-04): the kinds read by ExtraCollectionReader, in the user's order (GAPS.md C-04).
    internal Func<IReadOnlyList<CollectionEntry>?>? ExtraCollections { get; set; }
    internal Func<bool>? ExtraCollectionsUnavailable { get; set; }
    private static readonly CollectionKind[] ExtraKinds=[CollectionKind.Hairstyle,CollectionKind.Ornament,CollectionKind.Glasses,CollectionKind.Barding,
        CollectionKind.Portrait,CollectionKind.MasterBook,CollectionKind.Folklore,CollectionKind.McGuffin,CollectionKind.OccultLore];
    private object? collectionViewExtra;
    // 0.602 (T28): compared once per reading, and saved only when something changed.
    private CollectionState? collectionRecentState;
    private void DrawCollectionRecent(CollectionState state)
    {
        if(!ReferenceEquals(collectionRecentState,state))
        {
            collectionRecentState=state;
            if(state.CharacterId!=0 && !state.Loading && state.Error is null && !state.FromCache && state.Entries.Count>0)
            {
                var memories=configuration.CollectionMemories??=[];
                if(!memories.TryGetValue(state.CharacterId,out var read))memories[state.CharacterId]=read=new();
                if(CollectionRecent.Update(read,state.Entries,DateTimeOffset.UtcNow))
                    TrySave("Recently obtained");
            }
        }
        if(configuration.CollectionMemories?.GetValueOrDefault(state.CharacterId) is not {Recent.Count:>0} memory)return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("CollectionsBrowser.Recent",memory.Recent.Count)+"###CollectionRecent"))return;
        foreach(var gain in memory.Recent)ImGui.TextUnformatted(localization.Format("CollectionsBrowser.RecentLine",gain.At.ToLocalTime().ToString("d"),gain.Name));
    }
    private void DrawCollections(bool blueMagic = false)
    {
        Heading(localization.Get(blueMagic ? "Category.BlueMagic":"CollectionsBrowser.Title"),localization.Get(blueMagic ? "CollectionsBrowser.BlueDescription":"CollectionsBrowser.Description"));
        var state = collections.State;
        if (ImGui.Button(localization.Get("CollectionsBrowser.Refresh"))) collections.RequestRefresh();
        ImGui.SameLine();
        ImGui.TextDisabled(state.Loading ? localization.Get("CollectionsBrowser.Loading") : state.ObservedAt is { } date ? localization.Format("CollectionsBrowser.Observed",date.ToLocalTime().ToString("g")) : localization.Get("CollectionsBrowser.Offline"));
        if (state.Error is { } error) DrawDataError(error,"CollectionsBrowser.Error");
        DrawCacheStamp(TenshoAssistant.Caching.CacheArea.Collections,state.ObservedAt,state.FromCache);
        if(!blueMagic)DrawCollectionRecent(state);
        // 0.360 (g, C-09): the Wishlist tab, set again by its tab on every frame it is shown.
        collectionWishlistTab=false;collectionOutfitTab=false;
        if (!blueMagic && ImGui.BeginTabBar("CollectionKinds"))
        {
            try
            {
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Mounts")+"###CollectionMounts",focusCollectionKind==CollectionKind.Mount ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { collectionKind = CollectionKind.Mount; if(focusCollectionKind==CollectionKind.Mount)focusCollectionKind=null; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Companions")+"###CollectionMinions",focusCollectionKind==CollectionKind.Companion ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { collectionKind = CollectionKind.Companion; if(focusCollectionKind==CollectionKind.Companion)focusCollectionKind=null; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Emotes")+"###CollectionEmotes",focusCollectionKind==CollectionKind.Emote ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { collectionKind = CollectionKind.Emote; if(focusCollectionKind==CollectionKind.Emote)focusCollectionKind=null; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Orchestrion")+"###CollectionMusic",focusOrchestrionTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { focusOrchestrionTab=false;collectionKind = CollectionKind.Orchestrion; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Titles")+"###CollectionTitles",focusTitleTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { focusTitleTab=false;collectionKind=CollectionKind.Title;ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("CollectionsBrowser.Achievements")+"###CollectionAchievements",focusAchievementTab ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None)) { focusAchievementTab=false;collectionKind=CollectionKind.Achievement;ImGui.EndTabItem(); }
                foreach(var extraKind in ExtraKinds)
                    if (ImGui.BeginTabItem(localization.Get("ExtraCollections."+extraKind)+"###Collection"+extraKind)) { collectionKind=extraKind; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("Outfits.Title")+"###CollectionOutfits")) { collectionOutfitTab=true; ImGui.EndTabItem(); }
                if (ImGui.BeginTabItem(localization.Get("Wishlist.Title")+"###CollectionWishlist")) { collectionWishlistTab=true; ImGui.EndTabItem(); }
            }
            finally { ImGui.EndTabBar(); }
        }
        if(collectionWishlistTab){DrawWishlist();return;}
        if(collectionOutfitTab){DrawOutfits();return;}
        var kind = blueMagic ? CollectionKind.BlueMagic : collectionKind;
        var extra = ExtraKinds.Contains(kind) ? ExtraCollections?.Invoke() : null;
        if (ExtraKinds.Contains(kind) && extra is null)
            ImGui.TextDisabled(localization.Get(ExtraCollectionsUnavailable?.Invoke()==true ? "ExtraCollections.Unavailable":"ExtraCollections.Loading"));
        if(kind==CollectionKind.Orchestrion){DrawOrchestrionPlayer(state);DrawOrchestrionCategories(state);}
        if(kind==CollectionKind.Achievement){DrawTrackedAchievements(state);DrawAchievementCategories(state);}
        if(kind==CollectionKind.Title)DrawTitleTools(state);
        foreach(var issue in state.CategoryIssues ?? [])if(issue.Kind==kind)ImGui.TextWrapped(issue.Reason);
        ImGui.SetNextItemWidth(-1);
        // 0.144: titles and achievements are only known once the game sent their list (its window opened once this session).
        if(kind==CollectionKind.Title && collections.TitleListLoaded==false)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("CollectionsBrowser.TitlesNotLoaded"));
        if(kind==CollectionKind.Achievement && collections.AchievementListLoaded==false)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get("CollectionsBrowser.AchievementsNotLoaded"));
        ImGui.InputTextWithHint("##CollectionSearch", localization.Get("CollectionsBrowser.Search"), ref collectionQuery, 128);
        Tip("CollectionsBrowser.SearchTip");
        var completion=kind is CollectionKind.Title or CollectionKind.Achievement;
        if(!completion && collectionFilter==3)collectionFilter=0;
        var filters = completion ? new[]{"All",kind==CollectionKind.Achievement ? "Completed":"Acquired","Unknown",kind==CollectionKind.Achievement ? "NotCompleted":"NotAcquired"}:new[] { "All", "Acquired", "Unconfirmed" };
        for (var index = 0; index < filters.Length; index++)
        {
            if (index > 0) ImGui.SameLine();
            if (ImGui.RadioButton(localization.Get("CollectionsBrowser."+filters[index])+"###CollectionFilter"+index, collectionFilter == index)) collectionFilter = index;
            // Literal keys, so the tooltip key test sees them.
            switch (index)
            {
                case 0: Tip("CollectionsBrowser.AllTip"); break;
                case 1: Tip("CollectionsBrowser.AcquiredTip"); break;
                case 2: if (completion) Tip("CollectionsBrowser.UnknownTip"); else Tip("CollectionsBrowser.UnconfirmedTip"); break;
                case 3: Tip("CollectionsBrowser.NotAcquiredTip"); break;
            }
        }
        DrawCollectionSourceFilter(kind);
        // 0.800 (X-41): nearby monsters that drop what is missing here, set from here too.
        if(kind is CollectionKind.Mount or CollectionKind.Companion or CollectionKind.Orchestrion)
            BoolSetting("MapView.ShowCollectionMonsters","CollectionsBrowser.NearbyTip",()=>configuration.MapView.ShowCollectionMonsters,value=>configuration.MapView.ShowCollectionMonsters=value,c=>c.MapView.ShowCollectionMonsters);
        // 0.395 (user request): the blue magic list sorted by the lowest level of the monsters that teach each spell (wiki, read on opening).
        if(blueMagic){DrawBlueSpellOrder();DrawBlueSpellNearby();}
        var blueOrder=blueMagic ? blueSpellOrder:0;var blueRevision=blueMagic && blueSpellOrder!=0 ? BlueSpellIndex?.Revision ?? 0:0;
        var sourceFilter=HasSourceFilter(kind) ? collectionSourceFilter:-1;
        var musicCategory=kind==CollectionKind.Orchestrion ? orchestrionCategoryFilter:null;
        var achievementCategory=kind==CollectionKind.Achievement ? achievementCategoryFilter:null;
        if (!ReferenceEquals(collectionViewState, state) || !ReferenceEquals(collectionViewExtra, extra) || collectionViewKind != kind
            || collectionViewFilter != collectionFilter || collectionViewQuery != collectionQuery || collectionViewMusicCategory!=musicCategory || collectionViewAchievementCategory!=achievementCategory
            || collectionViewSourceFilter!=sourceFilter || collectionViewSourceRevision!=collectionSourceRevision
            || collectionViewBlueOrder!=blueOrder || collectionViewBlueRevision!=blueRevision)
        {
            collectionViewBlueOrder=blueOrder;collectionViewBlueRevision=blueRevision;
            collectionViewSourceFilter=sourceFilter;collectionViewSourceRevision=collectionSourceRevision;
            collectionViewState = state; collectionViewKind = kind; collectionViewExtra = extra;
            collectionViewFilter = collectionFilter; collectionViewQuery = collectionQuery;
            collectionViewMusicCategory=musicCategory;
            collectionViewAchievementCategory=achievementCategory;
            collectionViewEntries = (ExtraKinds.Contains(kind) ? extra ?? [] : state.Entries).Where(entry => entry.Kind == kind)
                .Where(entry=>musicCategory is null || entry.Music?.CategoryId==musicCategory)
                .Where(entry=>achievementCategory is null || entry.Achievement?.CategoryId==achievementCategory)
                .OrderBy(entry=>kind==CollectionKind.Orchestrion ? entry.Music?.CategoryOrder ?? int.MaxValue:kind==CollectionKind.Achievement ? entry.Achievement?.KindOrder ?? int.MaxValue:0)
                .ThenBy(entry=>kind==CollectionKind.Achievement ? entry.Achievement?.CategoryOrder ?? int.MaxValue:0)
                .ThenBy(entry => blueMagic || kind is CollectionKind.Ornament or CollectionKind.McGuffin or CollectionKind.Folklore ? entry.Number:kind==CollectionKind.Orchestrion ? entry.Music?.Order ?? int.MaxValue:kind==CollectionKind.Title ? entry.Title?.Order ?? int.MaxValue:kind==CollectionKind.Achievement ? entry.Achievement?.Order ?? int.MaxValue:0)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            if(blueOrder!=0)collectionViewEntries=Monsters.BlueSpellSources.SortByMonsterLevel(collectionViewEntries,entry=>BlueSpellLevel(entry.Number),blueOrder);
            collectionViewAcquired=collectionViewEntries.Count(entry=>entry.ConfirmedUnlocked);
            collectionViewNotAcquired=collectionViewEntries.Count(entry=>entry.Acquisition==CollectionAcquisition.NotAcquired);
            collectionViewUnknown=collectionViewEntries.Count(entry=>entry.Acquisition==CollectionAcquisition.Unknown);
            // 0.331: « Unconfirmed » (non-completion kinds) = everything the game does not confirm as obtained, by the same rule as the
            // ✓ / ✗ badge (CollectionStateOf), so a mount or minion read as not unlocked stays listed there.
            bool StateMatches(CollectionEntry entry)=>collectionFilter switch
            {
                0=>true,
                1=>entry.Acquisition==CollectionAcquisition.Acquired,
                2=>completion ? entry.Acquisition==CollectionAcquisition.Unknown:CollectionStateOf(entry)!=CollectionAcquisition.Acquired,
                _=>entry.Acquisition==CollectionAcquisition.NotAcquired,
            };
            collectionViewMatches = collectionViewEntries.Where(entry => StateMatches(entry) && CollectionSourceMatches(entry,sourceFilter)
                && (string.IsNullOrWhiteSpace(collectionQuery) || entry.Name.Contains(collectionQuery.Trim(), StringComparison.OrdinalIgnoreCase))).ToArray();
        }
        var entries = collectionViewEntries;
        ImGui.TextColored(Accent,localization.Format(kind==CollectionKind.Achievement ? "CollectionSummary.Completed":"CollectionSummary.Acquired",collectionViewAcquired,entries.Length));
        if(completion)ImGui.TextDisabled(localization.Format(kind==CollectionKind.Achievement ? "CollectionSummary.Incomplete":"CollectionSummary.Missing",collectionViewNotAcquired,collectionViewUnknown));
        ImGui.TextWrapped(localization.Get("CollectionSummary.Help"));
        if (state.CharacterId == 0) ImGui.TextDisabled(localization.Get("CollectionSummary.Login"));
        var openDetails = false;
        var openMusicDetails=kind==CollectionKind.Orchestrion && openOrchestrionDetailsRequested;
        if(openMusicDetails)openOrchestrionDetailsRequested=false;
        var openCompletionDetails=completion && completionDetailsRequested;
        if(openCompletionDetails)completionDetailsRequested=false;
        var visibleEntries = collectionViewMatches;
        if (!ImGui.BeginTable("CollectionEntries", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp)) return;
        try
        {
            ImGui.TableSetupColumn(localization.Get("CollectionsBrowser.Collection"), ImGuiTableColumnFlags.WidthStretch, 3);
            ImGui.TableSetupColumn(localization.Get("CollectionsBrowser.State"), ImGuiTableColumnFlags.WidthStretch, 1);
            ImGui.TableHeadersRow();
            var clipper = ImGui.ImGuiListClipper();
            try
            {
            clipper.Begin(visibleEntries.Length);
            while (clipper.Step())
            for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++)
            {
                var entry = visibleEntries[index];
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                var musicItem=kind==CollectionKind.Orchestrion ? itemCatalog.State.Catalog?.ForOrchestrion(entry.Id).FirstOrDefault():null;
                if(kind==CollectionKind.Title)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    try{if(ImGui.Button(FontAwesomeIcon.Crown.ToIconString()+$"##TitleIcon{entry.Id}",new Vector2(32))){selectedCompletionKind=kind;selectedCompletionId=entry.Id;openCompletionDetails=true;}}
                    finally{ImGui.PopFont();}
                }
                else if(musicItem is not null)DrawItemIcon(musicItem,new Vector2(32));
                else DrawGameIcon(entry.IconId,new Vector2(32));
                // An item icon (orchestrion roll) carries its own badge; the other entries get it here.
                if(musicItem is null)DrawEntryMark(entry);
                if(kind==CollectionKind.TripleTriad)TriadCardTooltip(entry.Id);
                if(kind==CollectionKind.Orchestrion && ImGui.IsItemClicked()){selectedOrchestrion=entry.Id;openMusicDetails=true;}
                if(kind==CollectionKind.Achievement && ImGui.IsItemClicked()){selectedCompletionKind=kind;selectedCompletionId=entry.Id;openCompletionDetails=true;}
                ImGui.SameLine();
                if (blueMagic)
                {
                    var teachingLevel=BlueSpellLevel(entry.Number);
                    if (ImGui.Selectable($"{entry.Card?.Label ?? entry.Number.ToString()} · {entry.Name}"+(teachingLevel is { } spellLevel ? " · "+localization.Format("BlueSpellSources.LevelShort",spellLevel):"")+$"##{entry.Id}"))
                    { selectedBlueSpell = entry.Id; openDetails = true; }
                }
                else if(kind==CollectionKind.Orchestrion)
                {
                    if(ImGui.Selectable($"{entry.Name}##Music{entry.Id}")){selectedOrchestrion=entry.Id;openMusicDetails=true;}
                }
                else if(completion)
                {
                    if(ImGui.Selectable($"{entry.Name}##Completion{kind}{entry.Id}")){selectedCompletionKind=kind;selectedCompletionId=entry.Id;openCompletionDetails=true;}
                }
                else ImGui.TextUnformatted(entry.Kind==CollectionKind.Portrait && entry.Description.Length>0
                    ? $"{entry.Name} · {localization.Get("ExtraCollections.Portrait"+entry.Description)}":entry.Name);
                // 0.142: how to obtain it (items, vendors, quests, recipes) for mounts, minions, emotes, cards and rolls.
                if (ImGui.IsItemHovered())
                {
                    if(entry.Kind is CollectionKind.Mount or CollectionKind.Companion or CollectionKind.Emote or CollectionKind.TripleTriad or CollectionKind.Orchestrion)DrawCollectionTooltip(entry);
                    // 0.387 (user report): the 0.365 kinds — the teaching item's full tooltip (how to obtain it) when the game data gives one,
                    // else the collection tooltip saying how to obtain it is not known.
                    else if(entry.ItemId!=0)DrawItemInfoTooltip(entry.ItemId);
                    else if(ExtraKinds.Contains(entry.Kind))DrawCollectionTooltip(entry);
                    else WrappedTooltip(developerMode ? $"{entry.Name}\n{entry.Kind} : {entry.Id}":entry.Name);
                }
                // 0.178: right click = links to the sources (NPC vendor, achievement, quest, recipe, gathering).
                if(entry.Kind is CollectionKind.Mount or CollectionKind.Companion or CollectionKind.Emote or CollectionKind.TripleTriad or CollectionKind.Orchestrion || entry.ItemId!=0)
                    CollectionSourcesMenu($"CollectionSources{entry.Kind}{entry.Id}",entry);
                ImGui.TableNextColumn();
                // 0.331: the State column says what the ✓ / ✗ badge shows: a mount, minion, card or roll read as not unlocked (CollectionStateOf)
                // reads « not acquired at observation », not « unconfirmed »; every other row keeps its label.
                ImGui.TextColored(entry.ConfirmedUnlocked ? Success : Muted,entry.Acquisition==CollectionAcquisition.Unknown && CollectionStateOf(entry)==CollectionAcquisition.NotAcquired
                    ? localization.Get("CollectionState.NotAcquired"):CollectionAcquisitionLabel(entry));
            }
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndTable(); }
        if(openMusicDetails)ImGui.OpenPopup(localization.Get("OrchestrionDetails.Title")+"###OrchestrionDetails");
        if(kind==CollectionKind.Orchestrion)DrawOrchestrionDetails(state);
        if(openCompletionDetails)ImGui.OpenPopup(localization.Get("CompletionDetails.Title")+"###CompletionDetails");
        DrawCompletionDetails(state);
        var bluePopup=localization.Get("BlueSpell.Title")+"###BlueSpellDetails";
        if (openDetails) ImGui.OpenPopup(bluePopup);
        ImGui.SetNextWindowSize(new Vector2(520, 0), ImGuiCond.Appearing);
        var popupOpen = true;
        if (ImGui.BeginPopupModal(bluePopup, ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            try
            {
                var spell = state.Entries.FirstOrDefault(entry => entry.Kind == CollectionKind.BlueMagic && entry.Id == selectedBlueSpell);
                if (spell is not null)
                {
                    DrawGameIcon(spell.IconId,new Vector2(40));
                    ImGui.SameLine(); ImGui.TextColored(Accent, $"{spell.Number} · {spell.Name}");
                    ImGui.TextUnformatted(localization.Format("BlueSpell.Rank",spell.Rank));
                    ImGui.TextWrapped(spell.Description);
                    ImGui.TextWrapped(string.IsNullOrWhiteSpace(spell.Location) ? localization.Get("BlueSpell.MissingLocation") : localization.Format("BlueSpell.Location",spell.Location)+(spell.DutyLevel>0 ? " "+localization.Format("BlueSpell.DutyLevel",spell.DutyLevel):""));
                    ImGui.TextColored(spell.ConfirmedUnlocked ? Success : Muted,localization.Get(spell.ConfirmedUnlocked ? "BlueSpell.Learned":"CollectionState.Unknown"));
                    CopyButton(localization.Get("BlueSpell.Copy"),()=>spell.Name);
                    DrawBlueSpellMonsters(spell.Id);
                }
                if (ImGui.Button(localization.Get("BlueSpell.Close"))) ImGui.CloseCurrentPopup();
            }
            finally { ImGui.EndPopup(); }
        }
    }
}
