using TenshoAssistant.Chat;

// 0.1617 (X-455 E2, E3, E4, E7, the user's choices of 15/11): Ctrl+A and Ctrl+C in the chat window, a player's name with their world copied,
// a place, an item or a quest copied from its menu, a tab cleared (the other tabs keep their messages) and shown again.
internal static class ChatCopy1617Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,15,0,0,TimeSpan.Zero);
    private static ChatRecord Say(long id,int seconds,string sender="Ann Doe@Moogle",string text="hello")=>new(id,At.AddSeconds(seconds),10,ChatChannel.Say,sender,[new ChatSegment(text)]);
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath(Path.Combine("src","TenshoAssistant",name))).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat copy 1617: E2, Ctrl+C copies the whole tab once chosen, else the message under the mouse, else nothing",()=>
        {
            ChatRecord[] tab=[Say(1,0),Say(2,1),Say(3,2)];
            Check(ChatCopy.Chosen(true,tab[1],()=>tab)!.Count==3,"whole tab");
            Check(ChatCopy.Chosen(false,tab[1],()=>tab) is [{ Id:2 }],"the hovered message");
            Check(ChatCopy.Chosen(false,null,()=>tab) is null,"nothing");
        }),
        ("Chat copy 1617: E3, a player's name with their world, as the game writes it elsewhere",()=>
        {
            Check(ChatCopy.Player("Ann Doe","Moogle")=="Ann Doe@Moogle" && ChatCopy.Player(" Ann Doe ","")=="Ann Doe" && ChatCopy.Player("Ann Doe",null)=="Ann Doe","name and world");
            Check(ChatCopy.WorldOf("Ann Doe@Moogle")=="Moogle" && ChatCopy.WorldOf("Ann Doe")=="" && ChatCopy.WorldOf(null)=="" && ChatCopy.WorldOf("@x")=="","world after « @ »");
        }),
        ("Chat copy 1617: E7, behaviour: a cleared tab drops its messages until then, keeps the later ones, and shows them again; another tab keeps them",()=>
        {
            var store=new ChatStore();
            store.Add(At,10,"Ann Doe",[new ChatSegment("one")],1000);store.Add(At.AddSeconds(10),10,"Ann Doe",[new ChatSegment("two")],1000);
            var cleared=new ChatTabSettings{Name="A",Channels=[ChatChannel.Say],ClearedAt=At.AddSeconds(5)};
            var other=new ChatTabSettings{Name="B",Channels=[ChatChannel.Say]};
            var a=new ChatTabView();a.Refresh(store,cleared,1000);
            var b=new ChatTabView();b.Refresh(store,other,1000);
            Check(a.Count==1 && a.RecordAt(0).Time==At.AddSeconds(10),"the cleared tab: only the later message, "+a.Count);
            Check(b.Count==2,"the other tab keeps both");
            store.Add(At.AddSeconds(20),10,"Ann Doe",[new ChatSegment("three")],1000);a.Refresh(store,cleared,1000);
            Check(a.Count==2,"a new message shows in the cleared tab");
            cleared.ClearedAt=null;
            var again=new ChatTabView();again.Refresh(store,cleared,1000);
            Check(again.Count==3,"shown again");
            Check(ChatCopy.AfterClear(Say(4,5),At.AddSeconds(5))==false && ChatCopy.AfterClear(Say(5,6),At.AddSeconds(5)) && ChatCopy.AfterClear(Say(6,0),null),"the boundary");
        }),
        ("Chat copy 1617: wiring: the keys, the menus and the texts in both languages",()=>
        {
            var chat=Read("MainWindow.ChatWindow.cs");
            Check(chat.Contains("if(ImGui.IsKeyPressed(ImGuiKey.A,false))chatWholeTab=view.Serial;") && chat.Contains("ImGui.IsKeyPressed(ImGuiKey.C,false) && ChatCopy.Chosen(") && chat.Contains("ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows)"),"E2 keys, window in front only");
            Check(chat.Contains("if(!chatLinesBlocked && ImGui.IsItemHovered())chatHoveredRecord=record;"),"E2 the hovered message");
            Check(chat.Contains("localization.Format(\"ChatWindow.MenuCopyPlayer\",shown)") && chat.Contains("players.World(source.World)"),"E3");
            Check(chat.Contains("localization.Get(\"ChatWindow.CopyPlace\")") && chat.Contains("ItemLinksMenu(\"##ChatItem\"+id,item,copyName:true);") && chat.Contains("localization.Get(\"ChatWindow.CopyQuest\")"),"E4");
            Check(chat.Contains("ImGui.BeginPopupContextItem(\"###ChatTabMenu\"+index)") && chat.Contains("tab.ClearedAt=clear ? DateTimeOffset.Now:null;if(clear)ChatMessages.Unpin(tab);ChatTabChanged(tab);"),"E7 saved with the settings");
            foreach(var language in new[]{"en","fr"})
            {
                using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
                var keys=document.RootElement.GetProperty("ChatWindow");
                foreach(var key in new[]{"ClearTab","UnclearTab","CopyPlace","CopyItem","CopyQuest","MenuCopyPlayer","WholeTabChosen"})Check(keys.TryGetProperty(key,out _),language+": "+key);
            }
        }),
    ];
}
