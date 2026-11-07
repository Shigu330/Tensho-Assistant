using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using TenshoAssistant.Competitive;

namespace TenshoAssistant.Gathering;

/// <summary>
/// 0.728 (user request): where an action sits on the visible hotbars, read on the framework thread while one is wanted and copied for the drawing. Reads only: nothing is hooked, called or
/// used in the game. The cross hotbars are not covered. docs/evidence/0.728-gather-next.md.
/// </summary>
internal sealed class ActionBarLocator : IDisposable
{
    private static readonly string[] Bars=["_ActionBar","_ActionBar01","_ActionBar02","_ActionBar03","_ActionBar04","_ActionBar05","_ActionBar06","_ActionBar07","_ActionBar08","_ActionBar09"];
    private const int FramesBetweenReads=6,MaximumSlots=16;
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private int frames;
    private bool failed;
    private volatile IReadOnlyList<ActionBarRect> rects=[];
    private uint wanted,readFor;
    private long wantedAt;

    /// <summary>Asked at each frame by the drawing while an action is to be found; a request older than half a second is dropped.</summary>
    public void Want(uint action){Volatile.Write(ref wanted,action);Volatile.Write(ref wantedAt,System.Diagnostics.Stopwatch.GetTimestamp());}
    public IReadOnlyList<ActionBarRect> Rects=>rects;
    /// <summary>The action the rectangles are for; 0 until the bars have been read for one.</summary>
    public uint ReadFor=>Volatile.Read(ref readFor);

    public ActionBarLocator(IFramework framework,IGameGui gui,IPluginLog log)
    {
        this.framework=framework;this.gui=gui;this.log=log;
        CompetitiveGate.Add(framework,OnUpdate);
    }

    private static unsafe bool Visible(AtkResNode* node)
    {
        var count=0;
        while(node!=null){if(++count>32 || !node->IsVisible())return false;node=node->ParentNode;}
        return count>0;
    }

    private unsafe void OnUpdate(IFramework _)
    {
        var wanted=Volatile.Read(ref this.wanted);
        if(wanted!=0 && System.Diagnostics.Stopwatch.GetElapsedTime(Volatile.Read(ref wantedAt))>TimeSpan.FromMilliseconds(500))wanted=0;
        if(wanted==0){if(Volatile.Read(ref readFor)!=0){Volatile.Write(ref readFor,0u);rects=[];}return;}
        if(failed || ++frames<FramesBetweenReads)return;
        frames=0;
        try
        {
            var found=new List<ActionBarRect>();
            var module=RaptureHotbarModule.Instance();
            var actions=ActionManager.Instance();
            foreach(var name in Bars)
            {
                var handle=gui.GetAddonByName(name);
                if(handle.Address==nint.Zero)continue;
                var addon=(AddonActionBarBase*)handle.Address;
                if(!addon->AtkUnitBase.IsVisible)continue;
                var slots=addon->ActionBarSlotVector.AsSpan();
                var scale=addon->AtkUnitBase.Scale;
                for(var index=0;index<slots.Length && index<MaximumSlots;index++)
                {
                    if(slots[index].Icon==null)continue;
                    // 0.734 (user report): the game turns some actions into their next rank on the bar (Bountiful Harvest shows as II): the slot keeps the first one, so what it shows counts.
                    var matched=slots[index].ActionId==(int)wanted;
                    if(module!=null && addon->RaptureHotbarId<module->Hotbars.Length)
                    {
                        ref var saved=ref module->Hotbars[addon->RaptureHotbarId].Slots[index];
                        // An item or a macro can carry the same number: only an action counts.
                        if(saved.ApparentSlotType!=RaptureHotbarModule.HotbarSlotType.Action)continue;
                        matched|=saved.ApparentActionId==wanted || actions!=null && saved.CommandId!=0 && actions->GetAdjustedActionId(saved.CommandId)==wanted;
                    }
                    if(!matched)continue;
                    var node=&slots[index].Icon->AtkResNode;
                    if(!Visible(node))continue;
                    var rect=new ActionBarRect(node->ScreenX,node->ScreenY,node->Width*node->ScaleX*scale,node->Height*node->ScaleY*scale);
                    if(rect.Valid)found.Add(rect);
                }
            }
            rects=found;
            Volatile.Write(ref readFor,wanted);
        }
        catch(Exception error)
        {
            failed=true;rects=[];Volatile.Write(ref readFor,0u);
            log.Warning(error,"The hotbars could not be read: no highlight until the plugin reloads.");
        }
    }

    public void Dispose()=>CompetitiveGate.Remove(framework,OnUpdate);
}
