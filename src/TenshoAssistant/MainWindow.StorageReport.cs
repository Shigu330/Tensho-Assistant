using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// g, I-10 (0.348): Storage › Report — stacks that could be merged (live bags; saved saddlebags and retainers of the logged-in
// character) and armoury pieces used by no gear set (saved armoury, gear sets read from the game on request). A report only: nothing is
// moved (user's choice). Rebuilt only when one of its sources changes.
internal sealed partial class MainWindow
{
    internal Func<GearsetReader?>? Gearsets { get; set; }
    internal Func<uint,uint>? StackSizeOf { get; set; }
    // g, C-03 (0.357): items the armoire takes (Cabinet sheet, set by Plugin.cs).
    internal Func<IReadOnlySet<uint>>? CabinetItems { get; set; }
    private IReadOnlyList<ArmoireCandidate> storageArmoire=[];
    private DateTimeOffset? storageArmoireAt;
    // g, C-07 (0.358): glamour dresser items that look the same (Item models, set by Plugin.cs).
    internal Func<uint,ItemLook?>? ItemLookOf { get; set; }
    // Dye channels copied with the groups (no game data lookups in Draw).
    private IReadOnlyList<(uint Id,byte Dyes)[]> storageSameLook=[];
    private DateTimeOffset? storageDresserAt;
    private (InventorySnapshot? Live,BankState Bank,GearsetItems? Sets,ulong Character)? storageReportKey;
    private IReadOnlyList<MergeableStack> storageMergeable=[];
    private IReadOnlyList<InventoryStack> storageOutside=[];
    private DateTimeOffset? storageArmouryAt;

