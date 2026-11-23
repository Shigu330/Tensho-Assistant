using System.Net;
using TenshoAssistant.Caching;
using TenshoAssistant.Chat;
using TenshoAssistant.Macros;
using TenshoAssistant.Market;

// 0.1840 (X-836, security review before the alpha, minor M1 to M4). Behaviour tests (R28.14) on real files in folders of their own: the
// sellers' retainer names never reach the disk cache and the old copies that kept them are removed; the macro chain cancelled when PvP starts;
// the chat history of a character no longer played trimmed to the days kept; the desktop app never loads a DLL found above its own folder.
internal static class SecurityMinors1840Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Folder(string name)=>Path.Combine(Path.GetTempPath(),"tensho-1840-"+name+"-"+Guid.NewGuid().ToString("N"));
    private static void Clean(string folder){try{if(Directory.Exists(folder))Directory.Delete(folder,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
    private static void Until(Func<bool> done,string what)
    {
        var end=Environment.TickCount64+10_000;
        while(!done()){if(Environment.TickCount64>end)throw new Exception("timed out: "+what);Thread.Sleep(10);}
    }
    private static string AllText(string folder)=>string.Concat(Directory.GetFiles(folder,"*",SearchOption.AllDirectories).Select(File.ReadAllText));

    private static readonly DateTimeOffset Now=new(2026,11,23,12,0,0,TimeSpan.Zero);
    private const ulong Ann=18014398509481985UL,Bob=18014398509481986UL,Cid=18014398509481987UL,Dee=18014398509481988UL;
    private static ChatRecord Said(string text,double daysAgo)
        =>new(0,Now.AddDays(-daysAgo),10,ChatChannels.From(10),"Other Player",[new ChatSegment(text)],1,2);
    private static string WriteHistory(string folder,ulong who,DateTimeOffset written,params ChatRecord[] records)
    {
        Directory.CreateDirectory(folder);
        var path=ChatHistory.PathOf(folder,who);
        File.WriteAllText(path,ChatHistory.Header(who)+"\n"+string.Concat(records.Select(record=>ChatHistory.Encode(record)+"\n")));
        File.SetLastWriteTimeUtc(path,written.UtcDateTime);
        return path;
    }

    private const string Listings="{\"listings\":[{\"pricePerUnit\":120,\"quantity\":3,\"hq\":false,\"worldName\":\"Odin\",\"retainerName\":\"Sellerofthings\",\"tax\":18}],"
        +"\"recentHistory\":[],\"regularSaleVelocity\":2}";

    public static readonly (string Name,Action Run)[] Cases=
    [
        ("X-836 M1: the board kept on disk holds no retainer name; the one shown in memory keeps it",()=>
        {
            var folder=Folder("cache");
            try
            {
                using var cache=new DataCache(folder);
                using var service=new MarketBoardService(new HttpClient(new MarketTestHandler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(Listings)}))));
                service.ConfigureCache(()=>TimeSpan.FromMinutes(5),cache);
                service.Request("Odin",5057);
                Until(()=>service.State.Board is { Offers.Count:1 },"board read");
                Check(service.State.Board!.Offers[0].Retainer=="Sellerofthings" && !service.State.Board.NamesDropped,"in memory: the name, for the display and one's own offers");
                Until(()=>Directory.Exists(folder) && Directory.GetFiles(folder,"*.json").Length>0,"copy written");
                cache.FlushAsync().Wait();
                Check(!AllText(folder).Contains("Sellerofthings",StringComparison.Ordinal),"no retainer name in any cache file");
                var saved=cache.ReadAsync<MarketBoard>(MarketBoardService.Family,"Odin:5057",MarketBoardService.Version).Result;
                Check(saved is { Value:{ NamesDropped:true } board } && board.Offers.Count==1 && board.Offers[0].Retainer=="" && board.Offers[0].UnitPrice==120,
                    "the disk copy: same offer, name left out, marked");
            }
            finally{Clean(folder);}
        }),
        ("X-836 M1: the old copies that kept the names (v1) are removed at the next write; « Empty the cache »'s zip then holds no name",()=>
        {
            var folder=Folder("retired");
            try
            {
                using var cache=new DataCache(folder);
                var named=new MarketBoard("Odin",5057,Now,[new MarketOffer(120,3,false,"Odin","Sellerofthings",18,null)],[],null,null,null);
                // Written as 0.1839 wrote it: kept under v1, then removed by the trim of the same write.
                Check(cache.WriteAsync(MarketBoardService.Family,"Odin:5057","universalis-board-v1",named,Now).Result,"v1 written");
                Check(cache.WriteAsync(MarketBoardService.Family,"Odin:4","universalis-board-v1",named,Now).Result,"another v1 written");
                Check(cache.WriteAsync("other-family","x","v1",new[]{1,2,3},Now).Result,"another family written");
                cache.FlushAsync().Wait();
                Check(!AllText(folder).Contains("Sellerofthings",StringComparison.Ordinal),"no v1 copy left");
                Check(AllText(folder).Contains("other-family",StringComparison.Ordinal),"the other family's v1 kept (only market-board v1 is retired)");
                Check(cache.WriteAsync(MarketBoardService.Family,"Odin:5057",MarketBoardService.Version,named.WithoutNames(),Now).Result,"v2 written");
                cache.FlushAsync().Wait();
                Check(cache.ReadAsync<MarketBoard>(MarketBoardService.Family,"Odin:5057",MarketBoardService.Version).Result is { Value.NamesDropped:true },"v2 kept and read back");
            }
            finally{Clean(folder);}
        }),
        ("X-836 M1: a board read back without names leaves Universalis' offers out of the undercut price; the column says why",()=>
        {
            var text=File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant","MainWindow.MarketBoard.cs")));
            Check(text.Contains("(offer.Seen ? offer.Own==false:!ownRetainers.Contains(offer.Retainer)) && (offer.Seen || !board.NamesDropped)",StringComparison.Ordinal),"undercut: no guess on a board without names");
            Check(text.Contains("Tip(\"MarketBoard.NameNotKeptTip\")",StringComparison.Ordinal),"the empty column explained");
        }),
        ("X-836 M2: PvP or ranked content cancels a ready chain; it does not start again when the cut ends",()=>
        {
            using var sequence=new MacroSequence();
            sequence.Start(Ann,[new MacroSlot(0,1,"One"),new MacroSlot(0,2,"Two")]);
            Check(sequence.State.Status==MacroSequenceStatus.Ready,"ready");
            sequence.StopForCompetitive();
            Check(sequence.State.Status==MacroSequenceStatus.Cancelled && sequence.State.Message==MacroSequence.CompetitiveMessage,"cancelled, said: "+sequence.State.Message);
            Check(sequence.PrepareNext(Ann,true,false,false) is null,"after the cut: nothing asked to the game");
            var request=new MacroSequence();
            request.Start(Ann,[new MacroSlot(0,1,"One")]);
            Check(request.PrepareNext(Ann,true,false,false) is not null,"a request pending");
            request.StopForCompetitive();
            Check(request.State.Pending is null && request.State.Status==MacroSequenceStatus.Cancelled,"a pending request dropped");
            request.Dispose();
        }),
        ("X-836 M2: the bridge cancels the chain at the cut and ignores a marker read during it",()=>
        {
            var text=File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant","Macros","MacroChainBridge.Dalamud.cs")));
            Check(text.Contains("CompetitiveGate.Changed += OnCut",StringComparison.Ordinal) && text.Contains("CompetitiveGate.Changed -= OnCut",StringComparison.Ordinal),"subscribed and let go");
            Check(text.Contains("if (TenshoAssistant.Competitive.CompetitiveGate.Cut) { sequence.StopForCompetitive(); return; }",StringComparison.Ordinal),"a marker during the cut stops the chain");
        }),
        ("X-836 M3: a character no longer played: its old messages removed at another character's load, a file another client writes left alone",()=>
        {
            var folder=Folder("chat");
            try
            {
                var bob=WriteHistory(folder,Bob,Now.AddDays(-40),Said("bob old",40));
                var cid=WriteHistory(folder,Cid,Now.AddDays(-2),Said("cid old",31),Said("cid recent",2));
                var dee=WriteHistory(folder,Dee,Now.AddHours(-2),Said("dee old",31),Said("dee recent",0.1));
                File.WriteAllText(Path.Combine(folder,"notes.jsonl"),"not ours");
                var warned=new List<string>();
                var writer=new ChatHistoryWriter(folder,(_,work)=>work(),(_,message)=>warned.Add(message));
                ChatHistory.Loaded? got=null;
                writer.Open(Ann,Now,2000,30,read=>got=read);
                Check(got is not null && warned.Count==0,"Ann opened, nothing said: "+string.Join(" | ",warned));
                Check(!File.Exists(bob),"Bob: every message past the 30 days, the file deleted");
                Check(File.Exists(cid) && !File.ReadAllText(cid).Contains("cid old",StringComparison.Ordinal) && File.ReadAllText(cid).Contains("cid recent",StringComparison.Ordinal),
                    "Cid: the old message removed, the recent one kept");
                Check(File.ReadAllText(dee).Contains("dee old",StringComparison.Ordinal),"Dee, written two hours ago (another client may play it): left as it is");
                Check(File.Exists(Path.Combine(folder,"notes.jsonl")),"a file that is not a history left alone");
                Check(ChatHistory.Sweep(folder,Ann,Now,2000,ChatHistory.NoLimit)==0,"« No limit » days: nothing removed");
            }
            finally{Clean(folder);}
        }),
        ("X-836 M4: the desktop app takes the repository's build only when it runs from that repository, never a DLL above an installed copy",()=>
        {
            var root=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"repo-1840-"+Guid.NewGuid().ToString("N")));
            var asked=new List<string>();
            var dev=TenshoAssistant.Desktop.DesktopPaths.RepositoryBuild(Path.Combine(root,"src","TenshoAssistant.Desktop","bin","Release","net10.0-windows"),path=>{asked.Add(path);return true;});
            Check(dev==Path.Combine(root,"src","TenshoAssistant","bin","x64","Release","TenshoAssistant.dll"),"development: the repository's build: "+dev);
            asked.Clear();
            var installed=TenshoAssistant.Desktop.DesktopPaths.RepositoryBuild(Path.Combine(root,"Users","player","AppData","Local","TenshoAssistant","versions","0.1840.0"),path=>{asked.Add(path);return true;});
            Check(installed is null && asked.Count==0,"an installed copy: nothing searched above it, even where a DLL would exist: "+string.Join(", ",asked));
            var text=File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant.Desktop","Program.cs")));
            Check(text.Contains("return DesktopPaths.RepositoryBuild(AppContext.BaseDirectory,File.Exists);",StringComparison.Ordinal) && !text.Contains("folder=folder.Parent",StringComparison.Ordinal),"Program uses it, the old walk is gone");
        }),
        ("X-836 M5: PUBLISHING names the desktop app's download as an exception (DEC-056), in both halves",()=>
        {
            var publishing=File.ReadAllText(Path.GetFullPath(Path.Combine("docs","project","PUBLISHING.md")));
            Check(publishing.Contains("| Downloading and starting a program | **One exception, declared** (DEC-056)",StringComparison.Ordinal)
                && publishing.Contains("| Télécharger et lancer un programme | **Une exception, déclarée** (DEC-056)",StringComparison.Ordinal),"row in both halves");
            var decisions=File.ReadAllText(Path.GetFullPath(Path.Combine("docs","project","DECISIONS.md")));
            Check(decisions.Contains("ID: DEC-056",StringComparison.Ordinal) && decisions.Contains("DEC-ID: DEC-056",StringComparison.Ordinal),"public decision in both halves");
        }),
    ];
}
