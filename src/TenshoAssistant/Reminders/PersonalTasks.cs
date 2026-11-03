namespace TenshoAssistant.Reminders;

/// <summary>g, M-05 (0.375): how a personal task comes back (user's choice: day, week, every n hours, counter).</summary>
// 0.381 (g, X-01): Once = a free task (done stays done until removed), added last so saved numbers keep their meaning.
public enum TaskPeriod { Daily, Weekly, EveryHours, Counter, Once }

/// <summary>
/// One task the player typed on the Reminders page. Daily and weekly tasks follow the game's resets (ResetSchedule), « every n hours »
/// comes back n hours after it was done, a counter is done once it reaches its target (reset by hand).
/// </summary>
public sealed class PersonalTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public TaskPeriod Period { get; set; }
    public int Hours { get; set; } = 20;
    public int Target { get; set; } = 1;
    public int Count { get; set; }
    public DateTimeOffset? DoneAt { get; set; }
    public bool OnScreen { get; set; } = true;
}

public static class PersonalTasks
{
    public const int Capacity=50,MaximumName=60,MaximumHours=168,MaximumTarget=999;

    public static int ClampHours(int hours)=>Math.Clamp(hours,1,MaximumHours);
    public static int ClampTarget(int target)=>Math.Clamp(target,1,MaximumTarget);

    /// <summary>A trimmed name of 1 to 60 characters, or null when the name cannot be used.</summary>
    public static string? CleanName(string? name)
    {
        var clean=(name ?? "").Trim();
        return clean.Length is 0 or > MaximumName || clean.Any(char.IsControl) ? null:clean;
    }

    /// <summary>Whether the task is done now (a daily or weekly task done before the last reset is to do again).</summary>
    public static bool IsDone(PersonalTask task,DateTimeOffset now)=>task.Period switch
    {
        TaskPeriod.Daily=>task.DoneAt>=ResetSchedule.PreviousDaily(now,ResetSchedule.DailyHour),
        TaskPeriod.Weekly=>task.DoneAt>=ResetSchedule.PreviousWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour),
        TaskPeriod.EveryHours=>task.DoneAt is { } at && at.AddHours(ClampHours(task.Hours))>now,
        TaskPeriod.Counter=>task.Count>=ClampTarget(task.Target),
        TaskPeriod.Once=>task.DoneAt is not null,
        _=>false,
    };

    /// <summary>When a done task comes back (null for a counter, or when it is not done).</summary>
    public static DateTimeOffset? ComesBack(PersonalTask task,DateTimeOffset now)
    {
        if(!IsDone(task,now))return null;
        return task.Period switch
        {
            TaskPeriod.Daily=>ResetSchedule.NextDaily(now,ResetSchedule.DailyHour),
            TaskPeriod.Weekly=>ResetSchedule.NextWeekly(now,ResetSchedule.WeeklyDay,ResetSchedule.WeeklyHour),
            TaskPeriod.EveryHours=>task.DoneAt!.Value.AddHours(ClampHours(task.Hours)),
            _=>null,
        };
    }

    /// <summary>Marks the task done (or not); a counter moves by one step, between 0 and its target.</summary>
    public static void Mark(PersonalTask task,bool done,DateTimeOffset now)
    {
        if(task.Period==TaskPeriod.Counter){task.Count=Math.Clamp(task.Count+(done ? 1:-1),0,ClampTarget(task.Target));task.DoneAt=now;return;}
        task.DoneAt=done ? now:null;
    }

    /// <summary>
    /// 0.381 (g, X-01): « /tensho todo &lt;task&gt; » adds a free task (Once) at the end of the list; the cleaned name, or null when the name
    /// cannot be used or the list is full.
    /// </summary>
    public static PersonalTask? FromCommand(string text,int count)
        =>count>=Capacity || CleanName(text) is not { } name ? null:new PersonalTask{Name=name,Period=TaskPeriod.Once};

    /// <summary>The tasks the on-screen panel lists: those marked for it and not done now, in the list's order.</summary>
    public static IReadOnlyList<PersonalTask> Pending(IEnumerable<PersonalTask>? tasks,DateTimeOffset now)
        =>(tasks ?? []).Where(task=>task.OnScreen && !IsDone(task,now)).ToArray();
}
