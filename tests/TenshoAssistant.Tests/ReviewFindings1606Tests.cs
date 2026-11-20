using TenshoAssistant.Chat;
using TenshoAssistant.Inventory;

// 0.1606 (X-662, review of 0.1603 to 0.1605, minor findings). Behaviour tests (R28.14, the user's data and other players' data):
// M1: the retainers' total starts from the journal's last balance only when the character's gil did not change while Tensho was not
// watching (a deposit or a withdrawal made without Tensho is never a market sale nor a one-sided transfer); M2: the game's log line is
// read only for the message that waits for its identifiers, after the cheap checks, and nothing at all while Tensho is cut.
// Amounts invented (X-662 M3).
internal static class ReviewFindings1606Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset Start=new(2026,1,5,9,0,0,TimeSpan.Zero);
    private static GilCategory Bank(long delta)=>GilCategory.Retainer;
    private static List<GilEntry> Journal(long gil,long retainers)=>
    [
        new(Start.AddDays(-1),-500,gil,GilSource.Character,GilCategory.Retainer),
        new(Start.AddDays(-1),500,retainers,GilSource.Retainers,GilCategory.Retainer),
    ];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Review findings 1606: M1: the character's gil unchanged since the journal: the retainers' rise is their market sales",()=>
        {
            var entries=Journal(40000,1500);var at=Start;var recorder=new GilRecorder();
            recorder.Seed(entries.ToList());
            Check(recorder.Retainers is null,"not decided before the character's first reading");
            recorder.Read(40000,9800,null,at,entries,Bank);recorder.Read(40000,9800,null,at.AddSeconds(1),entries,Bank);
            Check(recorder.Retainers==9800 && entries.Count==3 && entries[2] is {Delta:8300,Category:GilCategory.Market,Source:GilSource.Retainers},"the 8 300 sold meanwhile, under Market");
        }),
        ("Review findings 1606: M1: a deposit made while Tensho was not watching is not a sale",()=>
        {
            // Journal: character 40 000, retainers 1 500. Without Tensho, 6 000 deposited: character 34 000, retainers 7 500.
            var entries=Journal(40000,1500);var at=Start;var recorder=new GilRecorder();
            recorder.Seed(entries.ToList());
            recorder.Read(34000,7500,null,at,entries,Bank);recorder.Read(34000,7500,null,at.AddSeconds(1),entries,Bank);
            Check(entries.Count==2 && recorder.Retainers==7500 && recorder.Gil==34000,"starting points, no entry: "+entries.Count);
            // A sale seen afterwards still counts.
            recorder.Read(34000,8100,null,at.AddSeconds(2),entries,Bank);recorder.Read(34000,8100,null,at.AddSeconds(3),entries,Bank);
            Check(entries.Count==3 && entries[2] is {Delta:600,Category:GilCategory.Market},"a sale seen by Tensho: counted");
        }),
        ("Review findings 1606: M1: a withdrawal made while Tensho was not watching is not a one-sided transfer",()=>
        {
            var entries=Journal(40000,1500);var at=Start;var recorder=new GilRecorder();
            recorder.Seed(entries.ToList());
            recorder.Read(41500,0,null,at,entries,Bank);recorder.Read(41500,0,null,at.AddSeconds(1),entries,Bank);
            Check(entries.Count==2 && recorder.Retainers==0,"no transfer written on the retainers' side only");
        }),
        ("Review findings 1606: M1: the retainers' readings wait for the character's; a journal without the character's balance seeds nothing",()=>
        {
            var entries=Journal(40000,1500);var at=Start;var recorder=new GilRecorder();
            recorder.Seed(entries.ToList());
            recorder.Read(null,9800,null,at,entries,Bank);recorder.Read(null,9800,null,at.AddSeconds(1),entries,Bank);
            Check(recorder.Retainers is null && entries.Count==2,"the character unknown: the retainers wait");
            recorder.Read(40000,9800,null,at.AddSeconds(2),entries,Bank);recorder.Read(40000,9800,null,at.AddSeconds(3),entries,Bank);
            Check(entries.Count==3 && entries[2] is {Delta:8300,Category:GilCategory.Market},"then seeded, the sale counted");
            var only=new List<GilEntry>{new(Start,500,1500,GilSource.Retainers,GilCategory.Market)};var other=new GilRecorder();
            other.Seed(only);
            other.Read(40000,9800,null,at,only,Bank);other.Read(40000,9800,null,at.AddSeconds(1),only,Bank);
            Check(only.Count==1 && other.Retainers==9800,"no character balance recorded: a starting point (R06)");
            other.Reset();other.Seed(entries.ToList());
            other.Read(40000,null,null,at,entries,Bank);other.Reset();
            other.Read(40000,9800,null,at,entries,Bank);other.Read(40000,9800,null,at.AddSeconds(1),entries,Bank);
            Check(other.Retainers==9800 && entries.Count==3,"another character: the seed forgotten");
        }),
        ("Review findings 1606: M2: the game's log line is read only for the waiting message, after the cheap checks",()=>
        {
            const ushort Say=10;const ulong Own=9;var sources=new ChatSources();var calls=0;
            ChatLogLine? Line(){calls++;return new ChatLogLine(Say,"Aya Lune",73);}
            Check(sources.ResolveRead(1001,2001,73,Say,Line,Own)==ChatSourceMatch.NotWaiting && calls==0,"no message waiting: not read");
            sources.Expect(1,"Aya Lune",73,Say);
            Check(sources.ResolveRead(0,0,73,Say,Line,Own)==ChatSourceMatch.NoIdentifiers && calls==0,"no identifiers: not read");
            sources.Expect(2,"Aya Lune",73,Say);
            Check(sources.ResolveRead(Own,1,73,Say,Line,Own)==ChatSourceMatch.Own && calls==0,"your own ids: not read");
            sources.Expect(3,"Aya Lune",73,Say);
            Check(sources.ResolveRead(1001,2001,73,11,Line,Own)==ChatSourceMatch.Kind && calls==0,"another kind: not read");
            sources.Expect(4,"Aya Lune",73,Say);
            Check(sources.ResolveRead(1001,2001,73,Say,Line,Own)==ChatSourceMatch.Given && calls==1 && sources.For(4) is {ContentId:1001},"the waiting message: read once, given");
            var hook=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs")).Replace("\r\n","\n");
            var cut=hook.IndexOf("if(TenshoAssistant.Competitive.CompetitiveGate.Cut){sources.Skip();return;}",StringComparison.Ordinal);
            Check(cut>0 && hook.IndexOf("()=>Line(module,messageIndex)",StringComparison.Ordinal)>cut && !hook.Contains(",Line(module,messageIndex),"),
                "the hook: nothing read while cut, the line passed to be read later");
        }),
    ];
}
