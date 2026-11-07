using Dalamud.Plugin.Services;
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
    private volatile bool read;

    /// <summary>The action to find; 0: none. Written by the drawing, read here.</summary>
    public uint Wanted { get; set; }
    public IReadOnlyList<ActionBarRect> Rects=>rects;
    /// <summary>False until the bars have been read for this action.</summary>
    public bool Read=>read;

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
        var wanted=Wanted;
        if(wanted==0){if(read){rects=[];read=false;}return;}
        if(failed || ++frames<FramesBetweenReads)return;
        frames=0;
        try
        {
            var found=new List<ActionBarRect>();
            var module=RaptureHotbarModule.Instance();
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
                    if(slots[index].ActionId!=(int)wanted || slots[index].Icon==null)continue;
                    // An item or a macro can carry the same number: only an action counts.
                    if(module!=null && addon->RaptureHotbarId<module->Hotbars.Length
                        && module->Hotbars[addon->RaptureHotbarId].Slots[index].ApparentSlotType!=RaptureHotbarModule.HotbarSlotType.Action)continue;
                    var node=&slots[index].Icon->AtkResNode;
                    if(!Visible(node))continue;
                    var rect=new ActionBarRect(node->ScreenX,node->ScreenY,node->Width*node->ScaleX*scale,node->Height*node->ScaleY*scale);
                    if(rect.Valid)found.Add(rect);
                }
            }
            rects=found;
            read=true;
        }
        catch(Exception error)
        {
            failed=true;rects=[];read=false;
            log.Warning(error,"The hotbars could not be read: no highlight until the plugin reloads.");
        }
    }

    public void Dispose()=>CompetitiveGate.Remove(framework,OnUpdate);
}
