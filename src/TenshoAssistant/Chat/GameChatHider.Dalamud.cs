using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-467 (0.1504, D-181 of 16/11, DEC-053 completed): « Hide the game chat's messages ». Each frame, on the game's thread: while
/// <c>wanted</c> says so, the game's message panels (ChatLogPanel_0 to _3) are made invisible; the window ChatLog (the game's typing box, its
/// channel menu and tabs) is never touched. Out of it (the option off, Tensho's chat window closed, « both chats », competitive content)
/// the panels Tensho hid are shown again (ChatGameHide), and on unload too. X-527 (0.1506): while the game hides its own interface (cutscene,
/// GPose, interface hidden) or its chat (ChatLog not shown), they are left as they are (Hold), never shown again over it. It also reads the box Tensho's chat window takes in that mode
/// (ChatFollow: the panel's box down to the typing box). Tensho sends nothing to the game: the typing stays the game's own. A reading that
/// fails stops the hider and gives the panels back (R08).
/// </summary>
internal sealed unsafe class GameChatHider : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<ChatHideWant> wanted;
    private readonly ChatGameHide hide=new();
    private volatile ChatFollowPlace? place;
    private bool failed,disposed,said;

    public GameChatHider(IFramework framework,IGameGui gui,IPluginLog log,Func<ChatHideWant> wanted)
    {
        this.framework=framework;this.gui=gui;this.log=log;this.wanted=wanted;
        // Always called, also during a competitive cut: the panels are given back there (wanted is false in a cut).
        TenshoAssistant.Competitive.CompetitiveGate.AddAlways(framework,OnUpdate);
    }

    /// <summary>Where Tensho's chat window goes while the game's messages are hidden (screen pixels); null when they are not hidden or the
    /// box could not be read.</summary>
    public ChatFollowPlace? Place=>place;

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
            for(var index=0;index<4;index++)
            {
                var unit=Panel(index);
                states.Add(new(index,unit!=null,unit!=null && unit->IsVisible));
                if(unit!=null)boxes.Add(new(index,unit->IsVisible,Window(unit)));
            }
            var (toHide,toShow)=hide.Step(now,states);
            foreach(var index in toHide){var unit=Panel(index);if(unit!=null)unit->IsVisible=false;}
            foreach(var index in toShow){var unit=Panel(index);if(unit!=null)unit->IsVisible=true;}
            if(!said && toHide.Count>0){said=true;log.Information("Game chat: its message panels hidden ({0}); the typing box stays the game's.",string.Join(", ",toHide));}
            if(toShow.Count>0)log.Information("Game chat: its message panels shown again ({0}).",string.Join(", ",toShow));
            place=now==ChatHideWant.Hide ? Read(boxes) : null;
        }
        catch(Exception error)
        {
            failed=true;
            place=null;
            log.Warning(error,"Hiding the game chat's messages stopped; they are shown again.");
            Release();
        }
    }

    /// <summary>X-527 (0.1506): the game's chat window (typing box) ready and shown.</summary>
    private bool ChatLogShown()
    {
        var handle=gui.GetAddonByName("ChatLog");
        return !handle.IsNull && handle.IsReady && ((AtkUnitBase*)handle.Address)->IsVisible;
    }

    private ChatFollowPlace? Read(List<ChatLogPanelBox> boxes)
    {
        var handle=gui.GetAddonByName("ChatLog");
        if(handle.IsNull || !handle.IsReady)return null;
        var chat=(AddonChatLog*)handle.Address;
        var unit=&chat->AtkUnitBase;
        if(!unit->IsVisible)return null;
        var input=(AtkComponentBase*)chat->TextInput;
        var typing=input==null || input->OwnerNode==null ? null : Node(&input->OwnerNode->AtkResNode,unit->Scale);
        return ChatFollow.Box(boxes,Window(unit),typing) is { } box ? new ChatFollowPlace(box) : null;
    }

    /// <summary>Shows again every panel Tensho hid (on the game's thread; skipped when the game is closing).</summary>
    private void Release()
    {
        try
        {
            foreach(var index in hide.GiveBack()){var unit=Panel(index);if(unit!=null)unit->IsVisible=true;}
        }
        catch(Exception error){log.Warning(error,"The game chat's message panels could not be shown again; reopen the chat tab or reload the interface.");}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.RemoveAlways(framework,OnUpdate);
        place=null;
        if(hide.Hidden.Count==0 || framework.IsFrameworkUnloading)return;
        try{framework.RunOnFrameworkThread(Release).Wait(TimeSpan.FromSeconds(2));}
        catch(Exception error){log.Warning(error,"The game chat's message panels could not be shown again on unload.");}
    }
}

/// <summary>X-467 (0.1504): the box Tensho's chat window takes while the game's messages are hidden (screen pixels).</summary>
internal sealed record ChatFollowPlace(ChatLogBox Box);
