// 0.872 (X-73.16): shopkeepers placed when their shop is opened, for the vendors no game sheet places.
using TenshoAssistant.Discoveries;
internal static class ShopNpc872Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Shop NPC: kept with its place, once per opening, behind the item-sources setting; sheet places first",() =>
        {
            var store=new DiscoveryStore();
            Check(DiscoveryStore.HasPlace(DiscoveryKind.NpcPlace),"has a place");
            Check(store.SeeSource(DiscoveryKind.NpcPlace,1000,0,1,DateTimeOffset.UtcNow,128,11,new System.Numerics.Vector3(10,0,20)),"with its place");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs"));
            Check(watcher.Contains("ObjectKind:Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventNpc") && watcher.Contains("npc.BaseId==shopNpcSeen || map==0"),"event NPC, once, with a map");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Discoveries.cs"));
            Check(page.Contains("vendor.Locations.Count>0 ? vendor.Locations:SeenNpcPlace(vendor.NpcId)"),"sheet first");
        }),
    ];
}
