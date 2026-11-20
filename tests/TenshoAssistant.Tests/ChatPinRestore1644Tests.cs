using TenshoAssistant.Chat;

// 0.1644 (X-673 M1, review of 0.1643): the pins follow their messages when the history read back numbers every line again.
internal static class ChatPinRestore1644Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static readonly DateTimeOffset At=new(2026,11,20,20,30,0,TimeSpan.Zero);
    private static ChatTabSettings Say()=>new(){Name="Say",Channels=[ChatChannel.Say]};
    private static ChatRecord Put(ChatStore store,string text,int cap=100)=>store.Add(At,10,"Ann Doe",[new ChatSegment(text)],cap);
    private static ChatRecord Before(string text,int minutes)=>new(1,At.AddMinutes(-minutes),10,ChatChannel.Say,"Bob Roe",[new ChatSegment(text)]);
    private static string Text(ChatRecord record)=>record.Body[0].Text;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat pin restore 1644: behaviour: the history read back after a pin: the pin takes its message's new number, a click finds it in the tab, its menu knows it pinned (never twice) and unpins it",()=>
        {
            var store=new ChatStore();
            var say=Say();
            Put(store,"first");var mine=Put(store,"pin me");Put(store,"last");
            Check(store.Pin(say,mine),"pinned");
            Check(store.Restore([Before("older",9),Before("old",5)],100)==2,"two lines put back before");
            var shown=store.After(0).Single(record=>Text(record)=="pin me");
            Check(shown.Id!=mine.Id,"every line numbered again");
            var pins=store.Pins(say);
            Check(pins.Count==1 && pins[0].Id==shown.Id,"the pin follows its message");
            var pin=pins[0];
            var view=store.ViewOf(say);
            view.Refresh(store,say,100);
            var index=view.IndexOfId(pin.Id);
            Check(index>=0 && Text(view.RecordAt(index))=="pin me","a click goes to the message, not to the top of the tab: "+index);
            var line=view.RecordAt(index);
            Check(store.Pinned(say,line) && !store.Pin(say,line) && store.Pins(say).Count==1,"its menu knows it pinned: never twice");
            store.Unpin(say,line);
            Check(store.Pins(say).Count==0,"unpinned from its menu");
        }),
        ("Chat pin restore 1644: behaviour: a pin whose line had left the store keeps its own number; the others follow theirs, in their order",()=>
        {
            var store=new ChatStore();
            var say=Say();
            var gone=Put(store,"gone",ChatStore.MinimumLines);
            Check(store.Pin(say,gone),"pinned");
            for(var at=0;at<ChatStore.MinimumLines+5;at++)Put(store,"line "+at,ChatStore.MinimumLines);
            Check(store.After(0).All(record=>Text(record)!="gone"),"its line has left the store");
            var kept=store.After(0).Last();
            Check(store.Pin(say,kept),"a second pin");
            store.Restore([Before("older",9)],ChatStore.MinimumLines);
            var last=store.After(0).Last();
            Check(Text(last)==Text(kept) && last.Id!=kept.Id,"numbered again");
            Check(store.Pins(say) is [var first,var second] && first.Id==gone.Id && Text(first)=="gone" && second.Id==last.Id,"the line gone keeps its number, the other follows");
        }),
    ];
}
