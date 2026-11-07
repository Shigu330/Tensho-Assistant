using TenshoAssistant.Deliveries;

// W-01 (0.730): what a client gives. Numbers: SatisfactionSupplyReward rows 9, 10, 11, as read on 2026-11-07, and the second source of docs/evidence/0.730-delivery-rewards.md.
internal static class DeliveryRewards730Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static DeliveryRewardRow Row(int rank,uint row,int slot,bool bonus,int percent,uint first,uint second)=>new(rank,row,slot,bonus,percent,new(0,0,0),new(1120,1400,1670),[(first,new(134,157,180)),(second,new(107,124,140))]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Delivery rewards: the codes are the scrips, and the bonus rounds down as the game does",() =>
        {
            Check(DeliveryBoard.ScripItem(2)==33913 && DeliveryBoard.ScripItem(4)==33914 && DeliveryBoard.ScripItem(6)==41784 && DeliveryBoard.ScripItem(7)==41785,"known");
            Check(DeliveryBoard.ScripItem(3) is null,"nothing guessed");
            Check(new DeliveryTiers(134,157,180).Times(150)==new DeliveryTiers(201,235,270),"purple");
            Check(new DeliveryTiers(107,124,140).Times(150)==new DeliveryTiers(160,186,210),"orange");
            Check(new DeliveryTiers(1120,1400,1670).Times(150)==new DeliveryTiers(1680,2100,2505),"gil");
        }),
        ("Delivery rewards: one line per rank and kind, gathered and fished together, the bonus after, an unknown currency left out",() =>
        {
            var lines=DeliveryBoard.Rewards([Row(5,11,1,true,150,2,6),Row(5,10,3,false,100,4,7),Row(5,10,2,false,100,4,7),Row(5,9,1,false,100,2,6),Row(5,9,1,false,100,2,6),Row(4,7,1,false,100,3,6)]);
            Check(lines.Count==3,"three lines");
            Check(lines[0] is { Rank:5,Crafting:true,Bonus:false,Percent:100 } && lines[1] is { Crafting:false,Bonus:false } && lines[2] is { Crafting:true,Bonus:true,Percent:150 },"order");
            Check(lines[0].Currencies[0]==(33913u,new DeliveryTiers(134,157,180)) && lines[1].Currencies[1]==(41785u,new DeliveryTiers(107,124,140)),"plain");
            Check(lines[2].Gil==new DeliveryTiers(1680,2100,2505) && lines[2].Currencies[0].Amount==new DeliveryTiers(201,235,270),"bonus amounts");
            Check(DeliveryBoard.Rewards([Row(5,9,1,false,150,2,6)])[0].Gil.Low==1120,"a multiplier without the bonus flag is not applied");
            var none=new DeliveryRewardRow(1,1,1,false,100,new(0,0,0),new(1,1,1),[(0u,new(0,0,0))]);
            Check(DeliveryBoard.Rewards([none]).Count==0,"no currency, no line");
        }),
        ("Delivery rewards: on their own tab, without waiting for the character, read in the self-check",() =>
        {
            var page=Read("MainWindow.CustomDeliveries.cs");
            Check(page.Contains("DrawDeliveryStatus(clients)") && page.Contains("DrawDeliveryRewards(clients)") && page.IndexOf("BeginTabBar")<page.IndexOf("deliverySnapshot is not"),"tabs before the reading");
            Check(Read("MainWindow.SelfCheckPages.cs").Contains("reward lines"),"self-check");
            Check(Read("Deliveries/CustomDeliveries.Dalamud.cs").Contains("GetSubrowExcelSheet<SatisfactionSupply>"),"sheet");
        }),
    ];
}
