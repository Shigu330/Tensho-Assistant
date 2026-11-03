namespace TenshoAssistant.Items;

/// <summary>
/// g, I-12 (0.353): filters of the item search — UI category, equip level and item level ranges (0 = no bound), equipment slot, tradable
/// only, sold on the market only. Original code (ideas from Allagan Item Search and Item Search, read only). Job filter: not yet, the
/// catalogue does not keep ClassJobCategory.
/// </summary>
public sealed record ItemSearchFilter(uint Category=0,int MinEquipLevel=0,int MaxEquipLevel=0,uint MinItemLevel=0,uint MaxItemLevel=0,
    EquipmentSlots Slot=EquipmentSlots.None,bool TradableOnly=false,bool MarketableOnly=false)
{
    public static readonly ItemSearchFilter None=new();
    public bool IsEmpty=>this==None;

    public bool Matches(ItemDefinition item)
        =>(Category==0 || item.UiCategoryId==Category)
          && (MinEquipLevel<=0 || item.EquipLevel>=MinEquipLevel) && (MaxEquipLevel<=0 || item.EquipLevel<=MaxEquipLevel)
          && (MinItemLevel==0 || item.ItemLevel>=MinItemLevel) && (MaxItemLevel==0 || item.ItemLevel<=MaxItemLevel)
          && (Slot==EquipmentSlots.None || (item.Equipment & Slot)!=0)
          && (!TradableOnly || !item.Traits.HasFlag(ItemTraits.Untradable))
          && (!MarketableOnly || item.IsMarketable);
}
