using System.Numerics;
using TenshoAssistant.Maps;

// 0.525 (user reports): party members jumping on the map; lag when the plugin loads.
internal static class PartyAndLoad525Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Party on the map: a marker glides to its new position, frame by frame; a teleport is not glided over",() =>
        {
            var smoother=new MapPartySmoother();var start=new Vector3(0,0,0);var target=new Vector3(10,0,0);
            Check(smoother.At("A",start,0.016f)==start,"first seen: where it is");
            var first=smoother.At("A",target,0.016f);
            Check(first.X is >0 and <10,"moves a part of the way: "+first.X);
            var last=first;
            for(var frame=0;frame<120;frame++){var next=smoother.At("A",target,0.016f);Check(next.X>=last.X && next.X<=10,"never past it");last=next;}
            Check(10-last.X<0.1f,"there after two seconds: "+last.X);
            var far=new Vector3(500,0,0);
            Check(smoother.At("A",far,0.016f)==far,"a teleport");
            Check(smoother.At("B",target,0.016f)==target,"each member apart");
            Check(smoother.At("B",new Vector3(20,0,0),0)==target,"no time, no move");
            for(var member=0;member<100;member++)smoother.At("M"+member,start,0.016f);
        }),
        ("Party on the map: read ten times a second, the exact position when in view; drawn through the smoother",() =>
        {
            var reader=Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("partyAt=now.AddMilliseconds(100);") && reader.Contains("member.GameObject?.Position ?? member.Position"),"read");
            Check(Read("MainWindow.MapView.cs").Contains("mapPartySmoother.At(member.Name,member.Position,io.DeltaTime)"),"drawn");
        }),
        ("Load: the cost is said in the log; the voyage data is built off the framework thread",() =>
        {
            var plugin=Read("Plugin.cs");
            Check(plugin.Contains("GC.GetTotalPauseDuration()") && plugin.Contains("SayLoad(\"10 s later\")"),"measured");
            Check(!plugin.Contains("GC.Collect(") && !plugin.Contains("GCSettings"),"the plugin does not touch the game's collector");
            Check(Read("FreeCompany/VoyageWatcher.Dalamud.cs").Contains("Task.Run(()=>\n            {\n                try{Volatile.Write(ref catalog,BuildCatalog());}".Replace("\n",Read("FreeCompany/VoyageWatcher.Dalamud.cs").Contains("\r\n") ? "\r\n":"\n")),"off the framework thread");
        }),
    ];
}
