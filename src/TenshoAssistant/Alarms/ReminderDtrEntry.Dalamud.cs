using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using TenshoAssistant.Reminders;

namespace TenshoAssistant.Alarms;

/// <summary>
/// 0.455 (user request, in-game campaign): reminders apart from alarms in the server info bar: their own icon and the number of finished
/// reminders (ReminderCounts); hover lists them, click = the Reminders page. Read every 5 seconds on the framework thread once the character is
/// loaded; a failed read hides the entry (logged once); removed on dispose.
/// </summary>
public sealed class ReminderDtrEntry : IDisposable
{
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly IPlayerState player;
    private readonly IPluginLog log;
    private readonly Configuration configuration;
    private readonly Func<string,string> text;
    private readonly Dalamud.Game.Gui.Dtr.IDtrBarEntry entry;
    private IReadOnlyList<(FinishedKind Kind,int Count)> finished=[];
    private (Vector2 Min,Vector2 Max)? bounds;
    private DateTimeOffset nextRead;
    private bool disposed,warned;

    public ReminderDtrEntry(Dalamud.Plugin.Services.IDtrBar bar,IFramework framework,IGameGui gui,IPlayerState player,IPluginLog log,Configuration configuration,Func<string,string> text,Action open)
    {
        this.framework=framework;this.gui=gui;this.player=player;this.log=log;this.configuration=configuration;this.text=text;
        entry=bar.Get("Tensho Assistant (Reminders)");
        entry.Shown=false;
        entry.OnClick=_=>open();
        framework.Update+=OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(5);
        if(!configuration.ReminderServerBar || !player.IsLoaded){entry.Shown=false;bounds=null;return;}
        try
        {
            var timers=GameTimers.Read();
            finished=ReminderCounts.Finished(timers.Ventures?.Select(venture=>venture.Done),configuration.Voyages,
                timers.SquadronMission is { } mission ? (mission.Id,mission.Done):null,timers.SquadronTraining is { } training ? (training.Id,training.Done):null,
                configuration.GardenBeds,now);
            var shown=SeIconChar.QuestRepeatable.ToIconString()+" "+finished.Sum(item=>item.Count);
            entry.Text=new SeStringBuilder().AddText(shown).Build();
            entry.Shown=true;
            bounds=AlarmDtrEntry.FindTextNode(gui,shown);
            entry.Tooltip=configuration.AlarmBarTooltip && bounds is not null ? null:new SeStringBuilder().AddText(string.Join("\n",Lines())).Build();
            warned=false;
        }
        catch(Exception error)
        {
            entry.Shown=false;bounds=null;nextRead=now.AddSeconds(60);
            if(!warned){warned=true;log.Warning(error,"Finished reminders could not be read (perhaps after a game update).");}
        }
    }

    private IEnumerable<string> Lines()
    {
        if(finished.Count==0){yield return text("Reminders.BarNone");yield break;}
        foreach(var (kind,count) in finished)
            yield return string.Format(System.Globalization.CultureInfo.CurrentCulture,text("Reminders.Bar"+kind),count);
    }

    /// <summary>Tensho's tooltip below the entry while the mouse is over it (ImGui, from the plugin's draw).</summary>
    public void DrawTooltip()
    {
        if(disposed || !configuration.AlarmBarTooltip || bounds is not { } box || !entry.Shown)return;
        var mouse=ImGui.GetIO().MousePos;
        if(mouse.X<box.Min.X || mouse.X>box.Max.X || mouse.Y<box.Min.Y || mouse.Y>box.Max.Y)return;
        ImGui.SetNextWindowPos(new Vector2(box.Min.X,box.Max.Y+4));
        ImGui.BeginTooltip();
        try
        {
            ImGui.TextColored(UI.FfxivTheme.Gold,text("Reminders.BarTitle"));
            foreach(var line in Lines())ImGui.TextUnformatted(line);
            ImGui.TextDisabled(text("Reminders.BarClick"));
        }
        finally{ImGui.EndTooltip();}
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
        entry.Remove();
    }
}
