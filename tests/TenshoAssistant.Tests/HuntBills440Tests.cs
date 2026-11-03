using TenshoAssistant.Reminders;

// N-01 (0.440): held hunt bills with their targets, kills and zone.
internal static class HuntBills440Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Hunt bills: targets of the held order, kills clamped, done when all are; unknown orders left out",() =>
        {
            // Order 1 of docs/evidence/0.440-hunt-bills.md (five targets, the last two elite FATE marks).
            IReadOnlyList<HuntOrderTarget> Order(uint row)=>row==1
                ? [new(0,"Géant bûcheron",53,"Coteaux rocheux",3,0),new(1,"Hoplomachus",25,"Fangebrume",3,0),new(2,"Épine",18,"Frai de Sapsa",3,0),
                   new(3,"Faucheux",20,"Vallée des Pas perdus",1,366),new(4,"",0,"",0,0)]
                : [];
            var views=HuntBills.Views([new(0,1,[3,1,5,0,0]),new(4,999,[1,0,0,0,0])],Order,index=>$"Bill {index}");
            Check(views.Count==1 && views[0].Bill=="Bill 0" && views[0].Targets.Count==4,"one known bill, empty slot dropped");
            Check(views[0].Targets[0].Done && !views[0].Targets[1].Done && views[0].Targets[2].Killed==3 && !views[0].Done,"kills clamped to the need");
            var done=HuntBills.Views([new(0,1,[3,3,3,1,0])],Order,_=>"B");
            Check(done.Single().Done,"all targets: done");
            Check(HuntBills.Views(null,Order,_=>"").Count==0,"nothing read");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs"));
            Check(reader.Contains("(nint)MobHunt.MemberFunctionPointers.GetObtainedHuntOrderRowId!=0"),"address checked");
        }),
    ];
}
