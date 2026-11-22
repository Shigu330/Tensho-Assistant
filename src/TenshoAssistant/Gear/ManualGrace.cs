namespace TenshoAssistant.Gear;

/// <summary>
/// X-221 (0.1760, the user's request of 12/11/2026, K-37): what Tensho counted of one manual's Grace (status 45 « Crafter's Grace » or 46
/// « Gatherer's Grace »), kept per character in the settings (<see cref="Configuration.GraceCounts"/>) so that a reload of the plugin
/// goes on counting. <see cref="Used"/>: the manual's bonus points counted since its start; <see cref="StartSeen"/>: Tensho saw the
/// status appear (else the points left are unknown); <see cref="Remaining"/>: the status's seconds left when last read, to see a new manual
/// (its time starts again); <see cref="Jobs"/>: the levels and experience of the jobs it boosts when last read, to see experience
/// gained while Tensho was not watching.
/// </summary>
public sealed class GraceCount
{
    public uint Status { get; set; }
    public ushort Param { get; set; }
    public double Used { get; set; }
    public bool StartSeen { get; set; }
    public float Remaining { get; set; }
    public string Jobs { get; set; } = "";

    /// <summary>X-770 (0.1769): the readings in a row, while you were loaded, that did not see the status (not saved: it starts again at
    /// a reload; internal, so that no serializer writes it). At <see cref="ManualGrace.GoneReadings"/>, the game took the status away and
    /// its count is dropped.</summary>
    internal int Missed { get; set; }
}

/// <summary>
/// X-221 (0.1760): the bonus points left of a manual (docs/evidence/0.1231-grace-cap.md, completed in 0.1760). The game says in each
/// manual's description « Bonus maximum : 1 750 000 points d'expérience » (Item 12667, « up to a maximum of 1,750,000 points »): the cap
/// counts the bonus points. The status's Param (70 read in game on 15/11 with the Commercial Engineering Manual, « +150% up to level 69,
/// then +75% ») is the level from which the bonus is halved; the four manuals of 150% halve it from 40, 50, 70 and 80, with caps of
/// 250,000, 500,000, 1,750,000 and 2,000,000 points (their descriptions). Each experience line of the job worn (LogMessage 589: lnum2 the
/// points gained, lnum3 the bonus percent of every bonus together) gives the manual's part of it: gained × rate ÷ (100 + percent), the rate
/// being 150 below the Param's level and 75 from it, never more than the percent shown. Other bonuses are taken as added to it (the
/// game shows one percent): when there are some, the count is approximate, so the page says « about ». Pure (tests).
/// </summary>
public static class ManualGrace
{
    public const uint CrafterGrace=45,GathererGrace=46;

    /// <summary>The log message of the experience gained by a job (« You gain N (+P%) Carpenter experience points. »).</summary>
    public const uint ExperienceMessage=589;

    /// <summary>A new manual: the status's seconds left grew by more than this since the last reading.</summary>
    public const float RefreshSeconds=60;

    /// <summary>X-769 (0.1765): a manual's Grace lasts 18 hours from its use (each manual's description: « Duration: 18 hours », « Durée
    /// maximum d'effet : 18 heures », Item 4632 to 26554 column 8; 64,774 s read in game 26 s after a use on 15/11).</summary>
    public const float FullSeconds=64_800;

    /// <summary>X-769 (0.1765): a new count is from the manual's start only when its seconds left are within this of the full 18 hours (one
    /// reading each second, a loading screen in between, during which no experience is gained); later, its points left are unknown.</summary>
    public const float StartSeconds=120;

    /// <summary>X-770 (0.1769): a Grace not seen at this many readings in a row while you are loaded (about half a minute, one reading a
    /// second; the status missed at the first readings after a load or a loading screen comes back well before) was taken away by the game
    /// (its cap reached, removed by hand, or ended): its count is dropped, so that a manual used later is never taken for it.</summary>
    public const int GoneReadings=30;

    /// <summary>The Grace that boosts this job: crafters (ClassJob 8 to 15) status 45, gatherers (16 to 18) status 46; none else.</summary>
    public static uint StatusFor(uint job)=>job is >= 8 and <= 15 ? CrafterGrace:job is >= 16 and <= 18 ? GathererGrace:0;

    /// <summary>The jobs a Grace boosts.</summary>
    public static IEnumerable<uint> JobsOf(uint status)=>status==CrafterGrace ? Enumerable.Range(8,8).Select(job=>(uint)job)
        :status==GathererGrace ? Enumerable.Range(16,3).Select(job=>(uint)job):[];

    /// <summary>The cap of the manual whose status holds this Param (the level from which its bonus is halved); null: not known.</summary>
    public static int? Cap(ushort param)=>param switch{40=>250_000,50=>500_000,70=>1_750_000,80=>2_000_000,_=>null};

    /// <summary>The manual's bonus percent at this level: 150 below the Param's level, 75 from it; null when the manual is not known.</summary>
    public static int? Rate(ushort param,int level)=>Cap(param) is null || level<=0 ? null:level<param ? 150:75;

    /// <summary>The jobs' levels and experience, as one text compared at the next load (« job:level:experience », by job).</summary>
    public static string Snapshot(IEnumerable<(uint Job,int Level,int Experience)> jobs)=>
        string.Join(",",jobs.OrderBy(job=>job.Job).Select(job=>$"{job.Job}:{job.Level}:{job.Experience}"));

