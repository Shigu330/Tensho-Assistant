using Newtonsoft.Json;
using TenshoAssistant;
using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

var recipe=new RecipeDefinition(2819,12892,3,"Cuisine",59,[],7);
var preset=CraftPresets.Create(recipe,"Essai JSON local",[CraftActionKind.Reflect,CraftActionKind.TrainedPerfection,CraftActionKind.BasicSynthesis]);
var sortScope=InventorySortScope.FromStorage(new(7,BankStorageKind.Retainer,91));
var sortProfile=InventorySortRules.Default(sortScope) with{Criteria=Array.AsReadOnly(new[]{new InventorySortCriterion(InventorySortField.Category),new InventorySortCriterion(InventorySortField.Quantity,true)}),Zones=Array.AsReadOnly(new[]{new InventorySortZone("Fruits",25,10,12,InventoryQualityFilter.HighQuality,"pomme",ReverseFill:true,Marketable:InventoryPropertyFilter.Yes,CanBeHq:InventoryPropertyFilter.No,Equipment:EquipmentSlots.MainHand|EquipmentSlots.OffHand),new InventorySortZone("Keep",50,5,Preserve:true)})};
var sortTemplate=InventorySortTemplates.Create(sortProfile,sortScope,"Modèle test");
var configuration=new Configuration{CraftPresets=[preset],ItemUsagesOnHover=true,MarketPricesOnHover=true,ItemContextMenuEnabled=true,InventorySortProfiles=[sortProfile],InventorySortTemplates=[sortTemplate]};
configuration.Cache=configuration.Cache.With(TenshoAssistant.Caching.CacheArea.Prices,TenshoAssistant.Caching.CacheRetention.SevenDays)
    .With(TenshoAssistant.Caching.CacheArea.Quests,TenshoAssistant.Caching.CacheRetention.OneDay);
foreach(var handling in new[]{TypeNameHandling.None,TypeNameHandling.Auto,TypeNameHandling.Objects})
{
    // Local generated fixture only; never deserialize external JSON with type names enabled.
    var settings=new JsonSerializerSettings{TypeNameHandling=handling};
    var encoded=JsonConvert.SerializeObject(configuration,settings);
    var decoded=JsonConvert.DeserializeObject<Configuration>(encoded,settings) ?? throw new Exception("Missing configuration");
    if(decoded.Cache.Prices!=TenshoAssistant.Caching.CacheRetention.SevenDays || decoded.Cache.Quests!=TenshoAssistant.Caching.CacheRetention.OneDay)
        throw new Exception("Cache policy roundtrip mismatch");
    var restored=decoded.CraftPresets?.Single() ?? throw new Exception("Missing sequence");
    if(decoded.Version!=1 || !CraftPresets.Valid(restored) || restored.Id!=preset.Id || restored.RecipeId!=2819 || restored.CraftTypeId!=7
        || !restored.Actions.SequenceEqual(preset.Actions) || restored.SavedAt!=preset.SavedAt || !decoded.ItemUsagesOnHover || !decoded.MarketPricesOnHover || !decoded.ItemContextMenuEnabled)throw new Exception("Serialization mismatch");
    var legacy=JsonConvert.DeserializeObject<Configuration>("{\"Version\":1}",settings) ?? throw new Exception("Missing legacy configuration");
    var restoredSort=InventorySortRules.Load(decoded.InventorySortProfiles,sortScope,InventorySortField.Name,false);
    if(restoredSort.Criteria.Count!=2 || !restoredSort.Criteria.SequenceEqual(sortProfile.Criteria) || !restoredSort.Zones.SequenceEqual(sortProfile.Zones)
        || legacy.InventorySortProfiles?.Count>0)throw new Exception("Sort profile serialization mismatch");
    var restoredTemplate=decoded.InventorySortTemplates?.Single() ?? throw new Exception("Missing sort template");
    InventorySortTemplates.Validate(restoredTemplate);
    if(restoredTemplate.Id!=sortTemplate.Id || !restoredTemplate.Criteria.SequenceEqual(sortTemplate.Criteria) || !restoredTemplate.Zones.SequenceEqual(sortTemplate.Zones)
        || legacy.InventorySortTemplates?.Count>0)throw new Exception("Sort template serialization mismatch");
    if(legacy.ItemUsagesOnHover || legacy.MarketPricesOnHover || legacy.ItemContextMenuEnabled)throw new Exception("Missing fields enabled item integration unexpectedly.");
    Console.WriteLine($"PASS Newtonsoft.Json {typeof(JsonConvert).Assembly.GetName().Version}, mode {handling}: configuration v1, recipe ids, immutable actions and UTC date roundtrip");
}
