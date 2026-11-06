namespace TenshoAssistant;

/// <summary>
/// « Restore every setting » (Settings, 0.258): which public properties of <see cref="Configuration"/> keep their value (saved data,
/// the language, the interface's folded groups) and which go back to their default. 0.321 (review 0.316, point 5): both lists live
/// here and a test requires every property to be in exactly one, so new saved data is never erased by mistake (it happened in 0.315).
/// </summary>
public static class ConfigurationReset
{
    public static readonly IReadOnlySet<string> Kept = new HashSet<string>
    {
        "Version", "Alarms", "ReminderDone", "TreasureMapAt", "MacroChainPlan", "MapBookmarks", "PlaceDirectory", "CraftPresets", "CraftLogTargets", "GcSupplySeen", "GpRegenBase", "CollectableScrutinyGain", "CollectableIntuitionGain", "TripleTriadDeck",
        "TripleTriadDecks", "FollowedQuests", "Observations", "InventorySortProfiles", "InventorySortTemplates", "LanguageCode",
        // 0.315: saved data added since 0.258 (vessels, gear and materia plans).
        "Voyages", "GearPlans", "GearPlanSets", "CollectionMemories", "LastCraftStats", "FashionOutfit", "WeatherFavorites", "GearMateria", "SubmarineUnlocked", "VesselPlans",
        // 0.321: interface state, not a setting (which navigation groups are folded).
        "NavigationCollapsed",
        // 0.503 (ONBOARD-001): the pages whose first-visit help was read.
        "HelpSeen",
        // 0.505 (ONBOARD-001): where the guide stands: a reset of the settings does not start it again.
        "GuideDone", "GuideOffered", "GuideStep", "GuideStop", "GuideInterests", "GuideWindowsDone", "GuideChecklistHidden",
        // 0.347 (g, I-08): the player's item notes and labels.
        "ItemNotes",
        // 0.360 (g, C-09): the player's wishlist.
        "Wishlist",
        // 0.368 (g, C-15): the achievements the player tracks.
        "TrackedAchievements",
        // 0.369 (g, X-03): the title chosen for each job.
        "JobTitles",
        // 0.370 (g, X-02): favourite orchestrion rolls.
        "OrchestrionFavorites",
        // 0.375 (g, M-05): the player's own tasks.
        "PersonalTasks",
        // 0.448 (N-08): the garden journal.
        "GardenBeds",
    };

    public static readonly IReadOnlySet<string> Reset = new HashSet<string>
    {
        // 0.509 (A-04): the suggested move during a synthesis.
        "CraftLiveSuggest",
        // 0.589 (C5): the sequence followed during a synthesis.
        "CraftLiveFollow",
        // 0.594 (I11): the Bank as a list.
        "BankListView",
        // 0.508 (I-05): the panel beside the retainer sale list.
        "RetainerSalesPanel",
        // 0.503 (ONBOARD-001): first-visit help on the pages.
        "HelpBanners",
        // e) (audit 2026-11-02): the four parts of the item hover panel.
        "HoverShowCollection", "HoverShowStock", "HoverShowUsages", "HoverShowSources",
        // 0.377 (g, M-07): comparison with the worn piece under the item tooltip.
        "HoverShowCompare",
        "Cache", "OpenOnLoad", "AutoBankCapture", "ShowCharacterPanel", "RemindersFromGame", "ShowEnglishItemName", "CostDataCenter", "CostUseVendors", "HidePanelsBehindGame", "HideBarsBehindGame", "ShowDutyFinderLoot", "MacroCopy", "AlarmServerBar", "ReminderServerBar", "ReminderFinishedServerBar", "ReminderBarHidden", "AlarmBarTooltip",
        // 0.344 (g, I-02): currency alerts, their server bar entry and the thresholds.
        "CurrencyAlerts", "CurrencyServerBar", "CurrencyThresholds", "CurrencyWeeklyCapAlert",
        "AlarmBarTooltipCount", "RouteUnlockedOnly", "GearLevelsBelow", "GearExactOutsideCaps", "GearMinItemLevel", "GearAssumeHq", "GearAllJobs",
        "GearOwnedOnly", "ShowHeaderClock", "GatheringPanel", "FishingPanel", "FashionOptions", "ShowHeaderEorzeaTime", "ShowIds", "DeveloperMode",
        "HomeShowTools", "HomeTodayHidden", "HomeTilesHidden", "CommunityLumina", "CommunityGarland", "CommunityWiki", "CommunityGatherBuddy", "CommunityTeamcraftBites", "CommunityFashion", "CommunityUniversalis", "AlarmOptions",
        "Discord", "ThemeMode", "ThemeModeBeforeTool", "CraftLogTryAllPresets", "GatherPlanFromList", "CollectableUseScrutiny", "CollectableUseRestore", "CollectableUseFocus", "CollectableUsePriming", "CollectableRiskTolerance", "GpRegenPerTick", "GpForbiddenActions", "QuestList", "MapView", "CraftSearch", "OpenOnLogin", "SortPreviewField", "SortPreviewDescending", "CraftLogHelperEnabled", "CraftLogAutoSolve", "MarketBoardScope",
        "MarketPricesOnHover", "ItemContextMenuEnabled", "ItemUsagesOnHover", "HoverPriceRefresh", "UndercutClipboard", "JobTitlesEnabled",
        // 0.347 (g, I-08): the two shop reminder settings (the notes themselves are saved data, kept by « restore every setting »).
        "ItemNoteShopReminder", "ItemNoteImportantSound",
        // 0.327: one switch per in-game Triple Triad aid.
        "TriadMatchOverlay", "TriadChallengePanel", "TriadDeckEditorHighlight", "HoverHideInDuty",
        // 0.327 (queue step e): opening page; the remembered page goes with its setting.
        "OpenOnLastPage", "LastPage",
        // 0.327 (queue step e): number of item search results.
        "ItemSearchLimit",
        // 0.375 (g, M-05): on-screen « to do » panel.
        "TaskPanel",
        // 0.376 (g, M-06): wear alert, bar and thresholds.
        "GearWear",
        // 0.386 (g, X-10): clocks of other time zones.
        "WorldClocks",
        // 0.388 (user request): sort button on the game's inventory window.
        "InventorySortButton", "InventorySortButtonX", "InventorySortButtonY",
        // 0.391 (g, X-06): GP tick bar.
        "GpTickBar",
        // 0.394 (user request): fonts of the Tensho window.
        "Fonts",
        // 0.399 (user request): « Go there » teleports at once.
        "GoThereTeleport",
        // 0.412 (g, R-10, user's choice): « Go there » also wears the job's gear set.
        "GoThereEquip",
        // 0.415 (g, R-13): pinned tracking window.
        "TrackerWindow",
        // 0.426 (user request): Tensho closes when it opens a game window.
        "CloseOnGameWindow",
        // 0.445 (domain 6, N-05): PaissaDB plot watch.
        "PlotWatch", "PlotWatchMinutes", "PlotWatchSize",
        // 0.446 (N-09): alert when an A or S hunt mark appears nearby.
        "HuntMarkAlert",
        // 0.447 (X-13): chat channel of the conductor's call.
        "HuntAnnounceChannel",
        // 0.448 (N-08): garden journal reminders.
        "GardenAlerts", "GardenDetect", "CraftLivePanel",
        // 0.403 (g, R-08): GP alert and its threshold.
        "GpAlert", "GpAlertThreshold",
    };
}

