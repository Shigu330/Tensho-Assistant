namespace TenshoAssistant.Chat;

/// <summary>X-690 (0.1666): a job's role, from the ClassJob sheet's Role column (1 tank, 2 melee, 3 ranged and casters, 4 healer, 0 crafters,
/// gatherers and adventurer; XbmProbe of 21/11, docs/evidence/0.1666-chat-job-roles.md).</summary>
public enum JobRole : byte { None, Tank, Healer, Dps }

/// <summary>
/// X-690 (0.1666, the user's request of 20/11 around 22:40: « as in the game's chat: a player's name in the colour of their role, and their
/// job's icon before it, mostly in a party, a full party and an alliance »): the jobs the game shows now (your party, your alliance, the
/// characters nearby), refreshed once a second on the framework thread, in memory only (R04: nothing about other players on disk); the
/// sender's job is taken when the message arrives (ChatSources.Expect) and kept with its sender, in memory too. A job never known is never
/// guessed (R06): no icon, the name's usual colour. The role colours are the game's own (its chat's role colour settings, UiConfig
/// LogColorRoleTank, LogColorRoleHealer, LogColorRoleDPS, 0xAARRGGBB), else the game's defaults. Pure (tests).
/// </summary>
public sealed class ChatJobs
{
    public const int MaximumKept=500;
    /// <summary>The game's job icons: 62100 + ClassJob row (already used by the map and the gathering panels).</summary>
    public const uint IconBase=62100;
    /// <summary>The game's role colours of its chat, as its character settings keep them (0xAARRGGBB, read in COMMON.DAT on 21/11).</summary>
    public const uint GameTank=0xFF0080FF,GameHealer=0xFF80F860,GameDps=0xFFFF807F;
    private readonly object gate=new();
    private Dictionary<(string Name,ushort World),uint> seen=[];
    private Dictionary<uint,JobRole> roles=[];
    private uint tank=GameTank,healer=GameHealer,dps=GameDps;

    public static JobRole RoleOf(byte sheetRole)=>sheetRole switch{1=>JobRole.Tank,4=>JobRole.Healer,2 or 3=>JobRole.Dps,_=>JobRole.None};
    public static uint Icon(uint job)=>job==0 ? 0:IconBase+job;
    /// <summary>A colour 0xAARRGGBB as red, green and blue from 0 to 1.</summary>
    public static (float R,float G,float B) Rgb(uint argb)=>(((argb>>16)&0xFF)/255f,((argb>>8)&0xFF)/255f,(argb&0xFF)/255f);

    /// <summary>Each job's role, from the ClassJob sheet (row, Role column), read once.</summary>
    public void SetRoles(IEnumerable<(uint Job,byte Role)> rows)
    {
        var made=rows.ToDictionary(row=>row.Job,row=>RoleOf(row.Role));
        lock(gate)roles=made;
    }
    public JobRole Role(uint job){lock(gate)return roles.GetValueOrDefault(job);}

    /// <summary>The game's role colours (null, or a colour without its alpha: the game's default).</summary>
    public void SetColours(uint? tankColour,uint? healerColour,uint? dpsColour)
    {
        lock(gate)
        {
            tank=tankColour is { } t && t>>24!=0 ? t:GameTank;
            healer=healerColour is { } h && h>>24!=0 ? h:GameHealer;
            dps=dpsColour is { } d && d>>24!=0 ? d:GameDps;
        }
    }
    public uint ColourOf(JobRole role){lock(gate)return role switch{JobRole.Tank=>tank,JobRole.Healer=>healer,JobRole.Dps=>dps,_=>0};}

    /// <summary>The players the game shows now (party first, then alliance, then nearby: the first job given for a player is kept); a player
    /// without a name, a world or a job is left out; at most <see cref="MaximumKept"/>.</summary>
    public void Replace(IEnumerable<(string Name,ushort World,uint Job)> players)
    {
        var made=new Dictionary<(string,ushort),uint>();
        foreach(var (name,world,job) in players)
        {
            if(made.Count>=MaximumKept)break;
            if(name.Length==0 || world==0 || job==0)continue;
            made.TryAdd((name,world),job);
        }
        lock(gate)seen=made;
    }

    /// <summary>The job the game shows now for that player (name and home world); 0: not known.</summary>
    public uint Find(string name,ushort world){lock(gate)return seen.GetValueOrDefault((name,world));}
    public int Count{get{lock(gate)return seen.Count;}}
    public void Clear(){lock(gate)seen=[];}

    /// <summary>The job icon before a sender's name: the setting on, a spoken channel, the job known; 0 otherwise.</summary>
    public static uint IconFor(ChatSettings settings,ChatChannel channel,uint job)=>settings.JobIcons && ChatChannels.Spoken(channel) ? Icon(job):0;

    /// <summary>The colour of a sender's name by role: the setting on, a spoken channel, the job's role known (not a crafter or gatherer).</summary>
    public uint? ColourFor(ChatSettings settings,ChatChannel channel,uint job)
        =>settings.RoleColors && ChatChannels.Spoken(channel) && job!=0 && Role(job) is var role and not JobRole.None ? ColourOf(role):null;
}