    /// <summary>
    /// The count after a reading of the status held (one not held has none). A status that appears while Tensho watches starts a count; one
    /// already there at the first reading after a load or a login (<paramref name="firstLook"/>) goes on with the saved count only when it
    /// is the same manual (same Param, its time not started again) and no experience was gained in between (same <paramref name="jobs"/>),
    /// else its points left are unknown. A new manual (another Param, or its time started again) starts a count while Tensho watches.
    /// </summary>
    /// <param name="changed">Whether something worth saving changed (the seconds left alone are not).</param>
    public static GraceCount Observe(GraceCount? saved,uint status,ushort param,float remaining,string jobs,bool firstLook,out bool changed)=>
        Observe(saved,status,param,remaining,jobs,firstLook,firstLook,out changed);

    /// <summary>
    /// X-769 (0.1765): as above, <paramref name="resumed"/> telling a status seen again after a reading that did not see it (or after a
    /// load, a login or a loading screen): the saved count goes on only when the jobs' experience did not move in between (lines read
    /// while the status was not seen are not counted), else its points left are unknown. A new count is from the manual's start only when
    /// Tensho watches and the status's seconds left are within <see cref="StartSeconds"/> of the full 18 hours: a status first seen later
    /// (its count lost, or a manual used while Tensho was not watching) is unknown, never the whole cap.
    /// </summary>
    public static GraceCount Observe(GraceCount? saved,uint status,ushort param,float remaining,string jobs,bool firstLook,bool resumed,out bool changed)
    {
        changed=false;
        var same=saved is not null && saved.Status==status && saved.Param==param && remaining<=saved.Remaining+RefreshSeconds;
        if(!same || resumed && saved!.Jobs!=jobs && saved.StartSeen)
        {
            changed=true;
            return new GraceCount{Status=status,Param=param,StartSeen=!same && !firstLook && remaining>=FullSeconds-StartSeconds,Remaining=remaining,Jobs=jobs};
        }
        if(saved!.Jobs!=jobs)changed=true;
        saved.Remaining=remaining;
        saved.Jobs=jobs;
        return saved;
    }

    /// <summary>
    /// X-769 (0.1765): one reading of your statuses (once a second). Returns the counts to keep in the settings: those of the Graces held
    /// (<paramref name="current"/>, in the order 45, 46), then the saved ones not held at this reading, kept as they are: one reading that
    /// does not see a status no longer wipes its count. <paramref name="seenBefore"/>: the Graces held at the previous reading (updated here;
    /// emptied by the caller after a load, a login or a loading screen), so that one seen again is <c>resumed</c>. X-770 (0.1769): a saved
    /// count whose status is not seen at <see cref="GoneReadings"/> readings in a row is dropped (the game took the status away); until
    /// then it is kept, and the status seen again starts the readings in a row again.
    /// </summary>
    public static List<GraceCount> Read(IReadOnlyList<GraceCount> saved,IReadOnlyDictionary<uint,(ushort Param,float Remaining)> held,Func<uint,string> jobs,
        bool firstLook,HashSet<uint> seenBefore,out List<GraceCount> current,out bool changed)
    {
        changed=false;
        current=[];
        var kept=new List<GraceCount>();
        foreach(var grace in new[]{CrafterGrace,GathererGrace})
        {
            var last=saved.FirstOrDefault(count=>count.Status==grace);
            if(!held.TryGetValue(grace,out var status))
            {
                if(last is null)continue;
                if(++last.Missed>=GoneReadings){changed=true;continue;}
                kept.Add(last);
                continue;
            }
            var count=Observe(last,grace,status.Param,status.Remaining,jobs(grace),firstLook,firstLook || !seenBefore.Contains(grace),out var moved);
            count.Missed=0;
            if(moved || !ReferenceEquals(count,last))changed=true;
            current.Add(count);
        }
        seenBefore.Clear();
        foreach(var count in current)seenBefore.Add(count.Status);
        return [..current,..kept];
    }

    /// <summary>The manual's part of one experience line: gained × rate ÷ (100 + percent); none without a percent.</summary>
    public static double Share(long gained,int percent,int rate)=>gained<=0 || percent<=0 || rate<=0 ? 0:gained*(double)Math.Min(rate,percent)/(100+percent);

    /// <summary>Counts one experience line of a job the Grace boosts, at the job's level when it was earned. Not when the start was not seen.</summary>
    /// <returns>Whether the count changed.</returns>
    public static bool Gain(GraceCount count,long gained,int percent,int level)
    {
        if(!count.StartSeen || Rate(count.Param,level) is not { } rate)return false;
        var share=Share(gained,percent,rate);
        if(share<=0)return false;
        count.Used+=share;
        return true;
    }

    /// <summary>The bonus points left (never below 0); null when unknown (start not seen, or a manual not known).</summary>
    public static long? Left(GraceCount count)=>count.StartSeen && Cap(count.Param) is { } cap ? (long)Math.Max(0,Math.Floor(cap-count.Used)):null;
}

/// <summary>X-221 (0.1760): one Grace shown under the jobs' experience: the status's name (the game's), its points left (null: unknown), the
/// manual's cap and the level from which its bonus is halved.</summary>
public sealed record GraceShown(uint Status,string Name,long? Left,int? Cap,ushort Param);
