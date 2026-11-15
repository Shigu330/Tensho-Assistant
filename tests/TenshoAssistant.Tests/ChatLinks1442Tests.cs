using TenshoAssistant.Chat;

// 0.1442 (X-403 phase 2, second part; D-166, DEC-053): quest links kept and drawn as links; a message, or every message of a tab, copied as
// the window writes it.
internal static class ChatLinks1442Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).Replace("\r\n","\n");
    private static readonly DateTimeOffset Noon = new(2026,11,15,12,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat links 1442: a quest link's text is the quest's, a role Link run",() =>
        {
            var quest = new ChatLink(ChatSegmentKind.QuestLink,QuestId:65564);
            var body = ChatPieces.Segments([new("Do "),new(null,quest),new("Close to Home"),new(null,End:true),new(" first")]);
            Check(body.Count == 3 && body[1].Kind == ChatSegmentKind.QuestLink && body[1].Link!.QuestId == 65564,"the quest's segment");
            var runs = ChatFormat.Runs(new ChatRecord(1,Noon,14,ChatChannel.Party,"Ann",body),new ChatSettings{ShowTimestamps = false,ShowChannel = false},_ => "");
            Check(runs.Single(run => run.Link == quest).Role == ChatRole.Link,"drawn as a link");
        }),
        ("Chat links 1442: a message copied as the window writes it; a tab one message per line",() =>
        {
            var settings = new ChatSettings{ShowTimestamps = false,ShowChannel = true};
            var first = new ChatRecord(1,Noon,14,ChatChannel.Party,"Ann",[new("hello "),new("Limsa",ChatSegmentKind.MapLink,new ChatLink(ChatSegmentKind.MapLink,129,12,9.5f,11.2f))]);
            var second = new ChatRecord(2,Noon,57,ChatChannel.System,"",[new("You invite Bob to a party.")]);
            Check(ChatFormat.Plain(first,settings,_ => "P") == "[P] Ann: hello Limsa",ChatFormat.Plain(first,settings,_ => "P"));
            Check(ChatFormat.Plain([first,second],settings,_ => "P") == "[P] Ann: hello Limsa\nYou invite Bob to a party.","the tab");
            Check(ChatFormat.Plain([],settings,_ => "P") == "","no message: empty");
        }),
        ("Chat links 1442: the reader keeps quest links; the window opens them on the Quests page and has a message menu; both languages",() =>
        {
            // 0.1468 (X-479): told apart by the reader (ChatPayload), made into a piece by ChatPayloads.
            Check(Read("Chat/ChatReader.Dalamud.cs").Contains("QuestPayload quest=>new(ChatPayloadKind.Quest,\"Quest\",Link:new ChatLink(ChatSegmentKind.QuestLink,QuestId:quest.Quest.RowId)),"),"quest read");
            var window = Read("MainWindow.ChatWindow.cs");
            Check(window.Contains("if(ImGui.IsItemClicked(ImGuiMouseButton.Left)){SelectQuest(quest.Id);page=Page.Quests;IsOpen=true;}"),"quest: the Quests page");
            Check(window.Contains("ImGui.IsItemClicked(ImGuiMouseButton.Right)){chatWindowLine=record;ImGui.OpenPopup(\"###TenshoChatLine\");}")   /* 0.1472 (X-482 M2): not under the button */
                && window.Contains("ImGui.SetClipboardText(ChatFormat.Plain(said,options,ChatChannelShort,ChatLinkName));"),"a message's menu: copy");   // 0.1463 (X-461): with the links' names
            foreach(var lang in new[]{"en","fr"})
            {
                var text = Read("Localization/Languages/" + lang + ".json");
                foreach(var key in new[]{"QuestLinkTip","CopyLine","CopyTab"})Check(text.Contains("    \"" + key + "\": "),lang + ": " + key);
            }
        }),
    ];
}