/// <summary>
/// Crafting search limits (steps 1–100, beam width 1–512, budget of evaluated states 1–500 000) and macro export options (extra wait
/// 0–3 s, echo line), saved since 0.321; read through the bounded values so a hand-edited configuration stays in range.
/// </summary>
public sealed class CraftSearchSettings
{
    public int Steps { get; set; } = 40;
    public int Width { get; set; } = 128;
    public int Budget { get; set; } = 100000;
    public int ExtraWait { get; set; }
    public bool Echo { get; set; } = true;
    public int BoundedSteps => Math.Clamp(Steps, 1, 100);
    public int BoundedWidth => Math.Clamp(Width, 1, 512);
    public int BoundedBudget => Math.Clamp(Budget, 1, 500000);
    public int BoundedExtraWait => Math.Clamp(ExtraWait, 0, 3);
    // X-12 (0.419): extra wait of the buffs (2-second actions) apart from the other actions; null = same as ExtraWait (before 0.419).
    public int? BuffExtraWait { get; set; }
    // 0.572 (C15): the simulation runs again by itself after each change of the sequence or the attributes.
    public bool AutoSimulate { get; set; } = true;
    // 0.573 (C17): the sound of the end-of-page line, 1 to 16, 0 for none; and that line on the last page only.
    public int EchoSound { get; set; } = 1;
    public bool EchoLastOnly { get; set; }
    public int BoundedEchoSound => Math.Clamp(EchoSound, 0, 16);
    public int? BoundedBuffExtraWait => BuffExtraWait is { } wait ? Math.Clamp(wait, 0, 3) : null;
    // 0.438 (user request): crafting actions the player forbids (names of CraftActionKind); empty by default.
    public List<string> ForbiddenActions { get; set; } = [];
    // A-03 (0.465, DEC-028): the search uses Raphael (native library) instead of Tensho's own search; off by default.
    public bool Raphael { get; set; }
}

/// <summary>Tensho map display settings (MAP-016, Mappy-like): each layer can be turned off; values are clamped on use.</summary>
public sealed class QuestListSettings
{
    // 0.550 (T3): the level range of the quest list; 0 = no limit.
    public int LevelFrom { get; set; }
    public int LevelTo { get; set; }
}

