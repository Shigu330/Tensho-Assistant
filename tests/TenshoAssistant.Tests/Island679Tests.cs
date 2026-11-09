using TenshoAssistant;
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
        ("Island map: the five kinds are in the map's display menu, once each (All and None: MapLayersAll902Tests)",() =>
        {
            var view=Read("MainWindow.MapView.cs");
            var popup=view.IndexOf("BeginPopup(\"MapViewLayers\")",StringComparison.Ordinal);
            foreach(var kind in new[]{"Plants","Wood","Rocks","Earth","Fishing"})
            {
                var box=view.IndexOf($"SettingBox(\"MapView.ShowIsland{kind}\"",StringComparison.Ordinal);
                Check(popup>=0 && box>popup && box<popup+4000,kind+" in the display menu");
                Check(view.LastIndexOf($"SettingBox(\"MapView.ShowIsland{kind}\"",StringComparison.Ordinal)==box,kind+" once");
            }
        }),
        ("Island map: a place of one kind at the same spot is drawn once, and a far one or another kind stays apart",() =>
        {
            var points=IslandResources.Points([(37559,236,88,75),(37570,236,89,80),(37551,20,0,100),(37561,20,0,100),(37554,237,88,80)]);
            Check(points.Count==3,"sand and clay make one, the palm one, the rock one");
            var earth=points.Single(point=>point.Kind==IslandResourceKind.Earth);
            Check(earth.Items.Count==2 && earth.Radius==80,"both items, the wider radius");
            Check(points.Single(point=>point.Kind==IslandResourceKind.Wood).Items.Count==2,"two items of one place");
            Check(points.Any(point=>point.Kind==IslandResourceKind.Rocks),"another kind beside it stays");
        }),
        ("Island map: each kind has its own switch, and an unknown item stays while any kind is on",() =>
        {
            var settings=new MapViewSettings{ShowIslandRocks=false};
            Check(!settings.ShowsIslandKind(IslandResourceKind.Rocks) && settings.ShowsIslandKind(IslandResourceKind.Wood),"own switch");
            Check(settings.ShowsIslandKind(null),"unknown, others on");
            settings.ShowIslandPlants=settings.ShowIslandWood=settings.ShowIslandEarth=settings.ShowIslandFishing=false;
            Check(!settings.ShowsIslandKind(null),"unknown, all off");
        }),
        ("Island map: every item of the sheet read on 2026-11-06 has a kind",() =>
        {
            var items=new uint[]{37551,37552,37553,37554,37555,37556,37557,37558,37559,37560,37561,37562,37563,37564,37565,37566,37567,37568,37569,37570,37571,37572,37573,37574,37575,37576,37577,37583,37584,37591,37586,39224,39225,39226,39227,39228,39887,39888,39889,39890,39891,39892,39893,41630,41631,41632,41633,41634};
            Check(items.Length==48,"48 rows");
            foreach(var item in items)Check(IslandResources.KindOf(item) is not null,"item "+item);
        }),
        ("Island map: a marker with no text takes the name of what stands there, within reach",() =>
        {
            IslandPlace[] places=[new(-281,244,"panneau de construction : installation 1"),new(-253.9f,238.7f,"panneau de construction : installation 2"),new(0,0,"")];
            Check(IslandResources.NearestName(places,-281.5f,244.5f)=="Panneau de construction : installation 1","the nearest, first letter raised");
            Check(IslandResources.NearestName(places,-200,200) is null,"nothing near");
            Check(IslandResources.NearestName(places,0,0) is null,"an unnamed row is no name");
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
