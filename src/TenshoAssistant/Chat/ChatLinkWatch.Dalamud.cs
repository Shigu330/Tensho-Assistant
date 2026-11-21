using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-715 (0.1718, D-202 b, the user's choice of 21/11): developer mode only, read-only: what the game's chat sends when one of its links (a
/// player's name) is clicked, and what the menu the game then opens from its chat aims at (docs/evidence/0.1718-chat-link-click-probe.md).
/// A hook on LogViewer.HandleLinkClick (FFXIVClientStructs' signature), enabled only while developer mode is on (checked once a second) and
/// until the probe's lines are used up: the detour copies the link's fields, then always calls the game's own function with what it was
/// given; it writes nothing and calls nothing else of the game. The menu is read from Dalamud's IContextMenu. Nothing while Tensho is cut
/// (R03.2). The lines (ChatLinkProbe) never hold a name nor a whole identifier (R04). A failure turns the probe off, said once.
/// </summary>
internal sealed unsafe class ChatLinkWatch : IDisposable
{
    private readonly IPluginLog log;private readonly Func<bool> developerMode;
    private readonly ChatLinkProbe probe=new();
    private readonly RegistrationScope registrations=new();
    private Hook<LogViewer.Delegates.HandleLinkClick>? hook;
    private bool watching,failed,errorSaid;
    private long checkedAt=-100_000;

    public ChatLinkWatch(IGameInteropProvider interop,IFramework framework,IContextMenu menus,IPluginLog log,Func<bool> developerMode)
    {
        this.log=log;this.developerMode=developerMode;
        try
        {
            if((nint)LogViewer.MemberFunctionPointers.HandleLinkClick==0)throw new InvalidOperationException("LogViewer.HandleLinkClick not resolved");
            hook=interop.HookFromAddress<LogViewer.Delegates.HandleLinkClick>((nint)LogViewer.MemberFunctionPointers.HandleLinkClick,OnLinkClick);
        }
        catch(Exception error){hook=null;failed=true;log.Warning(error,"Chat link probe (developer mode): the game chat's link clicks cannot be watched (maybe after a game update).");}
        try
        {
            registrations.Register(()=>TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate),()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate));
            registrations.Register(()=>menus.OnMenuOpened+=OnMenuOpened,()=>menus.OnMenuOpened-=OnMenuOpened);
        }
        catch{registrations.Dispose();hook?.Dispose();throw;}
    }

    /// <summary>Once a second: the hook on while developer mode is on and the probe has lines left, off otherwise.</summary>
    private void OnUpdate(IFramework _)
    {
        var now=Environment.TickCount64;
        if(now-checkedAt<1000)return;
        checkedAt=now;
        var want=!failed && hook is not null && developerMode() && !probe.Full;
        if(want==watching)return;
        try
        {
            if(want)hook!.Enable();else hook!.Disable();
            watching=want;
            if(want)log.Information("Chat link probe (developer mode): watching the game chat's link clicks, read-only, {0} lines at most per session.",ChatLinkProbe.MaximumLines);
            else log.Information("Chat link probe: no longer watching.");
        }
        catch(Exception error){failed=true;watching=false;log.Warning(error,"Chat link probe: the hook could not be switched (said once); the probe is off.");}
    }

    private void OnLinkClick(LogViewer* viewer,LinkData* link)
    {
        string? line=null;
        // The link's fields copied before the game acts (its menu may open inside the game's function); its text is never read.
        try
        {
            if(link!=null && developerMode() && !TenshoAssistant.Competitive.CompetitiveGate.Cut)
                line=probe.Clicked(link->LinkType,link->LinkId,link->LinkIndex,link->LinkGroupId,link->IntValue1,link->IntValue2,link->IntValue3,link->Payload!=null,link->PayloadEnd,Environment.TickCount64);
        }
        catch(Exception error){Said(error);}
        hook!.Original(viewer,link);
        if(line is not null)log.Information(line);
    }

    private void OnMenuOpened(IMenuOpenedArgs args)
    {
        try
        {
            if(!watching || !developerMode() || TenshoAssistant.Competitive.CompetitiveGate.Cut)return;
            if(args.AddonName is not { } addon || !addon.StartsWith("ChatLog",StringComparison.Ordinal) || args.Target is not MenuTargetDefault target)return;
            if(probe.MenuOpened(addon,target.TargetContentId,target.TargetHomeWorld.RowId,target.TargetObjectId,target.TargetObject is not null,target.TargetName?.Length ?? 0,Environment.TickCount64) is { } line)
                log.Information(line);
        }
        catch(Exception error){Said(error);}
    }

    private void Said(Exception error)
    {
        if(errorSaid)return;
        errorSaid=true;
        log.Warning(error,"Chat link probe: a click or a menu could not be read (said once).");
    }

    public void Dispose(){registrations.Dispose();hook?.Dispose();hook=null;watching=false;}
}
