using TenshoAssistant.Chat;

// 0.1659 (X-696, review of 0.1656 to 0.1658): M1, a default tab put back while the removed one waits to be undone no longer comes back twice;
// M2, the settings that leave no chat shown in combat or in a duty are said under them.
internal static class ChatReview1659Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    private static List<ChatTabSettings> Defaults()=>ChatSettings.DefaultTabs("General","Battle","Event","Social");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat review 1659 (M1): « Restore « Battle » » during the undo, then « Undo »: one « Battle », not two",()=>
        {
            var tabs=Defaults();
            var undo=new ChatTabUndo();
            Check(undo.Remove(tabs,1,1_000) && tabs.Count==3,"Battle removed");
            var missing=ChatSettings.MissingDefaults(tabs,"General","Battle","Event","Social");
            Check(missing.Count==1 && missing[0].Tab.Name=="Battle","Battle is missing");
            Check(ChatSettings.AddDefault(tabs,missing[0].Place,missing[0].Tab),"Battle put back");
            undo.Replaced(missing[0].Tab);
            Check(undo.Waiting(2_000) is null && !undo.Undo(tabs,2_000),"the undo is gone");
            Check(tabs.Count(tab=>tab.Name=="Battle")==1 && tabs.Count==4,"one Battle only");
        }),
        ("Chat review 1659 (M1): a default tab put back that the removed one does not stand for leaves the undo as it was",()=>
        {
            var tabs=Defaults();
            tabs.Add(new(){Name="Mine",Channels=[ChatChannel.Say]});
            var undo=new ChatTabUndo();
            undo.Remove(tabs,4,0);   // « Mine »
            tabs.RemoveAt(2);         // « Event », not waiting
            var missing=ChatSettings.MissingDefaults(tabs,"General","Battle","Event","Social");
            Check(missing.Count==1 && missing[0].Tab.Name=="Event" && ChatSettings.AddDefault(tabs,missing[0].Place,missing[0].Tab),"Event put back");
            undo.Replaced(missing[0].Tab);
            Check(undo.Waiting(1) is { Name:"Mine" } && undo.Undo(tabs,1) && tabs.Count(tab=>tab.Name=="Mine")==1,"« Mine » still comes back");
        }),
        ("Chat review 1659 (M1): the settings call Replaced right after a default tab is put back",()=>
        {
            var text=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            var at=text.IndexOf("ChatSettings.AddDefault(tabs,place,missing)",StringComparison.Ordinal);
            Check(at>0 && text.IndexOf("chatTabUndo.Replaced(missing);",at,StringComparison.Ordinal) is var called && called>at && called-at<200,"Replaced after AddDefault");
        }),
        ("Chat review 1659 (M2): no chat in combat or in a duty only with the game's messages hidden, not both chats, and one of the two hidings",()=>
        {
            Check(ChatHideWants.LeavesNoChat(true,false,true,false) && ChatHideWants.LeavesNoChat(true,false,false,true) && ChatHideWants.LeavesNoChat(true,false,true,true),"hidden game messages and a hiding: no chat");
            Check(!ChatHideWants.LeavesNoChat(false,false,true,true),"the game's messages shown: its chat stays");
            Check(!ChatHideWants.LeavesNoChat(true,true,true,true),"both chats: the game's chat stays");
            Check(!ChatHideWants.LeavesNoChat(true,false,false,false),"no hiding: Tensho's chat stays");
        }),
        ("Chat review 1659 (M2): the warning is drawn under the settings and the tooltips say it, in both languages",()=>
        {
            var text=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(text.Contains("ChatHideWants.LeavesNoChat(configuration.Chat.HideGameMessages,configuration.Chat.BothChats,configuration.Chat.HideInCombat,configuration.Chat.HideInDuty)")
                  && text.Contains("localization.Get(\"ChatWindow.NoChatWarning\")"),"the warning line");
            var en=Read("src/TenshoAssistant/Localization/Languages/en.json");
            var fr=Read("src/TenshoAssistant/Localization/Languages/fr.json");
            Check(en.Contains("no chat is shown in combat") && en.Contains("no chat is shown in the duty") && en.Contains("\"NoChatWarning\":"),"English");
            Check(fr.Contains("aucun chat n'est visible en combat") && fr.Contains("aucun chat n'est visible dans la mission") && fr.Contains("\"NoChatWarning\":"),"French");
        }),
    ];
}