public sealed class MapViewSettings
{
    public bool ShowFixedIcons { get; set; } = true;
    public bool ShowPlaceNames { get; set; } = true;
    // 0.596 (P12): the names the game writes beside some icons.
    public bool ShowIconNames { get; set; } = true;
    public bool ShowQuests { get; set; } = true;
    public bool ShowQuestAreas { get; set; } = true;
    public bool ShowFlag { get; set; } = true;
    public bool ShowPlayer { get; set; } = true;
    public float IconScale { get; set; } = 1f;
    public float Opacity { get; set; } = 1f;
    public bool Locked { get; set; }
    public bool HideInCombat { get; set; }
    public bool HideInDuty { get; set; }
    // 0.107: nearby objects, teleport, per-layer options and replacing the game map (requested by the user, on by default).
    public bool ShowTreasure { get; set; } = true;
    public bool ShowGathering { get; set; } = true;
    // N-09 (0.441): living hunt marks streamed around the player (B, A, S).
    public bool ShowHuntMarks { get; set; } = true;
    public bool GatheringLabels { get; set; } = true;
    // 0.535 (user request): the map without its title bar; off by default.
    public bool HideTitleBar { get; set; }
    // 0.537 (user request): the map steps aside when a game window comes in front; off by default: it goes whole.
    public bool HideBehindGame { get; set; }
    public bool QuestTooltips { get; set; } = true;
    public bool ShowViewCone { get; set; } = true;
    // 0.288 (user request): player marker size, view cone length and opacity (fractions; clamped on use).
    public float PlayerIconScale { get; set; } = 1f;
    // 0.297: off-view targets (character, flag, active quests) drawn on the map's edge with their icon.
    public bool ShowEdgeMarkers { get; set; } = true;
    // 0.303: menus and tool column drawn over the map (shown while the mouse is over it), the map using the whole window.
    public bool OverlayControls { get; set; }
    // 0.305: with OverlayControls, the shown map's name stays written while the mouse is away from the map.
    public bool OverlayZoneName { get; set; } = true;
    // Kinds shown on the edge (each its own switch).
    public bool EdgePlayer { get; set; } = true;
    public bool EdgeFlag { get; set; } = true;
    public bool EdgeFlagCoordinates { get; set; } = true;
    public bool ShowPlayerCoordinates { get; set; } = true;
    public bool EdgeTarget { get; set; } = true;
    public bool EdgeQuests { get; set; } = true;
    public bool EdgeEvents { get; set; } = true;
    /// <summary>P17 (0.652): leves apart from the other events.</summary>
    public bool EdgeLeves { get; set; } = true;
    public bool EdgeFates { get; set; } = true;
    public bool EdgeGathering { get; set; } = true;
    public bool EdgeParty { get; set; } = true;
    public float ViewConeScale { get; set; } = 1f;
    public float ViewConeOpacity { get; set; } = 0.5f;
    // 0.289 (user request): view cone colour (RGB; the opacity stays its own setting); null = the theme's gold.
    public System.Numerics.Vector3? ViewConeColor { get; set; }
    public static float ClampMarker(float value)=>float.IsFinite(value) ? Math.Clamp(value,0.5f,3f):1f;
    public static float ClampConeOpacity(float value)=>float.IsFinite(value) ? Math.Clamp(value,0f,1f):0.5f;
    public bool TeleportOnClick { get; set; } = true;
    // 0.373 (g, M-03): map links of the chat kept in a list of the Maps page (this session only, never saved).
    public bool ChatLinks { get; set; } = true;
    // 0.378 (g, M-08): the player's bookmarks drawn on the Tensho map (a click places the flag).
    public bool ShowBookmarks { get; set; } = true;
    public float PlaceNameScale { get; set; } = 1f;
    // 0.546 (P16, user request: on a setting): a short left click on an empty spot places the flag; off by default.
    public bool FlagOnClick { get; set; }
    // 0.545 (P5): the wheel's step and direction; 1.15 is what the map always did.
    public float WheelStep { get; set; } = 1.15f;
    public bool WheelInvert { get; set; }
    /// <summary>By how much one wheel notch multiplies the zoom.</summary>
    public float WheelFactor(float wheel)
    {
        var step=Math.Clamp(float.IsFinite(WheelStep) ? WheelStep:1.15f,1.02f,1.5f);
        return wheel>0 != WheelInvert ? step:1/step;
    }
    // 0.543 (P2): names follow the zoom on their own (null: as the icons) and hide under a zoom; gathering labels have their size and threshold, 0.6 being the old fixed one.
    public float? NameZoomFollow { get; set; }
    public float NameMinZoom { get; set; }
    public float GatheringLabelMinZoom { get; set; } = 0.6f;
    public float GatheringLabelScale { get; set; } = 1f;
    public float NameFactor(float zoom)=>MathF.Pow(Math.Max(0.01f,zoom),ClampFollow(NameZoomFollow ?? ZoomFollow));
    public bool NamesShown(float zoom)=>zoom>=Math.Clamp(float.IsFinite(NameMinZoom) ? NameMinZoom:0,0,4);
    public bool GatheringLabelShown(float zoom)=>zoom>=Math.Clamp(float.IsFinite(GatheringLabelMinZoom) ? GatheringLabelMinZoom:0.6f,0,4);
    public float GatheringLabelSize(float font)=>font*Math.Clamp(float.IsFinite(GatheringLabelScale) ? GatheringLabelScale:1,0.5f,2f);
    public bool ReplaceGameMap { get; set; } = true;
    // 0.114: FATEs running in the zone (live), with their area.
    public bool ShowFates { get; set; } = true;
    // 0.127: party members, sightseeing vistas, aether currents (collected ones greyed, or hidden on request).
    public bool ShowParty { get; set; } = true;
    public bool ShowSightseeing { get; set; } = true;
    public bool ShowAetherCurrents { get; set; } = true;
    public bool HideCollected { get; set; } = true;
    // 0.137: NPC vendors of the cached vendor catalogue (dense, but on by default: the vendors were validated in game).
    public bool ShowVendors { get; set; } = true;
    // 0.151: areas per kind — shown or not, and colour (RGBA, the fill uses the alpha, the outline is stronger).
    // 0.153: how much icons and names follow the zoom — 0 = fixed size on screen (like the game), 1 = proportional to the map.
    public float ZoomFollow { get; set; } = 0.15f;
    // 0.542 (P1): the zoom at opening. The last one starts at 50 %, what the map always did; the zoom of a quest or a link was fixed at 100 % at least.
    public TenshoAssistant.Maps.MapOpenZoom OpenZoom { get; set; } = TenshoAssistant.Maps.MapOpenZoom.Last;
    public float LastZoom { get; set; } = 0.5f;
    public float OpenZoomValue { get; set; } = 0.5f;
    public float FocusZoom { get; set; } = 1f;
    public static float ClampFollow(float value)=>float.IsFinite(value) ? Math.Clamp(value,0f,1f):0.15f;
    /// <summary>Zoom factor applied to icons and names: zoom^follow (1 at 100 % whatever the setting).</summary>
    public float ZoomFactor(float zoom)=>MathF.Pow(Math.Max(0.01f,zoom),ClampFollow(ZoomFollow));
    // 0.245 (R-04): icons whose right click opens the general map menu instead of their own menu (key = icon id).
    public HashSet<uint>? RightClickIgnore { get; set; }
    public bool RightClickIgnored(uint icon)=>RightClickIgnore?.Contains(icon)==true;
    public bool ShowEventAreas { get; set; } = true;
    public bool ShowLeveAreas { get; set; } = true;
    /// <summary>N-07 (0.667), 0.684: what can be gathered on the Island Sanctuary, on its map, by the game's five kinds.</summary>
    public bool ShowIslandPlants { get; set; } = true;
    public bool ShowIslandWood { get; set; } = true;
    public bool ShowIslandRocks { get; set; } = true;
    public bool ShowIslandEarth { get; set; } = true;
    public bool ShowIslandFishing { get; set; } = true;
    /// <summary>An item of no known kind stays as long as one kind is shown.</summary>
    public bool ShowsIslandKind(TenshoAssistant.Maps.IslandResourceKind? kind)=>kind switch
    {
        TenshoAssistant.Maps.IslandResourceKind.Plants=>ShowIslandPlants,
        TenshoAssistant.Maps.IslandResourceKind.Wood=>ShowIslandWood,
        TenshoAssistant.Maps.IslandResourceKind.Rocks=>ShowIslandRocks,
        TenshoAssistant.Maps.IslandResourceKind.Earth=>ShowIslandEarth,
        TenshoAssistant.Maps.IslandResourceKind.Fishing=>ShowIslandFishing,
        _=>ShowIslandPlants || ShowIslandWood || ShowIslandRocks || ShowIslandEarth || ShowIslandFishing,
    };
    public bool ShowFateAreas { get; set; } = true;
    public bool ShowGatheringAreas { get; set; } = true;
    public System.Numerics.Vector4 QuestAreaColor { get; set; } = new(0.95f,0.70f,0.25f,0.25f);
    public System.Numerics.Vector4 EventAreaColor { get; set; } = new(0.30f,0.75f,1.00f,0.30f);
    /// <summary>P17 (0.652): the game draws them dark green; matched by eye on a capture.</summary>
    public System.Numerics.Vector4 LeveAreaColor { get; set; } = new(0.15f,0.55f,0.20f,0.40f);
    public System.Numerics.Vector4 FateAreaColor { get; set; } = new(0.75f,0.45f,0.95f,0.25f);
    public System.Numerics.Vector4 GatheringAreaColor { get; set; } = new(0.55f,0.90f,0.55f,0.20f);
    // 0.111: size of each game icon (key = icon id) on top of IconScale; 0 hides that icon, absent keeps 100 %.
    // 0.289 (user request): the user's tuned values become the defaults — party members at 50 %; aetherytes (60453, 60430, 60959)
    // drawn above other icons (+1), 60442 and the side-quest icon 71021 below (−1). Icon ids read from the user's saved settings.
    public const uint PartyGlyph=0xFFFF0004;
    public static IReadOnlyDictionary<uint,float> DefaultIconSizes { get; }=new Dictionary<uint,float>{[PartyGlyph]=0.5f};
    public static IReadOnlyDictionary<uint,int> DefaultIconPriorities { get; }=new Dictionary<uint,int>{[60959]=1,[60453]=1,[60430]=1,[60442]=-1,[71021]=-1};
    public Dictionary<uint,float> IconSizes { get; set; } = new(DefaultIconSizes);
    public float SizeOf(uint icon)=>IconSizes is not null && IconSizes.TryGetValue(icon,out var value) ? CleanSize(value):1f;
    // 0.119: per-map overrides (map id → icon id → size) on top of the global table, and draw priorities (-9..9, higher on top).
    public Dictionary<uint,Dictionary<uint,float>> MapIconSizes { get; set; } = [];
    public Dictionary<uint,int> IconPriorities { get; set; } = new(DefaultIconPriorities);
    public Dictionary<uint,Dictionary<uint,int>> MapIconPriorities { get; set; } = [];
    public const int MinimumPriority=-9,MaximumPriority=9;
    public float SizeOf(uint icon,uint mapId)=>MapIconSizes?.GetValueOrDefault(mapId)?.TryGetValue(icon,out var value)==true ? CleanSize(value):SizeOf(icon);
    public int PriorityOf(uint icon,uint mapId)=>Math.Clamp(MapIconPriorities?.GetValueOrDefault(mapId)?.TryGetValue(icon,out var own)==true ? own
        :IconPriorities?.TryGetValue(icon,out var global)==true ? global:0,MinimumPriority,MaximumPriority);
    public bool HasMapOverrides(uint mapId)=>MapIconSizes?.ContainsKey(mapId)==true || MapIconPriorities?.ContainsKey(mapId)==true;
    /// <summary>Writes a size to the global table (mapId 0) or to that map's overrides.</summary>
    public void SetSize(uint icon,uint mapId,float size)
    {
        if(mapId==0){(IconSizes??=[])[icon]=size;return;}
        MapIconSizes??=[];if(!MapIconSizes.TryGetValue(mapId,out var own))MapIconSizes[mapId]=own=[];
        own[icon]=size;
    }
    public void SetPriority(uint icon,uint mapId,int priority)
    {
        priority=Math.Clamp(priority,MinimumPriority,MaximumPriority);
        if(mapId==0){(IconPriorities??=[])[icon]=priority;return;}
        MapIconPriorities??=[];if(!MapIconPriorities.TryGetValue(mapId,out var own))MapIconPriorities[mapId]=own=[];
        own[icon]=priority;
    }
    /// <summary>Back to the global settings for one map (mapId) or resets the global table (mapId 0), with the right-click mode of every
    /// icon (one list for all maps, so only the global reset clears it).</summary>
    public void Reset(uint mapId)
    {
        if(mapId!=0){MapIconSizes?.Remove(mapId);MapIconPriorities?.Remove(mapId);return;}
        IconSizes=new(DefaultIconSizes);IconPriorities=new(DefaultIconPriorities);RightClickIgnore=null;
    }
    private static float CleanSize(float value)=>!float.IsFinite(value) ? 1f:value<=0 ? 0:ClampScale(value);
    public static float ClampScale(float value)=>float.IsFinite(value) ? Math.Clamp(value,0.5f,2f):1f;
    public static float ClampOpacity(float value)=>float.IsFinite(value) ? Math.Clamp(value,0.2f,1f):1f;
}

