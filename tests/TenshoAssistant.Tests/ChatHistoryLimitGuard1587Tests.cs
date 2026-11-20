using TenshoAssistant.Chat;

// 0.1587 (X-643, review of 0.1586, I1): the guard of X-518 M2 as behaviour, not text. Unticking « No limit » while the history is kept
// does not change the setting and asks; « Cancel » leaves it ticked; only « Limit » writes the bound. The bounded value the history uses
// (BoundedHistoryMessages / Days, read at its next trim) is checked too: it stays « no limit » until « Limit ».
internal static class ChatHistoryLimitGuard1587Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static ChatSettings Kept()=>new(){KeepHistory=true,HistoryMessagesUnlimited=true,HistoryDaysUnlimited=true,HistoryMessages=500,HistoryDays=7};

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat history limit guard 1587: unticking « No limit » while the history is kept asks and changes nothing",() =>
        {
            foreach(var bound in new[]{ChatHistoryBound.Messages,ChatHistoryBound.Days})
            {
                var chat=Kept();
                var ask=ChatHistoryLimitGuard.Change(chat,bound,false);
                Check(ask==bound,$"{bound}: the question is asked ({ask})");
                Check(chat.HistoryMessagesUnlimited && chat.HistoryDaysUnlimited,$"{bound}: both boxes still ticked");
                Check(chat.BoundedHistoryMessages==ChatHistory.NoLimit && chat.BoundedHistoryDays==ChatHistory.NoLimit,$"{bound}: the trim still sees no limit");
            }
        }),
        ("Chat history limit guard 1587: « Cancel » leaves « No limit » ticked",() =>
        {
            foreach(var bound in new[]{ChatHistoryBound.Messages,ChatHistoryBound.Days})
            {
                var chat=Kept();
                var ask=ChatHistoryLimitGuard.Change(chat,bound,false);
                Check(!ChatHistoryLimitGuard.Answer(chat,ask,false),$"{bound}: nothing to save");
                Check(ChatHistoryLimitGuard.Unlimited(chat,bound),$"{bound}: still ticked");
                Check(chat.BoundedHistoryMessages==ChatHistory.NoLimit && chat.BoundedHistoryDays==ChatHistory.NoLimit,$"{bound}: nothing bounded");
            }
        }),
        ("Chat history limit guard 1587: only « Limit » writes the bound, and only that one",() =>
        {
            var chat=Kept();
            Check(ChatHistoryLimitGuard.Answer(chat,ChatHistoryLimitGuard.Change(chat,ChatHistoryBound.Messages,false),true),"messages: saved");
            Check(!chat.HistoryMessagesUnlimited && chat.HistoryDaysUnlimited,"messages unticked, days untouched");
            Check(chat.BoundedHistoryMessages==ChatHistory.BoundedMessages(500) && chat.BoundedHistoryDays==ChatHistory.NoLimit,"the messages' bound applies");
            chat=Kept();
            Check(ChatHistoryLimitGuard.Answer(chat,ChatHistoryLimitGuard.Change(chat,ChatHistoryBound.Days,false),true),"days: saved");
            Check(chat.HistoryMessagesUnlimited && !chat.HistoryDaysUnlimited,"days unticked, messages untouched");
            Check(chat.BoundedHistoryDays==ChatHistory.BoundedDays(7),"the days' bound applies");
            Check(!ChatHistoryLimitGuard.Answer(Kept(),ChatHistoryBound.None,true),"no question: nothing written");
        }),
        ("Chat history limit guard 1587: nothing to delete, nothing asked",() =>
        {
            // No history kept: unticking applies at once.
            var off=Kept();off.KeepHistory=false;
            Check(ChatHistoryLimitGuard.Change(off,ChatHistoryBound.Messages,false)==ChatHistoryBound.None && !off.HistoryMessagesUnlimited,"no history: applied");
            // Ticking « No limit » never deletes: applied at once.
            var bounded=Kept();bounded.HistoryDaysUnlimited=false;
            Check(ChatHistoryLimitGuard.Change(bounded,ChatHistoryBound.Days,true)==ChatHistoryBound.None && bounded.HistoryDaysUnlimited,"ticking: applied");
            // Already unticked (a repeated call): no question.
            var already=Kept();already.HistoryMessagesUnlimited=false;
            Check(ChatHistoryLimitGuard.Change(already,ChatHistoryBound.Messages,false)==ChatHistoryBound.None && !already.HistoryMessagesUnlimited,"already bounded: no question");
        }),
        ("Chat history limit guard 1587: the settings box and the question go through the guard",() =>
        {
            var dir=new DirectoryInfo(AppContext.BaseDirectory);
            while(dir!=null && !Directory.Exists(Path.Combine(dir.FullName,"src","TenshoAssistant")))dir=dir.Parent;
            var ui=File.ReadAllText(Path.Combine(dir!.FullName,"src","TenshoAssistant","MainWindow.ChatWindow.cs")).Replace("\r\n","\n");
            Check(ui.Contains("ChatHistoryLimitGuard.Change(configuration.Chat,ChatHistoryBound.Messages,value)"),"messages box");
            Check(ui.Contains("ChatHistoryLimitGuard.Change(configuration.Chat,ChatHistoryBound.Days,value)"),"days box");
            Check(ui.Contains("ChatHistoryLimitGuard.Answer(configuration.Chat,chatHistoryLimitAsking,true)"),"« Limit »");
            Check(!ui.Contains("HistoryMessagesUnlimited=value") && !ui.Contains("HistoryDaysUnlimited=value")
                && !ui.Contains("HistoryMessagesUnlimited=false") && !ui.Contains("HistoryDaysUnlimited=false"),"no other writer of the boxes");
        }),
    ];
}
