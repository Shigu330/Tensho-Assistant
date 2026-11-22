using TenshoAssistant.Craft;
using TenshoAssistant.MyTools;

// 0.1838 (X-832, security review I4, R28.14): with two game clients on one profile, the crafting lists and « My tools » followed « the last
// writer wins »: each client read the file once and wrote its whole copy back, without lock, new read nor merge; a list added in A was erased by
// any change in B, and two clients ticking their tasks of the day erased each other's ticks. Now each save holds the profile's lock, reads the
// file again and merges three-way (lists by name; tools by id, ticks and counters key by key). Here: two stores on one file, as two clients.
internal static class TwoClients1838Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static void InFolder(Action<string> run)
    {
        var folder=Path.Combine(Path.GetTempPath(),"tensho-tests","two-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try{run(folder);}
        finally{try{Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    private static string Tool(string id)=>$$"""{"schema":1,"id":"{{id}}","name":{"en":"Tool {{id}}"},"blocks":[{"type":"checklist","reset":"daily","entries":[{"en":"One"},{"en":"Two"}]},{"type":"counter","label":{"en":"Runs"},"target":5}]}""";

    public static IEnumerable<(string Name,Action Test)> Cases =>
    [
        ("Two clients 1838: behaviour: crafting lists added, changed and removed in two clients of one profile are all kept (X-832)",() => InFolder(folder =>
        {
            var path=Path.Combine(folder,"CraftLists.json");
            var said=new List<string>();
            var first=new CraftListStore(path,(_,message)=>said.Add(message));
            Check(first.Set(0,new("Gear",[new(10,5)])) && first.Set(1,new("Food",[new(20,1)])),"the profile's lists");
            var a=new CraftListStore(path,(_,message)=>said.Add(message));var b=new CraftListStore(path,(_,message)=>said.Add(message));
            Check(a.Lists.Count==2 && b.Lists.Count==2,"both clients read them");
            Check(a.Set(2,new("Added by A",[new(30,2)])),"A adds a list");
            Check(b.Set(0,new("Gear",[new(10,9)])),"B changes another (its copy did not have A's list)");
            var next=new CraftListStore(path,(_,_)=>{});
            Check(next.Lists.Select(list=>list.Name).SequenceEqual(["Gear","Food","Added by A"]) && next.Lists[0].Entries.Single().Quantity==9,
                "A's list kept, B's change kept: "+string.Join(", ",next.Lists.Select(list=>list.Name+" "+string.Join(".",list.Entries.Select(entry=>entry.Quantity)))));
            Check(b.Lists.Any(list=>list.Name=="Added by A"),"B shows A's list once it saved");
            Check(a.Set(1,null),"A removes Food");
            Check(b.Set(0,new("Gear",[new(10,11)])),"B changes Gear again (its copy still had Food)");
            var last=new CraftListStore(path,(_,_)=>{});
            Check(last.Lists.Select(list=>list.Name).SequenceEqual(["Gear","Added by A"]) && last.Lists[0].Entries.Single().Quantity==11,
                "the list A removed stays removed, B's change kept: "+string.Join(", ",last.Lists.Select(list=>list.Name)));
            Check(said.Count==0,"nothing said: "+string.Join(" | ",said));
        })),
        ("Two clients 1838: behaviour: two clients ticking « My tools » keep each other's ticks, counters and tools (X-832)",() => InFolder(folder =>
        {
            var path=Path.Combine(folder,"MyTools.json");
            var now=DateTimeOffset.UtcNow;
            var template=ToolTemplateFormat.Parse(Tool("daily")).Template!;var json=ToolTemplateFormat.Serialize(template);
            using(var first=new ToolTemplateStore(path,(_,_)=>{})){Check(first.Install(template,json),"installed");first.FlushAsync().Wait();}
            using(var a=new ToolTemplateStore(path,(_,_)=>{}))
            using(var b=new ToolTemplateStore(path,(_,_)=>{}))
            {
                a.SetDone("daily","0:0",true,now);a.FlushAsync().Wait();
                b.SetDone("daily","0:1",true,now);b.SetCount("daily","1:0",3);b.FlushAsync().Wait();
                var other=ToolTemplateFormat.Parse(Tool("weekly")).Template!;
                Check(a.Install(other,ToolTemplateFormat.Serialize(other)),"A adds a tool");a.FlushAsync().Wait();
                b.SetCount("daily","1:0",4);b.FlushAsync().Wait();
            }
            using(var next=new ToolTemplateStore(path,(_,_)=>{}))
            {
                var daily=next.Tools.Single(tool=>tool.Template.Id=="daily");
                Check(daily.Done.ContainsKey("0:0") && daily.Done.ContainsKey("0:1"),"A's tick and B's tick both kept: "+string.Join(",",daily.Done.Keys));
                Check(daily.Counts.GetValueOrDefault("1:0")==4,"B's counter, its last value");
                Check(next.Tools.Select(tool=>tool.Template.Id).SequenceEqual(["daily","weekly"]),"A's tool kept through B's save: "+string.Join(",",next.Tools.Select(tool=>tool.Template.Id)));
            }
        })),
        ("Two clients 1838: the merges case by case (X-832)",() =>
        {
            CraftList L(string name,long quantity)=>new(name,[new(1,quantity)]);
            var merged=CraftLists.Merge([L("A",1),L("B",1),L("C",1)],[L("A",2),L("B",1),L("D",1)],[L("A",1),L("B",3),L("C",1),L("E",1)]);
            Check(merged.Select(list=>list.Name+list.Entries[0].Quantity).SequenceEqual(["A2","B3","D1","E1"]),"changed here, changed there, removed here, added on each side: "+string.Join(",",merged.Select(list=>list.Name+list.Entries[0].Quantity)));
            var twice=CraftLists.Merge(null,[L("A",1),L("A",2)],[L("A",1)]);
            Check(twice.Select(list=>list.Entries[0].Quantity).SequenceEqual([1L,2L]),"a name met twice: by its rank");
            var removedThere=CraftLists.Merge([L("A",1),L("B",1)],[L("A",1),L("B",1)],[L("A",1)]);
            Check(removedThere.Select(list=>list.Name).SequenceEqual(["A"]),"removed by the other, unchanged here: stays removed");
        }),
    ];
}
