using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Reminders;

namespace TenshoAssistant;

// g, M-05 (0.375): the player's own tasks on the Reminders page (day, week, every n hours, counter; 50 at most) and the optional
// on-screen « to do » panel (off by default, hidden in combat), which lists the tasks marked for it that are not done.
internal sealed partial class MainWindow
{
    private string newTaskName="";
    private TaskPeriod newTaskPeriod;
    private int newTaskHours=20,newTaskTarget=5;
    private string? newTaskError;

    private void DrawPersonalTasks()
    {
        var tasks=configuration.PersonalTasks ?? [];
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("PersonalTasks.Title",tasks.Count)+"###PersonalTasks",ImGuiTreeNodeFlags.DefaultOpen))return;
        ImGui.TextDisabled(localization.Get("PersonalTasks.Help"));
        BoolSetting("PersonalTasks.Panel","PersonalTasks.PanelTip",()=>configuration.TaskPanel,value=>configuration.TaskPanel=value,c=>c.TaskPanel);
        var now=DateTimeOffset.UtcNow;PersonalTask? removed=null;var changed=false;var move=(-1,0);
        for(var index=0;index<tasks.Count;index++)
        {
            var task=tasks[index];
            ImGui.PushID(task.Id.ToString());
            try
            {
                var done=PersonalTasks.IsDone(task,now);
                if(task.Period==TaskPeriod.Counter)
                {
                    if(ImGui.SmallButton("-")){PersonalTasks.Mark(task,false,now);changed=true;}
                    ImGui.SameLine();
                    if(ImGui.SmallButton("+")){PersonalTasks.Mark(task,true,now);changed=true;}
                    ImGui.SameLine();
                    ImGui.TextColored(done ? Success:Accent,$"{task.Count}/{PersonalTasks.ClampTarget(task.Target)}");
                }
                else if(ImGui.Checkbox("##Done",ref done)){PersonalTasks.Mark(task,done,now);changed=true;}
                Tip("PersonalTasks.DoneTip");
                ImGui.SameLine();
                if(done)ImGui.TextDisabled(task.Name);else ImGui.TextUnformatted(task.Name);
                ImGui.SameLine();
                ImGui.TextDisabled(PeriodText(task)+(PersonalTasks.ComesBack(task,now) is { } back ? " · "+localization.Format("PersonalTasks.Back",back.ToLocalTime().ToString("g")):""));
                ImGui.SameLine();
                var onScreen=task.OnScreen;
                if(ImGui.Checkbox(localization.Get("PersonalTasks.OnScreen")+"##OnScreen",ref onScreen)){task.OnScreen=onScreen;changed=true;}
                Tip("PersonalTasks.OnScreenTip");
                ImGui.SameLine();
                ImGui.BeginDisabled(index==0);
                try{if(ImGui.ArrowButton("##Up",ImGuiDir.Up))move=(index,-1);}
                finally{ImGui.EndDisabled();}
                ImGui.SameLine();
                ImGui.BeginDisabled(index==tasks.Count-1);
                try{if(ImGui.ArrowButton("##Down",ImGuiDir.Down))move=(index,1);}
                finally{ImGui.EndDisabled();}
                ImGui.SameLine();
                if(task.Period==TaskPeriod.Counter && task.Count>0 && ImGui.SmallButton(localization.Get("PersonalTasks.ResetCounter"))){task.Count=0;changed=true;}
                if(task.Period==TaskPeriod.Counter && task.Count>0){Tip("PersonalTasks.ResetCounterTip");ImGui.SameLine();}
                if(ImGui.SmallButton(localization.Get("PersonalTasks.Remove")))removed=task;
                Tip("PersonalTasks.RemoveTip");
            }
            finally{ImGui.PopID();}
        }
        if(move.Item1>=0)
        {
            var list=new List<PersonalTask>(tasks);var (from,step)=move;
            (list[from],list[from+step])=(list[from+step],list[from]);
            configuration.PersonalTasks=list;changed=true;
        }
        if(removed is not null){configuration.PersonalTasks=tasks.Where(task=>task!=removed).ToList();changed=true;}
        if(changed)SaveReminders();
        DrawNewTask();
    }

    private void DrawNewTask()
    {
        var tasks=configuration.PersonalTasks ?? [];
        ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("##NewTaskName",localization.Get("PersonalTasks.NameHint"),ref newTaskName,PersonalTasks.MaximumName);
        Tip("PersonalTasks.NameTip");
        ImGui.SameLine();ImGui.SetNextItemWidth(170);
        if(ImGui.BeginCombo("##NewTaskPeriod",localization.Get("PersonalTasks.Period"+newTaskPeriod)))
        {
            try
            {
                foreach(var period in Enum.GetValues<TaskPeriod>())
                    if(ImGui.Selectable(localization.Get("PersonalTasks.Period"+period),period==newTaskPeriod))newTaskPeriod=period;
            }
            finally{ImGui.EndCombo();}
        }
        Tip("PersonalTasks.PeriodTip");
        if(newTaskPeriod==TaskPeriod.EveryHours)
        {
            ImGui.SameLine();ImGui.SetNextItemWidth(90);
            if(ImGui.InputInt(localization.Get("PersonalTasks.Hours")+"##NewTaskHours",ref newTaskHours))newTaskHours=PersonalTasks.ClampHours(newTaskHours);
            Tip("PersonalTasks.HoursTip");
        }
        else if(newTaskPeriod==TaskPeriod.Counter)
        {
            ImGui.SameLine();ImGui.SetNextItemWidth(90);
            if(ImGui.InputInt(localization.Get("PersonalTasks.Target")+"##NewTaskTarget",ref newTaskTarget))newTaskTarget=PersonalTasks.ClampTarget(newTaskTarget);
            Tip("PersonalTasks.TargetTip");
        }
        ImGui.SameLine();
        ImGui.BeginDisabled(tasks.Count>=PersonalTasks.Capacity);
        try
        {
            if(ImGui.Button(localization.Get("PersonalTasks.Add")))
            {
                if(PersonalTasks.CleanName(newTaskName) is not { } name)newTaskError=localization.Get("PersonalTasks.InvalidName");
                else
                {
                    configuration.PersonalTasks=[..tasks,new PersonalTask{Name=name,Period=newTaskPeriod,Hours=PersonalTasks.ClampHours(newTaskHours),Target=PersonalTasks.ClampTarget(newTaskTarget)}];
                    newTaskName="";newTaskError=null;SaveReminders();
                }
            }
        }
        finally{ImGui.EndDisabled();}
        Tip(tasks.Count>=PersonalTasks.Capacity ? "PersonalTasks.Full":"PersonalTasks.AddTip");
        if(newTaskError is { } error)ImGui.TextColored(UI.FfxivTheme.Error,error);
    }

    /// <summary>0.381 (g, X-01): « /tensho todo &lt;task&gt; »; the chat answer.</summary>
    internal string AddTaskFromCommand(string text)
    {
        var tasks=configuration.PersonalTasks ?? [];
        if(PersonalTasks.FromCommand(text,tasks.Count) is not { } task)
            return localization.Get(tasks.Count>=PersonalTasks.Capacity ? "PersonalTasks.Full":"PersonalTasks.InvalidName");
        configuration.PersonalTasks=[..tasks,task];
        SaveReminders();
        return saveError ?? localization.Format("PersonalTasks.Added",task.Name);
    }

    private string PeriodText(PersonalTask task)=>task.Period switch
    {
        TaskPeriod.EveryHours=>localization.Format("PersonalTasks.EveryHoursShort",PersonalTasks.ClampHours(task.Hours)),
        _=>localization.Get("PersonalTasks.Period"+task.Period),
    };

    // The on-screen « to do » panel (a PanelWindow since 0.514), only when it is on, a task is pending, and not in combat.
    private IReadOnlyList<PersonalTask> pendingOnScreen=[];
    private PanelDefinition TasksPanel=>new("TaskPanel","###TenshoTaskPanel",new Vector2(40,260),
        ImGuiWindowFlags.AlwaysAutoResize|ImGuiWindowFlags.NoFocusOnAppearing|ImGuiWindowFlags.NoNav,
        ()=>configuration.TaskPanel,TaskPanelVisible,DrawTaskPanelContents,CloseTaskPanel,()=>localization.Get("PersonalTasks.PanelTitle"),0.75f);

    private bool TaskPanelVisible()
    {
        if(windowDisposed || !configuration.TaskPanel || mapLive?.State is { InCombat:true })return false;
        pendingOnScreen=PersonalTasks.Pending(configuration.PersonalTasks,DateTimeOffset.UtcNow);
        return pendingOnScreen.Count>0;
    }

    private void CloseTaskPanel()
    {
        if(!configuration.TaskPanel)return;
        configuration.TaskPanel=false;SaveReminders();
    }

    private void DrawTaskPanelContents()
    {
        var now=DateTimeOffset.UtcNow;
        foreach(var task in pendingOnScreen)
        {
            ImGui.PushID(task.Id.ToString());
            try
            {
                if(task.Period==TaskPeriod.Counter)
                {
                    if(ImGui.SmallButton("+")){PersonalTasks.Mark(task,true,now);SaveReminders();}
                    Tip("PersonalTasks.DoneTip");
                    ImGui.SameLine();ImGui.TextUnformatted($"{task.Name} ({task.Count}/{PersonalTasks.ClampTarget(task.Target)})");
                }
                else
                {
                    var done=false;
                    if(ImGui.Checkbox(task.Name+"##PanelDone",ref done)){PersonalTasks.Mark(task,true,now);SaveReminders();}
                    Tip("PersonalTasks.DoneTip");
                }
            }
            finally{ImGui.PopID();}
        }
    }
}