// Pure managed settings; the Dalamud interface is attached in a separate partial.
public sealed partial class Configuration
{
    public int Version { get; set; } = 1;
    private Caching.CachePreferences cache=new();
    public Caching.CachePreferences Cache {get=>Volatile.Read(ref cache);set=>Volatile.Write(ref cache,value ?? new());}
    public List<TenshoAssistant.Macros.MacroSlot> MacroChainPlan { get; set; } = [];
    public List<TenshoAssistant.Maps.MapBookmark> MapBookmarks { get; set; } = [];
    public List<TenshoAssistant.Craft.CraftPreset>? CraftPresets { get; set; } = [];
    // 0.468 (user request): the crafting log's quality target, by recipe; a recipe not listed aims at its maximum.
    public Dictionary<uint,int>? CraftLogTargets { get; set; } = [];
    // 0.469 (user request): today's Grand Company request as seen in the game's supply missions window.
    public TenshoAssistant.Items.GcSupplySeen? GcSupplySeen { get; set; }
    public List<uint> TripleTriadDeck { get; set; } = [];
    // Decks generated for an NPC (0.162), named after it; at most one per NPC.
    public List<TriadSavedDeck> TripleTriadDecks { get; set; } = [];
    // 0.327 (audit 2026-11-02, queue step e): one switch per in-game Triple Triad aid, all on as before; they act only while the Triad
    // observation is on: match overlay (best card and square), NPC challenge panel with the deck choice odds, deck editor highlight.
    // Read in Draw only.
    public bool TriadMatchOverlay { get; set; } = true;
    public bool TriadChallengePanel { get; set; } = true;
    public bool TriadDeckEditorHighlight { get; set; } = true;
    public List<uint> FollowedQuests { get; set; } = [];
    // 0.264: sidebar categories folded by the user (category keys).
    public HashSet<string>? NavigationCollapsed { get; set; }
    public HashSet<string>? HelpSeen { get; set; }
    public bool GuideDone { get; set; }
    public bool GuideOffered { get; set; }
    public int GuideStep { get; set; }
    public int GuideStop { get; set; }
    public HashSet<string>? GuideInterests { get; set; }
    public HashSet<string>? GuideWindowsDone { get; set; }
    public bool GuideChecklistHidden { get; set; }
    public bool HelpBanners { get; set; } = true;
    public bool RetainerSalesPanel { get; set; } = true;
    public bool CraftLiveSuggest { get; set; } = true;
    public bool CraftLiveFollow { get; set; } = true;
    public bool BankListView { get; set; }
    public bool OpenOnLoad { get; set; }
    // Retainers and saddlebags copied into the bank cache each time they are opened (0.167), instead of an import button.
    public bool AutoBankCapture { get; set; } = true;
    // Real combat stats panel beside the game's Character window (0.182).
    public bool ShowCharacterPanel { get; set; } = true;
    // 0.537 (user request): Tensho's panels and bars step aside when a game window comes in front.
    public bool HidePanelsBehindGame { get; set; } = true;
    public bool HideBarsBehindGame { get; set; } = true;
    // Duty loot panel beside the game's Duty Finder (0.185).
    public bool ShowDutyFinderLoot { get; set; } = true;
    // Where crafting macros are written in the game's macros (0.191).
    public TenshoAssistant.Macros.MacroCopySettings MacroCopy { get; set; } = new();
    // Tensho alarms (0.201): the list and the options (lead, game alarm sound, chat, silence in duties).
    public List<TenshoAssistant.Alarms.TenshoAlarm> Alarms { get; set; } = [];
    // 0.300: alarm clock in the game's server info bar while alarms are on.
    public bool AlarmServerBar { get; set; } = true;
    // 0.455: finished reminders in the server bar, apart from the alarms.
    public bool ReminderServerBar { get; set; } = true;
    // 0.459: a third icon for what is finished.
    public bool ReminderFinishedServerBar { get; set; } = true;
    // 0.458: Reminders rows left out of that icon (their « Bar » box unticked); null = none.
    public List<string>? ReminderBarHidden { get; set; }
    // 0.558 (T17): a reminder whose counter the game reads at 0 ticks itself until the reset.
    public bool RemindersFromGame { get; set; } = true;
    // 0.344 (g, I-02): currency threshold alerts (chat, sound and Discord with the alarm options) and their server bar entry, read on
    // the framework thread; thresholds in percent of the cap per currency item (missing = 80 %, 0 = off), replaced whole, never edited
    // in place (copy on write) so the framework thread never sees a dictionary being changed.
    private bool currencyAlerts=true,currencyServerBar=true;
    private Dictionary<uint,int>? currencyThresholds;
    public bool CurrencyAlerts { get=>Volatile.Read(ref currencyAlerts); set=>Volatile.Write(ref currencyAlerts,value); }
    public bool CurrencyServerBar { get=>Volatile.Read(ref currencyServerBar); set=>Volatile.Write(ref currencyServerBar,value); }
    // 0.606: with the alerts, the weekly limit of the limited tomestones reached.
    public bool CurrencyWeeklyCapAlert { get; set; } = true;
    public Dictionary<uint,int>? CurrencyThresholds { get=>Volatile.Read(ref currencyThresholds); set=>Volatile.Write(ref currencyThresholds,value); }
    // 0.301: Tensho's own tooltip under the bar icon (the game's is hidden by plugin windows) and how many alarms it lists.
    public bool AlarmBarTooltip { get; set; } = true;
    public int AlarmBarTooltipCount { get; set; } = 5;
    // 0.300 (R-08): reminders ticked « done » (key → time, kept until the next reset) and the last treasure map opened.
    public Dictionary<string,DateTimeOffset>? ReminderDone { get; set; }
    public DateTimeOffset? TreasureMapAt { get; set; }
    // 0.307 (R-07): free company submarines and airships as last read in the workshop.
    public List<TenshoAssistant.FreeCompany.VoyageVessel>? Voyages { get; set; }
    // 0.315 (R-11): unlocked submarine sectors per workshop (house id) and planned vessel builds (key → rank, four part rows).
    public Dictionary<string,List<uint>>? SubmarineUnlocked { get; set; }
    public Dictionary<string,List<uint>>? VesselPlans { get; set; }
    // 0.315: the route planner proposes only sectors read as unlocked (when the workshop was visited).
    public bool RouteUnlockedOnly { get; set; } = true;
    // 0.310 (R-11): gear plans (job → slot → item) and the planner's filters.
    public Dictionary<uint,Dictionary<TenshoAssistant.Gear.GearSlot,uint>>? GearPlans { get; set; }
    // 0.593 (T25): the other named plans of each job; the one in use stays in GearPlans and GearMateria.
    public Dictionary<uint,TenshoAssistant.Gear.GearPlanSet>? GearPlanSets { get; set; }
    // 0.602 (T28): by character, what the collections held at the last reading and what was gained since.
    public Dictionary<ulong,TenshoAssistant.Collections.CollectionMemory>? CollectionMemories { get; set; }
    public int GearLevelsBelow { get; set; } = 5;
    public bool GearExactOutsideCaps { get; set; } = true;
    public int GearMinItemLevel { get; set; }
    public bool GearAssumeHq { get; set; } = true;
    // 0.311: other jobs' armor and accessories in the lists (glamour since patch 7.4; weapons stay job-locked).
    public bool GearAllJobs { get; set; }
    // 0.314: only gear the character owns (worn, bags, armoury, dresser, armoire, saddlebag, retainers) in the lists.
    public bool GearOwnedOnly { get; set; }
    // 0.312: planned materia per job and slot (MeldedMateria codes); a slot listed here overrides the worn piece's materia.
    public Dictionary<uint,Dictionary<TenshoAssistant.Gear.GearSlot,List<uint>>>? GearMateria { get; set; }
    // Header and advanced settings (0.206).
    public bool ShowHeaderClock { get; set; } = true;
    // In-game gathering help beside the game's gathering window (0.225); turns the gathering window reading on.
    public bool GatheringPanel { get; set; } = true;
    // In-game fishing help while fishing (0.226): fish of the current spot with their bait routes.
    public bool FishingPanel { get; set; } = true;
    // Fashion Report of the week (0.213): owned first, dim the others, by popularity, panel beside the game window.
    public TenshoAssistant.Fashion.FashionOptions FashionOptions { get; set; } = new();
    public TenshoAssistant.Fashion.FashionOutfit? FashionOutfit { get; set; }
    public bool ShowHeaderEorzeaTime { get; set; } = true;
    public bool ShowIds { get; set; }
    public bool DeveloperMode { get; set; }
    public bool HomeShowTools { get; set; } = true;
    // 0.599 (T10): the lines of « Today » and the tiles hidden on Home.
    public List<string>? HomeTodayHidden { get; set; }
    public List<string>? HomeTilesHidden { get; set; }
    // 0.327 (queue step e): how many results the item search lists (Items page, item pickers of Monster loot and NPC vendors). Read
    // through BoundedItemSearchLimit, so a hand-edited value stays within what ItemCatalog.Search accepts (it throws outside 1–100).
    public int ItemSearchLimit { get; set; } = 30;
    public const int MinimumItemSearchLimit = 10, MaximumItemSearchLimit = 100;
    public static int BoundedItemSearchLimit(int value) => Math.Clamp(value, MinimumItemSearchLimit, MaximumItemSearchLimit);
    public bool CommunityLumina { get; set; } = true;
    public bool CommunityGarland { get; set; } = true;
    public bool CommunityWiki { get; set; } = true;
    // GatherBuddy fish data (0.227, Apache-2.0): baits, mooches, bite, windows for the fishing help.
    public bool CommunityGatherBuddy { get; set; } = true;
    // Teamcraft bite times (0.231): seconds before the bite, per fish and spot.
    public bool CommunityTeamcraftBites { get; set; } = true;
    // 0.321: Fashion Report pieces and dyes of the week (FFXIVGachaSpreadsheet); off = no download, a fresh disk copy is still read.
    public bool CommunityFashion { get; set; } = true;
    // 0.324: Universalis as a whole (Market page, hover prices, preparation cost, data centres); « prices on hover » sits under it.
    public bool CommunityUniversalis { get; set; } = true;
    // 0.332 (step e, ULD): the game's window frame behind the main window; off until it is seen in game (decision kept for the end).
    public TenshoAssistant.Alarms.AlarmSettings AlarmOptions { get; set; } = new();
    // R-12 (0.319): Tensho alarms posted to a Discord webhook, off by default; the address is a secret (never logged nor reported).
    public TenshoAssistant.Alarms.DiscordSettings Discord { get; set; } = new();
    // 0 follows the game's interface theme; 1..6 force a PluginTheme (value - 1).
    public int ThemeMode { get; set; }
    // 0.478 (user request, in-game campaign): what the crafting list still needs, in the gathering help; since 0.515 information only.
    public bool GatherPlanFromList { get; set; } = true;
    // 0.480 (user report): Scrutiny in the collectable plan, and what the bonuses add, as measured in the game's window.
    public bool CollectableUseScrutiny { get; set; } = true;
    public bool CollectableUseRestore { get; set; } = true;
    public int CollectableScrutinyGain { get; set; }
    public int CollectableIntuitionGain { get; set; }
    public bool CollectableUseFocus { get; set; } = true;
    // 0.527 (user report): Priming Touch in the collectable plan.
    public bool CollectableUsePriming { get; set; } = true;
    // 0.484 (user request): 100 chooses on the average alone, 0 on the worst outcome alone.
    public int CollectableRiskTolerance { get; set; } = 50;
    public int GpRegenPerTick { get; set; }
    // 0.488: GP per tick without any Enhanced GP Regeneration trait, measured. Replaces GpRegenObserved, whose value could be wrong.
    public int GpRegenBase { get; set; }
    public List<string>? GpForbiddenActions { get; set; } = [];
    // 0.477 (user report): the theme in use before a tool's was chosen, to come back to it. Null once the theme is chosen in the settings.
    public int? ThemeModeBeforeTool { get; set; }
    // 0.328 (map audit): « "MapView": null » in a hand-edited file becomes the defaults here, like Cache, so no draw path reads null.
    private MapViewSettings mapView=new();
    public MapViewSettings MapView {get=>mapView;set=>mapView=value ?? new();}
    public QuestListSettings QuestList { get; set; } = new();
    // 0.321 (review 0.316, point 6): crafting search limits and macro export options, kept across loads (they were window fields).
    public CraftSearchSettings CraftSearch { get; set; } = new();
    // e) (audit 2026-11-02): scope of the Market page (world, data centre, region), saved when chosen there or in Settings › Interface;
    // the data centre by default, as before. An unknown saved value is read as the data centre (MainWindow.MarketScopeShown).
    public TenshoAssistant.Market.MarketScope MarketBoardScope { get; set; } = TenshoAssistant.Market.MarketScope.DataCenter;
    public bool OpenOnLogin { get; set; }
    // 0.327 (queue step e): opening page. Off = Home when the plugin loads (as before); on = LastPage, the name of the page shown
    // last (MainWindow.Page), written when the window closes or the plugin unloads.
    public bool OpenOnLastPage { get; set; }
    public string? LastPage { get; set; }
    public string? LanguageCode { get; set; }
    public Dictionary<string, bool> Observations { get; set; } = new();
    private bool craftLogHelperEnabled=true;
    public bool CraftLogHelperEnabled
    {
        get=>Volatile.Read(ref craftLogHelperEnabled);
        set=>Volatile.Write(ref craftLogHelperEnabled,value);
    }
    // 0.327 (audit 2026-11-02, queue step e): the crafting log panel computes as soon as a recipe is selected (default, as before) or only
    // on request (its « Compute » button, « Recalculate » or the plan's « Simulate this recipe »). Read in Draw only.
    public bool CraftLogAutoSolve { get; set; } = true;
    // 0.574 (I5): the preparation's cost at the cheapest of the data center, instead of the player's world.
    public bool CostDataCenter { get; set; }
    // 0.575 (I3): the preparation's cost also takes an NPC's gil price when lower.
    public bool CostUseVendors { get; set; } = true;
    // 0.563 (C31): the player's zones of the weather table, by territory, with the name shown when added.
    public Dictionary<uint,string>? WeatherFavorites { get; set; }
    // 0.562 (I19): an item's English name under its own, in the sheet and the tooltip; off by default.
    public bool ShowEnglishItemName { get; set; }
    // 0.554 (C3): per character and job, the attributes last read; the player's data, kept by « reset all ».
    public Dictionary<ulong,Dictionary<uint,TenshoAssistant.Craft.CraftJobStats>>? LastCraftStats { get; set; }
    // 0.552 (C2): the crafting log panel also tries the sequences saved for other recipes.
    public bool CraftLogTryAllPresets { get; set; } = true;
    public int SortPreviewField { get; set; }
    public bool SortPreviewDescending { get; set; }
    public List<TenshoAssistant.Inventory.InventorySortProfile>? InventorySortProfiles { get; set; }=[];
    public List<TenshoAssistant.Inventory.InventorySortTemplate>? InventorySortTemplates { get; set; }=[];
    private bool marketPricesOnHover;
    private bool itemUsagesOnHover;
    private bool itemContextMenuEnabled;
    public bool ItemContextMenuEnabled
    {
        get => Volatile.Read(ref itemContextMenuEnabled);
        set => Volatile.Write(ref itemContextMenuEnabled,value);
    }
    public bool ItemUsagesOnHover
    {
        get => Volatile.Read(ref itemUsagesOnHover);
        set => Volatile.Write(ref itemUsagesOnHover,value);
    }
    // e) (audit 2026-11-02): parts of the panel under the game's item tooltip (Tensho's own item tooltips keep them all); all on by default.
    public bool HoverShowCollection { get; set; } = true;
    public bool HoverShowStock { get; set; } = true;
    public bool HoverShowUsages { get; set; } = true;
    public bool HoverShowSources { get; set; } = true;
    public bool MarketPricesOnHover
    {
        get => Volatile.Read(ref marketPricesOnHover);
        set => Volatile.Write(ref marketPricesOnHover, value);
    }
    // 0.347 (g, I-08): item notes and labels (saved data, replaced whole on every change, read on the framework thread by the shop
    // reminder), the shop reminder (on by default) and its sound for important items (off by default).
    private Dictionary<uint,TenshoAssistant.Items.ItemNote>? itemNotes;
    private bool itemNoteShopReminder=true,itemNoteImportantSound;
    public Dictionary<uint,TenshoAssistant.Items.ItemNote>? ItemNotes { get=>Volatile.Read(ref itemNotes); set=>Volatile.Write(ref itemNotes,value); }
    public bool ItemNoteShopReminder { get=>Volatile.Read(ref itemNoteShopReminder); set=>Volatile.Write(ref itemNoteShopReminder,value); }
    public bool ItemNoteImportantSound { get=>Volatile.Read(ref itemNoteImportantSound); set=>Volatile.Write(ref itemNoteImportantSound,value); }
    // 0.360 (g, C-09): wishlist item ids, in the order they were added (replaced whole on every change).
    private List<uint>? wishlist;
    public List<uint>? Wishlist { get=>Volatile.Read(ref wishlist); set=>Volatile.Write(ref wishlist,value); }
    // 0.368 (g, C-15): tracked achievement ids, 100 at most (replaced whole on every change).
    private List<uint>? trackedAchievements;
    public List<uint>? TrackedAchievements { get=>Volatile.Read(ref trackedAchievements); set=>Volatile.Write(ref trackedAchievements,value); }
    // 0.369 (g, X-03): title by job (ClassJob row → Title row), worn on a job change only when JobTitlesEnabled (off by default).
    private Dictionary<uint,ushort>? jobTitles;
    private bool jobTitlesEnabled;
    public Dictionary<uint,ushort>? JobTitles { get=>Volatile.Read(ref jobTitles); set=>Volatile.Write(ref jobTitles,value); }
    public bool JobTitlesEnabled { get=>Volatile.Read(ref jobTitlesEnabled); set=>Volatile.Write(ref jobTitlesEnabled,value); }
    // 0.375 (g, M-05): the player's own tasks (50 at most, replaced whole when one is added, moved or removed) and the on-screen
    // « to do » panel (off by default, user's choice).
    private List<Reminders.PersonalTask>? personalTasks;
    public List<Reminders.PersonalTask>? PersonalTasks { get=>Volatile.Read(ref personalTasks); set=>Volatile.Write(ref personalTasks,value); }
    public bool TaskPanel { get; set; }
    // 0.448 (N-08, DEC-026): the garden journal (64 beds at most, durations typed by the player) and its chat reminders (on: the player
    // only gets them for beds they noted).
    private List<Reminders.GardenBed>? gardenBeds;
    public List<Reminders.GardenBed>? GardenBeds { get=>Volatile.Read(ref gardenBeds); set=>Volatile.Write(ref gardenBeds,value); }
    public bool GardenAlerts { get; set; } = true;
    // 0.460: plantings noted from the chat.
    public bool GardenDetect { get; set; } = true;
    // 0.464: the next-action projection beside the game's Synthesis window.
    public bool CraftLivePanel { get; set; } = true;
    // 0.403 (g, R-08): chat line and sound when a gatherer's GP rises to the threshold (0 = full); off by default.
    public bool GpAlert { get; set; }
    public int GpAlertThreshold { get; set; }
    // 0.399 (user request): « Go there » teleports to the nearest aetheryte at once, without the question (off by default).
    public bool GoThereTeleport { get; set; }
    // 0.412 (g, R-10, user's choice in GAPS): « Go there » also wears the source job's gear set (off by default).
    public bool GoThereEquip { get; set; }
    // 0.415 (g, R-13): small pinned window of the tracked alarms' items, soonest first (off by default).
    public bool TrackerWindow { get; set; }
    // X-09 (0.444): the place directory (residential addresses); empty by default.
    public List<Maps.PlaceAddress>? PlaceDirectory { get; set; }
    // 0.426 (user request): Tensho closes once it has opened a game window (crafting or gathering log, game map); on by default.
    public bool CloseOnGameWindow { get; set; } = true;
    // 0.445 (N-05): read the open plots of the current world from PaissaDB every N minutes and say the new ones (off by default);
    // size filter: 0 any, 1 small, 2 medium, 3 large.
    public bool PlotWatch { get; set; }
    public int PlotWatchMinutes { get; set; } = 30;
    public int PlotWatchSize { get; set; }
    // 0.446 (N-09): a chat line (and the alarm sound if the alarms play one) when an A or S hunt mark appears nearby; off by default.
    public bool HuntMarkAlert { get; set; }
    // 0.447 (X-13): chat channel the conductor's call is sent to, on click only (one of HuntTrain.AnnounceChannels).
    public string HuntAnnounceChannel { get; set; } = "/sh";
    // 0.394 (user request): titles, sections and text fonts (family and size); null = defaults.
    public UI.FontSettings? Fonts { get; set; }
    // 0.391 (g, X-06): on-screen GP regeneration tick bar of gatherers (off by default).
    public bool GpTickBar { get; set; }
    // 0.388 (user request): a « Sort » button above the game's inventory window, applying the bags' saved rules on click (on by default).
    public bool InventorySortButton { get; set; } = true;
    // Its place: game units from the window's left edge and above its bottom edge (button centre), scaled with the window.
    public int InventorySortButtonX { get; set; } = 24;
    public int InventorySortButtonY { get; set; } = 42;
    // 0.386 (g, X-10): system time zone ids shown as clocks on the Alarms page (6 at most).
    public List<string>? WorldClocks { get; set; }
    // 0.377 (g, M-07): under the game's item tooltip, the hovered gear against the worn piece of its slot (on by default).
    public bool HoverShowCompare { get; set; } = true;
    // 0.376 (g, M-06): wear and spiritbond alert (on), on-screen bar (off) and thresholds; null = defaults.
    public Gear.GearWearSettings? GearWear { get; set; }
    // 0.370 (g, X-02): favourite orchestrion rolls (Orchestrion rows), 300 at most.
    private List<uint>? orchestrionFavorites;
    public List<uint>? OrchestrionFavorites { get=>Volatile.Read(ref orchestrionFavorites); set=>Volatile.Write(ref orchestrionFavorites,value); }
    // 0.346 (g, I-07): lowest offer minus 1 copied to the clipboard when the player opens an item's offers; off by default (it
    // replaces the clipboard). Read on the framework thread.
    private bool undercutClipboard;
    public bool UndercutClipboard
    {
        get => Volatile.Read(ref undercutClipboard);
        set => Volatile.Write(ref undercutClipboard, value);
    }
    // 0.343 (I-01): Ctrl+Alt while hovering an item asks its price again (read by the hover reader on the framework thread); on by default.
    private bool hoverPriceRefresh=true;
    public bool HoverPriceRefresh
    {
        get => Volatile.Read(ref hoverPriceRefresh);
        set => Volatile.Write(ref hoverPriceRefresh, value);
    }
    // e) (audit 2026-11-02): no item hover panel while bound by duty (read by the hover reader on the framework thread); off by default.
    private bool hoverHideInDuty;
    public bool HoverHideInDuty
    {
        get => Volatile.Read(ref hoverHideInDuty);
        set => Volatile.Write(ref hoverHideInDuty,value);
    }
}

public sealed class TriadSavedDeck
{
    public string Name { get; set; } = string.Empty;
    public uint NpcId { get; set; }
    public List<uint> Cards { get; set; } = [];
}
