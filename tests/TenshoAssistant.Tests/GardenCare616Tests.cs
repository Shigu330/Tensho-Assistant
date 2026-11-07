using TenshoAssistant.Reminders;

// 0.616: tending and uprooting read from the chat (lines captured in game on 2026-11-06).
internal static class GardenCare616Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static GardenChat French()=>new(null,null,true,["Le semis gagne en vigueur !","Le semis gagne en vigueur !"],["Vous avez arraché le semis.","Vous avez arraché le semis."]);
    private static GardenBed Bed(string place)=>new(){Place=place,Seed="Seed"};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
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
        ("Garden care: several beds are not guessed, one is acted on; the capture stays for the harvest",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Garden.cs"));
            Check(view.Contains("if(here.Length>1){ChatNotice?.Invoke(localization.Get(\"Garden.CareSeveral\"));return;}"),"several: said, nothing changed");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GardenChatWatcher.Dalamud.cs")).Contains("Garden chat capture"),"the harvest line is still to capture");
        }),
    ];
}
