using TenshoAssistant.Gathering;

// 0.518 (user report): a point « open now » may still be closed to this character: a tome not read, a level too low.
internal static class GatherPointAccess518Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Point access: the tome of the point's own job; unknown says nothing",() =>
        {
            var known=new GatherKnowledge(85,70,new HashSet<int>{1,2},new HashSet<int>{7});
            var tome=new GatherPointNeed(GatherPointAccess.Miner,7,"Geological Folklore");
            Check(GatherPointAccess.Of(tome,known) is {TomeMissing:"Geological Folklore",JobLevel:85},"the miner has not read tome 7, the botanist's does not count");
            Check(GatherPointAccess.Of(tome with {JobId=GatherPointAccess.Botanist},known) is {TomeMissing:null,JobLevel:70},"read by the botanist");
            Check(GatherPointAccess.Of(new(GatherPointAccess.Miner,null,null),known) is {TomeMissing:null},"no tome asked");
            Check(GatherPointAccess.Of(tome,null) is null && GatherPointAccess.Of(null,known) is null,"nothing read, nothing said");
            Check(GatherPointAccess.Of(new(0,null,null),known) is null,"not a miner or a botanist");
            Check(GatherPointAccess.Of(tome,known with {MinerLevel=0})!.JobLevel is null,"a level of zero is unknown");
            Check(GatherPointAccess.JobOf(0)==16 && GatherPointAccess.JobOf(1)==16 && GatherPointAccess.JobOf(2)==17 && GatherPointAccess.JobOf(3)==17 && GatherPointAccess.JobOf(4)==0,"the types");
        }),
        ("Point access: both map tooltips and the edge markers use it; read on the framework thread and guarded",() =>
        {
            var map=Read("MainWindow.MapView.cs");
            Check(map.Split("DrawPointAvailability(point);").Length==3,"both tooltips");
            Check(map.Contains("\"MapView.TimedOpenNoTome\"") && map.Contains("\"MapView.TomeMissing\"") && map.Contains("\"MapView.PointLevelLow\""),"said");
            Check(map.Contains("point.ActiveAt(minutes) && GatherAccess?.Invoke(point.BaseId)?.TomeMissing is null"),"no edge marker for a point closed to the player");
            Check(map.Contains("ImGui.GetColorU32(TimedPointColor(timedPoint))"),"nor a green clock");
            var reader=Read("Gathering/GatherKnowledgeReader.Dalamud.cs");
            Check(reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);") && reader.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update)") && reader.Contains("failed=true;known=null;"),"owned, fails to unknown");
            Check(Read("Plugin.cs").Contains("gatherKnowledge?.Dispose()"),"cleaned up");
        }),
    ];
}
