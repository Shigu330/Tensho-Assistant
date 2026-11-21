using TenshoAssistant.Chat;

// 0.1642 (X-447 B9, the user's choice of 15/11): messages pinned at the top of a tab, in memory only (R04); a click goes to them.
internal static class ChatPins1642Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,19,45,0,TimeSpan.Zero);
    private static ChatTabSettings Say(string name="Say")=>new(){Name=name,Channels=[ChatChannel.Say]};
    private static ChatRecord Put(ChatStore store,string text,int cap=100)=>store.Add(At,10,"Ann Doe",[new ChatSegment(text)],cap);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat pins 1642: behaviour: pinned per tab in order, once, at most 5; unpinned one or all; a clear drops them all",()=>
        {
            var store=new ChatStore();
            var (say,other)=(Say(),Say("Other"));
            var first=Put(store,"one");var second=Put(store,"two");
            Check(store.Pin(say,first) && store.Pin(say,second) && !store.Pin(say,first),"pinned once each");
            Check(store.Pins(say) is [var a,var b] && a==first && b==second && store.Pins(other).Count==0 && store.Pinned(say,first) && !store.Pinned(other,first),"in order, per tab");
            for(var at=0;at<3;at++)Check(store.Pin(say,Put(store,"more "+at)),"up to the most");
            Check(store.Pins(say).Count==ChatStore.MaximumPins && !store.Pin(say,Put(store,"sixth")),"never more than "+ChatStore.MaximumPins);
            store.Unpin(say,first);
            Check(store.Pins(say).Count==4 && !store.Pinned(say,first),"one unpinned");
            store.Unpin(say);
            Check(store.Pins(say).Count==0,"all unpinned");
            store.Pin(say,second);store.Pin(other,second);
            store.Clear();
            Check(store.Pins(say).Count==0 && store.Pins(other).Count==0,"a clear (logout, unload) drops every pin");
        }),
        ("Chat pins 1642: behaviour: a pin stays while the session lasts, after its line has left the store past the cap",()=>
        {
            var store=new ChatStore();
            var say=Say();
            var pinned=Put(store,"keep me",ChatStore.MinimumLines);
            store.Pin(say,pinned);
            for(var at=0;at<ChatStore.MinimumLines+10;at++)Put(store,"line "+at,ChatStore.MinimumLines);
            Check(store.After(0).All(record=>record.Id!=pinned.Id) && store.Pins(say) is [var kept] && kept==pinned,"the line gone, the pin kept");
        }),
        ("Chat pins 1642: a pin clicked finds its message in the tab, or the one it was folded into; -1 once it has left",()=>
        {
            var store=new ChatStore();
            var say=Say();
            var ids=Enumerable.Range(0,5).Select(at=>Put(store,"m"+at).Id).ToArray();
            var view=store.ViewOf(say);
            view.Refresh(store,say,100);
            Check(view.IndexOfId(ids[2])==2 && view.IndexOfId(ids[4])==4 && view.IndexOfId(ids[0])==0,"found");
            Check(view.IndexOfId(ids[4]+10)==4 && view.IndexOfId(ids[0]-1)==-1,"the last one not after it; none before the first");
            Check(new ChatTabView().IndexOfId(1)==-1,"an empty tab");
        }),
        ("Chat pins 1642: a pinned message on one line: as it is when it fits, else its start and « … » within the width; line breaks as spaces",()=>
        {
            static float Width(string text)=>text.Length;
            Check(ChatLook.Shorten("short",10,Width)=="short","fits");
            var cut=ChatLook.Shorten("a long message here",10,Width);
            Check(cut=="a long me…" && Width(cut)<=10,"cut: "+cut);
            Check(ChatLook.Shorten("two\nlines",20,Width)=="two lines","line breaks");
            Check(ChatLook.Shorten("abc",0.5f,Width)=="…","nothing more fits");
        }),
        ("Chat pins 1642: wiring: the message menu pins and unpins, the pins drawn before the search line, the scroll after the bottom's, a tab cleared unpins",()=>
        {
            var chat=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs"));
            Check(chat.Contains("ChatMessages.Pin(pinTab,said)",StringComparison.Ordinal) && chat.Contains("ChatMessages.Unpin(pinTab,said)",StringComparison.Ordinal),"menu");
            Check(chat.IndexOf("DrawChatPins(tabs[shown],options);",StringComparison.Ordinal) is var pins and >0 && pins<chat.IndexOf("if(chatSearchOpen)view=DrawChatSearchLine(",StringComparison.Ordinal),"before the search line");
            Check(chat.IndexOf("else if(button is { } bottom)DrawChatBottomButton(",StringComparison.Ordinal) is var bottom and >0
                && bottom<chat.IndexOf("if(chatScrollTo>=0){var pinned=view.IndexOfId(chatScrollTo);",StringComparison.Ordinal),"the scroll after the bottom's");
            Check(chat.Contains("if(clear)ChatMessages.Unpin(tab);",StringComparison.Ordinal),"a tab cleared");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                foreach(var key in new[]{"Pin","PinTip","Unpin","UnpinTip","PinnedTip"})Check(text.Contains($"\"{key}\":",StringComparison.Ordinal),language+" "+key);
            }
        }),
    ];
}
