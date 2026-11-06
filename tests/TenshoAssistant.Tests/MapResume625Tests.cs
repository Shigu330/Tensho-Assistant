using TenshoAssistant.Maps;

// 0.625 (user report): the map closed by the game during an NPC's menu comes back.
internal static class MapResume625Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Workshop projects (C22): quantity per delivery by deliveries, totals over the phases",() =>
        {
            var material=new TenshoAssistant.FreeCompany.WorkshopMaterial(5094,3,3);
            Check(material.Total==9,"the sheet's first row: 3 per delivery, 3 deliveries");
            var project=new TenshoAssistant.FreeCompany.WorkshopProject(1,100,"Sous-marin",[new("Coque",[new(1,[material,new(7020,2,5)]),new(2,[new(5094,1,4)])])]);
            var totals=project.Totals();
            Check(totals.Count==2 && totals[0]==(5094u,13) && totals[1]==(7020u,10),"each item once, its whole quantity, first come first");
            Check(TenshoAssistant.FreeCompany.WorkshopStock.Missing(9,4)==5 && TenshoAssistant.FreeCompany.WorkshopStock.Missing(9,20)==0 && TenshoAssistant.FreeCompany.WorkshopStock.Missing(9,-1)==9,"what is missing");
            var loader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/WorkshopProjectsLoader.Dalamud.cs"));
            Check(loader.Contains("process.SetQuantity[index],process.SetsRequired[index]"),"the two columns, by the same index");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.WorkshopProjects.cs")).Contains("workshopProjects??=Task.Run(load);"),"off the frame, once");
        }),
        ("Teleport list (0.662): the game's function is called only with the player there for a moment",() =>
        {
            var at=new DateTimeOffset(2026,11,6,12,0,0,TimeSpan.Zero);
            var gate=new TenshoAssistant.Maps.TeleportListGate();
            Check(!gate.Allowed(false,false,at),"no player");
            Check(!gate.Allowed(true,true,at.AddSeconds(1)),"between zones");
            Check(!gate.Allowed(true,false,at.AddSeconds(2)),"just back");
            Check(!gate.Allowed(true,false,at.AddSeconds(4)),"not yet");
            Check(gate.Allowed(true,false,at.AddSeconds(5)),"settled");
            Check(!gate.Allowed(false,false,at.AddSeconds(6)) && !gate.Allowed(true,false,at.AddSeconds(7)),"it starts again after a change of zone");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs")).Replace("\r\n","\n");
            Check(reader.Contains("if(listAllowed && Interlocked.Exchange(ref costsRequest,0)==1)"),"a request waits, it is not spent");
            Check(reader.IndexOf("listAllowed=teleportGate.Allowed")<reader.IndexOf("ReadTeleportCosts());"),"checked first");
        }),
        ("Map resume: closed during an event, back once free for a moment, once",() =>
        {
            var at=new DateTimeOffset(2026,11,6,12,0,0,TimeSpan.Zero);
            var resume=new MapResume();
            resume.GameClosedMap(true);
            Check(!resume.ShouldReopen(true,false,100,at),"still talking");
            Check(!resume.ShouldReopen(false,false,100,at.AddSeconds(1)),"free for a moment only");
            Check(resume.ShouldReopen(false,false,100,at.AddSeconds(3)),"free again");
            Check(!resume.ShouldReopen(false,false,100,at.AddSeconds(4)),"only once");
        }),
        ("Map resume: closed by the player or opened meanwhile: nothing",() =>
        {
            var at=new DateTimeOffset(2026,11,6,12,0,0,TimeSpan.Zero);
            var resume=new MapResume();
            resume.GameClosedMap(false);
            Check(!resume.ShouldReopen(false,false,100,at) && !resume.ShouldReopen(false,false,100,at.AddSeconds(5)),"the map key: it stays closed");
            Check(!resume.ShouldReopen(false,false,200,at.AddSeconds(9)),"the map key, then another zone: it stays closed");
            resume.GameClosedMap(true);
            Check(!resume.ShouldReopen(true,true,100,at) && !resume.ShouldReopen(false,false,100,at.AddSeconds(5)),"already open again");
            resume.GameClosedMap(true);
            resume.Cancel();
            Check(!resume.ShouldReopen(false,false,100,at.AddSeconds(5)),"cancelled");
        }),
        ("Map resume (0.658, 0.661): a teleport or a duty brings the map back; the gap before the loading screen does not",() =>
        {
            var at=new DateTimeOffset(2026,11,6,12,0,0,TimeSpan.Zero);
            var resume=new MapResume();
            resume.GameClosedMap(true);
            Check(!resume.ShouldReopen(false,false,958,at),"the flags drop");
            Check(!resume.ShouldReopen(false,false,958,at.AddSeconds(1)),"a second later, not yet");
            Check(!resume.ShouldReopen(true,false,958,at.AddSeconds(1.2)),"the loading screen");
            Check(!resume.ShouldReopen(false,false,0,at.AddSeconds(5)),"no territory yet");
            Check(!resume.ShouldReopen(false,false,130,at.AddSeconds(6)),"arrived: the moment starts again");
            Check(resume.ShouldReopen(false,false,130,at.AddSeconds(8)),"arrived in another zone");
            Check(!resume.ShouldReopen(false,false,130,at.AddSeconds(9)),"once");
            resume.GameClosedMap(true);
            Check(!resume.ShouldReopen(true,false,1000,at.AddSeconds(10)),"a duty's opening scene");
            Check(!resume.ShouldReopen(false,false,1000,at.AddSeconds(11)) && resume.ShouldReopen(false,false,1000,at.AddSeconds(13)),"free in the duty: back too");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs")).Contains("ConditionFlag.Casting"),"the map key during a cast stays the player's");
        }),
        ("Job progress (T21): one row per experience slot, the job standing for its class, in the game's order",() =>
        {
            // ClassJob rows: 1 gladiator and 19 paladin share slot 1; 8 carpenter alone in slot 8.
            var jobs=TenshoAssistant.Gear.JobProgressList.Jobs(new Dictionary<uint,int>{[1]=1,[19]=1,[8]=8},new Dictionary<uint,int>{[19]=1,[8]=30});
            Check(string.Join(",",jobs.Select(job=>job.Job))=="19,8","paladin first, then carpenter");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,0,0,0) is { Unlocked:false },"level 0: not unlocked");
            Check(Math.Abs(new TenshoAssistant.Gear.JobProgressRow(19,90,50,200).Fraction-0.25f)<0.001f,"share of the level");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,100,0,0).Fraction==1,"nothing to earn");
            Check(new TenshoAssistant.Gear.JobProgressRow(19,90,500,200).Fraction==1,"never past the bar");
        }),
        ("Duty unlocks (T13): the finder row first, then the quest's own column; unknown stays 0",() =>
        {
            Check(TenshoAssistant.Duties.DutyUnlocks.FromFinder(1,67092,0,0)==67092,"le Palais des morts");
            Check(TenshoAssistant.Duties.DutyUnlocks.FromFinder(0,0,1,69408)==69408,"second pair");
            Check(TenshoAssistant.Duties.DutyUnlocks.FromFinder(2,20045,1,69501)==69501,"another duty first, then the quest");
            Check(TenshoAssistant.Duties.DutyUnlocks.FromFinder(4,594,0,0)==0,"not a quest");
            var duties=TenshoAssistant.Duties.DutyUnlocks.Apply([new TenshoAssistant.Duties.DutyDefinition(1,10,100,"A","Dungeon",15),new TenshoAssistant.Duties.DutyDefinition(2,20,200,"B","Dungeon",16,0,77),new TenshoAssistant.Duties.DutyDefinition(3,30,300,"C","Dungeon",17)],new Dictionary<uint,uint>{[10]=65579,[20]=99});
            Check(duties[0].UnlockQuest==65579 && duties[1].UnlockQuest==77 && duties[2].UnlockQuest==0,"filled only where unknown");
            var catalog=new TenshoAssistant.Duties.DutyCatalog(duties);
            Check(catalog.UnlockedBy(65579).Single().DutyId==1 && !catalog.UnlockedBy(0).Any(),"and the other way round");
        }),
        ("Gathering sources (C29): spearfishing points name their fish, and the cache follows",() =>
        {
            var loader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/GatheringCatalogLoader.Dalamud.cs"));
            Check(loader.Contains("reference.RowType == typeof(SpearfishingItem)"),"read before the gathering items are filtered");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/GatheringCatalogCache.cs")).Contains("CopyRules=2"),"an old cache without them is rebuilt");
        }),
        ("Company actions (T27): the statuses paired with the timers in order, or no end at all",() =>
        {
            var now=new DateTimeOffset(2026,11,6,10,0,0,TimeSpan.Zero);
            // The reading of 2026-11-06: 19131 and 19135 seconds at the last update, 76 seconds ago.
            var read=TenshoAssistant.Reminders.CompanyBenefits.Build([364,2932],[19131,19135,0],76,now);
            Check(read[0].Status==364 && read[0].Ends==now.AddSeconds(19055),"first status, first timer");
            Check(read[1].Status==2932 && read[1].Ends==now.AddSeconds(19059),"second status, second timer");
            Check(TenshoAssistant.Reminders.CompanyBenefits.Build([364,2932],[0,0,0],0,now).All(benefit=>benefit.Ends is null),"window not opened: unknown");
            Check(TenshoAssistant.Reminders.CompanyBenefits.Build([364],[19131,19135,0],0,now).All(benefit=>benefit.Ends is null),"counts differ: unknown");
            Check(TenshoAssistant.Reminders.CompanyBenefits.Build([364],[50],76,now).All(benefit=>benefit.Ends is null),"run out: unknown");
            Check(TenshoAssistant.Reminders.CompanyBenefits.Build([],[0,0,0],0,now).Count==0,"none active");
        }),
        ("Reading capture: developer mode only, stopped by the competitive cut, limited",() =>
        {
            var code=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Diagnostics/ReadingCapture.Dalamud.cs"));
            Check(code.Contains("CompetitiveGate.Add(framework,OnUpdate)") && code.Contains("CompetitiveGate.Remove(framework,OnUpdate)"),"the frame goes through the gate");
            Check(code.Contains("CompetitiveGate.Cut") && code.Contains("ChatMessageUnhandled-=OnMessage"),"the chat too, with its cleanup");
            Check(code.Contains("if(disposed || !enabled())return;"),"nothing outside developer mode");
            Check(code.Contains("if(count>=limit)return;"),"a limit on the lines");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs")).Contains("()=>configuration.DeveloperMode,log)"),"wired to the setting");
        }),
        ("Map resume: wired where the game closes its map",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Maps/NativeMapReplacer.Dalamud.cs"));
            Check(view.Contains("resume.GameClosedMap(ClosedByEvent());"),"noted on closing");
            Check(view.Contains("if(resume.ShouldReopen(ClosedByEvent(),visible,client.TerritoryType,now)"),"asked at each tick");
            Check(view.Contains("resume.Cancel();"),"not after the competitive cut");
        }),
    ];
}
