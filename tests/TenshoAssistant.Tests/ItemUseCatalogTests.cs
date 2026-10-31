using TenshoAssistant.Items;

internal static class ItemUseCatalogTests
{
    private static ItemLeveUse Leve(uint row=1,uint leve=100)=>new(row,leve,"Mandat",10,"MEN","Ville",2,[new(10,3),new(20,1)],123);
    private static ItemUseCatalog Catalog(params ItemLeveUse[] leves)=>new(leves,[10,30,0,30],[20,40,0],"French","fixture");
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static void Reject(Action action){var rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,"invalid catalogue accepted");}
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Item uses index each requested item and distinguish independent static catalogues",()=>
        {
            var catalog=Catalog(Leve());
            Check(catalog.LevesFor(10).Single().LeveId==100 && catalog.LevesFor(20).Single().Requests.Count==2,"full demand attached to every requested item");
            Check(catalog.IsSupplyCandidate(10) && !catalog.IsAquariumFish(10) && catalog.IsAquariumFish(20) && !catalog.IsSupplyCandidate(20),"independent static references");
            Check(catalog.LevesFor(99).Count==0 && !catalog.IsSupplyCandidate(0) && !catalog.IsAquariumFish(0),"unknown id has no indexed reference");
            Check(catalog.LeveCount==1 && catalog.SupplyItemCount==2 && catalog.AquariumItemCount==2 && catalog.Language=="French" && catalog.DataVersion=="fixture","counts and provenance");
        }),
        ("Item use repeated request lines and leve variants are preserved without summing",()=>
        {
            var first=Leve() with {Requests=[new(10,3),new(10,5)]};var variant=Leve(2,100) with {Requests=[new(10,null)]};
            var catalog=Catalog(first,variant);var uses=catalog.LevesFor(10);
            Check(uses.Count==2 && uses[0].Requests.Select(row=>row.Quantity).SequenceEqual(new int?[]{3,5}),"same item lines are not summed or duplicate index hits");
            Check(uses[1].Requests.Single().Quantity is null && uses[1].Repeats==2,"unknown quantity stays unknown; repeat parameter retained raw");
        }),
        ("Item use catalogue owns nested inputs and exposes read-only references",()=>
        {
            var requests=new List<ItemUseRequest>{new(10,3)};var leves=new List<ItemLeveUse>{Leve() with {Requests=requests}};var gc=new List<uint>{10};var aq=new List<uint>{20};
            var catalog=new ItemUseCatalog(leves,gc,aq,"French","v");requests.Clear();leves.Clear();gc.Clear();aq.Clear();
            Check(catalog.LevesFor(10).Single().Requests.Single().Quantity==3 && catalog.IsSupplyCandidate(10) && catalog.IsAquariumFish(20),"nested ownership");
            var rejected=false;try{((IList<ItemLeveUse>)catalog.LevesFor(10)).Clear();}catch(NotSupportedException){rejected=true;}Check(rejected,"index immutable");
            rejected=false;try{((IList<ItemUseRequest>)catalog.LevesFor(10)[0].Requests).Clear();}catch(NotSupportedException){rejected=true;}Check(rejected,"demand immutable");
        }),
        ("Item use identity validity and provenance errors never choose a default row",()=>
        {
            foreach(var bad in new[]{Leve() with {RowId=0},Leve() with {LeveId=0},Leve() with {Name=""},Leve() with {Level=-1},Leve() with {Requests=[]},Leve() with {Requests=[new(0,1)]},Leve() with {Requests=[new(10,0)]},Leve() with {Requests=[new(10,-1)]}})
                Reject(()=>Catalog(bad));
            Reject(()=>Catalog(Leve(),Leve()));
            Reject(()=>new ItemUseCatalog([],[],[],"","v"));Reject(()=>new ItemUseCatalog([],[],[],"French",""));
            Reject(()=>new ItemUseCatalog([],[],[],"French","v",-1));
        }),
        ("Item use stable order retains unresolved coverage rather than claiming complete usability",()=>
        {
            var catalogue=new ItemUseCatalog([Leve(3,300) with {Name="Zulu",Level=20},Leve(2,200) with {Name="Beta"},Leve(1,100) with {Name="Alpha"}],[],[],"French","v",5);
            Check(catalogue.LevesFor(10).Select(row=>row.RowId).SequenceEqual(new uint[]{1,2,3}) && catalogue.UnresolvedLeveRows==5,"level name row order and partial coverage");
        }),
        ("Item use cancellation interrupts input copying and never returns partial publication",()=>
        {
            using var source=new CancellationTokenSource();
            IEnumerable<ItemLeveUse> Input(){yield return Leve();source.Cancel();yield return Leve(2,200);}
            var rejected=false;try{_=new ItemUseCatalog(Input(),[],[],"French","v",cancellation:source.Token);}catch(OperationCanceledException){rejected=true;}Check(rejected,"cancel during producer enumeration");
            rejected=false;try{_=new ItemUseCatalog([],[],[],"French","v",cancellation:source.Token);}catch(OperationCanceledException){rejected=true;}Check(rejected,"pre-cancel");
        }),
        ("Item use service fail reload and clear boundaries remove old catalogue evidence",()=>
        {
            var service=new ItemUseCatalogService();service.Publish(Catalog(Leve()));Check(service.State.Catalog is not null,"ready");
            service.Progress(4,20);Check(service.State.Catalog is null && service.State.Copied==4 && service.State.Total==20,"loading not empty known usages");
            service.Publish(Catalog(Leve()));service.Fail(new Exception("fixture"));Check(service.State.Catalog is null && service.State.Error=="fixture","failed catalogue unknown");
            service.Clear();Check(service.State.Catalog is null && service.State.Error is null && service.State.Total==0,"clear");
        })
    };
}
