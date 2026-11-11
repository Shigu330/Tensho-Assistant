// Test audit of 2026-11-09, batch 2 (0.1021): Garden448Tests, GardenChat460Tests, GardenCare616Tests, GardenHarvest978Tests merged into GardenJournalTests; every case is kept.
using TenshoAssistant.Reminders;
internal static class GardenJournalTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
    private static GardenChat French()=>new(null,null,true,["Le semis gagne en vigueur !","Le semis gagne en vigueur !"],["Vous avez arraché le semis.","Vous avez arraché le semis."]);
    private static GardenBed Bed(string place)=>new(){Place=place,Seed="Seed"};
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        // From Garden448Tests: N-08 (0.448, DEC-026): the garden journal; growing and tending hours are typed by the player, each reminder is said once.
        ("Garden journal: ripe and tending times from the typed hours, each reminder said once",() =>
        {
            var start=new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            var bed=GardenJournal.Plant("House, pot 1","Seed",30,10,start);
            Check(GardenJournal.RipeAt(bed)==start.AddHours(30) && GardenJournal.TendAt(bed)==start.AddHours(10),"times");
            Check(GardenJournal.State(bed,start.AddHours(9))==GardenState.Growing && GardenJournal.State(bed,start.AddHours(10))==GardenState.ToTend,"tend due");
            Check(GardenJournal.Due([bed],start.AddHours(9)).Count==0,"nothing yet");
            Check(GardenJournal.Due([bed],start.AddHours(11)) is [(_, GardenState.ToTend)] && GardenJournal.Due([bed],start.AddHours(12)).Count==0,"tend said once");
            GardenJournal.Tend(bed,start.AddHours(12));
            Check(GardenJournal.TendAt(bed)==start.AddHours(22) && GardenJournal.State(bed,start.AddHours(13))==GardenState.Growing,"tending moves the next one");
            Check(GardenJournal.TendAt(bed) is not null && GardenJournal.Due([bed],start.AddHours(23)) is [(_, GardenState.ToTend)],"next tending said");
            Check(GardenJournal.Due([bed],start.AddHours(31)) is [(_, GardenState.Ripe)] && GardenJournal.Due([bed],start.AddHours(40)).Count==0,"ripe said once");
            GardenJournal.Tend(bed,start.AddHours(24));
            Check(GardenJournal.TendAt(bed) is null,"no tending once the next one would come after the harvest");
        }),
        ("Garden journal: no tending reminder at 0 h, typed values bounded, texts cleaned, future plantings silent",() =>
        {
            var start=new DateTimeOffset(2026,11,3,12,0,0,TimeSpan.Zero);
            var plain=GardenJournal.Plant("Pot","Seed",5,0,start);
            Check(GardenJournal.TendAt(plain) is null && GardenJournal.State(plain,start.AddHours(4))==GardenState.Growing,"no tending");
            Check(GardenJournal.ClampGrow(0)==1 && GardenJournal.ClampGrow(9999)==GardenJournal.MaximumGrowHours && GardenJournal.ClampTend(-3)==0 && GardenJournal.ClampTend(500)==GardenJournal.MaximumTendHours,"bounds");
            Check(GardenJournal.CleanText("  Pot 1 ")=="Pot 1" && GardenJournal.CleanText("")==null && GardenJournal.CleanText("a\nb")==null && GardenJournal.CleanText(new string('x',41))==null,"texts");
            var future=GardenJournal.Plant("Pot","Seed",1,1,start.AddDays(1));
            Check(GardenJournal.Due([future],start).Count==0,"planted in the future: silent");
            Check(GardenJournal.Due(null,start).Count==0,"no journal");
        }),
        ("Garden journal: kept by « reset all », reminders a reset setting, checked every 10 s from the frame, nothing read from the game",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");
            // 0.1019 (test audit #24): the lists themselves, not the text of Configuration.cs.
            Check(TenshoAssistant.ConfigurationReset.Kept.Contains("GardenBeds") && TenshoAssistant.ConfigurationReset.Reset.Contains("GardenAlerts") && new TenshoAssistant.Configuration().GardenAlerts,"kept list, reset setting");
            var ui=Read("MainWindow.Garden.cs");
            Check(ui.Contains("if(now-gardenObservedAt<TimeSpan.FromSeconds(10))return;") && ui.Contains("if(!configuration.GardenAlerts ||"),"throttled, setting");
            Check(!ui.Contains("unsafe") && !ui.Contains("FFXIVClientStructs"),"no game reading");
            Check(Read("Plugin.cs").Contains("mainWindow.ObserveGarden"),"wired (the page's sections: Todo458Tests)");
        }),
        // From GardenChat460Tests: 0.460 (in-game campaign): plantings noted from the game's chat lines (docs/evidence/0.459-garden-detection.md).
        ("Garden chat: the planting line is read in each language, bed and patch in the row's order; a player's line or another line never counts",() =>
        {
            var french=new GardenChat(["Vous avez planté des graines dans l'emplacement ",null," du potager ",null,"."],["Vous avez planté des graines dans le pot."],GardenChat.BedComesFirst("French"));
            Check(french.Read("","Vous avez planté des graines dans l'emplacement 5 du potager 2.") is { Kind:GardenChatKind.PlantedBed, Patch:2, Bed:5 },"French: bed, then patch");
            Check(french.Read("","Vous avez planté des graines dans le pot.") is { Kind:GardenChatKind.PlantedPot, Patch:null, Bed:null },"pot");
            var english=new GardenChat(["You plant seeds in the ",null," bed, ",null," patch."],null,GardenChat.BedComesFirst("English"));
            Check(english.Read("","You plant seeds in the 3rd bed, 1st patch.") is { Patch:1, Bed:3 },"English ordinals");
            var german=new GardenChat(["Du hast etwas in Beet ",null,", Furche ",null,", ausgesät."],null,GardenChat.BedComesFirst("German"));
            Check(german.Read("","Du hast etwas in Beet 1, Furche 4, ausgesät.") is { Patch:1, Bed:4 },"German: patch, then bed");
            Check(!GardenChat.BedComesFirst("Japanese"),"Japanese: patch first");
            Check(french.Read("Player Name","Vous avez planté des graines dans le pot.") is null,"a player's line");
            Check(french.Read("","Vous avez fertilisé le semis.") is null && french.Read("","") is null,"another line");
            Check(french.Read("","Vous avez planté des graines dans l'emplacement 9 du potager 1.") is null && french.Read("","Vous avez planté des graines dans l'emplacement 1 du potager 4.") is null,"out of range");
            Check(new GardenChat(null,null,true).Read("","Vous avez planté des graines dans le pot.") is null,"rows unknown: nothing");
        }),
        ("Garden journal: a detected planting replaces the bed at that place; a full journal refuses it",() =>
        {
            var now=new DateTimeOffset(2026,11,4,12,0,0,TimeSpan.Zero);
            var old=GardenJournal.Plant("Plot · patch 1, bed 1","Old",24,0,now.AddDays(-3));
            var other=GardenJournal.Plant("Plot · patch 1, bed 2","Other",24,0,now.AddDays(-3));
            var beds=GardenJournal.Detected([old,other],"Plot · patch 1, bed 1","New",120,48,now)!;
            Check(beds.Count==2 && beds.Contains(other) && !beds.Contains(old),"replaced");
            var added=beds.Single(bed=>bed.Seed=="New");
            Check(added.PlantedAt==now && added.GrowHours==120 && added.TendHours==48,"hours");
            var full=Enumerable.Range(0,GardenJournal.Capacity).Select(index=>GardenJournal.Plant("Pot "+index,"Seed",1,0,now)).ToList();
            Check(GardenJournal.Detected(full,"Elsewhere","New",1,0,now) is null && GardenJournal.Detected(full,"Pot 3","New",1,0,now)?.Count==GardenJournal.Capacity,"capacity");
        }),
        ("Garden detection: read from the chat, on by default with its setting, seed read only, no game function that acts, said on the page",() =>
        {
            var watcher=Read("Reminders/GardenChatWatcher.Dalamud.cs");
            Check(watcher.Contains("chat.ChatMessageUnhandled+=OnMessage;") && watcher.Contains("chat.ChatMessageUnhandled-=OnMessage;") && watcher.Contains("TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);"),"subscribed and released");
            Check(watcher.Contains("agent->SelectedItems") && !watcher.Contains("ConfirmSeedAndSoilSelection"),"read only");
            Check(new TenshoAssistant.Configuration().GardenDetect && TenshoAssistant.ConfigurationReset.Reset.Contains("GardenDetect"),"setting");
            foreach(var language in new[]{"en","fr"})
                Check(Read($"Localization/Languages/{language}.json").Contains(language=="en" ? "from the game's chat lines":"à partir des lignes du chat du jeu"),"explained: "+language);
            Check(Read("MainWindow.Garden.cs").Contains("if(known.Seed is null){ChatNotice?.Invoke(localization.Get(\"Garden.DetectedUnknown\"));return;}"),"an unknown seed is not guessed");
        }),
        // From GardenCare616Tests: 0.616: tending and uprooting read from the chat (lines captured in game on 2026-11-06).
        ("Garden care: the game's lines, as the chat writes them and as the sheet does",() =>
        {
            Check(French().Read("","Le semis gagne en vigueur!")?.Kind==GardenChatKind.Tended,"the chat, without the space (captured)");
            Check(French().Read("","Le semis gagne en vigueur !")?.Kind==GardenChatKind.Tended,"the sheet, with it");
            Check(French().Read("","Vous avez arraché le semis.")?.Kind==GardenChatKind.Uprooted,"uprooted (captured)");
            Check(new GardenChat(null,null,true,["Mot\u00ADif !"]).Read("","Motif!")?.Kind==GardenChatKind.Tended,"an invisible hyphen in the sheet");
        }),
        ("Garden care: a player's words, or another line, are not it",() =>
        {
            Check(French().Read("Someone","Le semis gagne en vigueur!") is null,"a player");
            Check(French().Read("","Le semis de poignée de prunes sylphes se porte bien.") is null,"looking at the plant is not tending it");
            Check(new GardenChat(null,null,true).Read("","Le semis gagne en vigueur!") is null,"no sheet read: nothing");
        }),
        ("Garden care: the bed at this place, nothing guessed",() =>
        {
            GardenBed[] beds=[Bed("Shirogane 2-17 · potager 1, emplacement 6"),Bed("Shirogane 2-17 · potager 1, emplacement 7"),Bed("Lavender Beds 3-1 · potager 2, emplacement 1"),Bed("Shirogane 2-170 · potager 1, emplacement 1"),Bed("Ma note")];
            Check(GardenJournal.At(beds,"Shirogane 2-17").Length==2,"two here, not the longer number");
            Check(GardenJournal.At(beds,"Lavender Beds 3-1") is [{Place:"Lavender Beds 3-1 · potager 2, emplacement 1"}],"one");
            Check(GardenJournal.At(beds,"Mist 1-1").Length==0 && GardenJournal.At(beds,null).Length==0 && GardenJournal.At(null,"Mist 1-1").Length==0,"none, or unknown place");
            Check(GardenJournal.At(beds,"Ma note").Length==1,"a place written alone");
        }),
        ("Garden care: several beds are not guessed, one is acted on; no capture line any more (#36, 0.1110)",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Garden.cs"));
            Check(view.Contains("if(here.Length>1){ChatNotice?.Invoke(localization.Get(\"Garden.CareSeveral\"));return;}"),"several: said, nothing changed");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs")).Contains("Garden chat capture"),"tending and harvest recognized: the capture line is gone");
        }),
        // From GardenHarvest978Tests: #36 (0.978, docs/evidence/0.978-garden-harvest.md): a harvest is an item gain line while a garden bed is the target.
        ("Garden harvest 978: gain line on a bed only, no sender, wired to the journal",() =>
        {
            Check(GardenChat.IsHarvest(62,"",2003757),"harvest");
            Check(!GardenChat.IsHarvest(62,"",0) && !GardenChat.IsHarvest(57,"",2003757) && !GardenChat.IsHarvest(62,"Player",2003757),"not a harvest");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs"));
            Check(watcher.Contains("InHousing() && GardenChat.IsHarvest((int)message.LogKind,sender,target())"),"housing only");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Garden.cs"));
            Check(page.Contains("\"Garden.HarvestedNotice\"") && page.Contains("kind==GardenChatKind.Harvested"),"journal");
        }),
    ];
}
