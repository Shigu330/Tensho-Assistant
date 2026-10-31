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
    // 0.111: size of each game icon (key = icon id) on top of IconScale; 0 hides that icon, absent keeps 100 %.
    public Dictionary<uint,float> IconSizes { get; set; } = [];
    public float SizeOf(uint icon)=>IconSizes is not null && IconSizes.TryGetValue(icon,out var value) ? !float.IsFinite(value) ? 1f:value<=0 ? 0:ClampScale(value):1f;
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
    public List<uint> FollowedQuests { get; set; } = [];
    public bool OpenOnLoad { get; set; }
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
