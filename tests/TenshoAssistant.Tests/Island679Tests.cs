using TenshoAssistant.Maps;

// 0.679 (user reports): on the Island Sanctuary, a marker placed on another version of its map, a pad guide taken for a window, two NPCs with no icon.
internal static class Island679Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static MapDefinition Map(uint id,uint range,uint territory=1055,int offset=-175)=>new(id,territory,"r","z","",0,100,OffsetX:offset,OffsetY:138,MarkerRange:range);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Island map: a marker on another version of the land counts, not one on another floor or zone",() =>
        {
            var shown=Map(774,533);
            var catalog=new MapCatalog([Map(772,533),Map(773,533),shown,Map(775,533),Map(780,533),Map(794,556),Map(800,533,territory:1),Map(801,533,offset:0)]);
            var same=catalog.SameLand(shown);
            foreach(var id in new uint[]{772,773,774,775,780})Check(same.Contains(id),"version "+id);
            Check(!same.Contains(794u),"the caverns have their own marker range");
            Check(!same.Contains(800u),"another territory");
            Check(!same.Contains(801u),"another frame");
        }),
        ("Island map: a map with no marker range is only itself",() =>
        {
            var alone=Map(10,0);
            var same=new MapCatalog([alone,Map(11,0)]).SameLand(alone);
            Check(same.Count==1 && same[0]==10,"no range: nothing to compare");
        }),
        ("Island map: the map draws the markers of the whole land",() =>
        {
            var view=Read("MainWindow.MapView.cs");
            Check(view.Contains("liveEvents.Where(marker=>eventMaps.Contains(marker.MapId))"),"events");
            Check(!view.Contains("liveEvents.Where(marker=>marker.MapId==map.MapId)"),"not one map");
        }),
        ("Island modes: the pad guide is not a window in front of the map, and a real island window still is",() =>
        {
            Check(TenshoAssistant.UI.GameWindows.Ignored("MJIPadGuide"),"pad guide");
            Check(!TenshoAssistant.UI.GameWindows.Ignored("MJICraftSchedule"),"the agenda stays a window");
        }),
        ("Island NPCs: Laracheteuse stands where the game was read, on a map the catalogue knows",() =>
        {
            var place=TenshoAssistant.Npcs.NpcShopLinks.ObservedPlaces.Single(item=>item.Npc==1043464);
            Check(place.Territory==1055 && place.Map==773,"the island");
            Check(Math.Abs(place.X+266.316f)<0.01f && Math.Abs(place.Z-207.874f)<0.01f,"the position read");
            Check(Read("Npcs/NpcVendorLoader.Dalamud.cs").Contains("if(locations.ContainsKey(place.Npc)"),"a position from the game files would win");
        }),
        ("Island NPCs: the fields, the pasture and the guide are placed too, and the exchange opens the shops priced in island currencies",() =>
        {
            foreach(var id in new uint[]{1043465,1043466,1043080})Check(TenshoAssistant.Npcs.NpcShopLinks.ServiceNpcs.Contains(id),"npc "+id);
            Check(TenshoAssistant.Npcs.NpcShopLinks.ObservedTalkShops[721685].Count==8,"the exchange's shops");
        }),
        ("Island NPCs: the two service NPCs are placed with their own name, and no sale is made up",() =>
        {
            Check(TenshoAssistant.Npcs.NpcShopLinks.ServiceNpcs.Contains(1043463u) && TenshoAssistant.Npcs.NpcShopLinks.ServiceNpcs.Contains(1043464u),"both");
            Check(Read("Npcs/NpcVendorLoader.Dalamud.cs").Contains("foreach(var id in NpcShopLinks.ServiceNpcs)vendorShops.TryAdd(id,[]);"),"no shop added");
            Check(Read("MainWindow.MapView.cs").Contains("if(vendor.ShopIds.Count==0)return name;"),"the label says no shop line");
        }),
    ];
}
