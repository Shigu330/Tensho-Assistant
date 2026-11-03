using TenshoAssistant.Craft;

// A-05 (0.433): crafting lists of several items, in a file of their own.
internal static class CraftLists433Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft lists: add merges, bounds kept, remove, quantities clamped, names cleaned",() =>
        {
            var list=new CraftList("Gear",[]);
            list=CraftLists.Add(list,10,2)!;list=CraftLists.Add(list,11,1)!;list=CraftLists.Add(list,10,3)!;
            Check(list.Entries.Count==2 && list.Entries[0]==new CraftListEntry(10,5),"merged");
            Check(CraftLists.Add(list,0,1) is null && CraftLists.Add(list,12,0) is null,"invalid");
            Check(CraftLists.Add(list,10,CraftLists.MaxQuantity)!.Entries[0].Quantity==CraftLists.MaxQuantity && CraftLists.SetQuantity(list,11,-4).Entries[1].Quantity==1,"clamped");
            var full=new CraftList("Full",Enumerable.Range(1,CraftLists.MaxEntries).Select(index=>new CraftListEntry((uint)index,1)).ToArray());
            Check(CraftLists.Add(full,999,1) is null && CraftLists.Add(full,5,1) is not null,"64 items, existing still merges");
            Check(CraftLists.Remove(list,10).Entries.Single().ItemId==11,"remove");
            Check(CraftLists.CleanName("  Lot 1 ")=="Lot 1" && CraftLists.CleanName("") is null && CraftLists.CleanName(new string('a',41)) is null && CraftLists.CleanName("a\nb") is null,"names");
        }),
        ("Craft lists: ingredients summed over the plans; file round trip; unreadable file never written over",() =>
        {
            var a=new CraftPlan(2,2,2,[new CraftPlanMaterial(100,4,null!),new CraftPlanMaterial(101,2,null!)]);
            var b=new CraftPlan(1,1,1,[new CraftPlanMaterial(100,3,null!)]);
            Check(CraftLists.Ingredients([a,b]).SequenceEqual([(100u,7L),(101u,2L)]),"summed in order");
            var folder=Path.Combine(Path.GetTempPath(),"tensho-lists-"+Guid.NewGuid().ToString("N"));
            try
            {
                var path=Path.Combine(folder,"CraftLists.json");
                var store=new CraftListStore(path,(_,_)=>{});
                Check(store.Lists.Count==0 && store.Set(0,new("Gear",[new(10,5)])) && store.Set(1,new("Food",[])),"created");
                var again=new CraftListStore(path,(_,_)=>{});
                Check(again.Lists.Select(list=>list.Name).SequenceEqual(["Gear","Food"]) && again.Lists[0].Entries.Single()==new CraftListEntry(10,5),"round trip");
                File.WriteAllText(path,"{ not json");
                var broken=new CraftListStore(path,(_,_)=>{});
                Check(broken.Unavailable && !broken.Set(0,new("X",[])) && File.ReadAllText(path)=="{ not json","not written over");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }),
    ];
}
