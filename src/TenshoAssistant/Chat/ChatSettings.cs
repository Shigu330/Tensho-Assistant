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
    /// <summary>X-484 (0.1777, beyond the game; the user's request of 15/11, reading R-X-484 of 20/11): the combat lines in which another player
    /// readies or uses Teleport or Return are not shown in this tab (<see cref="ChatTravel"/>).</summary>
    public bool HideOthersTravel{get;set;}
    /// <summary>X-455 E7 (0.1617): when the player cleared this tab: the messages before are no longer shown in it (the other tabs keep them);
    /// null: never cleared, or shown again.</summary>
    public DateTimeOffset? ClearedAt{get;set;}
    /// <summary>X-447 B2 (0.1622): this tab's own text size (60 to 200 %); null: the window's.</summary>
    public int? FontScale{get;set;}
    public int ScaleIn(ChatSettings window)=>FontScale is { } own ? Math.Clamp(own,60,200):window.BoundedFontScale;
    /// <summary>X-447 B11 (0.1714): the window of its own this tab is drawn in (1 to <see cref="ChatWindows.Maximum"/>); 0: the chat window.
    /// A conversation's tab keeps it in memory only, as the rest of it.</summary>
    public int Window{get;set;}
    public bool Shows(ChatChannel channel)=>Channels?.Contains(channel)==true;
    /// <summary>X-447 B10 (0.1654): a conversation's tab: the player whose tells it shows (ChatConversations); null for the tabs of the settings.
    /// Such a tab lives in memory only: it is never in <see cref="ChatSettings.Tabs"/>, never saved (R04).</summary>
    public string? Conversation{get;set;}
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
    /// <summary>X-447 D8 (0.1620): the window hidden during a cutscene (also those that keep the game's interface) and in a duty; it comes back by
    /// itself. The messages are still copied.</summary>
    public bool HideInCutscene{get;set;}
    public bool HideInDuty{get;set;}
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
    /// <summary>X-447 D1 (0.1620): while locked, the clicks and the wheel go through the window to the game, except on what answers them (the
    /// tabs and their buttons, the links, « back to the bottom »; a menu of the chat open takes them again).</summary>
    public bool MouseThrough{get;set;}
    /// <summary>X-447 D4 (0.1618): the tabs' names hidden: the tab shown stays, the buttons at the end of the bar stay, with the one that shows
    /// the tabs again.</summary>
    public bool HideTabs{get;set;}
    /// <summary>X-447 D5 (0.1618): the window's margins (pixels, across and down), its corners' rounding and its frame's thickness when it has
    /// one; the defaults are the window as before (ChatLook.Padding, the theme's corners, a one-pixel frame).</summary>
    public int MarginX{get;set;}=6;
    public int MarginY{get;set;}=4;
    public int Rounding{get;set;}=2;
    public int Border{get;set;}=1;
    /// <summary>X-447 D6 (0.1618): the window's opacity, text included, while the mouse is not on it (percent); full under the mouse.</summary>
    public int TextOpacity{get;set;}=100;
    /// <summary>X-447 B1 (0.1622, Glass and Prat): seconds without a new message in the tab shown before the whole window fades out (0: never);
    /// the mouse on it, a menu of the chat or a new message brings it back.</summary>
    public int FadeAfter{get;set;}
    public int BoundedFadeAfter=>Math.Clamp(FadeAfter,0,600);
    /// <summary>X-732 (0.1736, the user's report of 21/11): faded out, a tell received or a message holding an alert (your first name, an alert
    /// word) in any tab brings the window back too, not only a new message of the tab shown (off by default).</summary>
    public bool FadeBackOnTell{get;set;}
    /// <summary>X-447 B2 (0.1622, Prat, Chattynator): pixels added between the lines.</summary>
    public int LineSpacing{get;set;}
    public int BoundedLineSpacing=>Math.Clamp(LineSpacing,0,12);
    /// <summary>X-485 (0.1479): what the window does while a window of the game covers it: it hides (the default, as the panels), it fades
    /// (no background, text faint, clicks go through), or it stays.</summary>
    public ChatCovered WhenCovered{get;set;}=ChatCovered.Hide;
    /// <summary>X-484 (0.1482, beyond the game): players whose messages no tab shows, and players whose name stands out (commas between names).</summary>
    public string? HiddenPlayers{get;set;}
    public string? HighlightedPlayers{get;set;}
    /// <summary>X-484 (0.1520, beyond the game; the « alerts » of the chat study, X-447): words that make a message stand out (commas between
    /// them, capitals ignored): its line on a band, and its tab marked « ! » until it is seen.</summary>
    public string? AlertWords{get;set;}
    /// <summary>X-447 B7 (0.1643, the user's choice of 15/11, after WIM): a tab not shown holding a message with an alert word not seen yet blinks
    /// (besides its « ! »). On by default: it only blinks once alert words are chosen.</summary>
    public bool BlinkAlerts{get;set;}=true;
    /// <summary>X-447 B7 (0.1646, the user's choice of 15/11, after Prat): your character's first name said by a player (a spoken channel)
    /// alerts as an alert word does. On by default.</summary>
    public bool AlertOwnName{get;set;}=true;
    /// <summary>X-447 B7 (0.1646, after Prat and WIM): the game's sound (the macros' &lt;se.1&gt; to &lt;se.16&gt;) a message with an alert plays;
    /// 0: none (the default).</summary>
    public int AlertSound{get;set;}
    /// <summary>X-447 B7 (0.1646, after Prat): the sound each spoken channel's messages play (1 to 16); a channel not listed plays none.</summary>
    public Dictionary<ChatChannel,int>? Sounds{get;set;}
    /// <summary>X-447 B7 (0.1647, after WIM): the messages with an alert not seen yet counted in the game's server bar while the window is on; a
    /// click shows the first tab holding one. Off by default.</summary>
    public bool ServerBarAlerts{get;set;}
    /// <summary>X-447 B10 (0.1654, the user's choice of 15/11, after WIM): each player's tells in a tab of their own, opened by the first tell and
    /// closed by the player; in memory only (ChatConversations). Off by default.</summary>
    public bool Conversations{get;set;}
    /// <summary>X-447 B13 (0.1655, D-198 a): the window's look and the tabs shown change by themselves by context (ChatContexts). Off by default.</summary>
    public bool ContextProfiles{get;set;}
    /// <summary>X-447 B13: each context's profile (its look, its tabs).</summary>
    public Dictionary<ChatContext,ChatContextProfile>? Profiles{get;set;}
    /// <summary>X-447 B13: the context whose look the window wears now (None: its own), and its own look kept meanwhile (a B16 code), so that
    /// it comes back even after a restart in the middle of a duty.</summary>
    public ChatContext ProfileApplied{get;set;}
    public string? OwnLook{get;set;}
    /// <summary>X-447 B7 (0.1646): the sound of <paramref name="channel"/>'s messages, 0 for none (or a number out of 1 to 16).</summary>
    public int SoundOf(ChatChannel channel)=>Sounds is { } sounds && sounds.TryGetValue(channel,out var sound) && Alarms.MacroSound.Valid(sound) ? sound:0;
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
    // X-574 (0.1793, the user's choice of 17/11): the one-time upgrades of settings saved before 0.1488 (LookVersion: the game's look) and
    // 0.1466 (TabsVersion: the Battle and Event tabs) are gone, with their markers; a marker still in a saved file is ignored at load.
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
    public Dictionary<ChatChannel,uint>? Colors{get;set;}
    /// <summary>X-801 (0.1809, the user's request of 22/11): the combat lines coloured by kind and side (damage and healing, done by you or to
    /// you: ChatCombatTones); off: the channel's single colour.</summary>
    public bool CombatTones{get;set;}=true;
    /// <summary>X-801 (0.1809): the colours chosen for the combat lines' tones; absent: ChatCombatTones.DefaultColor.</summary>
    public Dictionary<ChatCombatTone,uint>? ToneColors{get;set;}
    /// <summary>X-447 B4 (0.1621): a short name chosen per channel, shown in brackets before its messages instead of the game's own short name;
    /// empty or absent: the game's.</summary>
    public Dictionary<ChatChannel,string>? ChannelNames{get;set;}
    public const int MaximumChannelName=8;
    /// <summary>X-447 B4: the short name chosen for a channel, cleaned and bounded; null when none is chosen.</summary>
    public string? ShortNameOf(ChatChannel channel)
        =>ChannelNames?.GetValueOrDefault(channel) is { } chosen && ChatFormat.Clean(chosen).Trim() is { Length:>0 } name
            ? name.Length>MaximumChannelName ? name[..MaximumChannelName]:name:null;
    /// <summary>X-447 B5 (0.1621): the players' names in brackets ([Name]: text), as WoW's chat addons write them.</summary>
    public bool SenderBrackets{get;set;}
    /// <summary>X-447 B5: each player's name in a colour of its own, taken from the name itself (the same name, the same colour; nothing is kept).
    /// The class colour of WoW is not taken: the game shows a player's class only in the party or nearby (R04).</summary>
    public bool NameColors{get;set;}
    /// <summary>X-447 B15 (0.1623, R25): capture mode: the players' names before their messages replaced by a stand-in taken from the name
    /// (ChatFormat.StandIn: the same player, the same stand-in), for screenshots. The messages' own text is not changed. X-669 (review of
    /// 0.1627): the stand-in is made with a secret drawn at each start (a guessed name cannot be checked against it); the names' colours and
    /// the message menu follow it.</summary>
    public bool CaptureNames{get;set;}
    /// <summary>X-447 B14 (0.1641, the user's choice of 15/11, after Prat): an item link's icon before its name, the icon of Tensho's item list
    /// (the game's Item sheet); drawn only, nothing is kept. Off by default: the game's own chat draws none.</summary>
    public bool ItemIcons{get;set;}
    /// <summary>X-690 (0.1666, the user's request of 20/11): a player's name in the colour of their role (tank, healer, DPS), the game's own role
    /// colours, when the game shows their job (party, alliance, nearby); in capture mode too (the name is replaced, the colour stays).</summary>
    public bool RoleColors{get;set;}=true;
    /// <summary>X-690 (0.1666): the player's job icon before their name, when the game shows their job (party, alliance, nearby).</summary>
    public bool JobIcons{get;set;}=true;
    /// <summary>X-448 (0.1454): down to 0 (transparent; was 10).</summary>
    public int BoundedOpacity=>Math.Clamp(Opacity,0,100);
    public int BoundedFontScale=>Math.Clamp(FontScale,60,200);
    public int BoundedMarginX=>Math.Clamp(MarginX,0,24);
    public int BoundedMarginY=>Math.Clamp(MarginY,0,24);
    public int BoundedRounding=>Math.Clamp(Rounding,0,16);
    public int BoundedBorder=>Math.Clamp(Border,0,4);
    public int BoundedTextOpacity=>Math.Clamp(TextOpacity,20,100);
    public int BoundedMaxLines=>ChatStore.Bounded(MaxLines);
    public uint ColorOf(ChatChannel channel)=>Colors?.TryGetValue(channel,out var color)==true ? color&0xFFFFFF:ChatChannels.DefaultColor(channel);
    /// <summary>X-801 (0.1809): a combat line's tone's colour (chosen, else its default).</summary>
    public uint ColorOf(ChatCombatTone tone)=>ToneColors?.TryGetValue(tone,out var color)==true ? color&0xFFFFFF:ChatCombatTones.DefaultColor(tone);
    /// <summary>X-801 (0.1809): a message's colour: its combat tone's when the tones are on and it has one, else its channel's.</summary>
    public uint ColorOf(ChatRecord record)=>CombatTones && ChatCombatTones.Of(record.Kind,record.Source,record.Target) is var tone and not ChatCombatTone.None ? ColorOf(tone):ColorOf(record.Channel);
    /// <summary>The tabs, made at first use. X-445 (0.1458): the game's three default log tabs (General: everything but the combat lines and the
    /// NPC dialogue; Battle: the combat lines; Event: the NPC dialogue), then Social (only what people say to you or your groups). Tabs saved
    /// stay as they are (X-574, 0.1793: no upgrade of tabs saved before 0.1466 any more).</summary>
    public List<ChatTabSettings> EnsureTabs(string generalName,string battleName,string eventName,string socialName)
    {
        if(Tabs is { Count:>0 })return Tabs;
        Tabs=DefaultTabs(generalName,battleName,eventName,socialName);
        return Tabs;
    }

    /// <summary>X-445 (0.1458): the tabs a new player starts with: the game's three default log tabs, then Social.</summary>
    public static List<ChatTabSettings> DefaultTabs(string generalName,string battleName,string eventName,string socialName)=>
    [
        new(){Name=generalName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.General).ToList()},
        new(){Name=battleName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.Battle).ToList(),ShowUnread=false},
        new(){Name=eventName,Channels=ChatChannels.All.Where(channel=>ChatChannels.DefaultTab(channel)==ChatGameTab.Event).ToList(),ShowUnread=false},
        new(){Name=socialName,Channels=ChatChannels.All.Where(ChatChannels.Spoken).ToList()},
    ];

    /// <summary>X-686 (the user's report of 20/11): the tabs back to the ones a new player starts with. Only the tabs change: the history, the
    /// look, the colours, the players and the contexts' profiles stay as they are (a profile naming a tab gone shows every tab).</summary>
    public List<ChatTabSettings> ResetTabs(string generalName,string battleName,string eventName,string socialName)
    {
        Tabs=DefaultTabs(generalName,battleName,eventName,socialName);
        return Tabs;
    }

    /// <summary>X-686: the default tabs no tab stands for any more (none with the same name, none with the same channels by channel), with the place
    /// each has among the default ones, so that one can come back alone.</summary>
    public static List<(int Place,ChatTabSettings Tab)> MissingDefaults(IReadOnlyList<ChatTabSettings> tabs,string generalName,string battleName,
        string eventName,string socialName)
        =>DefaultTabs(generalName,battleName,eventName,socialName).Select((tab,place)=>(place,tab))
            .Where(missing=>!tabs.Any(tab=>StandsFor(tab,missing.tab))).ToList();

    internal static bool StandsFor(ChatTabSettings tab,ChatTabSettings standard)
        =>string.Equals((tab.Name ?? "").Trim(),standard.Name,StringComparison.OrdinalIgnoreCase)
          || (tab.Filters is null && tab.Channels is { Count:>0 } channels && channels.ToHashSet().SetEquals(standard.Channels));

    /// <summary>X-686: one default tab back at its own place (or the end), when there is room for it.</summary>
    public static bool AddDefault(List<ChatTabSettings> tabs,int place,ChatTabSettings tab)
    {
        if(tabs.Count>=MaximumTabs)return false;
        tabs.Insert(Math.Clamp(place,0,tabs.Count),tab);
        return true;
    }
}

