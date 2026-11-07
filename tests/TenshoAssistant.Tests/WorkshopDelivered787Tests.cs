using TenshoAssistant.FreeCompany;

// 0.787 (X-28, user request): what the workshop already received, from its delivery window (docs/evidence/0.787-workshop-delivered.md).
internal static class WorkshopDelivered787Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // The capture of 2026-11-07 16:33:27, Bronco hull, phase 2 of 3, after three sets of rivets.
    private static int[] Window()
    {
        var numbers=Enumerable.Repeat(-1,157).ToArray();
        void Set(int at,params int[] values){for(var index=0;index<values.Length;index++)numbers[at+index]=values[index];}
        Set(0,10156);Set(3,27701);Set(6,1,3);Set(9,0);Set(11,4);Set(12,5378,5091,5079,5066);Set(24,22465,21003,20956,20812);Set(48,62008,62009,62010,62011);
        Set(60,6,6,6,6);Set(72,13,81,3,0);Set(96,0,0,0,0);Set(108,3,3,0,0);Set(120,3,3,3,3);Set(132,1,1,0,0);Set(144,50,3,38,42);
        return numbers;
    }
    private static WorkshopProject Hull()=>new(1,10156,"",[new("",[
        new(1,[new(9001,6,3)]),
        new(2,[new(5378,6,3),new(5091,6,3),new(5079,6,3),new(5066,6,3)]),
        new(3,[new(5079,6,3),new(9003,2,3)])])]);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Workshop delivered: the captured window read as phase 2 of 3, rivets and planks delivered",() =>
        {
            var read=WorkshopDelivery.FromWindow(Window(),true,DateTimeOffset.UnixEpoch);
            Check(read is { Result:10156 } && read.Value.Delivery is { Phase:1, Phases:3, Items.Count:4 } delivery && delivery.Items[1] is { Item:5091, Sets:3, PerSet:6, Required:3 },"layout");
            Check(WorkshopDelivery.FromWindow(Window(),false,DateTimeOffset.UnixEpoch) is null,"window not loaded");
            var broken=Window();broken[108]=4;
            Check(WorkshopDelivery.FromWindow(broken,true,DateTimeOffset.UnixEpoch) is null,"more sets than required: out of shape");
        }),
        ("Workshop delivered: the phases done left out, the sets delivered taken off, the rest whole",() =>
        {
            var delivery=WorkshopDelivery.FromWindow(Window(),true,DateTimeOffset.UnixEpoch)!.Value.Delivery;
            var left=WorkshopStock.Remaining(Hull(),delivery).ToDictionary(row=>row.Item,row=>row.Total);
            Check(!left.ContainsKey(9001),"phase 1 validated");
            Check(!left.ContainsKey(5378) && !left.ContainsKey(5091),"delivered sets gone");
            Check(left[5079]==18+18 && left[5066]==18 && left[9003]==6,"phase 2 not started items and phase 3 whole");
        }),
        ("Workshop delivered: used only when it fits the project, else the whole need",() =>
        {
            var delivery=WorkshopDelivery.FromWindow(Window(),true,DateTimeOffset.UnixEpoch)!.Value.Delivery;
            var other=new WorkshopProject(2,10156,"",[new("",[new(1,[new(9001,6,3)])])]);
            Check(!WorkshopStock.Applies(other,delivery) && WorkshopStock.Remaining(other,delivery).Sum(row=>row.Total)==18,"phases differ: nothing taken off");
            Check(WorkshopStock.Remaining(Hull(),null).Sum(row=>row.Total)==Hull().Totals().Sum(row=>row.Total),"never seen");
        }),
        ("Workshop delivered: read once a second while shown, kept, and used by the panel and the page",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Plugin.cs").Contains("new FreeCompany.WorkshopDeliveryReader(framework,gameGui,log,mainWindow.WorkshopDeliverySeen)"),"wired");
            Check(Read("MainWindow.ListPanels.cs").Contains("WorkshopStock.Totals(workshopPanelShown,WorkshopDelivered)"),"panel");
            var page=Read("MainWindow.WorkshopProjects.cs");
            Check(page.Contains("WorkshopStock.Remaining(selected,delivered)") && page.Contains("WorkshopStock.Totals(shown,WorkshopDelivered)") && page.Contains("if(all.TryGetValue(result,out var known) && known.SameAs(delivery))return;"),"page, saved on change only");
        }),
    ];
}
