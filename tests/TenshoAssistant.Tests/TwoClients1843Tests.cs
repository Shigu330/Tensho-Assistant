using TenshoAssistant.Craft;
using TenshoAssistant.Inventory;
using TenshoAssistant.MyTools;

// 0.1843 (X-842, review of 0.1833 to 0.1839, minor M1, M2, M4, M5; R28.14). Behaviour on real files, two or three stores on one file as game
// clients of one profile: « My tools » sees the other client's counts and removals (a « + » no longer starts from a stale count); the crafting
// lists of two clients are never cut in silence; the window's thread waits a tenth of a second at most for the lists' lock; a gil journal read
// from its .bak is said.
internal static class TwoClients1843Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    private static void InFolder(Action<string> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-tests","two1843-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{run(folder);}
        finally{try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    private static string Tool(string id)=>$$"""{"schema":1,"id":"{{id}}","name":{"en":"Tool {{id}}"},"blocks":[{"type":"checklist","reset":"daily","entries":[{"en":"One"},{"en":"Two"}]},{"type":"counter","label":{"en":"Runs"},"target":5}]}""";

    private static int Count(ToolTemplateStore store,string id)
        =>store.Tools.FirstOrDefault(tool=>tool.Template.Id==id)?.Counts.GetValueOrDefault("1:0") ?? -1;

    private static void Settle(ToolTemplateStore store){_=store.Tools;store.FlushAsync().Wait();}

    public static IEnumerable<(string Name,Action Test)> Cases=>
    [
        ("X-842 M1: « My tools »: A sees B's count and removal; A's « + » starts from B's count, not from its stale one",()=>InFolder(folder=>
        {
            var path=Path.Combine(folder,"MyTools.json");
            var template=ToolTemplateFormat.Parse(Tool("daily")).Template!;var json=ToolTemplateFormat.Serialize(template);
            var other=ToolTemplateFormat.Parse(Tool("weekly")).Template!;
            using(var first=new ToolTemplateStore(path,(_,_)=>{}))
            {
                Check(first.Install(template,json) && first.Install(other,ToolTemplateFormat.Serialize(other)),"installed");
                first.FlushAsync().Wait();
            }
            var said=new List<string>();
            using var a=new ToolTemplateStore(path,(_,message)=>said.Add(message)){LookEveryMs=0};
            using var b=new ToolTemplateStore(path,(_,message)=>said.Add(message)){LookEveryMs=0};
            Settle(a);
            b.SetCount("daily","1:0",3);b.FlushAsync().Wait();
            Settle(a);
            Check(Count(a,"daily")==3,"A shows B's count: "+Count(a,"daily"));
            a.SetCount("daily","1:0",Count(a,"daily")+1);a.FlushAsync().Wait();
            using(var check=new ToolTemplateStore(path,(_,_)=>{}))Check(Count(check,"daily")==4,"A's « + » from B's 3: "+Count(check,"daily"));
            b.Remove("weekly");b.FlushAsync().Wait();
            Settle(a);
            Check(a.Tools.All(tool=>tool.Template.Id!="weekly"),"A shows B's removal");
            a.SetDone("daily","0:0",true,DateTimeOffset.UtcNow);a.FlushAsync().Wait();
            using(var check=new ToolTemplateStore(path,(_,_)=>{}))
                Check(check.Tools.Select(tool=>tool.Template.Id).SequenceEqual(["daily"]) && Count(check,"daily")==4,"the removed tool does not come back with A's tick");
            Check(said.Count==0,"nothing said: "+string.Join(" | ",said));
        })),
        ("X-842 M1: an edit made while the file is read again is never replaced by the file's copy",()=>InFolder(folder=>
        {
            var path=Path.Combine(folder,"MyTools.json");
            var template=ToolTemplateFormat.Parse(Tool("daily")).Template!;
            using(var first=new ToolTemplateStore(path,(_,_)=>{})){Check(first.Install(template,ToolTemplateFormat.Serialize(template)),"installed");first.FlushAsync().Wait();}
            using var a=new ToolTemplateStore(path,(_,_)=>{}){LookEveryMs=0};
            using var b=new ToolTemplateStore(path,(_,_)=>{}){LookEveryMs=0};
            b.SetCount("daily","1:0",2);b.FlushAsync().Wait();
            _=a.Tools;                       // a look is queued…
            a.SetCount("daily","1:0",5);     // …then an edit here before it ran
            a.FlushAsync().Wait();
            Check(Count(a,"daily")==5,"A's own edit stays: "+Count(a,"daily"));
            using var check=new ToolTemplateStore(path,(_,_)=>{});
            Check(Count(check,"daily")==5,"and is saved: "+Count(check,"daily"));
        })),
        ("X-842 M2: two clients' lists past 32 are all kept; past 64 nothing is saved and it is said, never cut in silence",()=>InFolder(folder=>
        {
            var path=Path.Combine(folder,"CraftLists.json");
            var said=new List<string>();
            var a=new CraftListStore(path,(_,message)=>said.Add(message));var b=new CraftListStore(path,(_,message)=>said.Add(message));
            Check(a.Lists.Count==0 && b.Lists.Count==0,"two clients, no list yet");
            for(var i=0;i<CraftLists.MaxLists;i++)Check(a.Set(i,new($"A{i}",[new(10,1)])),"A adds "+i);
            Check(b.Set(0,new("B0",[new(20,1)])),"B, whose copy is older, adds one: 33 lists with A's");
            var file=new CraftListStore(path,(_,_)=>{}).Lists;
            Check(file.Count==CraftLists.MaxLists+1 && file.Count(list=>list.Name.StartsWith('A'))==CraftLists.MaxLists && file.Any(list=>list.Name=="B0"),
                $"none cut (before: the 33rd was dropped from the file): {file.Count}");
            Check(b.Lists.Count==CraftLists.MaxLists+1,"B shows the 33");
            CraftLists.Write(path,[..Enumerable.Range(0,CraftLists.MaxListsMerged).Select(i=>new CraftList($"F{i}",[new(10,1)]))]);
            var stale=new CraftListStore(path,(_,message)=>said.Add(message));
            Check(stale.Lists.Count==CraftLists.MaxListsMerged,"64 lists read");
            var d=new CraftListStore(path,(_,message)=>said.Add(message));
            Check(d.Lists.Count==CraftLists.MaxListsMerged,"D read the 64");
            CraftLists.Write(path,[..Enumerable.Range(0,CraftLists.MaxListsMerged).Select(i=>new CraftList($"G{i}",[new(10,1)]))]);
            Check(!d.Set(0,new("F0",[new(10,2)])),"a change that would make 65 with the other client's: refused");
            Check(said.Any(message=>message=="Crafting lists could not be saved."),"said: "+string.Join(" | ",said));
            var after=new CraftListStore(path,(_,_)=>{}).Lists;
            Check(after.Count==CraftLists.MaxListsMerged && after.All(list=>list.Name.StartsWith('G')),$"the other client's 64 lists untouched ({after.Count})");
        })),
        ("X-842 M4: the crafting lists' save waits a tenth of a second at most on the window's thread, then refuses and says it",()=>InFolder(folder=>
        {
            var path=Path.Combine(folder,"CraftLists.json");
            var said=new List<string>();
            var store=new CraftListStore(path,(_,message)=>said.Add(message));
            Check(store.Lists.Count==0,"read");
            using(TenshoAssistant.FairFileLock.Hold(path+".lock",1000,error=>new IOException("held",error)))
            {
                var watch=System.Diagnostics.Stopwatch.StartNew();
                var saved=store.Set(0,new("Gear",[new(10,1)]));
                watch.Stop();
                Check(!saved && watch.ElapsedMilliseconds<1000,$"refused after {watch.ElapsedMilliseconds} ms (wait {CraftListStore.DrawLockWaitMs} ms)");
            }
            Check(said.Count==1 && said[0]=="Crafting lists could not be saved.","said once: "+string.Join(" | ",said));
            Check(store.Set(0,new("Gear",[new(10,1)])),"saved once the lock is free");
        })),
        ("X-842 M5: a gil journal read from its .bak is said; the store passes its data file's message",()=>InFolder(folder=>
        {
            var path=Path.Combine(folder,"1.json");
            var first=new GilEntry(new DateTimeOffset(2026,11,20,10,0,0,TimeSpan.Zero),100,1100,default,default);
            var second=first with{At=first.At.AddHours(1),Delta=50,Balance=1150};
            GilJournal.Write(path,[first]);GilJournal.Write(path,[first,second]);
            File.WriteAllText(path,"{ damaged");
            var heard=new List<string>();
            var read=GilJournal.Read(path,(aside,_)=>heard.Add(aside));
            Check(read.Count==1 && read[0]==first,"the previous version read: "+read.Count);
            Check(heard.Count==1,"said once");
            var store=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Inventory/GilJournalStore.cs"));
            var voyages=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/VoyageHistory.cs"));
            Check(store.Contains("GilJournal.Read(json,data.Recovered(\"The gil journal\"))",StringComparison.Ordinal)
                && voyages.Contains("VoyageHistory.Read(file,data?.Recovered(\"The voyage history\"))",StringComparison.Ordinal),"both stores pass their message");
        })),
    ];
}
