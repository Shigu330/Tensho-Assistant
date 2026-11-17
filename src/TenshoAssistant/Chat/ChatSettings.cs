namespace TenshoAssistant.Chat;

/// <summary>One tab of the chat window: its name and the channels it shows.</summary>
public sealed class ChatTabSettings
{
    public string Name{get;set;}="";
    public List<ChatChannel> Channels{get;set;}=[];
    public bool ShowUnread{get;set;}=true;
    /// <summary>X-478 (the user's request of 15/11): the colour of the tab's name (0xRRGGBB); null: the theme's text colour.</summary>
    public uint? NameColor{get;set;}
    /// <summary>X-484 (0.1482): the game's log filters this tab shows (rows of the LogFilter sheet); null: its channels decide, as before.</summary>
    public List<uint>? Filters{get;set;}
    /// <summary>X-484 (0.1482): with the game's filters, also the messages none of them covers (kinds the game does not list).</summary>
    public bool Unlisted{get;set;}=true;
    /// <summary>X-484 (0.1482, beyond the game): only messages with one of these words (commas between them), and never those with one of
    /// <see cref="HideWords"/>; <see cref="WordsMatchCase"/>: capitals count.</summary>
    public string? KeepWords{get;set;}
    public string? HideWords{get;set;}
    public bool WordsMatchCase{get;set;}
    /// <summary>X-484 (0.1519, beyond the game; the « duplicates » of the chat study, X-447): a message repeated right after itself (same channel,
    /// same sender, same text, within <see cref="ChatRepeat.Window"/>) is not shown again: the first one counts the repeats (« ×3 »).</summary>
    public bool FoldRepeats{get;set;}
    public bool Shows(ChatChannel channel)=>Channels?.Contains(channel)==true;
}

