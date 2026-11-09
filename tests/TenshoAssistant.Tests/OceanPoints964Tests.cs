using TenshoAssistant.Discoveries;

// X-85 V2 and V6 (0.964): points of each catch from the stop's FishData, noted and shown; the voyage's scrips logged at the end for proof.
internal static class OceanPoints964Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean points 964: catch points from the zone data, kind, panel, result log",() =>
        {
            Check((int)DiscoveryKind.FishPoints==36,"kind");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/OceanVoyageReader.Dalamud.cs"));
            Check(reader.Contains("if(nq+hq==was.Count+1 && points>was.Points)CatchPoints?.Invoke(item,points-was.Points);"),"one catch, its points");
            Check(reader.Contains("Ocean voyage result: route {0}, points {1}, experience {2}, scrip1 {3}, scrip2 {4}."),"result logged");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.OceanVoyagePanel.cs")).Contains("Find(Discoveries.DiscoveryKind.FishPoints,fishId,1)"),"panel");
            Check(File.ReadAllText(Path.GetFullPath("tools/discoveries-merge.py")).Contains("KINDS=set(range(1,38))-{26,27}"),"merge tool");
        }),
    ];
}
