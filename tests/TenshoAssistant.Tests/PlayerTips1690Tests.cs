using TenshoAssistant.Chat;

// 0.1690 (X-684, ninth part, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one player tooltip (PlayerTip,
// ChatCopy.SenderTip: name and world, then job and level) for the chat window's senders and the Tensho map's party members, on the map and at
// its edge (before: « name · level » there, without world nor job); built once per member state, not every frame.
internal static class PlayerTips1690Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Player tips 1690: a party member says its world, job and level, as a chat sender does", () =>
        {
            static string W(string who, string where) => $"{who} ({where})";
            static string J(string job, int level) => $"{job}, level {level}";
            // MapPartyMarker lives in the Dalamud reader (not compiled here): its world is checked on the code in the wiring case.
            Check(ChatCopy.SenderTip("Ann Doe", "Moogle", "White Mage", 100, W, J) == "Ann Doe (Moogle)\nWhite Mage, level 100", "name and world, job and level");
            Check(ChatCopy.SenderTip("Ann Doe", "", null, 100, W, J) == "Ann Doe", "neither world nor job known: the name only, no level alone");
            Check(ChatCopy.SenderTip("Ann Doe", "", "Paladin", 90, W, J) == "Ann Doe\nPaladin, level 90", "a world not read: name, then job and level");
        }),
        ("Player tips 1690: wiring: one body for the chat and the map; the world read with the party; cached per member state and language", () =>
        {
            var body = Read("src/TenshoAssistant/MainWindow.PlayerTooltip.cs");
            Check(body.Contains("private string PlayerTip(string name,string world,string? job,int level)", StringComparison.Ordinal) && body.Contains("Chat.ChatCopy.SenderTip(name,world,job,level,", StringComparison.Ordinal), "the body");
            Check(body.Contains("if(partyTips.TryGetValue(key,out var tip))return tip;", StringComparison.Ordinal) && body.Contains("partyTipsRevision!=localization.Revision", StringComparison.Ordinal), "built once per member state and language");
            var chat = Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(chat.Contains("return PlayerTip(name,world,seen?.Job,seen?.Level ?? 0);", StringComparison.Ordinal) && !chat.Contains("ChatCopy.SenderTip(", StringComparison.Ordinal), "the chat's senders through it");
            var map = Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(map.Contains("{tooltip=PartyMemberTip(member);", StringComparison.Ordinal) && map.Contains("0,PartyMemberTip(member),false,member.Job));", StringComparison.Ordinal) && !map.Contains("MapView.PartyMember", StringComparison.Ordinal), "the map's party, on the map and at its edge");
            var reader = Read("src/TenshoAssistant/Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("(ushort)member.World.RowId));", StringComparison.Ordinal) && reader.Contains("public sealed record MapPartyMarker(string Name,uint Job,int Level,Vector3 Position,ushort World=0);", StringComparison.Ordinal), "the world read with the party, 0 when not read");
            foreach (var code in new[] { "en", "fr" })
                Check(!Read($"src/TenshoAssistant/Localization/Languages/{code}.json").Contains("\"PartyMember\": \"", StringComparison.Ordinal), code + ": the old label gone");
        }),
    ];
}
