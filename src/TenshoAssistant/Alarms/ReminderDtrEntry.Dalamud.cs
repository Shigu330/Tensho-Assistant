using TenshoAssistant.Competitive;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using TenshoAssistant.Reminders;

namespace TenshoAssistant.Alarms;

/// <summary>
/// 0.455, 0.458, 0.459 (user requests, in-game campaign): reminders apart from alarms in the server info bar, on two icons: what is left to do (rows
/// not ticked since their reset and the player's tasks on screen, ReminderCounts) and what is finished (ventures, vessels, squadron, ripe beds; shown only when
/// there is something). The Reminders page chooses with its « Bar » boxes; hover lists them, click = the Reminders page. Read every 5 seconds on the framework
/// thread once the character is loaded; a failed read hides both (logged once); removed on dispose.
/// </summary>
public sealed class ReminderDtrEntry : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly Configuration configuration;
    private readonly Func<string,string> text;
    private readonly Func<DateTimeOffset,IReadOnlyList<string>> todo;
    private readonly IDtrBarEntry todoEntry,finishedEntry;
    private IReadOnlyList<string> todoLines=[],finishedLines=[];
    private (Vector2 Min,Vector2 Max)? todoBounds,finishedBounds;
    private DateTimeOffset nextRead;
    private bool disposed,warned;

    private readonly Action<GameTimerSnapshot>? fromGame;
    public ReminderDtrEntry(IDtrBar bar,IFramework framework,IGameGui gui,IPlayerState player,IPluginLog log,Configuration configuration,Func<string,string> text,Action open,Func<DateTimeOffset,IReadOnlyList<string>> todo,Action<GameTimerSnapshot>? fromGame=null)
    {
        this.fromGame=fromGame;
        this.framework=framework;this.gui=gui;this.player=player;this.log=log;this.configuration=configuration;this.text=text;this.todo=todo;
        todoEntry=bar.Get("Tensho Assistant (Reminders)");
        finishedEntry=bar.Get("Tensho Assistant (Finished reminders)");
        todoEntry.Shown=false;finishedEntry.Shown=false;
        todoEntry.OnClick=_=>open();finishedEntry.OnClick=_=>open();
        CompetitiveGate.AddAlways(framework,OnUpdate);
    }

    private void Hide(){todoEntry.Shown=false;finishedEntry.Shown=false;todoBounds=null;finishedBounds=null;}

    private void OnUpdate(IFramework _)
    {
        // DEC-038: gone from the server bar during a competitive cut; the next tick after it shows it again.
        if(CompetitiveGate.Cut){todoEntry.Shown=false;finishedEntry.Shown=false;return;}
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(5);
        if((!configuration.ReminderServerBar && !configuration.ReminderFinishedServerBar) || !player.IsLoaded){Hide();return;}
        try
        {
            var timers=GameTimers.Read();
            fromGame?.Invoke(timers);
            var finished=ReminderCounts.Finished(timers.Ventures?.Select(venture=>venture.Done),configuration.Voyages,
                timers.SquadronMission is { } mission ? (mission.Id,mission.Done):null,timers.SquadronTraining is { } training ? (training.Id,training.Done):null,
                configuration.GardenBeds,now).Where(item=>configuration.ReminderBarHidden?.Contains(item.Kind.ToString())!=true).ToArray();
            finishedLines=finished.Select(item=>string.Format(System.Globalization.CultureInfo.CurrentCulture,text("Reminders.Bar"+item.Kind),item.Count)).ToArray();
            todoLines=todo(now);
            todoBounds=Show(todoEntry,configuration.ReminderServerBar,SeIconChar.QuestRepeatable,todoLines.Count,text("Reminders.BarTitle"),todoLines.Count==0 ? [text("Reminders.BarNone")]:todoLines);
            finishedBounds=Show(finishedEntry,configuration.ReminderFinishedServerBar && finished.Length>0,SeIconChar.BoxedStar,finished.Sum(item=>item.Count),text("Reminders.BarFinishedTitle"),finishedLines);
            warned=false;
        }
        catch(Exception error)
        {
            Hide();nextRead=now.AddSeconds(60);
            if(!warned){warned=true;log.Warning(error,"Reminders could not be read for the server bar (perhaps after a game update).");}
        }
    }

    private (Vector2,Vector2)? Show(IDtrBarEntry entry,bool shown,SeIconChar icon,int count,string title,IEnumerable<string> lines)
    {
        entry.Shown=shown;
        if(!shown)return null;
        var label=icon.ToIconString()+" "+count;
        entry.Text=new SeStringBuilder().AddText(label).Build();
        var bounds=AlarmDtrEntry.FindTextNode(gui,label);
        entry.Tooltip=configuration.AlarmBarTooltip && bounds is not null ? null:new SeStringBuilder().AddText(title+"\n"+string.Join("\n",lines)).Build();
        return bounds;
    }

    /// <summary>Tensho's tooltips below the entries while the mouse is over one (ImGui, from the plugin's draw).</summary>
    public void DrawTooltip()
    {
        if(disposed || !configuration.AlarmBarTooltip)return;
        Tooltip(todoBounds,text("Reminders.BarTitle"),todoLines.Count==0 ? [text("Reminders.BarNone")]:todoLines);
        Tooltip(finishedBounds,text("Reminders.BarFinishedTitle"),finishedLines);
    }

    private void Tooltip((Vector2 Min,Vector2 Max)? bounds,string title,IEnumerable<string> lines)
    {
        if(bounds is not { } box)return;
        var mouse=ImGui.GetIO().MousePos;
        if(mouse.X<box.Min.X || mouse.X>box.Max.X || mouse.Y<box.Min.Y || mouse.Y>box.Max.Y)return;
        ImGui.SetNextWindowPos(new Vector2(box.Min.X,box.Max.Y+4));
        ImGui.BeginTooltip();
        try
        {
            ImGui.TextColored(UI.FfxivTheme.Gold,title);
            foreach(var line in lines)ImGui.TextUnformatted(line);
            ImGui.TextDisabled(text("Reminders.BarClick"));
        }
        finally{ImGui.EndTooltip();}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        CompetitiveGate.RemoveAlways(framework,OnUpdate);
        todoEntry.Remove();finishedEntry.Remove();
    }
}
