using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Chat;

/// <summary>
/// X-467 (0.1502, developer mode only): writes to the log where the game's chat window puts its typing box, channel menu, tabs, background
/// and its four message panels (ChatLogLayout.Line), when the layout changes, at most once a second and MaximumLines times per load. Read-only:
/// it moves, hides and focuses nothing, and reads no text (docs/evidence/0.1502-game-chat-input.md). Off in competitive content (CompetitiveGate).
/// </summary>
internal sealed unsafe class ChatLogLayoutProbe : IDisposable
{
    private const int MaximumLines = 40;
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPluginLog log;
    private readonly Func<bool> developerMode;
    private string last = "";
    private int lines;
    private DateTimeOffset nextRead;
    private bool failed;

    public ChatLogLayoutProbe(IFramework framework,IGameGui gui,IPluginLog log,Func<bool> developerMode)
    {
        this.framework=framework;this.gui=gui;this.log=log;this.developerMode=developerMode;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private static ChatLogBox? Node(AtkResNode* node,float scale)
        =>node==null ? null : new ChatLogBox(node->ScreenX,node->ScreenY,node->Width*node->ScaleX*scale,node->Height*node->ScaleY*scale);

    private static ChatLogBox? Component(AtkComponentBase* component,float scale)
        =>component==null || component->OwnerNode==null ? null : Node(&component->OwnerNode->AtkResNode,scale);

    private static ChatLogBox Window(AtkUnitBase* unit)
    {
        ushort width=0,height=0;
        unit->GetSize(&width,&height,true);
        return new(unit->X,unit->Y,width,height);
    }

    private void OnUpdate(IFramework _)
    {
        if(failed || lines>=MaximumLines || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        var now=DateTimeOffset.UtcNow;
        if(now<nextRead || !developerMode())return;
        nextRead=now+TimeSpan.FromSeconds(1);
        try
        {
            var handle=gui.GetAddonByName("ChatLog");
            if(handle.IsNull || !handle.IsReady)return;
            var chat=(AddonChatLog*)handle.Address;
            var unit=&chat->AtkUnitBase;
            var scale=unit->Scale;
            var input=(AtkComponentBase*)chat->TextInput;
            var tab=chat->TabCount>0 ? (AtkComponentBase*)chat->ChatTabs[0].Value : null;
            var focused=input!=null && input->OwnerNode!=null && unit->FocusNode==&input->OwnerNode->AtkResNode;
            var panels=new List<ChatLogPanelBox>();
            for(var index=0;index<4;index++)
            {
                var panel=gui.GetAddonByName($"ChatLogPanel_{index}");
                if(panel.IsNull || !panel.IsReady)continue;
                panels.Add(new(index,panel.IsVisible,Window((AtkUnitBase*)panel.Address)));
            }
            var layout=new ChatLogLayout(unit->IsVisible,scale,Window(unit),Component(input,scale),Component((AtkComponentBase*)chat->ChannelSelectDropDown,scale),
                Node((AtkResNode*)chat->BackgroundNode,scale),Component(tab,scale),focused,panels);
            var line=layout.Line();
            if(line==last)return;
            last=line;
            lines++;
            log.Information("Game chat layout: {0}",line);
        }
        catch(Exception error)
        {
            failed=true;
            log.Warning(error,"Game chat layout probe stopped.");
        }
    }

    public void Dispose()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
}
