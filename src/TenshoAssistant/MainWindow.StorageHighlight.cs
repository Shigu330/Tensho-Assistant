using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-54 (0.820, user request): in an open storage other than the bags, the items the crafting and workshop lists still need, framed.
internal sealed partial class MainWindow
{
    internal Inventory.StorageHighlightReader? StorageHighlight { get; set; }
    private long storageIconsAt=-100_000;private IReadOnlySet<uint> storageItems=new HashSet<uint>();

    /// <summary>Every frame (overlay pass): the wanted icons refreshed once a second from the lists, the frames drawn from the last read.</summary>
    internal void DrawStorageHighlight()
    {
        if(windowDisposed || !configuration.ListPanelsStorageHighlight || StorageHighlight is not { } reader)return;
        var now=Environment.TickCount64;
        if(now-storageIconsAt>=1000)
        {
            storageIconsAt=now;
            // 0.839 (X-66): the items, no longer their icons: the reader tells apart items sharing an icon by the storage's own stacks.
            var items=new HashSet<uint>();
            foreach(var key in new[]{"CraftList","Workshop"})
                if(ListPanelLinesFor(key) is { } known)
                    foreach(var (item,_) in known.Lines)items.Add(item);
            storageItems=items;
        }
        if(storageItems.Count==0 || itemCatalog.State.Catalog is not { } catalog)return;
        reader.Want(storageItems,item=>catalog.Find(item)?.IconId ?? 0);
        foreach(var cell in reader.Cells)DrawGameFrame(cell.X,cell.Y,cell.Width,cell.Height);
    }
}
