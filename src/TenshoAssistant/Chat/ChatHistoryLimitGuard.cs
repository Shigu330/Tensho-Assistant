namespace TenshoAssistant.Chat;

/// <summary>X-643 (review of 0.1586, I1): which « No limit » box of the chat history a question is about.</summary>
public enum ChatHistoryBound{None=0,Messages=1,Days=2}

/// <summary>X-518 M2, made testable by X-643: unticking « No limit » while the history is kept would delete, at the history's next trim,
/// everything past the bound (possibly months of messages); so the box is not changed at once, a question is asked, and only « Limit »
/// writes the bound. « Cancel », or closing the question, leaves « No limit » ticked. Ticking the box, or any change while no history is
/// kept, applies at once (nothing is deleted).</summary>
public static class ChatHistoryLimitGuard
{
    /// <summary>The « No limit » box of <paramref name="bound"/> was set to <paramref name="value"/>: either writes it and returns
    /// <see cref="ChatHistoryBound.None"/>, or leaves the settings as they are and returns the bound to ask about.</summary>
    public static ChatHistoryBound Change(ChatSettings chat,ChatHistoryBound bound,bool value)
    {
        if(bound==ChatHistoryBound.None)return ChatHistoryBound.None;
        if(!value && chat.KeepHistory && Unlimited(chat,bound))return bound;
        Set(chat,bound,value);
        return ChatHistoryBound.None;
    }

    /// <summary>The answer to the question about <paramref name="bound"/>: <paramref name="limit"/> true (« Limit ») unticks « No limit »
    /// and returns true (the settings changed, to be saved); false (« Cancel », closed) changes nothing.</summary>
    public static bool Answer(ChatSettings chat,ChatHistoryBound bound,bool limit)
    {
        if(!limit || bound==ChatHistoryBound.None)return false;
        Set(chat,bound,false);
        return true;
    }

    public static bool Unlimited(ChatSettings chat,ChatHistoryBound bound)=>bound==ChatHistoryBound.Days ? chat.HistoryDaysUnlimited:chat.HistoryMessagesUnlimited;

    private static void Set(ChatSettings chat,ChatHistoryBound bound,bool value)
    {
        if(bound==ChatHistoryBound.Days)chat.HistoryDaysUnlimited=value;else chat.HistoryMessagesUnlimited=value;
    }
}
