using TenshoAssistant.Craft;

internal static class CraftLiveTests
{
    private static CraftLiveRaw Raw()=>new(1,2,0,1,1,4,0,8,new(100,3000,2800,600),400,600,"Objet","Normale","",
        new("1 234","2 000","999","1 500","40","80","1"),[new("Innovation","3")],[new(2189,3,0)]);
    private static RecipeCatalog Catalog()=>RecipeCatalog.Build([new(1,2,1,"Menuisier",100,[],0)],"fixture");
    private static void Check(bool condition,string why){if(!condition)throw new Exception(why);}
    private static void Throws<T>(Action run) where T:Exception{try{run();}catch(T){return;}throw new Exception(typeof(T).Name);}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Craft live numeric parsing accepts strict localized groups and keeps unknown distinct from zero",()=>
        {
            foreach(var value in new[]{"1,234","1.234","1 234","1\u202f234","1\u00a0234","1\u2009234"})Check(CraftLiveFrame.ParseNumber(value)==1234,"valid thousands");
            Check(CraftLiveFrame.ParseNumber("0")==0 && CraftLiveFrame.ParseNumber(" 120 ")==120,"exact zero distinct");
            foreach(var value in new[]{"","—","12%","1.2","12,34","1,234.000","-1","12/20","١٢","2147483648","1  234"})Check(CraftLiveFrame.ParseNumber(value) is null,"unsupported text remains unknown");
        }),
        ("Craft live recipe identity uses ids job and craft type rather than names",()=>
        {
            var frame=new CraftLiveFrame(Raw(),Catalog());
            Check(frame.Recipe?.RecipeId==1 && frame.Progress==1234 && frame.Quality==999 && frame.Durability==40 && frame.UiStep==1,"joined id and numbers");
            foreach(var raw in new[]{Raw() with {RecipeId=9},Raw() with {ResultItemId=99},Raw() with {CraftType=1},Raw() with {JobId=9}})
                Check(new CraftLiveFrame(raw,Catalog()).Recipe is null,"mismatch stays unlinked even same displayed name");
            Check(new CraftLiveFrame(Raw(),null).Recipe is null,"loading catalog not fabricated identity");
        }),
        ("Craft live copies effect and status values and preserves raw flags and unknowns",()=>
        {
            var effects=new List<CraftLiveEffect>{new("Innovation","3")};var statuses=new List<CraftLiveStatus>{new(2189,3,0)};
            var frame=new CraftLiveFrame(Raw() with {Effects=effects,Statuses=statuses,Condition=255,Flags=1,StateFlags=0xFFFFFFFF,Numbers=Raw().Numbers with {Quality="2000",Durability="?"}},Catalog());
            effects.Clear();statuses.Clear();
            Check(frame.Raw.Effects.Count==1 && frame.Raw.Statuses.Count==1 && frame.Raw.Condition==255 && frame.Raw.StateFlags==uint.MaxValue,"immutable raw facts");
            Check(frame.Animating && frame.Quality is null && frame.Durability is null,"out of range and invalid text not zero");
            Throws<NotSupportedException>(()=>((IList<CraftLiveEffect>)frame.Raw.Effects).Clear());
            Throws<ArgumentException>(()=>new CraftLiveFrame(Raw() with {Effects=Enumerable.Repeat(new CraftLiveEffect("a","1"),10).ToArray()},null));
            Throws<ArgumentException>(()=>new CraftLiveFrame(Raw() with {Cp=601},null));
            Throws<ArgumentException>(()=>new CraftLiveFrame(Raw() with {Cp=0,MaxCp=0},null));
            Throws<ArgumentException>(()=>new CraftLiveFrame(Raw() with {Statuses=Enumerable.Repeat(new CraftLiveStatus(1,0,0),61).ToArray()},null));
        }),
        ("Craft live opt-in owner changes disabling and disposal reject stale observations",()=>
        {
            var service=new CraftLiveService();Check(service.BeginRead(42) is null,"default disabled");
            service.SetEnabled(true);var ticket=service.BeginRead(42)!.Value;service.Publish(ticket,42,Raw(),Catalog());
            Check(service.State.Frame is not null && service.State.ObservedAt is not null,"published current snapshot");
            var newer=service.BeginRead(99)!.Value;service.Publish(ticket,42,Raw(),Catalog());
            Check(service.State.Owner==99 && service.State.Frame is null,"old owner cannot republish");
            service.SetEnabled(false);service.Publish(newer,99,Raw(),Catalog());Check(service.State.Frame is null && !service.State.Enabled,"disable rejects");
            service.SetEnabled(true);ticket=service.BeginRead(99)!.Value;service.Dispose();service.Publish(ticket,99,Raw(),Catalog());service.SetEnabled(true);
            Check(!service.State.Enabled && service.State.Frame is null && service.BeginRead(99) is null,"disposed cannot resume");
        }),
        ("Craft live absent hidden offline and error states clear prior observations",()=>
        {
            using var service=new CraftLiveService();service.SetEnabled(true);var ticket=service.BeginRead(42)!.Value;
            foreach(var reason in new[]{CraftLiveStatusKind.AddonAbsent,CraftLiveStatusKind.NotReady})
            {
                service.Publish(ticket,42,Raw(),Catalog());service.Missing(ticket,42,reason);Check(service.State.Frame is null && service.State.ObservedAt is null && service.State.Status==reason,"missing clears");
            }
            service.Publish(ticket,42,Raw(),Catalog());service.Fail(ticket,42,new Exception("read failed"));Check(service.State.Frame is null && service.State.Error=="read failed","error clears");
            service.Publish(ticket,42,Raw(),Catalog());Throws<ArgumentException>(()=>service.Publish(ticket,42,Raw() with {ItemName=new string('x',513)},Catalog()));
            Check(service.State.Frame is null && service.State.Status==CraftLiveStatusKind.Error,"invalid publication clears previous frame");
            service.BeginRead(0);service.Publish(ticket,42,Raw(),Catalog());Check(service.State.Status==CraftLiveStatusKind.Offline && service.State.Frame is null,"logout rejects late publication");
        })
    };
}
