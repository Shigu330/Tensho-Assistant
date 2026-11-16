using TenshoAssistant.Beastmaster;
using TenshoAssistant.Chat;
using TenshoAssistant.Maps;

// X-492 (review of 0.1468 to 0.1472, minor findings): the chat wheel's rest is dropped when the wheel turns the other way (M1); MapFloors' doc
// says what At does, the smallest box wins (M2); the Beastmaster's saved plans read back a file written before 0.1470 and keep a result's
// bonus and total (M3); the icons before a sender's name are left out instead of « · » (M4).
internal static class Review1472Minor1499Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Review 1472 minor 1499 M1: small values up then a notch down: the notch moves its whole line down",() =>
        {
            var rest=0f;
            var scroll=400f;
            for(var i=0;i<9;i++)scroll=ChatScroll.Wheel(scroll,0.1f,20,600,ref rest);
            Check(scroll==400 && rest>0.85f,"nine tenths up kept, nothing moved");
            Check(ChatScroll.Wheel(scroll,-1,20,600,ref rest)==420 && rest==0,"a notch down: one line down at once, not eaten by the rest up");
            rest=-0.5f;
            Check(ChatScroll.Wheel(400,-0.6f,20,600,ref rest)==420 && Math.Abs(rest+0.1f)<0.001f,"the same way: the rest still adds up");
            var source=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(source.Contains("if(!ReferenceEquals(view,chatWheelView)){chatWheelView=view;chatWheelRest=0;}",StringComparison.Ordinal),"another tab starts from nothing");
        }),
        ("Review 1472 minor 1499 M2: in boxes of two floors the smallest box wins, as the doc now says",() =>
        {
            var ranges=new[]{new MapFloorRange(1,0,0,0,100,100,100,0),new MapFloorRange(2,0,0,0,10,10,10,0)};
            Check(MapFloors.At(ranges,1,1,1)==2 && MapFloors.At(ranges,50,0,0)==1 && MapFloors.At(ranges,500,0,0)==0,"smallest box, then the big one, then none");
            var doc=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapFloors.cs"));
            Check(doc.Contains("in boxes of two floors, the smallest box wins",StringComparison.Ordinal) && !doc.Contains("or in boxes of two floors, stays on the",StringComparison.Ordinal),"doc aligned");
        }),
        ("Review 1472 minor 1499 M3: a plans file written before 0.1470 is read back whole; a result's bonus, bonuses and total survive a write and a read",() =>
        {
            var old="{\"Plans\":{\"7\":{\"Board\":7,\"Team\":[{\"Beast\":3,\"Rank\":2,\"RankKnown\":true}],\"Battles\":[{\"Battle\":1,\"Team\":[{\"Beast\":3,\"Rank\":2,\"RankKnown\":true}],\"WeaknessesHit\":1,\"Enemies\":2}],\"RanksAssumed\":false}},\"Keys\":{\"7\":\"k\"},\"Ranks\":{\"3\":2}}";
            var read=BeastTeams.Read(old);
            Check(read is not null && read.Plans[7].Team[0].Beast==3 && read.Plans[7].Battles[0].WeaknessesHit==1 && read.Keys[7]=="k" && read.Ranks![3]==2 && read.Result is null,"the old file: every plan kept, no result");
            var result=new BoardResult(new DateTimeOffset(2026,11,16,14,0,0,TimeSpan.Zero),3,[new BeastGain(3,2,10,2,40)],Bonus:120,Bonuses:2,Total:560);
            var saved=read!;
            var back=BeastTeams.Read(BeastTeams.Write(saved with{Result=result}));
            Check(back?.Result is { Score:3, Bonus:120, Bonuses:2, Total:560 } r && r.Beasts.Single()==new BeastGain(3,2,10,2,40) && r.At==result.At,"result kept");
            var none=BeastTeams.Read(BeastTeams.Write(saved with{Result=result with{Bonus=null,Bonuses=null,Total=null}}));
            Check(none?.Result is { Bonus:null, Bonuses:null, Total:null },"unknown stays unknown, not zero");
            Check(BeastTeams.Read("{broken")==null,"a broken file reads as nothing (the caller keeps it aside)");
        }),
        ("Review 1472 minor 1499 M4: the novice network's and the jobs' icons before a name are left out; others still « · »",() =>
        {
            foreach(var icon in new uint[]{77,78,79,80,81,95,127,128,150,171})Check(ChatGlyphs.Icon(icon)=="",$"icon {icon} left out");
            foreach(var icon in new uint[]{1,76,82,94,96,126,172})Check(ChatGlyphs.Icon(icon)=="·",$"icon {icon} still written");
            Check(ChatGlyphs.Icon(ChatGlyphs.CrossWorld)=="@","world icon kept");
        }),
    ];
}
