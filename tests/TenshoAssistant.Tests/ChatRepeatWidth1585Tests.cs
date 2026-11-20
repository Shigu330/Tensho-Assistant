using TenshoAssistant.Chat;

// 0.1585 (X-556 M3, review of 0.1520): the « ×n » of a folded message ran past the window's edge when the message's last line filled the
// width, and a growing count did not lay the message out again. The message is now laid out narrower by its count, again when it grows.
internal static class ChatRepeatWidth1585Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly DateTimeOffset Start=new(2026,11,20,7,0,0,TimeSpan.Zero);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat repeat width 1585: a folded message is laid out again when its count grows",() =>
        {
            var store=new ChatStore();
            var tab=new ChatTabSettings{Name="t",Channels=[ChatChannel.Shout],FoldRepeats=true};
            store.Add(Start,11,"Cloud Strife",[new ChatSegment("Selling gil!")],100);
            var view=new ChatTabView();
            view.Refresh(store,tab,100);
            Check(view.NextToLayOut()==0,"the new message waits for its layout");
            view.SetLines(0,[[new ChatRun("Selling gil!",ChatRole.Body)]]);
            Check(view.NextToLayOut()==-1,"laid out");
            store.Add(Start.AddSeconds(10),11,"Cloud Strife",[new ChatSegment("Selling gil!")],100);
            view.Refresh(store,tab,100);
            Check(view.RepeatsAt(0)==1 && view.LinesAt(0) is null && view.NextToLayOut()==0,"the count grew: laid out again");
        }),
        ("Chat repeat width 1585: the text keeps the width left beside the count, never below half",() =>
        {
            Check(ChatLook.WidthBeside(400,0)==400,"nothing beside: the whole width");
            Check(ChatLook.WidthBeside(400,30)==370,"the count's room taken off");
            Check(ChatLook.WidthBeside(40,30)==20,"never below half the width");
            var lines=ChatFormat.Wrap([new ChatRun("aaaa bbbb cccc dddd",ChatRole.Body)],ChatLook.WidthBeside(19,3),text=>text.Length);
            Check(lines.All(line=>line.Sum(run=>run.Text.TrimEnd().Length)+3<=19),"every line leaves room for a 3-wide count");
        }),
    ];
}
