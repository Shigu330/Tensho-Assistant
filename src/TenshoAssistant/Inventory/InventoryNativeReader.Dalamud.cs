using Dalamud.Game.Inventory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace TenshoAssistant.Inventory;

internal static class InventoryNativeReader
{
    internal static unsafe InventorySnapshot Capture(IGameInventory inventory, InventoryOwnerId owner,
        GameInventoryType[] types, string source)
    {
        var observedAt = DateTimeOffset.UtcNow;
        var containers = new List<InventoryContainerSnapshot>(types.Length);
        var displayOrder = ReadDisplayOrder(owner, types);
        var manager = InventoryManager.Instance();
        foreach (var bag in types)
        {
            var nativeType = (InventoryType)(int)bag;
            var native = manager == null ? null : manager->GetInventoryContainer(nativeType);
            var before = ReadEvidence(native, nativeType);
            var location = new InventoryLocation(owner, new InventoryContainerId((int)bag));
            if (!before.CanRead)
            {
                containers.Add(new InventoryContainerSnapshot(location, InventoryCoverage.Unknown, []));
                continue;
            }
            var originalBuffer = native->Items;
            // Span is borrowed from the game; consume here and retain only managed primitives.
            var slots = inventory.GetInventoryItems(bag);
            var stacks = new List<InventoryStack>(slots.Length);
            var seenSlots = new HashSet<uint>();
            foreach (ref readonly var slot in slots)
            {
                if (slot.IsEmpty) continue;
                if (slot.ContainerType != bag || slot.InventorySlot >= slots.Length || !seenSlots.Add(slot.InventorySlot)
                    || slot.BaseItemId == 0 || slot.Quantity <= 0)
                    throw new InvalidOperationException("Inventory slot inconsistent with requested bag.");
                stacks.Add(new InventoryStack(new InventoryItem(new InventoryItemId(slot.BaseItemId), slot.IsHq), slot.Quantity,
                    checked((int)slot.InventorySlot), displayOrder.GetValueOrDefault(((int)bag, (int)slot.InventorySlot), -1)));
            }
            var afterManager = InventoryManager.Instance();
            var afterNative = afterManager == null ? null : afterManager->GetInventoryContainer(nativeType);
            var after = ReadEvidence(afterNative, nativeType);
            var stable = manager == afterManager && native == afterNative && afterNative != null
                && originalBuffer == afterNative->Items;
            var coverage = InventoryReadEvidence.GetCoverage(before, after, slots.Length, stable);
            // If the container unloaded during capture, discard its copied values as well.
            containers.Add(new InventoryContainerSnapshot(location, coverage,
                coverage == InventoryCoverage.Unknown ? [] : stacks));
        }
        return new InventorySnapshot(observedAt, source, containers);
    }

    private static unsafe Dictionary<(int Container, int Slot), int> ReadDisplayOrder(InventoryOwnerId owner,
        GameInventoryType[] types)
    {
        var result = new Dictionary<(int, int), int>();
        var module = ItemOrderModule.Instance();
        if (module == null || types.Length == 0) return result;
        var first = (int)types[0];
        ItemOrderModuleSorter* sorter = first switch
        {
            0 => module->InventorySorter,
            4000 => module->SaddleBagSorter,
            4100 => module->PremiumSaddleBagSorter,
            10000 when module->ActiveRetainerId == owner.Value => module->GetActiveRetainerSorter(),
            _ => null,
        };
        var expected = first switch { 0 => 140, 10000 => 175, 4000 or 4100 => 70, _ => 0 };
        if (sorter == null || expected == 0 || sorter->Items.Count != expected
            || sorter->SortFunctionIndex != -1) return result;
        var perContainer = first == 10000 ? 25 : 35;
        for (var index = 0; index < expected; index++)
        {
            var entry = sorter->Items[index].Value;
            if (entry == null || entry->Slot >= perContainer || entry->Page >= expected / perContainer
                || !result.TryAdd((first + entry->Page, entry->Slot), index))
                return new();
        }
        return result;
    }

    private static unsafe InventoryReadEvidence ReadEvidence(InventoryContainer* container, InventoryType expected) =>
        container == null ? default
            : new InventoryReadEvidence(container->IsLoaded, container->Items != null,
                container->Type == expected, container->Size);

}
