using TenshoAssistant.Competitive;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Alarms;

/// <summary>
/// 0.300 (user request): an alarm clock in the game's server info bar (next to the server name) while at least one Tensho alarm is
/// on — the game's clock glyph, the number of alarms and the next time; click = the Alarms page. Refreshed once a second on the
/// framework thread; removed on dispose. Hidden when the setting is off or no alarm is on.
/// 0.301 (user request): the game draws its own tooltip under plugin windows (the Tensho map hid it). Tensho finds its entry's text
/// node in the « _DTR » addon and draws its own tooltip below it (above every window), listing the next alarms; settings choose it
/// and how many alarms it lists. When the node cannot be found, the game's tooltip is used.
/// </summary>
public sealed unsafe class AlarmDtrEntry : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly Configuration configuration;
    private readonly Func<string,string> text;
    private readonly IDtrBarEntry entry;
    private DateTimeOffset nextRefresh;
    private string shownText="";
    private (Vector2 Min,Vector2 Max)? bounds;
    // 0.331: Weather = the alarm has a weather condition (its time is shown with « if the weather matches »: Ring may skip it).
    private IReadOnlyList<(string Label,DateTimeOffset At,bool Weather)> upcoming=[];
    private bool disposed;

    public AlarmDtrEntry(IDtrBar bar,IFramework framework,IGameGui gui,Configuration configuration,Func<string,string> text,Action open)
    {
        this.framework=framework;this.gui=gui;this.configuration=configuration;this.text=text;
        entry=bar.Get("Tensho Assistant");
        entry.Shown=false;
        entry.OnClick=_=>open();
        CompetitiveGate.AddAlways(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        // DEC-038: gone from the server bar during a competitive cut; the next tick after it shows it again.
        if(CompetitiveGate.Cut){entry.Shown=false;return;}
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRefresh)return;
        nextRefresh=now.AddSeconds(1);
        var enabled=(configuration.Alarms ?? []).Where(alarm=>alarm.Enabled).ToArray();
        entry.Shown=configuration.AlarmServerBar && enabled.Length>0;
        if(!entry.Shown){bounds=null;return;}
        upcoming=enabled.Select(alarm=>(Label:alarm.Label,At:AlarmSchedule.Next(alarm,now),Weather:alarm.Weathers.Count+alarm.PreviousWeathers.Count>0)).Where(pair=>pair.At is not null)
            .OrderBy(pair=>pair.At).Take(10).Select(pair=>(pair.Label,pair.At!.Value,pair.Weather)).ToArray();
        // 0.302 (user request): time left before the next alarm, not its clock time.
        var time=upcoming.Count==0 ? "":" · "+Remaining(upcoming[0].At-now);
        shownText=SeIconChar.Clock.ToIconString()+" "+enabled.Length+time;
        entry.Text=new SeStringBuilder().AddText(shownText).Build();
        bounds=FindTextNode(gui,shownText);
        // Own tooltip when the entry was found on screen; else the game's.
        if(configuration.AlarmBarTooltip && bounds is not null)entry.Tooltip=null;
        else entry.Tooltip=upcoming.Count==0 ? text("Alarms.BarNone")
            :string.Format(System.Globalization.CultureInfo.CurrentCulture,text("Alarms.BarNext"),upcoming[0].Label,upcoming[0].At.ToLocalTime().ToString("t",System.Globalization.CultureInfo.CurrentCulture))
                +(upcoming[0].Weather ? " · "+text("Alarms.WeatherCondition"):"");
    }

    /// <summary>« 2 h 15 » from one hour on, « 45 min » below, « < 1 min » at the end.</summary>
    public static string Remaining(TimeSpan left)
        =>left.TotalHours>=1 ? $"{(int)left.TotalHours} h {left.Minutes:00}":left.TotalMinutes>=1 ? $"{(int)left.TotalMinutes} min":"< 1 min";

    // The entry's text node in the server info bar addon: Dalamud adds it under the addon's root; matched by its text.
    internal static (Vector2 Min,Vector2 Max)? FindTextNode(IGameGui gui,string wanted)
    {
        var addon=(AtkUnitBase*)gui.GetAddonByName("_DTR").Address;
        if(addon==null || !addon->IsVisible || addon->RootNode==null)return null;
        var stack=new Stack<nint>();stack.Push((nint)addon->RootNode->ChildNode);
        var visited=0;
        while(stack.Count>0 && visited++<512)
        {
            var node=(AtkResNode*)stack.Pop();
            for(;node!=null && visited++<512;node=node->PrevSiblingNode)
            {
                if(node->ChildNode!=null)stack.Push((nint)node->ChildNode);
                if(node->Type!=NodeType.Text || !node->IsVisible())continue;
                var textNode=(AtkTextNode*)node;
                if(textNode->NodeText.ToString()!=wanted)continue;
                var scale=addon->Scale;
                var min=new Vector2(node->ScreenX,node->ScreenY);
                return (min,min+new Vector2(node->Width*node->ScaleX*scale,node->Height*node->ScaleY*scale));
            }
        }
        return null;
    }

    /// <summary>Draws Tensho's tooltip below the entry while the mouse is over it (ImGui, from the plugin's draw).</summary>
    public void DrawTooltip()
    {
        if(disposed || !configuration.AlarmBarTooltip || bounds is not { } box || !entry.Shown)return;
        var mouse=ImGui.GetIO().MousePos;
        if(mouse.X<box.Min.X || mouse.X>box.Max.X || mouse.Y<box.Min.Y || mouse.Y>box.Max.Y)return;
        ImGui.SetNextWindowPos(new Vector2(box.Min.X,box.Max.Y+4));
        ImGui.BeginTooltip();
        try
        {
            if(upcoming.Count==0){ImGui.TextUnformatted(text("Alarms.BarNone"));return;}
            ImGui.TextColored(UI.FfxivTheme.Gold,text("Alarms.BarTitle"));
            foreach(var (label,at,conditioned) in upcoming.Take(Math.Clamp(configuration.AlarmBarTooltipCount,1,10)))
                ImGui.TextUnformatted($"{Remaining(at-DateTimeOffset.UtcNow)}  ({at.ToLocalTime().ToString("t",System.Globalization.CultureInfo.CurrentCulture)})  {label}"
                    +(conditioned ? " · "+text("Alarms.WeatherCondition"):""));
            ImGui.TextDisabled(text("Alarms.BarClick"));
        }
        finally{ImGui.EndTooltip();}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        CompetitiveGate.RemoveAlways(framework,OnUpdate);
        entry.Remove();
    }
}
