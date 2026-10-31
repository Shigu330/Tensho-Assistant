using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private InventorySortProfile? sortRulesDraft;
    private InventorySortScope? sortRulesScope;
    private string? sortRulesError;
    private string sortCategoryFilter="";
    // Indexed by InventorySortField.
    private static readonly string[] SortFieldLabels=["Name","GameCategory","Quantity","QualityField","FieldItemLevel","FieldEquipLevel","FieldRarity","FieldSellPrice","FieldItemId"];
    private static readonly string[] SortTraitLabels=["TraitUnique","TraitUntradable","TraitDyeable","TraitRepairable","TraitCollectable"];
    private static readonly (EquipmentSlots Mask,string Name)[] SortEquipmentChoices=
    [ (EquipmentSlots.None,"AllItems"),(EquipmentSlots.All,"AnyEquipment"),
      (EquipmentSlots.MainHand|EquipmentSlots.OffHand,"EitherHand"),(EquipmentSlots.MainHand,"MainHand"),(EquipmentSlots.OffHand,"OffHand"),(EquipmentSlots.SoulCrystal,"SoulCrystal"),
      (EquipmentSlots.Wearable,"Wearable"),(EquipmentSlots.Head,"Head"),(EquipmentSlots.Body,"Body"),(EquipmentSlots.Hands,"Hands"),
      (EquipmentSlots.Legs,"Legs"),(EquipmentSlots.Feet,"Feet"),(EquipmentSlots.Ears,"Ears"),(EquipmentSlots.Neck,"Neck"),(EquipmentSlots.Wrists,"Wrists"),(EquipmentSlots.Ring,"Ring") ];

    private InventorySortProfile? DrawSortRules(InventorySortScope scope,ItemCatalog catalog)
    {
        if(sortRulesScope!=scope)
        {
            sortRulesScope=scope;sortRulesDraft=null;sortRulesError=null;
            try{sortRulesDraft=InventorySortRules.Load(configuration.InventorySortProfiles,scope,(InventorySortField)Math.Clamp(configuration.SortPreviewField,0,SortFieldLabels.Length-1),configuration.SortPreviewDescending);}
            catch(Exception error){sortRulesError=error.Message;}
        }
        if(sortRulesDraft is null)
        {
            if(sortRulesError is { } issue)DrawSortMessage(issue);else ImGui.TextWrapped(localization.Get("SortUi.Unavailable"));
            if(ImGui.Button(localization.Get("SortUi.Reset")))
            {sortRulesDraft=InventorySortRules.Default(scope);sortRulesError=null;}
            return null;
        }
        var draft=DrawSortTemplates(scope,sortRulesDraft);
        ImGui.TextDisabled(localization.Get("SortUi.Criteria"));
        var criteria=draft.Criteria.ToList();var criteriaChanged=false;
        for(var index=0;index<criteria.Count;index++)
        {
            ImGui.PushID($"SortCriterion{index}");
            try
            {
                var criterion=criteria[index];var sortField=(int)criterion.Field;var descending=criterion.Descending;
                ImGui.SetNextItemWidth(170);
                if(ImGui.BeginCombo("##SortField",localization.Get("SortUi."+SortFieldLabels[sortField])))
                {
                    try
                    {
                        for(var next=0;next<SortFieldLabels.Length;next++)
                            if((next==sortField || !criteria.Any(value=>(int)value.Field==next)) && ImGui.Selectable(localization.Get("SortUi."+SortFieldLabels[next]),next==sortField))
                            {sortField=next;criteriaChanged=true;}
                    }
                    finally{ImGui.EndCombo();}
                }
                ImGui.SameLine();if(ImGui.Checkbox(localization.Get("SortUi.Descending"),ref descending))criteriaChanged=true;
                criteria[index]=new((InventorySortField)sortField,descending);
                ImGui.SameLine();
                if(index>0 && ImGui.SmallButton(localization.Get("SortUi.MoveUp"))){(criteria[index-1],criteria[index])=(criteria[index],criteria[index-1]);criteriaChanged=true;}
                ImGui.SameLine();
                if(criteria.Count>1 && ImGui.SmallButton(localization.Get("SortUi.Remove"))){criteria.RemoveAt(index);criteriaChanged=true;break;}
            }
            finally{ImGui.PopID();}
        }
        if(criteria.Count<InventorySortPreview.MaximumCriteria && ImGui.SmallButton(localization.Get("SortUi.AddCriterion")))
        {criteria.Add(new(Enum.GetValues<InventorySortField>().First(value=>!criteria.Any(c=>c.Field==value))));criteriaChanged=true;}
        if(criteriaChanged){draft=draft with{Criteria=Array.AsReadOnly(criteria.ToArray())};sortRulesError=null;}

        if(ImGui.CollapsingHeader(localization.Format("SortUi.Zones",draft.Zones.Count)+"###SortZones"))
        {
            ImGui.TextWrapped(localization.Get("SortUi.ZonesHelp"));
            var zones=draft.Zones.ToList();var changed=false;
            for(var index=0;index<zones.Count;index++)
            {
                ImGui.PushID($"SortZone{index}");
                try
                {
                    var zone=zones[index];
                    if(!ImGui.TreeNode(localization.Format("SortUi.ZoneSlots",zone.Name,zone.Start+1,zone.Start+zone.Count)+"###ZoneDetails"))continue;
                    try
                    {
                        var name=zone.Name;var start=zone.Start+1;var count=zone.Count;var query=zone.Query;var quality=(int)zone.Quality;
                        var categoryId=zone.CategoryId;var categoryIds=zone.CategoryIds;var itemIds=zone.ItemIds;
                        var itemLevelMin=zone.ItemLevelMin;var itemLevelMax=zone.ItemLevelMax;var equipLevelMin=zone.EquipLevelMin;var equipLevelMax=zone.EquipLevelMax;
                        var sellMin=(int)Math.Min(zone.SellPriceMin,int.MaxValue);var sellMax=(int)Math.Min(zone.SellPriceMax,int.MaxValue);var rarity=zone.Rarity;
                        var traits=new[]{(int)zone.Unique,(int)zone.Untradable,(int)zone.Dyeable,(int)zone.Repairable,(int)zone.Collectable};
                        changed|=ImGui.InputText(localization.Get("SortUi.ZoneName"),ref name,80);
                        ImGui.SetNextItemWidth(100);changed|=ImGui.InputInt(localization.Get("SortUi.FirstSlot"),ref start);
                        ImGui.SetNextItemWidth(100);changed|=ImGui.InputInt(localization.Get("SortUi.SlotCount"),ref count);
                        var preserve=zone.Preserve;var reverse=zone.ReverseFill;var share=zone.ShareUnused;
                        var marketable=(int)zone.Marketable;var potentialHq=(int)zone.CanBeHq;var equipment=zone.Equipment;
                        if(ImGui.Checkbox(localization.Get("SortUi.Protect"),ref preserve)){changed=true;if(preserve){reverse=false;share=false;}}
                        if(preserve)ImGui.TextWrapped(localization.Get("SortUi.ProtectHelp"));
                        ImGui.BeginDisabled(preserve);
                        try
                        {
                            changed|=ImGui.Checkbox(localization.Get("SortUi.ShareUnused"),ref share);
                            changed|=ImGui.Checkbox(localization.Get("SortUi.Reverse"),ref reverse);
                            var categories=InventorySortRules.Categories(zone);
                            ImGui.TextUnformatted(localization.Get("SortUi.Categories"));
                            if(categories.Count==0)ImGui.TextDisabled(localization.Get("SortUi.AllCategories"));
                            var categoryAction=DrawSortChips(categories.Select(id=>(id,catalog.Categories.FirstOrDefault(value=>value.Id==id)?.Name ?? localization.Format("SortUi.UnavailableCategory",id))).ToArray(),true);
                            if(categoryAction.Removed is { } removedCategory)
                            {categoryId=0;categoryIds=SortIds(categories.Where(id=>id!=removedCategory));changed=true;}
                            else if(categoryAction.Raised is { } raisedCategory)
                            {
                                var ordered=categories.ToList();var at=ordered.IndexOf(raisedCategory);
                                (ordered[at-1],ordered[at])=(ordered[at],ordered[at-1]);
                                categoryId=0;categoryIds=SortIds(ordered);changed=true;
                            }
                            if(categories.Count>1)ImGui.TextDisabled(localization.Get("SortUi.CategoryPriorityHelp"));
                            if(categories.Count<InventorySortRules.MaxZoneCategories)
                            {
                                ImGui.SetNextItemWidth(250);
                                if(ImGui.BeginCombo("##AddCategory",localization.Get("SortUi.AddCategory")))
                                {
                                    try
                                    {
                                        if(ImGui.IsWindowAppearing()){sortCategoryFilter="";ImGui.SetKeyboardFocusHere();}
                                        ImGui.SetNextItemWidth(-1);
                                        ImGui.InputTextWithHint("##CategoryFilter",localization.Get("SortUi.CategoryFilterHint"),ref sortCategoryFilter,64);
                                        var filter=sortCategoryFilter.Trim();var shown=0;
                                        foreach(var value in catalog.Categories.Where(value=>!categories.Contains(value.Id)
                                            && (filter.Length==0 || System.Globalization.CultureInfo.InvariantCulture.CompareInfo.IndexOf(value.Name,filter,
                                                System.Globalization.CompareOptions.IgnoreCase|System.Globalization.CompareOptions.IgnoreNonSpace)>=0)))
                                        {
                                            shown++;
                                            if(ImGui.Selectable($"{value.Name}##{value.Id}")){categoryId=0;categoryIds=SortIds(categories.Append(value.Id));changed=true;}
                                        }
                                        if(shown==0)ImGui.TextDisabled(localization.Get("SortUi.NoCategoryMatch"));
                                    }
                                    finally{ImGui.EndCombo();}
                                }
                            }
                            ImGui.SetNextItemWidth(170);changed|=ImGui.Combo(localization.Get("SortUi.Quality"),ref quality,localization.Get("SortUi.QualityChoices"));
                            ImGui.TextUnformatted(localization.Get("SortUi.Items"));
                            if(itemIds is not { Count:>0 })ImGui.TextDisabled(localization.Get("SortUi.NoItems"));
                            else if(DrawSortChips(itemIds.Select(id=>(id,localization.Format("SortUi.ItemChip",catalog.Find(id)?.Name ?? localization.Format("BankUi.Item",id),id))).ToArray()).Removed is { } removedItem)
                            {itemIds=SortIds(itemIds.Where(id=>id!=removedItem));changed=true;}
                            changed|=ImGui.InputTextWithHint("##ZoneQuery",localization.Get("SortUi.QueryHint"),ref query,128);
                            ImGui.TextDisabled(localization.Get("SortUi.SelectionHelp"));
                            ImGui.SetNextItemWidth(170);changed|=ImGui.Combo(localization.Get("SortUi.Marketable"),ref marketable,localization.Get("SortUi.BooleanChoices"));
                            ImGui.SetNextItemWidth(170);changed|=ImGui.Combo(localization.Get("SortUi.CanHq"),ref potentialHq,localization.Get("SortUi.BooleanChoices"));
                            ImGui.SetNextItemWidth(250);
                            if(ImGui.BeginCombo(localization.Get("SortUi.Equipment"),localization.Get("SortUi."+(SortEquipmentChoices.FirstOrDefault(value=>value.Mask==equipment).Name ?? "MultipleSlots"))))
                            {
                                try{foreach(var value in SortEquipmentChoices)if(ImGui.Selectable(localization.Get("SortUi."+value.Name),equipment==value.Mask)){equipment=value.Mask;changed=true;}}
                                finally{ImGui.EndCombo();}
                            }
                            if(ImGui.TreeNode(localization.Get("SortUi.Advanced")+"###Advanced"))
                            {
                                try
                                {
                                    changed|=DrawSortRange("SortUi.ItemLevelRange",ref itemLevelMin,ref itemLevelMax,9999);
                                    changed|=DrawSortRange("SortUi.EquipLevelRange",ref equipLevelMin,ref equipLevelMax,255);
                                    changed|=DrawSortRange("SortUi.SellPriceRange",ref sellMin,ref sellMax,int.MaxValue);
                                    var rarityIndex=Math.Max(0,InventorySortRules.Rarities.ToList().IndexOf(rarity));
                                    ImGui.SetNextItemWidth(170);
                                    if(ImGui.Combo(localization.Get("SortUi.RarityFilter"),ref rarityIndex,localization.Get("SortUi.RarityChoices"))){rarity=InventorySortRules.Rarities[rarityIndex];changed=true;}
                                    for(var trait=0;trait<traits.Length;trait++)
                                    {ImGui.SetNextItemWidth(170);changed|=ImGui.Combo(localization.Get("SortUi."+SortTraitLabels[trait])+$"##Trait{trait}",ref traits[trait],localization.Get("SortUi.BooleanChoices"));}
                                    ImGui.TextDisabled(localization.Get("SortUi.AdvancedHelp"));
                                }
                                finally{ImGui.TreePop();}
                            }
                            ImGui.TextWrapped(localization.Get("SortUi.MetadataHelp"));
                        }
                        finally{ImGui.EndDisabled();}
                        zones[index]=zone with{Name=name,Start=Math.Clamp(start,1,scope.Capacity)-1,Count=Math.Clamp(count,1,scope.Capacity),Quality=(InventoryQualityFilter)quality,Query=query,Preserve=preserve,ReverseFill=reverse,
                            Marketable=(InventoryPropertyFilter)marketable,CanBeHq=(InventoryPropertyFilter)potentialHq,Equipment=equipment,CategoryId=categoryId,CategoryIds=categoryIds,ItemIds=itemIds,ShareUnused=share,
                            ItemLevelMin=itemLevelMin,ItemLevelMax=itemLevelMax,EquipLevelMin=equipLevelMin,EquipLevelMax=equipLevelMax,SellPriceMin=(uint)sellMin,SellPriceMax=(uint)sellMax,Rarity=rarity,
                            Unique=(InventoryPropertyFilter)traits[0],Untradable=(InventoryPropertyFilter)traits[1],Dyeable=(InventoryPropertyFilter)traits[2],
                            Repairable=(InventoryPropertyFilter)traits[3],Collectable=(InventoryPropertyFilter)traits[4]};
                        if(index>0 && ImGui.SmallButton(localization.Get("SortUi.Priority"))){(zones[index-1],zones[index])=(zones[index],zones[index-1]);changed=true;}
                        ImGui.SameLine();if(ImGui.SmallButton(localization.Get("SortUi.DeleteZone"))){zones.RemoveAt(index);changed=true;break;}
                    }
                    finally{ImGui.TreePop();}
                }
                finally{ImGui.PopID();}
            }
            if(zones.Count<32 && ImGui.SmallButton(localization.Get("SortUi.AddZone")))
            {
                var free=Enumerable.Range(0,scope.Capacity).FirstOrDefault(slot=>!zones.Any(zone=>slot>=zone.Start && slot<zone.Start+zone.Count),-1);
                if(free>=0){zones.Add(new(localization.Get("SortUi.NewZone"),free,1,ShareUnused:true));changed=true;}
                else sortRulesError="SortUi.AllReserved";
            }
            if(changed){draft=draft with{Zones=Array.AsReadOnly(zones.ToArray())};sortRulesError=null;}
        }
        sortRulesDraft=draft;
        try{InventorySortRules.Validate(draft,scope);}
        catch(Exception error){sortRulesError=error.Message;DrawSortMessage(sortRulesError);return null;}
        if(ImGui.Button(localization.Get("SortUi.Save")))
        {
            var oldProfiles=configuration.InventorySortProfiles;var oldField=configuration.SortPreviewField;var oldDescending=configuration.SortPreviewDescending;
            try
            {
                configuration.InventorySortProfiles=InventorySortRules.Save(oldProfiles,draft,scope);
                configuration.SortPreviewField=(int)draft.Criteria[0].Field;configuration.SortPreviewDescending=draft.Criteria[0].Descending;
                save();sortRulesError=null;
            }
            catch(Exception error)
            {
                configuration.InventorySortProfiles=oldProfiles;configuration.SortPreviewField=oldField;configuration.SortPreviewDescending=oldDescending;
                sortRulesError=TenshoAssistant.UI.UiMessages.Key(error.Message) is not null ? error.Message:"SortUi.ProfileSaveError";log.Warning(error,"Could not persist inventory sort profile.");
            }
        }
        ImGui.TextDisabled(localization.Get("SortUi.DraftHelp"));
        if(sortRulesError is { } message)DrawSortMessage(message);
        return draft;
    }

    // Two bounded inputs on one line; 0 leaves that bound open.
    private bool DrawSortRange(string key,ref int min,ref int max,int limit)
    {
        ImGui.PushID(key);
        try
        {
            var changed=false;
            ImGui.SetNextItemWidth(90);changed|=ImGui.InputInt("##Min",ref min,0);
            ImGui.SameLine();ImGui.TextUnformatted("–");ImGui.SameLine();
            ImGui.SetNextItemWidth(90);changed|=ImGui.InputInt("##Max",ref max,0);
            ImGui.SameLine();ImGui.TextUnformatted(localization.Get(key));
            min=Math.Clamp(min,0,limit);max=Math.Clamp(max,0,limit);
            return changed;
        }
        finally{ImGui.PopID();}
    }

    private static IReadOnlyList<uint>? SortIds(IEnumerable<uint> ids)
    {var values=ids.Distinct().ToArray();return values.Length==0 ? null:Array.AsReadOnly(values);}

    // Removable labels wrapped on the available width; an ordered list numbers them and offers "<" to raise one.
    private static (uint? Removed,uint? Raised) DrawSortChips(IReadOnlyList<(uint Id,string Label)> chips,bool ordered=false)
    {
        uint? removed=null,raised=null;
        var style=ImGui.GetStyle();
        var right=ImGui.GetWindowPos().X+ImGui.GetWindowContentRegionMax().X;
        for(var index=0;index<chips.Count;index++)
        {
            var label=ordered ? $"{index+1}. {chips[index].Label} ×":$"{chips[index].Label} ×";
            var raise=ordered && index>0;
            var width=ImGui.CalcTextSize(label).X+2*style.FramePadding.X
                +(raise ? ImGui.CalcTextSize("<").X+2*style.FramePadding.X+style.ItemSpacing.X:0);
            if(index>0 && ImGui.GetItemRectMax().X+style.ItemSpacing.X+width<=right)ImGui.SameLine();
            if(raise)
            {
                if(ImGui.SmallButton($"<##Raise{chips[index].Id}"))raised=chips[index].Id;
                ImGui.SameLine();
            }
            if(ImGui.SmallButton($"{label}##Chip{chips[index].Id}"))removed=chips[index].Id;
        }
        return (removed,raised);
    }

    // Edits the shared draft from the preview; validation and persistence stay in DrawSortRules.
    private void EditSortZone(int index,Func<InventorySortZone,InventorySortZone> edit)
    {
        if(sortRulesDraft is not { } draft || index<0 || index>=draft.Zones.Count)return;
        var zones=draft.Zones.ToArray();zones[index]=edit(zones[index]);
        sortRulesDraft=draft with{Zones=Array.AsReadOnly(zones)};sortRulesError=null;
    }
}
