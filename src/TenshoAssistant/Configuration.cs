namespace TenshoAssistant;

/// <summary>Tensho map display settings (MAP-016, Mappy-like): each layer can be turned off; values are clamped on use.</summary>
public sealed class MapViewSettings
{
    public bool ShowFixedIcons { get; set; } = true;
    public bool ShowPlaceNames { get; set; } = true;
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
    public bool ShowGathering { get; set; }
    public bool GatheringLabels { get; set; } = true;
    public bool QuestTooltips { get; set; } = true;
    public bool ShowViewCone { get; set; } = true;
    public bool TeleportOnClick { get; set; } = true;
    public float PlaceNameScale { get; set; } = 1f;
    public bool ReplaceGameMap { get; set; } = true;
    // 0.114: FATEs running in the zone (live), with their area.
    public bool ShowFates { get; set; } = true;
    // 0.127: party members, sightseeing vistas, aether currents (collected ones greyed, or hidden on request).
    public bool ShowParty { get; set; } = true;
    public bool ShowSightseeing { get; set; } = true;
    public bool ShowAetherCurrents { get; set; } = true;
    public bool HideCollected { get; set; }
    // 0.137: NPC vendors of the cached vendor catalogue (dense, off by default).
    public bool ShowVendors { get; set; }
    // 0.151: areas per kind — shown or not, and colour (RGBA, the fill uses the alpha, the outline is stronger).
    // 0.153: how much icons and names follow the zoom — 0 = fixed size on screen (like the game), 1 = proportional to the map.
    public float ZoomFollow { get; set; } = 0.5f;
    public static float ClampFollow(float value)=>float.IsFinite(value) ? Math.Clamp(value,0f,1f):0.5f;
    /// <summary>Zoom factor applied to icons and names: zoom^follow (1 at 100 % whatever the setting).</summary>
    public float ZoomFactor(float zoom)=>MathF.Pow(Math.Max(0.01f,zoom),ClampFollow(ZoomFollow));
    public bool ShowEventAreas { get; set; } = true;
    public bool ShowFateAreas { get; set; } = true;
    public bool ShowGatheringAreas { get; set; } = true;
    public System.Numerics.Vector4 QuestAreaColor { get; set; } = new(0.95f,0.70f,0.25f,0.25f);
    public System.Numerics.Vector4 EventAreaColor { get; set; } = new(0.30f,0.75f,1.00f,0.30f);
    public System.Numerics.Vector4 FateAreaColor { get; set; } = new(0.75f,0.45f,0.95f,0.25f);
    public System.Numerics.Vector4 GatheringAreaColor { get; set; } = new(0.55f,0.90f,0.55f,0.20f);
    // 0.111: size of each game icon (key = icon id) on top of IconScale; 0 hides that icon, absent keeps 100 %.
    public Dictionary<uint,float> IconSizes { get; set; } = [];
    public float SizeOf(uint icon)=>IconSizes is not null && IconSizes.TryGetValue(icon,out var value) ? CleanSize(value):1f;
    // 0.119: per-map overrides (map id → icon id → size) on top of the global table, and draw priorities (-9..9, higher on top).
    public Dictionary<uint,Dictionary<uint,float>> MapIconSizes { get; set; } = [];
    public Dictionary<uint,int> IconPriorities { get; set; } = [];
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
    /// <summary>Back to the global settings for one map (mapId) or resets the global table (mapId 0).</summary>
    public void Reset(uint mapId)
    {
        if(mapId!=0){MapIconSizes?.Remove(mapId);MapIconPriorities?.Remove(mapId);return;}
        IconSizes?.Clear();IconPriorities?.Clear();
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
    public List<uint> TripleTriadDeck { get; set; } = [];
    // Decks generated for an NPC (0.162), named after it; at most one per NPC.
    public List<TriadSavedDeck> TripleTriadDecks { get; set; } = [];
    public List<uint> FollowedQuests { get; set; } = [];
    public bool OpenOnLoad { get; set; }
    // Retainers and saddlebags copied into the bank cache each time they are opened (0.167), instead of an import button.
    public bool AutoBankCapture { get; set; } = true;
    // Real combat stats panel beside the game's Character window (0.182).
    public bool ShowCharacterPanel { get; set; } = true;
    // Duty loot panel beside the game's Duty Finder (0.185).
    public bool ShowDutyFinderLoot { get; set; } = true;
    // Where crafting macros are written in the game's macros (0.191).
    public TenshoAssistant.Macros.MacroCopySettings MacroCopy { get; set; } = new();
    // Tensho alarms (0.201): the list and the options (lead, game alarm sound, chat, silence in duties).
    public List<TenshoAssistant.Alarms.TenshoAlarm> Alarms { get; set; } = [];
    // Header and advanced settings (0.206).
    public bool ShowHeaderClock { get; set; } = true;
    // Fashion Report of the week (0.213): owned first, dim the others, by popularity, panel beside the game window.
    public TenshoAssistant.Fashion.FashionOptions FashionOptions { get; set; } = new();
    public bool ShowHeaderEorzeaTime { get; set; } = true;
    public bool ShowIds { get; set; }
    public bool DeveloperMode { get; set; }
    public bool HomeShowTools { get; set; } = true;
    public bool CommunityLumina { get; set; } = true;
    public bool CommunityGarland { get; set; } = true;
    public bool CommunityWiki { get; set; } = true;
    public TenshoAssistant.Alarms.AlarmSettings AlarmOptions { get; set; } = new();
    // 0 follows the game's interface theme; 1..6 force a PluginTheme (value - 1).
    public int ThemeMode { get; set; }
    public MapViewSettings MapView { get; set; } = new();
    public bool OpenOnLogin { get; set; }
    public string? LanguageCode { get; set; }
    public Dictionary<string, bool> Observations { get; set; } = new();
    private bool craftLogHelperEnabled=true;
    public bool CraftLogHelperEnabled
    {
        get=>Volatile.Read(ref craftLogHelperEnabled);
        set=>Volatile.Write(ref craftLogHelperEnabled,value);
    }
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
    // Default on: requested by the user; only active with ItemUsagesOnHover, after a stable hover, one Garland request at a time.
    private bool monsterSourcesOnHover=true;
    public bool MonsterSourcesOnHover
    {
        get => Volatile.Read(ref monsterSourcesOnHover);
        set => Volatile.Write(ref monsterSourcesOnHover,value);
    }
    public bool MarketPricesOnHover
    {
        get => Volatile.Read(ref marketPricesOnHover);
        set => Volatile.Write(ref marketPricesOnHover, value);
    }
}

public sealed class TriadSavedDeck
{
    public string Name { get; set; } = string.Empty;
    public uint NpcId { get; set; }
    public List<uint> Cards { get; set; } = [];
}
