using TenshoAssistant.Reminders;

// 0.460 (in-game campaign): plantings noted from the game's chat lines (docs/evidence/0.459-garden-detection.md).
internal static class GardenChat460Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
    ];
}