/// <summary>
/// X-687 (0.1663, the user's request of 20/11 around 22:05: « no way to sort the tabs »): the settings' tabs in a new order, kept in
/// ChatSettings.Tabs (saved, so kept at reload). One order for the window and its contexts' profiles (a profile names the tabs it shows; they
/// come in this order). The conversations' tabs (memory only) are not in the list and stay after. Pure (tests).
/// </summary>
public static class ChatTabOrder
{
    /// <summary>The tab one place to the left (<paramref name="step"/> -1) or right (+1); false at an end or out of the list.</summary>
    public static bool Move(List<ChatTabSettings> tabs,int index,int step)
    {
        var to=index+step;
        if(step==0 || index<0 || index>=tabs.Count || to<0 || to>=tabs.Count)return false;
        (tabs[index],tabs[to])=(tabs[to],tabs[index]);
        return true;
    }

    /// <summary>After a drag in the window: the settings' tabs <paramref name="shown"/> in the order the window now shows them, each in one of the
    /// places these tabs had (a profile may show some only: the others keep their places); tabs not in the list (conversations) left out. False
    /// when nothing changes.</summary>
    public static bool Apply(List<ChatTabSettings> tabs,IReadOnlyList<ChatTabSettings> shown)
    {
        var moved=shown.Where(tabs.Contains).Distinct().ToList();
        var places=moved.Select(tab=>tabs.IndexOf(tab)).Order().ToList();
        var changed=false;
        for(var at=0;at<places.Count;at++)
            if(!ReferenceEquals(tabs[places[at]],moved[at])){tabs[places[at]]=moved[at];changed=true;}
        return changed;
    }
}

