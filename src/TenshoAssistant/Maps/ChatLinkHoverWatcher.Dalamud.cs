using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Maps;

/// <summary>
/// X-365 (0.1351): which chat map link is under the mouse. The chat panels (ChatLogPanel_0 to _3) receive the game's link events
/// (AtkEventType.LinkMouseOver 76, LinkMouseOut 77) with the link's data (AtkEventData.LinkData: Payload points at the link in the
/// message text, NUL-terminated, the same bytes Dalamud reads for its own chat links); Dalamud's IAddonLifecycle shows them to plugins
/// (PreReceiveEvent), so Tensho adds no hook. Read only, copied on the game thread; the event goes on to the game unchanged.
/// Evidence: docs/evidence/0.1351-chat-map-link-menu.md.
/// </summary>
public sealed unsafe class ChatLinkHoverWatcher : IDisposable
{
    internal static readonly string[] Panels=["ChatLogPanel_0","ChatLogPanel_1","ChatLogPanel_2","ChatLogPanel_3"];
    internal const byte LinkMouseOver=76,LinkMouseOut=77;
    // A link payload is a few dozen bytes; the copy stops at the text's NUL or at this bound.
    private const int MaximumBytes=512;
    private readonly RegistrationScope registrations=new();
    private readonly IPluginLog log;
    private readonly Func<bool> enabled;
    private readonly Func<bool> developer;
    private bool disposed,warned;private string lastSeen="";
    public ChatLinkHover Hover{get;}=new();

    public ChatLinkHoverWatcher(IAddonLifecycle lifecycle,IPluginLog log,Func<bool> enabled,Func<bool> developer)
    {
        this.log=log;this.enabled=enabled;this.developer=developer;
        try{registrations.Register(()=>lifecycle.RegisterListener(AddonEvent.PreReceiveEvent,Panels,OnEvent),()=>lifecycle.UnregisterListener(AddonEvent.PreReceiveEvent,Panels,OnEvent));}
        catch{registrations.Dispose();throw;}
    }

    private void OnEvent(AddonEvent type,AddonArgs args)
    {
        if(disposed)return;
        if(TenshoAssistant.Competitive.CompetitiveGate.Cut || !enabled()){Hover.Clear();return;}
        if(args is not AddonReceiveEventArgs received)return;
        var kind=(byte)received.AtkEventType;
        if(kind==LinkMouseOut){Hover.Out(Environment.TickCount64);return;}
        if(kind!=LinkMouseOver)return;
        try
        {
            var link=Read(received.AtkEventData,out var linkType);
            Hover.Over(link);
            warned=false;
            // Developer mode: what the chat's link events say, once per change (the in-game test reads it, R15.2).
            var seen=link is null ? $"link type {linkType}, not a map link":$"map link, map {link.MapId}";
            if(seen!=lastSeen){lastSeen=seen;if(developer())log.Information("Chat link hovered ({0}): {1}.",args.AddonName,seen);}
        }
        catch(Exception error)
        {
            Hover.Clear();
            if(!warned){warned=true;log.Warning(error,"A chat link could not be read.");}
        }
    }

    private static HoveredMapLink? Read(nint eventData,out int linkType)
    {
        linkType=-1;
        if(eventData==0)return null;
        var data=((AtkEventData*)eventData)->LinkData;
        if(data==null || data->Payload==null)return null;
        linkType=data->LinkType;
        var bytes=new List<byte>(64);
        for(var index=0;index<MaximumBytes && data->Payload[index]!=0;index++)bytes.Add(data->Payload[index]);
        // The link's own payload comes first; a link cut by the bound does not parse and is left out.
        if(SeString.Parse(bytes.ToArray()).Payloads.FirstOrDefault() is not MapLinkPayload map)return null;
        return new(map.TerritoryType.RowId,map.Map.RowId,map.XCoord,map.YCoord,map.PlaceName ?? "");
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        registrations.Dispose();
        Hover.Clear();
    }
}
