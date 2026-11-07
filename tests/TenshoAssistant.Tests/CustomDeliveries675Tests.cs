using TenshoAssistant.Deliveries;

// W-01 (0.675): the custom delivery page reads what the game holds and says what is unknown.
internal static class CustomDeliveries675Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static readonly DateTimeOffset At=new(2026,11,6,12,0,0,TimeSpan.Zero);
    private static DeliveryClient Client(uint row,uint quest=0)=>new(row,"c"+row,"p"+row,60,6,quest,[0,90,480,780,1080,0]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Custom deliveries: a list too short or an impossible value is refused, not shown",() =>
        {
            var clients=new[]{Client(1),Client(2)};
            Check(DeliveryBoard.Check(clients,[1,2],[0,0],[0,0],12)==DeliveryReadStatus.Loaded,"good");
            Check(DeliveryBoard.Check(clients,[1],[0,0],[0,0],12)==DeliveryReadStatus.Unavailable,"short ranks");
            Check(DeliveryBoard.Check(clients,[1,6],[0,0],[0,0],12)==DeliveryReadStatus.Unavailable,"rank above the sheets");
            Check(DeliveryBoard.Check(clients,[1,2],[0,0],[0,0],-1)==DeliveryReadStatus.Unavailable,"negative allowances");
            Check(DeliveryBoard.Check([Client(0)],[1],[0],[0],12)==DeliveryReadStatus.Unavailable,"the empty row has no index");
        }),
        ("Custom deliveries: a client takes what its limit and the week both allow",() =>
        {
            Check(DeliveryBoard.Left(6,2,12)==4,"its own limit");
            Check(DeliveryBoard.Left(6,2,3)==3,"the week's allowances");
            Check(DeliveryBoard.Left(6,6,12)==0,"done");
            Check(DeliveryBoard.Left(6,7,12)==0,"never negative");
        }),
        ("Custom deliveries: each line takes its own client's values, by row minus one",() =>
        {
            var rows=DeliveryBoard.Rows([Client(2,10),Client(1,11)],new(DeliveryReadStatus.Loaded,At,5,[3,5],[200,40],[1,2],new HashSet<uint>{10}));
            Check(rows[0].Client.Row==2 && rows[0].Rank==5 && rows[0].Points==40 && rows[0].Used==2,"row 2 reads index 1");
            Check(rows[0].Unlocked && rows[0].Left==4,"unlocked, its limit");
            Check(!rows[1].Unlocked && rows[1].Left==0,"quest not done: locked and takes nothing");
        }),
        ("Custom deliveries: the points asked follow the rank, and the last rank asks nothing",() =>
        {
            var rows=DeliveryBoard.Rows([Client(1),Client(2)],new(DeliveryReadStatus.Loaded,At,12,[1,5],[30,0],[0,0],new HashSet<uint>()));
            Check(rows[0].Needed==90,"rank 1");
            Check(rows[1].Needed is null,"rank 5");
        }),
        ("Custom deliveries: no line for a reading that did not load",() =>
        {
            Check(DeliveryBoard.Rows([Client(1)],DeliverySnapshot.Of(DeliveryReadStatus.NotLoaded,At)).Count==0,"not loaded");
        }),
        ("Custom deliveries: the page only reads, on the framework thread",() =>
        {
            var reader=Read("Deliveries/CustomDeliveries.Dalamud.cs");
            Check(Read("Plugin.cs").Contains("mainWindow.ReadDeliveries=clients=>framework.RunOnFrameworkThread(()=>"),"framework thread");
            Check(reader.Contains("player->IsLoaded"),"character loaded first");
            foreach(var call in new[]{"ProcessChainedAction","CallSupply","UseAction","SendEvent","FireCallback"})Check(!reader.Contains(call),call);
        }),
    ];
}