/// <summary>X-686 (the user's report of 20/11: a tab removed by mistake, at the first click): « Remove » takes the tab away at once and keeps
/// it a few seconds, the time to undo it; it comes back at its place, with its own settings. Only the last tab removed can come back.</summary>
public sealed class ChatTabUndo
{
    public const long UndoMilliseconds=10_000;
    private ChatTabSettings? removed;
    private int place;
    private long until;

    /// <summary>The tab removed that can still come back, else null.</summary>
    public ChatTabSettings? Waiting(long now)=>removed is not null && now<until ? removed:null;
    public int SecondsLeft(long now)=>Waiting(now) is null ? 0:(int)Math.Ceiling((until-now)/1000.0);

    /// <summary>Removes the tab at this place (never the last tab); false when nothing was removed.</summary>
    public bool Remove(List<ChatTabSettings> tabs,int index,long now)
    {
        if(index<0 || index>=tabs.Count || tabs.Count<=1)return false;
        removed=tabs[index];
        place=index;
        until=now+UndoMilliseconds;
        tabs.RemoveAt(index);
        return true;
    }

    /// <summary>The tab back at its place (or the end); false when the time is over, nothing waits, or there is no room.</summary>
    public bool Undo(List<ChatTabSettings> tabs,long now)
    {
        if(Waiting(now) is not { } tab || tabs.Count>=ChatSettings.MaximumTabs || tabs.Contains(tab))return false;
        tabs.Insert(Math.Min(place,tabs.Count),tab);
        removed=null;
        return true;
    }

    public void Forget()=>removed=null;

    /// <summary>X-696 M1 (review of 0.1658): a default tab put back by « Restore « … » » while the tab it stands for waits to be undone; that
    /// tab no longer comes back, or there would be two of it (two « Battle »).</summary>
    public void Replaced(ChatTabSettings added)
    {
        if(removed is not null && ChatSettings.StandsFor(removed,added))removed=null;
    }
}
