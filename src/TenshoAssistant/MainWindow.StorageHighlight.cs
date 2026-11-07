using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-54 (0.820, user request): in an open storage other than the bags, the items the crafting and workshop lists still need, framed.
internal sealed partial class MainWindow
{
    internal Inventory.StorageHighlightReader? StorageHighlight { get; set; }
    private long storageIconsAt=long.MinValue;private IReadOnlySet<uint> storageIcons=new HashSet<uint>();

    /// <summary>Every frame (overlay pass): the wanted icons refreshed once a second from the lists, the frames drawn from the last read.</summary>
    internal void DrawStorageHighlight()
    {
        if(windowDisposed || !configuration.ListPanelsStorageHighlight || StorageHighlight is not { } reader)return;
        var now=Environment.TickCount64;
        if(now-storageIconsAt>=1000)
        {
            storageIconsAt=now;
            var icons=new HashSet<uint>();
            if(itemCatalog.State.Catalog is { } catalog)
            {
                foreach(var key in new[]{"CraftList","Workshop"})
                    if(ListPanelLinesFor(key) is { } known)
                        foreach(var (item,_) in known.Lines)
                            if(catalog.Find(item) is { IconId:not 0 } found)
                            {icons.Add(found.IconId);icons.Add(1_000_000+found.IconId);}
            }
            storageIcons=icons;
        }
        if(storageIcons.Count==0)return;
        reader.Want(storageIcons);
        foreach(var cell in reader.Cells)DrawGameFrame(cell.X,cell.Y,cell.Width,cell.Height);
    }
}
