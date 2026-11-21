using TenshoAssistant.Chat;

// 0.1662 (X-680, the user's request of 20/11 around 21:40, two pictures of the game's menu and Tensho's on the same « Yell »: « we still miss
// things »): a player's menu holds the game's entries in the game's order, the blocking functions in their submenu; what the game's library
// cannot do is greyed with why; « Party finder » by the player's identifiers. docs/evidence/0.1662-chat-player-menu-game-entries.md.
internal static class ChatPlayerMenu1662Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat player menu 1662: the game's entries in the game's order, the blocking functions together, then « Game menu… »",()=>
        {
            var known=new ChatSource("Aya Lune",73,1001,2001);
            var entries=ChatPlayerMenu.Entries(known,ChatChannel.Yell,true,true);
            Check(entries.Select(e=>e.Action).SequenceEqual([ChatPlayerAction.Tell,ChatPlayerAction.Reply,ChatPlayerAction.Invite,ChatPlayerAction.Friend,ChatPlayerAction.Mute,
                ChatPlayerAction.Blacklist,ChatPlayerAction.Report,ChatPlayerAction.PartyFinder,ChatPlayerAction.Target,ChatPlayerAction.Plate,ChatPlayerAction.GameMenu]),"the game's order: "+string.Join(", ",entries.Select(e=>e.Action)));
            Check(entries.Where(e=>e.Block).Select(e=>e.Action).SequenceEqual([ChatPlayerAction.Mute,ChatPlayerAction.Blacklist]),"mute list and blacklist in « Blocking functions »");
            Check(entries.Select(e=>e.AddonRow).SequenceEqual(new uint[]{50,368,54,61,14833,59,10870,2200,8320,15083,0}) && ChatPlayerMenu.BlockRow==16061,"the client's texts (Addon rows)");
        }),
        ("Chat player menu 1662: known and near, all on; what only the game's own menus do (friend request, blacklist, report) greyed with why far away",()=>
        {
            var entries=ChatPlayerMenu.Entries(new ChatSource("Aya Lune",73,1001,2001),ChatChannel.Say,true,true);
            // X-699 (0.1688): near you, friend request, blacklist and report open the game's menu of that character (before: greyed, GameOnly).
            Check(entries.Where(e=>e.Action is ChatPlayerAction.Friend or ChatPlayerAction.Blacklist or ChatPlayerAction.Report).All(e=>e.Enabled && e.Why==ChatPlayerWhy.None && ChatPlayerMenu.OpensGameMenu(e.Action)),"near: by the game's menu");
            var far=ChatPlayerMenu.Entries(new ChatSource("Aya Lune",73,1001,2001),ChatChannel.Say,false,true);
            Check(far.Where(e=>e.Action is ChatPlayerAction.Friend or ChatPlayerAction.Blacklist or ChatPlayerAction.Report).All(e=>!e.Enabled && e.Why==ChatPlayerWhy.GameOnly),"far: game only");
            Check(entries.All(e=>e.Enabled && e.Why==ChatPlayerWhy.None),"all on");
            var named=ChatPlayerMenu.Entries(new ChatSource("Aya Lune",73),ChatChannel.Say,false,false);
            Check(named.Single(e=>e.Action==ChatPlayerAction.PartyFinder) is {Enabled:false,Why:ChatPlayerWhy.NoIdentifiers} && named.Single(e=>e.Action==ChatPlayerAction.Tell).Enabled,"party finder needs the identifiers; the tell not");
            var none=ChatPlayerMenu.Entries(null,ChatChannel.Say,false,false);
            Check(none.All(e=>!e.Enabled && e.Why==ChatPlayerWhy.NoSender) && none.All(e=>e.Action is not (ChatPlayerAction.Invite or ChatPlayerAction.GameMenu)),"no sender: all greyed, no sender said first");
        }),
        ("Chat player menu 1662: « Reply in the same mode » on every spoken channel, on only where the mode is known (tells, say, party, alliance)",()=>
        {
            var source=new ChatSource("Aya Lune",73,1001,2001);
            foreach(var channel in ChatChannels.All.Where(ChatChannels.Spoken))
            {
                var reply=ChatPlayerMenu.Entries(source,channel,false,false).SingleOrDefault(e=>e.Action==ChatPlayerAction.Reply);
                // X-697 M3 (0.1665): on for every spoken channel (before: tells, say, party and alliance only).
                Check(reply.AddonRow==368 && reply.Enabled && reply.Why==ChatPlayerWhy.None,"reply on "+channel);
            }
            Check(ChatPlayerMenu.Entries(source,ChatChannel.Emote,false,false).All(e=>e.Action!=ChatPlayerAction.Reply) && ChatPlayerMenu.Entries(source,ChatChannel.System,false,false).All(e=>e.Action!=ChatPlayerAction.Reply),"no reply where nobody speaks");
        }),
        ("Chat player menu 1662: the submenu drawn, party finder by the identifiers on the click, the reasons in both languages",()=>
        {
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("blockOpen=ImGui.BeginMenu(blockLabel+\"###ChatPlayerBlock\");") && window.Contains("if(blockOpen)ImGui.EndMenu();"),"the blocking submenu");
            var actions=Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(actions.Contains("case ChatPlayerAction.PartyFinder:\n                if(source.ContentId!=0)AgentLookingForGroup.Instance()->OpenListingByContentId(source.ContentId);"),"party finder");
            Check(!actions.Contains("/friendlist") && !actions.Contains("/blist") && !actions.Contains("ProcessChatBoxEntry"),"no command sent (the user's choice of 20/11)");
            foreach(var key in new[]{"PlayerWhyGameOnly","PlayerWhyNoReplyMode"})
                Check(window.Contains("\"ChatWindow."+key+"\"") && Read("src/TenshoAssistant/Localization/Languages/en.json").Contains("\""+key+"\":") && Read("src/TenshoAssistant/Localization/Languages/fr.json").Contains("\""+key+"\":"),key);
        }),
    ];
}
