using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private InventorySnapshot? sortingViewSnapshot;
    private ItemCatalog? sortingViewCatalog;
    private InventorySortResult? sortingViewResult;
    private InventorySortProfile? sortingViewRules;
    private string? sortApplyError;
    private uint sortSlotMenuItem;
    private void DrawSortPreview()
    {
        Heading(localization.Get("SortUi.Title"), localization.Get("SortUi.Subtitle"));
        var character = bankBridge.State.CharacterId;
        if (character == 0) { ImGui.TextDisabled(localization.Get("SortUi.Login")); return; }
        var entries = bank.State.Entries.Where(entry => entry.Key.CharacterId == character).ToArray();
        var open=new[]{bankBridge.State.Retainer,bankBridge.State.Saddlebag,bankBridge.State.Premium}
            .Where(context=>context is not null && context.Key.CharacterId==character).Select(context=>context!).ToArray();
        var captured=visualSort.State.Capture;
        var choices=entries.Select(entry=>(entry.Key,entry.Name)).Concat(open.Select(context=>(context.Key,context.Name))).ToList();
        if(captured?.Scope.Storage is { } capturedKey && capturedKey.CharacterId==character)choices.Add((capturedKey,captured.Name));
        var origins=choices.DistinctBy(value=>value.Key).ToArray();
        if(sortPreviewOrigin is { } selected && !origins.Any(origin=>origin.Key==selected))sortPreviewOrigin=null;
        var chosenName=sortPreviewOrigin is { } chosenKey ? origins.First(origin=>origin.Key==chosenKey).Name:localization.Get("SortUi.Personal");
        ImGui.SetNextItemWidth(260);
        if (ImGui.BeginCombo(localization.Get("SortUi.Storage"), chosenName))
        {
            try
            {
                if (ImGui.Selectable(localization.Get("SortUi.Personal"), sortPreviewOrigin is null)) sortPreviewOrigin = null;
                foreach (var origin in origins)
                    if (ImGui.Selectable($"{origin.Name}##{origin.Key}", sortPreviewOrigin == origin.Key)) {sortPreviewOrigin=origin.Key;sortApplyError=null;}
            }
            finally { ImGui.EndCombo(); }
        }
        var scope=sortPreviewOrigin is { } storage ? InventorySortScope.FromStorage(storage):InventorySortScope.Personal(character);
        var live=captured is not null && captured.Scope==scope ? captured:null;
        var context=open.FirstOrDefault(value=>value.Key==sortPreviewOrigin);
        if(scope.Storage is not null)
        {
            ImGui.BeginDisabled(visualSort.State.Pending || context?.Loaded!=true);
            try
            {
                if(ImGui.Button(localization.Get("SortUi.Read")))
                {visualSort.RequestCapture(scope,DateTimeOffset.UtcNow);sortApplyError=null;}
            }
            finally{ImGui.EndDisabled();}
            ImGui.TextWrapped(localization.Get("SortUi.ReadHelp"));
        }
        if(visualSort.State.Message is { } status)DrawServiceMessage(status,"ServiceErrors.Sort");
        var imported=entries.FirstOrDefault(entry=>entry.Key==sortPreviewOrigin);
        var snapshot=scope.Storage is null ? inventory.Current:live?.Snapshot ?? imported?.Snapshot;
        var containers=snapshot?.Containers.Values.Where(container=>container.Location.Owner.Value==scope.InventoryOwner
            && container.Location.Container.Value>=scope.FirstContainer && container.Location.Container.Value<scope.FirstContainer+scope.ContainerCount).ToArray() ?? [];
        if (containers.Length != scope.ContainerCount || containers.Any(container => container.Coverage != InventoryCoverage.Complete))
        {
            ImGui.TextWrapped(localization.Get("SortUi.Coverage"));
            return;
        }
        if (itemCatalog.State.Catalog is not { } catalog) { DrawCatalogStatus(); return; }
        var rules=DrawSortRules(scope,catalog);if(rules is null)return;
        var capacity=scope.Capacity;
        if (sortingViewResult is null || !ReferenceEquals(sortingViewSnapshot, snapshot) || !ReferenceEquals(sortingViewCatalog, catalog)
            || !ReferenceEquals(sortingViewRules,rules))
        {
            try{sortingViewResult=InventorySortRules.Build(containers,scope,rules,id=>InventorySortMetadata.FromItem(id,catalog.Find(id)));}
            catch(Exception error){ImGui.TextWrapped(localization.Get("SortUi.PreviewError"));if(developerMode)ImGui.TextWrapped(error.Message);return;}
            sortingViewSnapshot = snapshot; sortingViewCatalog = catalog;
            sortingViewRules=rules;
        }
        var preview = sortingViewResult;
        ImGui.Spacing();
        if(scope.Storage is null || live is not null)
        {
            ImGui.BeginDisabled(visualSort.State.Pending || scope.Storage is not null
                && (context?.Loaded!=true || live?.Fresh(character,DateTimeOffset.UtcNow)!=true));
            try
            {
                if(ImGui.Button(scope.Storage is null ? localization.Get("SortUi.ApplyPersonal"):localization.Get("SortUi.ApplyStorage")))
                {
                    try
                    {
                        sortApplyError=visualSort.Request(InventoryVisualSortPlan.Build(snapshot!,preview,scope),DateTimeOffset.UtcNow)
                            ? null:"SortUi.Rejected";
                    }
                    catch(Exception error){sortApplyError=error.Message;}
                }
            }
            finally{ImGui.EndDisabled();}
            ImGui.TextWrapped(localization.Get("SortUi.ApplyHelp"));
        }
        else ImGui.TextWrapped(localization.Get("SortUi.Cached"));
        if(sortApplyError is { } applyError)DrawSortMessage(applyError);
        ImGui.TextDisabled(localization.Format("SortUi.Occupied",preview.Stacks.Count,capacity));
        if(scope.Storage is not null)ImGui.TextDisabled(live is null ? localization.Format("SortUi.CachedAt",snapshot!.ObservedAt.ToLocalTime().ToString("g"))
            : localization.Format("SortUi.LiveAt",live.Snapshot.ObservedAt.ToLocalTime().ToString("T")));
        const float slot = 34;
        var padding=ImGui.GetStyle().WindowPadding;
        var frameWidth=5*(slot+4)+2*padding.X+2;
        var frameHeight=(scope.SlotsPerContainer/5)*(slot+4)+ImGui.GetTextLineHeightWithSpacing()+2*padding.Y+4;
        var spacing=ImGui.GetStyle().ItemSpacing.X;
        var framesPerRow=Math.Max(1,(int)((ImGui.GetContentRegionAvail().X+spacing)/(frameWidth+spacing)));
        ImGui.TextDisabled(localization.Get("SortUi.ClickHelp"));
        var openSlotMenu=false;
        for (var bag = 0; bag < scope.ContainerCount; bag++)
        {
            if (bag % framesPerRow != 0) ImGui.SameLine();
            var visible = ImGui.BeginChild($"SortPreviewBag{bag}", new Vector2(frameWidth,frameHeight), true,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            try
            {
                if (!visible) continue;
                ImGui.TextColored(Accent,localization.Format("SortUi.Bag",bag+1,bag*scope.SlotsPerContainer+1,(bag+1)*scope.SlotsPerContainer));
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding,new Vector2(2));
                try
                {
                    if (!ImGui.BeginTable("Slots", 5, ImGuiTableFlags.SizingFixedFit)) continue;
                    try
                    {
                        for (var index = 0; index < scope.SlotsPerContainer; index++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.PushID(index);
                            try
                            {
                                var target = bag * scope.SlotsPerContainer + index;
                                var previewStack=preview.Slots is { } positions ? positions[target]:target<preview.Stacks.Count ? preview.Stacks[target]:null;
                                if (previewStack is { } positioned)
                                {
                                    var stack=positioned.Stack;
                                    var corner=ImGui.GetCursorScreenPos();
                                    DrawBankSlot(new(stack.Item.Id.Value, stack.Item.IsHighQuality ? 0 : stack.Quantity,
                                        stack.Item.IsHighQuality ? stack.Quantity : 0), slot);
                                    ImGui.SetCursorScreenPos(corner);ImGui.Dummy(new Vector2(slot));
                                    if(ImGui.IsItemClicked()){sortSlotMenuItem=stack.Item.Id.Value;openSlotMenu=true;}
                                }
                                else
                                {
                                    var corner=ImGui.GetCursorScreenPos();
                                    ImGui.GetWindowDrawList().AddRectFilled(corner,corner+new Vector2(slot),ImGui.GetColorU32(ImGuiCol.FrameBg),4);
                                    ImGui.Dummy(new Vector2(slot));
                                }
                                if(rules.Zones.FirstOrDefault(zone=>target>=zone.Start && target<zone.Start+zone.Count) is { } reserved)
                                {
                                    var min=ImGui.GetItemRectMin();
                                    ImGui.GetWindowDrawList().AddRect(min,min+new Vector2(slot),ImGui.GetColorU32(reserved.Preserve ? Success:Accent),4);
                                    if(ImGui.IsItemHovered())ImGui.SetTooltip(previewStack is { } zoneItem
                                        ? $"{catalog.Find(zoneItem.Stack.Item.Id.Value)?.Name ?? localization.Format("BankUi.Item",zoneItem.Stack.Item.Id.Value)}\n{(zoneItem.Stack.Item.IsHighQuality ? "HQ":"NQ")} : {zoneItem.Stack.Quantity:N0}\n"+localization.Format("SortUi.Slot",target+1,reserved.Name,reserved.Preserve ? localization.Get("SortUi.Protected"):"")
                                        : localization.Format("SortUi.EmptySlot",target+1,reserved.Name,reserved.Preserve ? localization.Get("SortUi.Protected"):""));
                                }
                            }
                            finally { ImGui.PopID(); }
                        }
                    }
                    finally { ImGui.EndTable(); }
                }
                finally { ImGui.PopStyleVar(); }
            }
            finally { ImGui.EndChild(); }
        }
        if(openSlotMenu)ImGui.OpenPopup("SortSlotMenu");
        DrawSortSlotMenu(scope,catalog);
    }

    // Every action only edits the draft zones; nothing is saved or applied from this menu.
    private void DrawSortSlotMenu(InventorySortScope scope,ItemCatalog catalog)
    {
        if(!ImGui.BeginPopup("SortSlotMenu"))return;
        try
        {
            var id=sortSlotMenuItem;var item=catalog.Find(id);
            var name=item?.Name ?? localization.Format("BankUi.Item",id);
            var category=item?.UiCategoryId ?? 0;
            var categoryName=catalog.Categories.FirstOrDefault(value=>value.Id==category)?.Name ?? localization.Format("SortUi.UnavailableCategory",category);
            ImGui.TextColored(Accent,localization.Format("SortUi.ItemChip",name,id));
            var zones=sortRulesDraft?.Zones ?? [];
            if(!zones.Any(zone=>!zone.Preserve))ImGui.TextDisabled(localization.Get("SortUi.NoEditableZone"));
            for(var index=0;index<zones.Count;index++)
            {
                var zone=zones[index];if(zone.Preserve)continue;
                ImGui.PushID(index);
                try
                {
                    ImGui.Separator();ImGui.TextDisabled(localization.Format("SortUi.ZoneSlots",zone.Name,zone.Start+1,zone.Start+zone.Count));
                    var items=zone.ItemIds ?? [];var categories=InventorySortRules.Categories(zone);var zoneIndex=index;
                    ImGui.BeginDisabled(items.Contains(id) || items.Count>=InventorySortRules.MaxZoneItems);
                    try{if(ImGui.Selectable(localization.Get("SortUi.AddItem")))EditSortZone(zoneIndex,value=>value with{ItemIds=SortIds(items.Append(id))});}
                    finally{ImGui.EndDisabled();}
                    if(category!=0)
                    {
                        ImGui.BeginDisabled(categories.Contains(category) || categories.Count>=InventorySortRules.MaxZoneCategories);
                        try
                        {
                            if(ImGui.Selectable(localization.Format("SortUi.AddItemCategory",categoryName)))
                                EditSortZone(zoneIndex,value=>value with{CategoryId=0,CategoryIds=SortIds(categories.Append(category))});
                        }
                        finally{ImGui.EndDisabled();}
                    }
                    var query=string.IsNullOrWhiteSpace(zone.Query) ? name:zone.Query.TrimEnd()+"; "+name;
                    ImGui.BeginDisabled(query.Length>128 || InventorySortRules.QueryTerms(zone.Query).Contains(name,StringComparer.OrdinalIgnoreCase));
                    try{if(ImGui.Selectable(localization.Get("SortUi.AddItemName")))EditSortZone(zoneIndex,value=>value with{Query=query});}
                    finally{ImGui.EndDisabled();}
                }
                finally{ImGui.PopID();}
            }
            ImGui.Separator();
            ImGui.BeginDisabled(sortRulesDraft is not { Zones.Count:<32 });
            try
            {
                if(ImGui.Selectable(localization.Get("SortUi.NewZoneForItem")) && sortRulesDraft is { } draft)
                {
                    var free=Enumerable.Range(0,scope.Capacity).FirstOrDefault(slot=>!draft.Zones.Any(zone=>slot>=zone.Start && slot<zone.Start+zone.Count),-1);
                    if(free<0)sortRulesError="SortUi.AllReserved";
                    else
                    {
                        var zone=new InventorySortZone(name.Length>80 ? name[..80]:name,free,1,ItemIds:Array.AsReadOnly(new[]{id}),ShareUnused:true);
                        sortRulesDraft=draft with{Zones=Array.AsReadOnly(draft.Zones.Append(zone).ToArray())};sortRulesError=null;
                    }
                }
            }
            finally{ImGui.EndDisabled();}
        }
        finally{ImGui.EndPopup();}
    }
}