/// <summary>
/// X-403 phase 1 (D-166; back from 0.471): the Tensho chat window's settings, read through bounded values so a hand-edited configuration stays in range.
/// Off by default: while the window is off, no message is copied. Settings only: the messages themselves are never saved here (X-475: the chat
/// history, off by default, keeps them in its own files, ChatHistory).
/// </summary>
public sealed class ChatSettings
{
    public const int MaximumTabs=12,MaximumTabName=24;
    public bool WindowOpen{get;set;}
    public bool HideInCombat{get;set;}
    /// <summary>X-467 (0.1504, D-181 of 16/11, DEC-053 completed): the game chat's messages (its four panels) hidden and this window in their
    /// place, above the game's own typing box (never hidden). Off by default; given back as soon as it is no longer wanted (GameChatHider).</summary>
    public bool HideGameMessages{get;set;}
    /// <summary>X-467 (0.1504): with <see cref="HideGameMessages"/>, both chats shown for a while, to compare (nothing hidden, this window at
    /// its own place).</summary>
    public bool BothChats{get;set;}
    public bool ShowTimestamps{get;set;}=true;
    public bool Clock24{get;set;}=true;
    public bool ShowSeconds{get;set;}
    public bool ShowChannel{get;set;}=true;
    /// <summary>X-476 (0.1481): a light background, as the game's chat (was 85); the text keeps its outline. X-505 (0.1488, the user's
    /// captures of 11/16): lighter still, nearly invisible as the game's (was 40).</summary>
    public int Opacity{get;set;}=DefaultOpacity;
    public const int DefaultOpacity=15;
    public int FontScale{get;set;}=100;
    public int MaxLines{get;set;}=2000;
    /// <summary>X-448 (0.1454): no title bar nor frame; the buttons at the end of the tab bar stay.</summary>
    public bool Borderless{get;set;}
    /// <summary>X-448 (0.1454): the window is neither moved nor resized.</summary>
    public bool Locked{get;set;}
    /// <summary>X-485 (0.1479): what the window does while a window of the game covers it: it hides (the default, as the panels), it fades
    /// (no background, text faint, clicks go through), or it stays.</summary>
    public ChatCovered WhenCovered{get;set;}=ChatCovered.Hide;
    /// <summary>X-484 (0.1482, beyond the game): players whose messages no tab shows, and players whose name stands out (commas between names).</summary>
    public string? HiddenPlayers{get;set;}
    public string? HighlightedPlayers{get;set;}
    /// <summary>X-476 (0.1481): the tabs under the messages instead of over them. X-505 (0.1488, the user's answer to X-476): under them by
    /// default, as the game's chat.</summary>
    public bool TabsAtBottom{get;set;}=true;
    /// <summary>X-505 (0.1488): a thin, discreet scroll bar, as the game's chat (a line), instead of the theme's wide one.</summary>
    public bool ThinScrollbar{get;set;}=true;
    /// <summary>X-505 (0.1488): the hour written with a leading zero ([09:21]); off, as the game writes it ([9:21]).</summary>
    public bool HourLeadingZero{get;set;}
    /// <summary>X-505 (0.1488): links drawn as the game's: its orange arrow before them, their text in the message's colour, not underlined.</summary>
    public bool GameLinks{get;set;}=true;
    /// <summary>X-505 (0.1488): rounded tabs, the one shown marked by a dot, as the game's chat.</summary>
    public bool GameTabs{get;set;}=true;
    /// <summary>X-505 (0.1488): the lines a message wraps onto indented, as the game's chat.</summary>
    public bool WrapIndent{get;set;}=true;
    /// <summary>X-505 (0.1488): <see cref="CurrentLook"/> once the look follows the game's (a configuration saved before gets the new defaults once).</summary>
    public int LookVersion{get;set;}
    public const int CurrentLook=1;
    /// <summary>X-505 (0.1488): a configuration saved before gets the game's look once: the tabs under the lines (the user's answer to X-476), the
    /// background lighter when it was still the old default (40 %); a background chosen by the player is kept. True when something changed.</summary>
    public bool UpgradeLook()
    {
        if(LookVersion>=CurrentLook)return false;
        if(Opacity==40)Opacity=DefaultOpacity;
        TabsAtBottom=true;
        LookVersion=CurrentLook;
        return true;
    }
    /// <summary>X-475: the chat history kept on this PC (ChatHistory), per character, so that the window shows it again after a reload or a
    /// restart. Off by default; the window turns it on only after the player said yes to what is kept, where, how long and how to erase it.</summary>
    public bool KeepHistory{get;set;}
    /// <summary>X-475: the history's bounds: the last messages kept (combat lines and the others each) and the days. X-513: 30 days by
    /// default (7 before; a saved value is kept as it is).</summary>
    public int HistoryMessages{get;set;}=ChatHistory.DefaultMessages;
    public int HistoryDays{get;set;}=ChatHistory.DefaultDays;
    /// <summary>X-513 (the user's request of 16/11, choice (a)): « No limit » for the messages and for the days, each apart (off by
    /// default); the number beside it is kept for when the box is unticked.</summary>
    public bool HistoryMessagesUnlimited{get;set;}
    public bool HistoryDaysUnlimited{get;set;}
    /// <summary>The bounds as the history uses them: <see cref="ChatHistory.NoLimit"/> when « No limit » is ticked.</summary>
    public int BoundedHistoryMessages=>HistoryMessagesUnlimited ? ChatHistory.NoLimit:ChatHistory.BoundedMessages(HistoryMessages);
    public int BoundedHistoryDays=>HistoryDaysUnlimited ? ChatHistory.NoLimit:ChatHistory.BoundedDays(HistoryDays);
    /// <summary>X-476 (0.1481, X-447 A3): a dark outline around the letters, readable on a light background, as the game's chat.</summary>
    public bool TextOutline{get;set;}=true;
    public List<ChatTabSettings>? Tabs{get;set;}
    /// <summary>X-445 (0.1458): <see cref="CurrentTabs"/> once the tabs follow the game's default tabs (tabs saved before get Battle and Event
    /// once).</summary>
    public int TabsVersion{get;set;}
    /// <summary>X-464 (0.1466): 2, the Battle and Event tabs count no unread line; the General tab saved before 0.1458 leaves the NPC dialogue to Event.</summary>
    public const int CurrentTabs=2;
    public Dictionary<ChatChannel,uint>? Colors{get;set;}
    /// <summary>X-448 (0.1454): down to 0 (transparent; was 10).</summary>
    public int BoundedOpacity=>Math.Clamp(Opacity,0,100);
    public int BoundedFontScale=>Math.Clamp(FontScale,60,200);
    public int BoundedMaxLines=>ChatStore.Bounded(MaxLines);
    public uint ColorOf(ChatChannel channel)=>Colors?.TryGetValue(channel,out var color)==true ? color&0xFFFFFF:ChatChannels.DefaultColor(channel);
    /// <summary>The tabs, made at first use. X-445 (0.1458): the game's three default log tabs (General: everything but the combat lines and the
    /// NPC dialogue; Battle: the combat lines; Event: the NPC dialogue), then Social (only what people say to you or your groups). Tabs saved
    /// before get Battle and Event once, after the first tab, unless a tab already shows that channel alone; the others stay as they are.</summary>
    public List<ChatTabSettings> EnsureTabs(string generalName,string battleName,string eventName,string socialName)
    {
        UpgradeLook();
        if(Tabs is { Count:>0 })
        {
            if(TabsVersion<1)
            {
                var at=1;
                foreach(var (name,channel) in new[]{(battleName,ChatChannel.Combat),(eventName,ChatChannel.Npc)})
                {
                    if(Tabs.Count>=MaximumTabs || Tabs.Any(tab=>tab.Channels is [var only] && only==channel))continue;
                    Tabs.Insert(Math.Min(at,Tabs.Count),new(){Name=name,Channels=[channel],ShowUnread=false});
                    at++;
                }
            }
            if(TabsVersion<2)
            {
                // X-464 M2 (0.1466): the Battle and Event tabs made in 0.1458 (one channel) count no unread line, as the game counts none.
                foreach(var tab in Tabs)
                    if(tab.Channels is [var only] && only is ChatChannel.Combat or ChatChannel.Npc)tab.ShowUnread=false;
                // X-464 M3 (0.1466): the General tab saved before 0.1458 (every channel but Combat, exactly) leaves the NPC dialogue to Event.
                if(Tabs.Any(tab=>tab.Channels is [ChatChannel.Npc]))
                {
                    var oldGeneral=ChatChannels.All.Where(channel=>channel!=ChatChannel.Combat).ToHashSet();
                    foreach(var tab in Tabs)
                        if(tab.Channels is { Count:>1 } channels && channels.ToHashSet().SetEquals(oldGeneral))channels.Remove(ChatChannel.Npc);
                }
            }
            TabsVersion=CurrentTabs;
            return Tabs;
        }
        Tabs=
        [
            new(){Name=generalName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.General).ToList()},
            new(){Name=battleName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.Battle).ToList(),ShowUnread=false},
            new(){Name=eventName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.Event).ToList(),ShowUnread=false},
            new(){Name=socialName,Channels=ChatChannels.All.Where(ChatChannels.Spoken).ToList()},
        ];
        TabsVersion=CurrentTabs;
        return Tabs;
    }
}
