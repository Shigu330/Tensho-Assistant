using TenshoAssistant.Maps;

// P11 (0.803, user request): undiscovered parts of the map as blank parchment (docs/evidence/0.803-map-discovery-layout.md).
internal static class MapDiscovery803Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map discovery: regions from the flag, cells of 128 px, three channels each",() =>
        {
            Check(MapDiscovery.Regions(16382).SequenceEqual(Enumerable.Range(1,13)),"Central Shroud: 1 to 13");
            Check(MapDiscovery.Slot(1)==(0,0,1) && MapDiscovery.Slot(3)==(128,0,0) && MapDiscovery.Slot(12)==(0,128,0) && MapDiscovery.Slot(35)==(384,256,2) && MapDiscovery.Slot(36) is null,"grid");
            Check(MapDiscovery.Files("ui/map/f1f1/00/f1f100_m.tex")==("ui/map/f1f1/00/f1f100m_m.tex","ui/map/f1f1/00/f1f100d.tex") && MapDiscovery.Files("x.tex")==("",""),"files");
        }),
        ("Map discovery: unknown is not « undiscovered »; coverage takes the region's channel",() =>
        {
            Check(MapDiscovery.Undiscovered(0b110,region=>region==1)!.SequenceEqual(new[]{2}),"one left");
            Check(MapDiscovery.Undiscovered(0b110,region=>region==1 ? true:null) is null,"not read: nothing");
            var bgra=new byte[512*384*4];
            // Region 4 (cell 1, green): pixel (5,6) of its cell.
            bgra[((6)*512+128+5)*4+1]=200;
            var mask=MapDiscovery.Coverage(bgra,512,384,[4]);
            Check(mask[6*128+5]==200 && mask.Count(value=>value!=0)==1,"its pixel only");
            Check(MapDiscovery.Coverage(bgra,512,384,[3,5]).All(value=>value==0),"other channels of the cell: nothing");
        }),
        ("Map discovery: the parchment shows only where the regions are",() =>
        {
            var parchment=Enumerable.Repeat((byte)100,8*8*4).ToArray();
            var coverage=new byte[128*128];for(var i=0;i<64;i++)coverage[i]=255;
            var image=MapDiscovery.Overlay(parchment,8,coverage,128);
            Check(image[3]==255 && image[(127*128+127)*4+3]==0,"opaque on the region, clear elsewhere");
        }),
        ("Map discovery: read from the game's tables, built off the frame, behind its setting",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Maps/MapLiveReader.Dalamud.cs").Contains("manager->MapsWithUpTo16Regions") && !Read("Maps/MapLiveReader.Dalamud.cs").Contains("IsMapRegionDiscovered("),"fields, no function");
            Check(Read("MainWindow.MapDiscovery.cs").Contains("_=Task.Run(()=>"),"off the frame");
            Check(new TenshoAssistant.MapViewSettings().HideUndiscovered,"on by default");
        }),
    ];
}
