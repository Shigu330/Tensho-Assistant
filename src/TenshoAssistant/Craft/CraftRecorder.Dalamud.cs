using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Craft;

/// <summary>
/// A-14 (0.436, user's choice in GAPS: a read-only hook on the action used): while the player records, Tensho hooks the game's
/// ActionManager.UseAction (address checked first), always calls the game's own function first and returns its result unchanged; when
/// the game accepted an Action or CraftAction that is a crafting action of the model, it is noted. The hook exists only while recording
/// (made on Start, disabled on Stop, disposed with the plugin); nothing is changed or sent.
/// </summary>
public sealed unsafe class CraftRecorder : IDisposable
{
    private readonly IGameInteropProvider interop;private readonly IPluginLog log;private readonly Func<CraftActionCatalog?> catalog;
    // A-14 (0.452, review): ClientStructs' own delegate: its assembly disables runtime marshalling, so bool is the game's 1 byte. A delegate
    // declared here marshalled it as a 4-byte BOOL, testing undefined bits: a refused action could read as accepted, and that « true » went back to the game.
    private Hook<ActionManager.Delegates.UseAction>? hook;
    private int warned;
    public CraftRecording Recording { get; }=new();

    public CraftRecorder(IGameInteropProvider interop,IPluginLog log,Func<CraftActionCatalog?> catalog){this.interop=interop;this.log=log;this.catalog=catalog;}

    /// <summary>Starts recording; false when the game function is not resolved or the hook could not be made.</summary>
    public bool Start()
    {
        try
        {
            if(hook is null)
            {
                if((nint)ActionManager.MemberFunctionPointers.UseAction==0)return false;
                hook=interop.HookFromAddress<ActionManager.Delegates.UseAction>((nint)ActionManager.MemberFunctionPointers.UseAction,OnUseAction);
            }
            Recording.Start();
            hook.Enable();
            return true;
        }
        catch(Exception error){Recording.Stop();log.Warning(error,"Crafting rotation recorder unavailable.");return false;}
    }

    public void Stop()
    {
        Recording.Stop();
        try{hook?.Disable();}catch(Exception error){log.Warning(error,"Crafting rotation recorder could not be stopped cleanly.");}
    }

    private bool OnUseAction(ActionManager* manager,ActionType type,uint actionId,ulong targetId,uint extraParam,ActionManager.UseActionMode mode,uint comboRouteId,bool* outOptAreaTargeted)
    {
        // A-14 (0.452, review): OriginalDisposeSafe: Original throws into the game's frames once the hook is disposed. X-142 (0.1121, audit A09):
        // the same safe call in the competitive cut, which only skips the observation.
        var result=hook!.OriginalDisposeSafe(manager,type,actionId,targetId,extraParam,mode,comboRouteId,outOptAreaTargeted);
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut)return result;
        try
        {
            // An action only queued returns true, then returns true again when the queue replays it (mode Queue): it is noted when it runs, once.
            var queued=manager!=null && manager->ActionQueued && manager->QueuedActionType==type && manager->QueuedActionId==actionId;
            if(result && Recording.Recording && CraftRecording.Runs(type is ActionType.Action or ActionType.CraftAction,mode==ActionManager.UseActionMode.Queue,queued))
            {
                var known=catalog()?.KindOf(actionId);
                if(known is not null || type==ActionType.CraftAction)Recording.Observe(known);
            }
        }
        catch(Exception error){if(Interlocked.Exchange(ref warned,1)==0)log.Warning(error,"Crafting rotation recorder could not note an action.");}
        return result;
    }

    public void Dispose(){Recording.Stop();hook?.Dispose();}
}
