using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-467 (0.1504, D-181 of 16/11, DEC-053 completed): « Hide the game chat's messages ». Each frame, on the game's thread: while
/// <c>wanted</c> says so, the game's message panels (ChatLogPanel_0 to _3) are made invisible; the window ChatLog itself (the game's typing
/// box and its channel menu) is never hidden. Out of it (the option off, Tensho's chat window closed, « both chats », competitive content)
/// the panels Tensho hid are shown again (ChatGameHide), and on unload too. X-527 (0.1506): while the game hides its own interface (cutscene,
/// GPose, interface hidden) or its chat (ChatLog not shown), they are left as they are (Hold), never shown again over it. It also reads the box Tensho's chat window takes in that mode
/// (ChatFollow: the frame of the messages and tabs down to the typing row). X-533 (0.1508, the user's choice of 16/11): the tab bar's nodes
/// of ChatLog (ChatTabBar) are hidden and given back with the panels, and whether the channel menu is open is read for Tensho's window to
/// step aside. Tensho sends nothing to the game: the typing stays the game's own. A reading that fails stops the hider and gives the panels
/// and the tabs back (R08).
/// </summary>
internal sealed unsafe class GameChatHider : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<ChatHideWant> wanted;
    private readonly ChatGameHide hide=new();
    private readonly ChatGameHide tabs=new();
    private volatile ChatFollowPlace? place;
    private volatile bool menuOpen;
    private bool failed,disposed,said,saidTabs;
    private string lastMenu="",lastPlace="";

    public GameChatHider(IFramework framework,IGameGui gui,IPluginLog log,Func<ChatHideWant> wanted)
    {
        this.framework=framework;this.gui=gui;this.log=log;this.wanted=wanted;
        // Always called, also during a competitive cut: the panels are given back there (wanted is false in a cut).
        TenshoAssistant.Competitive.CompetitiveGate.AddAlways(framework,OnUpdate);
    }

    /// <summary>Where Tensho's chat window goes while the game's messages are hidden (screen pixels); null when they are not hidden or the
    /// box could not be read.</summary>
    public ChatFollowPlace? Place=>place;

    /// <summary>X-533 (0.1508): the game's channel menu is open while the game's messages are hidden (Tensho's window steps aside).</summary>
    public bool ChannelMenuOpen=>menuOpen;

    /// <summary>X-533 (0.1508): developer mode, for the log lines of the channel menu and of Tensho's place (none when unset).</summary>
    public Func<bool> DeveloperMode{get;init;}=()=>false;

    private static ChatLogBox Window(AtkUnitBase* unit)
    {
        ushort width=0,height=0;
        unit->GetSize(&width,&height,true);
        return new(unit->X,unit->Y,width,height);
    }

    private static ChatLogBox? Node(AtkResNode* node,float scale)
        =>node==null ? null : new ChatLogBox(node->ScreenX,node->ScreenY,node->Width*node->ScaleX*scale,node->Height*node->ScaleY*scale);

    private AtkUnitBase* Panel(int index)
    {
        var handle=gui.GetAddonByName($"ChatLogPanel_{index}");
        return handle.IsNull || !handle.IsReady ? null : (AtkUnitBase*)handle.Address;
    }

    private AddonChatLog* Chat()
    {
        var handle=gui.GetAddonByName("ChatLog");
        return handle.IsNull || !handle.IsReady ? null : (AddonChatLog*)handle.Address;
    }

    /// <summary>X-533 (0.1508): the node of a slot of the tab bar (ChatTabBar), null when the game has none there.</summary>
    private static AtkResNode* TabNode(AddonChatLog* chat,int slot)
    {
        if(slot<ChatTabBar.Tabs)
        {
            var tab=(AtkComponentBase*)chat->ChatTabs[slot].Value;
            return tab==null || tab->OwnerNode==null ? null : &tab->OwnerNode->AtkResNode;
        }
        return slot switch
        {
            ChatTabBar.AddTab=>chat->AddTabComponentNode==null ? null : &chat->AddTabComponentNode->AtkResNode,
            ChatTabBar.BarStart=>chat->TabBarStartImageNode==null ? null : &chat->TabBarStartImageNode->AtkResNode,
            ChatTabBar.BarEnd=>chat->TabBarEndImageNode==null ? null : &chat->TabBarEndImageNode->AtkResNode,
            ChatTabBar.Settings=>chat->SettingsComponentNode==null ? null : &chat->SettingsComponentNode->AtkResNode,
            _=>null,
        };
    }

    /// <summary>A node and all its parents shown (bounded: no recursion on the game's data, R07).</summary>
    private static bool Shown(AtkResNode* node)
    {
        for(var depth=0;node!=null && depth<32;depth++,node=node->ParentNode)
            if(!node->IsVisible())return false;
        return true;
    }

    private void OnUpdate(IFramework _)
    {
        if(failed || disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        try
        {
            // Given back during a competitive cut, whatever the option says.
            var now=wanted();
            if(TenshoAssistant.Competitive.CompetitiveGate.Cut)now=ChatHideWant.Release;
            // X-527 (0.1506): the game's chat not shown (interface hidden by the game, cutscene, HUD): nothing touched, nothing shown again.
            if(!ChatLogShown())now=ChatHideWant.Hold;
            var states=new List<ChatPanelState>(4);
            var boxes=new List<ChatLogPanelBox>(4);
            // X-528 M2 (0.1507): only the panels under Tensho's window (panel 0 and the docked tabs at its place) are hidden.
            var main=Panel(0);
            ChatLogBox? mainBox=main!=null ? Window(main):null;
            for(var index=0;index<4;index++)
            {
                var unit=Panel(index);
                var box=unit!=null ? Window(unit):default;
                states.Add(new(index,unit!=null,unit!=null && unit->IsVisible,unit!=null && ChatFollow.Under(index,box,mainBox)));
                if(unit!=null)boxes.Add(new(index,unit->IsVisible,box));
            }
            var (toHide,toShow)=hide.Step(now,states);
            foreach(var index in toHide){var unit=Panel(index);if(unit!=null)unit->IsVisible=false;}
            foreach(var index in toShow){var unit=Panel(index);if(unit!=null)unit->IsVisible=true;}
            if(!said && toHide.Count>0){said=true;log.Information("Game chat: its message panels hidden ({0}); the typing box stays the game's.",string.Join(", ",toHide));}
            if(toShow.Count>0)log.Information("Game chat: its message panels shown again ({0}).",string.Join(", ",toShow));
            StepTabs(now);
            place=now==ChatHideWant.Hide ? Read(boxes) : null;
            menuOpen=now==ChatHideWant.Hide && MenuOpen();
        }
        catch(Exception error)
        {
            failed=true;
            place=null;
            menuOpen=false;
            log.Warning(error,"Hiding the game chat's messages stopped; they are shown again.");
            Release();
        }
    }

    /// <summary>X-533 (0.1508): the tab bar, with the same choice as the panels (hidden, held, given back).</summary>
    private void StepTabs(ChatHideWant now)
    {
        var chat=Chat();
        if(chat==null){tabs.Step(ChatHideWant.Hold,[]);return;}
        var states=new List<ChatPanelState>(ChatTabBar.Count);
        for(var slot=0;slot<ChatTabBar.Count;slot++)
        {
            var node=TabNode(chat,slot);
            states.Add(new(slot,ChatTabBar.Ready(slot,chat->TabCount,node!=null),node!=null && node->IsVisible()));
        }
        var (toHide,toShow)=tabs.Step(now,states);
        foreach(var slot in toHide){var node=TabNode(chat,slot);if(node!=null)node->ToggleVisibility(false);}
        foreach(var slot in toShow){var node=TabNode(chat,slot);if(node!=null)node->ToggleVisibility(true);}
        if(!saidTabs && toHide.Count>0){saidTabs=true;log.Information("Game chat: its tab bar hidden ({0}); the typing box and the channel menu stay the game's.",string.Join(", ",toHide));}
        if(toShow.Count>0)log.Information("Game chat: its tab bar shown again ({0}).",string.Join(", ",toShow));
    }

    /// <summary>X-533 (0.1508): whether the channel menu (ChatLog's ChannelSelectDropDown) is open: the window's open drop-down
    /// (CurrentDropDownOwnerNode) or the menu's list shown. Both signals are written to the log on each change in developer mode, for the
    /// in-game trial to tell which one the game sets (docs/evidence/0.1508-game-chat-channel-menu.md).</summary>
    private bool MenuOpen()
    {
        var chat=Chat();
        if(chat==null)return false;
        var owner=chat->AtkUnitBase.CurrentDropDownOwnerNode!=null;
        var drop=chat->ChannelSelectDropDown;
        var list=drop==null ? null : (AtkComponentBase*)drop->List;
        var node=list==null || list->OwnerNode==null ? null : &list->OwnerNode->AtkResNode;
        var shown=node!=null && Shown(node);
        var open=owner || shown;
        if(DeveloperMode())
        {
            var said=$"{(open ? "open" : "closed")} (drop-down owner {(owner ? "set" : "none")}, list {(shown ? "shown" : "hidden")} {Node(node,chat->AtkUnitBase.Scale)?.ToString() ?? "unread"})";
            if(said!=lastMenu){lastMenu=said;log.Information("Game chat: channel menu {0}.",said);}
        }
        return open;
    }

    /// <summary>X-527 (0.1506): the game's chat window (typing box) ready and shown.</summary>
    private bool ChatLogShown()
    {
        var handle=gui.GetAddonByName("ChatLog");
        return !handle.IsNull && handle.IsReady && ((AtkUnitBase*)handle.Address)->IsVisible;
    }

    private ChatFollowPlace? Read(List<ChatLogPanelBox> boxes)
    {
        var chat=Chat();
        if(chat==null)return null;
        var unit=&chat->AtkUnitBase;
        if(!unit->IsVisible)return null;
        var input=(AtkComponentBase*)chat->TextInput;
        var typing=input==null || input->OwnerNode==null ? null : Node(&input->OwnerNode->AtkResNode,unit->Scale);
        // X-533 (0.1508): the current channel's name (« Say ») is drawn above the typing line: Tensho's window stops above it too.
        var name=chat->CurrentChannelTextNode==null ? null : Node(&chat->CurrentChannelTextNode->AtkResNode,unit->Scale);
        var window=Window(unit);
        var box=ChatFollow.Box(boxes,window,typing,name);
        if(DeveloperMode())
        {
            var said=$"{box?.ToString() ?? "none"} (window {window}, panel {boxes.FirstOrDefault(each=>each.Index==0).Box}, input {typing?.ToString() ?? "unread"}, channel name {name?.ToString() ?? "unread"})";
            if(said!=lastPlace){lastPlace=said;log.Information("Game chat: Tensho's place {0}.",said);}
        }
        return box is { } found ? new ChatFollowPlace(found) : null;
    }

    /// <summary>Shows again every panel and every part of the tab bar Tensho hid (on the game's thread; skipped when the game is closing).</summary>
    private void Release()
    {
        try
        {
            foreach(var index in hide.GiveBack()){var unit=Panel(index);if(unit!=null)unit->IsVisible=true;}
        }
        catch(Exception error){log.Warning(error,"The game chat's message panels could not be shown again; reopen the chat tab or reload the interface.");}
        try
        {
            var slots=tabs.GiveBack();
            var chat=slots.Count>0 ? Chat():null;
            if(chat!=null)foreach(var slot in slots){var node=TabNode(chat,slot);if(node!=null)node->ToggleVisibility(true);}
        }
        catch(Exception error){log.Warning(error,"The game chat's tab bar could not be shown again; reload the interface.");}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.RemoveAlways(framework,OnUpdate);
        place=null;
        menuOpen=false;
        if(hide.Hidden.Count==0 && tabs.Hidden.Count==0 || framework.IsFrameworkUnloading)return;
        try{framework.RunOnFrameworkThread(Release).Wait(TimeSpan.FromSeconds(2));}
        catch(Exception error){log.Warning(error,"The game chat's message panels could not be shown again on unload.");}
    }
}

/// <summary>X-467 (0.1504): the box Tensho's chat window takes while the game's messages are hidden (screen pixels).</summary>
internal sealed record ChatFollowPlace(ChatLogBox Box);