    private void DrawStorageReport()
    {
        ImGui.TextWrapped(localization.Get("StorageReport.Help"));
        var character=bankBridge.State.CharacterId;
        if(character==0){ImGui.TextDisabled(localization.Get("BankUi.LoginBank"));return;}
        var reader=Gearsets?.Invoke();
        reader?.Want();
        var sets=reader?.State is { } read && read.Character==character ? read:null;
        var key=(inventory.Current,bank.State,sets,character);
        if(storageReportKey!=key)
        {
            var storages=new List<(string,IEnumerable<InventoryStack>)>();
            if(inventory.Current is { } live)
                storages.Add((localization.Get("BankUi.Bags"),live.Containers.Values.Where(container=>container.Location.Owner.Value==character
                    && container.Location.Container.Value is >= 0 and <= 3 && container.Coverage!=InventoryCoverage.Unknown).SelectMany(container=>container.Stacks)));
            foreach(var entry in bank.State.Entries.Where(entry=>entry.Key.CharacterId==character
                && entry.Key.Kind is BankStorageKind.Retainer or BankStorageKind.Saddlebag or BankStorageKind.PremiumSaddlebag))
                storages.Add((BankEntryName(entry),entry.Snapshot.Containers.Values.Where(container=>container.Coverage!=InventoryCoverage.Unknown).SelectMany(container=>container.Stacks)));
            storageMergeable=StorageReport.Mergeable(storages,id=>StackSizeOf?.Invoke(id) ?? 0);
            var armoury=bank.State.Entries.FirstOrDefault(entry=>entry.Key.CharacterId==character && entry.Key.Kind==BankStorageKind.Armoury);
            storageOutside=sets is null || armoury is null ? []:StorageReport.OutsideGearsets(armoury.Snapshot.Containers.Values,sets.Items);
            storageArmouryAt=armoury?.Snapshot.ObservedAt;
            // Armoire candidates: bags, armoury (gear set pieces left out), saddlebags, retainers and the glamour dresser's own slots.
            if(CabinetItems?.Invoke() is { Count:>0 } cabinetItems)
            {
                var armoireEntry=bank.State.Entries.FirstOrDefault(entry=>entry.Key.CharacterId==character && entry.Key.Kind==BankStorageKind.Armoire);
                var inArmoire=armoireEntry?.Snapshot.Containers.Values.SelectMany(container=>container.Stacks).Select(stack=>stack.Item.Id.Value).ToHashSet();
                var places=new List<(string,IEnumerable<InventoryStack>)>(storages);
                // Unknown gear sets: no armoury piece is offered (it may belong to a set).
                foreach(var entry in bank.State.Entries.Where(entry=>entry.Key.CharacterId==character
                    && (entry.Key.Kind==BankStorageKind.GlamourDresser || entry.Key.Kind==BankStorageKind.Armoury && sets is not null)))
                    places.Add((BankEntryName(entry),entry.Snapshot.Containers.Values.Where(container=>container.Coverage!=InventoryCoverage.Unknown)
                        .SelectMany(container=>container.Stacks).Where(stack=>entry.Key.Kind!=BankStorageKind.GlamourDresser || stack.Slot<StorageReport.DresserSlots)));
                storageArmoire=StorageReport.ForArmoire(places,cabinetItems,inArmoire,sets?.Items);
                storageArmoireAt=armoireEntry?.Snapshot.ObservedAt;
            }
            else{storageArmoire=[];storageArmoireAt=null;}
            // The dresser's own slots only (outfit pieces are numbered after them).
            var dresser=bank.State.Entries.FirstOrDefault(entry=>entry.Key.CharacterId==character && entry.Key.Kind==BankStorageKind.GlamourDresser);
            storageDresserAt=dresser?.Snapshot.ObservedAt;
            storageSameLook=dresser is null || ItemLookOf is not { } lookOf ? []:StorageReport.SameLook(dresser.Snapshot.Containers.Values
                .SelectMany(container=>container.Stacks).Where(stack=>stack.Slot<StorageReport.DresserSlots).Select(stack=>stack.Item.Id.Value),lookOf)
                .Select(group=>group.Select(id=>(id,lookOf(id)?.Dyes ?? (byte)0)).ToArray()).ToArray();
            storageReportKey=key;
        }
        var items=itemCatalog.State.Catalog;var size=new Vector2(ImGui.GetTextLineHeight());
        if(TenshoAssistant.UI.UiComponents.Header(localization.Format("StorageReport.Merge",storageMergeable.Sum(entry=>entry.Freed)),ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(storageMergeable.Count==0)ImGui.TextDisabled(localization.Get("StorageReport.MergeNone"));
            foreach(var entry in storageMergeable.Take(100))
            {
                DrawItemIcon(items?.Find(entry.ItemId),size);ImGui.SameLine();
                ImGui.TextUnformatted(localization.Format("StorageReport.MergeLine",items?.Find(entry.ItemId)?.Name ?? $"#{entry.ItemId}",entry.Hq ? " HQ":"",
                    entry.Storage,entry.Stacks,entry.Quantity.ToString("N0",Numbers),entry.Freed));
            }
            if(storageMergeable.Count>100)ImGui.TextDisabled(localization.Format("Events.More",storageMergeable.Count-100));
        }
        if(TenshoAssistant.UI.UiComponents.Header(localization.Format("StorageReport.Outside",storageOutside.Count),ImGuiTreeNodeFlags.DefaultOpen))
        {
            if(reader?.Unavailable==true)ImGui.TextDisabled(localization.Get("StorageReport.SetsUnavailable"));
            else if(sets is null)ImGui.TextDisabled(localization.Get("StorageReport.SetsLoading"));
            else if(storageArmouryAt is null)ImGui.TextDisabled(localization.Get("StorageReport.NoArmoury"));
            else
            {
                ImGui.TextDisabled(localization.Format("StorageReport.OutsideHelp",sets.Sets,storageArmouryAt.Value.ToLocalTime().ToString("g")));
                if(storageOutside.Count==0)ImGui.TextDisabled(localization.Get("StorageReport.OutsideNone"));
                foreach(var stack in storageOutside.Take(150))
                {
                    var definition=items?.Find(stack.Item.Id.Value);
                    DrawItemIcon(definition,size);ImGui.SameLine();
                    ImGui.TextUnformatted((definition?.Name ?? $"#{stack.Item.Id.Value}")+(stack.Item.IsHighQuality ? " HQ":"")
                        +(definition is { ItemLevel:>1 } leveled ? $"  ({localization.Format("StorageReport.ItemLevel",leveled.ItemLevel)})":""));
                }
                if(storageOutside.Count>150)ImGui.TextDisabled(localization.Format("Events.More",storageOutside.Count-150));
            }
        }
        if(TenshoAssistant.UI.UiComponents.Header(localization.Format("StorageReport.Armoire",storageArmoire.Count)+"###StorageArmoire",ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextDisabled(storageArmoireAt is { } armoireAt ? localization.Format("StorageReport.ArmoireHelp",armoireAt.ToLocalTime().ToString("g"))
                :localization.Get("StorageReport.ArmoireNotRead"));
            if(storageArmoire.Count==0)ImGui.TextDisabled(localization.Get("StorageReport.ArmoireNone"));
            foreach(var entry in storageArmoire.Take(150))
            {
                var definition=items?.Find(entry.ItemId);
                DrawItemIcon(definition,size);ImGui.SameLine();
                ImGui.TextUnformatted(localization.Format("StorageReport.ArmoireLine",definition?.Name ?? $"#{entry.ItemId}",entry.Hq ? " HQ":"",entry.Storage));
                if(entry.InArmoire==true){ImGui.SameLine();ImGui.TextColored(Accent,localization.Get("StorageReport.ArmoireSpare"));}
            }
            if(storageArmoire.Count>150)ImGui.TextDisabled(localization.Format("Events.More",storageArmoire.Count-150));
        }
        if(TenshoAssistant.UI.UiComponents.Header(localization.Format("StorageReport.SameLook",storageSameLook.Count)+"###StorageSameLook",ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.TextDisabled(storageDresserAt is { } dresserAt ? localization.Format("StorageReport.SameLookHelp",dresserAt.ToLocalTime().ToString("g"))
                :localization.Get("StorageReport.NoDresser"));
            if(storageDresserAt is not null && storageSameLook.Count==0)ImGui.TextDisabled(localization.Get("StorageReport.SameLookNone"));
            foreach(var group in storageSameLook.Take(100))
            {
                DrawItemIcon(items?.Find(group[0].Id),size);ImGui.SameLine();
                ImGui.TextUnformatted(string.Join("  ·  ",group.Select(entry=>(items?.Find(entry.Id)?.Name ?? $"#{entry.Id}")
                    +(entry.Dyes>0 ? " "+localization.Format("StorageReport.Dyes",entry.Dyes):""))));
            }
            if(storageSameLook.Count>100)ImGui.TextDisabled(localization.Format("Events.More",storageSameLook.Count-100));
        }
        ImGui.TextDisabled(localization.Get("StorageReport.Nothing"));
    }
}
