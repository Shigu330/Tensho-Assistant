namespace TenshoAssistant.Gear;

// Gear planner data and pure rules (R-11 since 0.310; page: MainWindow.Gear.cs): equipment copied from the game's Item sheet,
// worn pieces, materia kinds and caps (0.312–0.313), candidate filters, planned stats and the « TENSHO-GEAR1: » share code.
// Game data only (docs/evidence/0.310-gear-sheets.md, docs/evidence/0.312-materia.md); rules tested in tests/TenshoAssistant.Tests.

/// <summary>Character window slots, in the game's EquippedItems order (Waist and the soul crystal left out).</summary>
public enum GearSlot { MainHand, OffHand, Head, Body, Hands, Legs, Feet, Ears, Neck, Wrists, RingRight, RingLeft }

/// <summary>
/// R-11 (0.310): one piece of equipment copied from the Item sheet: level, item level, slot, jobs that may wear it (bit = ClassJob
/// row), base parameters (BaseParam id → value), the extra values of the HQ version, physical / magic defense and weapon damage.
/// </summary>
public sealed record GearItem(uint Id,string Name,uint Icon,int Level,int ItemLevel,GearSlot Slot,bool BlocksOffHand,ulong Jobs,
    IReadOnlyDictionary<int,int> Params,IReadOnlyDictionary<int,int> HqParams,int MateriaSlots,bool CanBeHq,bool Unique,
    bool AdvancedMelding=false,IReadOnlyDictionary<int,int>? Caps=null)
{
    public bool WearableBy(uint job)=>job<64 && (Jobs>>(int)job & 1)!=0;

    /// <summary>Materia sockets: the guaranteed ones, up to 5 when advanced melding (overmelding) is permitted (0.312).</summary>
    public int SocketCount=>AdvancedMelding ? Math.Max(MateriaSlots,5):MateriaSlots;

    /// <summary>Parameters of the NQ or HQ version.</summary>
    public IReadOnlyDictionary<int,int> ParamsFor(bool hq)
    {
        if(!hq || HqParams.Count==0)return Params;
        var total=new Dictionary<int,int>(Params);
        foreach(var (param,value) in HqParams)total[param]=total.GetValueOrDefault(param)+value;
        return total;
    }
}

/// <summary>An equipped piece read from the game: item, HQ, and its melded materia as (BaseParam, value) and as (row, grade).</summary>
// 0.376 (g, M-06): Condition = the game's own percentage (GetConditionPercentage), Spiritbond = the raw value (10 000 = 100 %); null = unknown.
public sealed record EquippedPiece(GearSlot Slot,uint ItemId,bool Hq,IReadOnlyList<(int Param,int Value)> Materia,IReadOnlyList<MeldedMateria>? Melds=null,
    int? Condition=null,int? Spiritbond=null);

/// <summary>A materia in a socket: Materia sheet row and grade (0 = I … 11 = XII); Code packs both for the configuration.</summary>
public readonly record struct MeldedMateria(uint Row,int Grade)
{
    public uint Code=>Row<<8|(uint)(Grade&0xFF);
    public static MeldedMateria From(uint code)=>new(code>>8,(int)(code&0xFF));
}

/// <summary>0.338: overmeld success rates (percent) of one materia grade, per advanced socket (first to fourth), NQ and HQ piece.</summary>
public sealed record OvermeldRate(IReadOnlyList<float> Nq,IReadOnlyList<float> Hq);

/// <summary>A kind of materia (Materia row): the stat it gives and, per grade, its item, name and value (0.312).</summary>
public sealed record MateriaKind(uint Row,int Param,IReadOnlyList<(int Grade,uint ItemId,string Name,int Value)> Grades)
{
    public int ValueOf(int grade)=>Grades.FirstOrDefault(entry=>entry.Grade==grade).Value;
    public string NameOf(int grade)=>Grades.FirstOrDefault(entry=>entry.Grade==grade).Name ?? "";
}

/// <summary>
/// Filters of the candidate list (settings). AllJobs (0.311): since patch 7.4 any armor or accessory can be a glamour whatever the
/// job and level; weapons stay job-locked.
/// </summary>
public sealed record GearFilter(int LevelsBelow,bool ExactOutsideCaps,int MinItemLevel=0,string Search="",bool AllJobs=false);

public sealed class GearCatalog(IReadOnlyList<GearItem> items,IReadOnlyList<MateriaKind>? materia=null,
    IReadOnlyDictionary<int,OvermeldRate>? combatRates=null,IReadOnlyDictionary<int,OvermeldRate>? craftRates=null,IReadOnlyList<BardingSet>? bardings=null)
{
    /// <summary>0.340 (R-11 step 4): chocobo bardings (BuddyEquip) with the item that teaches each.</summary>
    public IReadOnlyList<BardingSet> Bardings { get; }=bardings ?? [];
    private readonly Dictionary<uint,GearItem> byId=items.ToDictionary(item=>item.Id);
    private readonly Dictionary<uint,MateriaKind> materiaByRow=(materia ?? []).ToDictionary(kind=>kind.Row);
    public IReadOnlyList<GearItem> Items { get; }=items;
    public IReadOnlyList<MateriaKind> Materia { get; }=materia ?? [];
    public GearItem? Find(uint id)=>byId.GetValueOrDefault(id);
    public MateriaKind? FindMateria(uint row)=>materiaByRow.GetValueOrDefault(row);

    /// <summary>
    /// 0.338 (DEC-023, point 8): success rate (percent) of melding this grade in the n-th advanced socket (0 = first) of an NQ or HQ piece,
    /// from MateriaJoinRate (combat stats) or MateriaJoinRateGatherCraft (crafting and gathering stats), row = grade. The sheets stop at
    /// grade X: for XI and XII the rates of FFXIV Teamcraft are used and said so (Community = true). Null = known nowhere (no sheet
    /// loaded, or past the 4th advanced socket): unknown, never 0 % or 100 %.
    /// </summary>
    public (float Percent,bool Community)? OvermeldRate(MateriaKind kind,int grade,int advancedIndex,bool hq)
    {
        var sheet=GearRules.CraftParam(kind.Param) ? craftRates:combatRates;
        if(sheet is null)return null;
        var community=!sheet.ContainsKey(grade);
        var rate=community ? GearRules.TeamcraftOvermeldRates.GetValueOrDefault(grade):sheet[grade];
        var values=rate is null ? null:hq ? rate.Hq:rate.Nq;
        return values is not null && advancedIndex>=0 && advancedIndex<values.Count ? (values[advancedIndex],community):null;
    }

    /// <summary>(BaseParam, value) of melded materia (unknown rows or grades give nothing).</summary>
    public IReadOnlyList<(int Param,int Value)> MateriaValues(IEnumerable<MeldedMateria> melds)
        =>melds.Select(meld=>FindMateria(meld.Row) is { } kind ? (kind.Param,kind.ValueOf(meld.Grade)):(0,0)).Where(entry=>entry.Item2>0).ToArray();
}

/// <summary>Pure rules of the gear planner (tested).</summary>
public static class GearRules
{
    /// <summary>Level caps of the expansions: at these levels « near my level » keeps its full window.</summary>
    public static readonly IReadOnlySet<int> Caps=new HashSet<int>{50,60,70,80,90,100};

    /// <summary>Ring items fit both ring slots.</summary>
    public static bool Fits(GearItem item,GearSlot slot)
        =>item.Slot==slot || item.Slot is GearSlot.RingRight or GearSlot.RingLeft && slot is GearSlot.RingRight or GearSlot.RingLeft;

    /// <summary>
    /// Levels shown for a character level: level − N to level; with « exact outside caps », only the character's level when it is not
    /// an expansion cap (user request: at 50 / 60 / … / 100 the whole window, otherwise the gear of exactly that level).
    /// </summary>
    public static (int Min,int Max) LevelWindow(int level,GearFilter filter)
        =>filter.ExactOutsideCaps && !Caps.Contains(level) ? (level,level):(Math.Max(1,level-Math.Max(0,filter.LevelsBelow)),level);

    /// <summary>« All jobs (glamour) » (0.311): armor and accessories of every job and every level; weapons keep the job and the level window.</summary>
    public static bool AnyJobAndLevel(GearSlot slot,GearFilter filter)=>filter.AllJobs && slot is not (GearSlot.MainHand or GearSlot.OffHand);

    /// <summary>The other ring slot; null for any other slot (0.331).</summary>
    public static GearSlot? OtherRing(GearSlot slot)=>slot switch{GearSlot.RingRight=>GearSlot.RingLeft,GearSlot.RingLeft=>GearSlot.RingRight,_=>null};

    /// <summary>
    /// 0.331: the game refuses two copies of a Unique item, so a plan holding the same Unique ring in both ring slots keeps the right one
    /// and drops the left one. True when a ring was dropped.
    /// </summary>
    public static bool DropUniqueTwin(GearCatalog catalog,IDictionary<GearSlot,uint> plan)
        =>plan.TryGetValue(GearSlot.RingRight,out var right) && plan.TryGetValue(GearSlot.RingLeft,out var left) && right==left
            && catalog.Find(right)?.Unique==true && plan.Remove(GearSlot.RingLeft);

    public static IEnumerable<GearItem> Candidates(GearCatalog catalog,GearSlot slot,uint job,int level,GearFilter filter)
    {
        var (min,max)=LevelWindow(level,filter);
        var anyone=AnyJobAndLevel(slot,filter);
        return catalog.Items.Where(item=>Fits(item,slot) && (anyone || item.WearableBy(job) && item.Level>=min && item.Level<=max) && item.ItemLevel>=filter.MinItemLevel
                && (filter.Search.Length==0 || item.Name.Contains(filter.Search,StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(item=>item.ItemLevel).ThenByDescending(item=>item.Level).ThenBy(item=>item.Name,StringComparer.CurrentCulture);
    }

    /// <summary>A piece the character cannot wear now (other job or higher level): planned for its look only, not counted in the stats.</summary>
    public static bool GlamourOnly(GearItem item,uint job,int level)=>!item.WearableBy(job) || item.Level>level;

    /// <summary>
    /// Parameters of one piece with its materia (0.312): materia raise a stat only up to the piece's cap for it (ItemLevel value ×
    /// BaseParam slot percentage / 1000, docs/evidence/0.312-materia.md); a stat already at its cap gains nothing.
    /// </summary>
    public static Dictionary<int,int> PieceParams(GearItem item,bool hq,IReadOnlyList<(int Param,int Value)> materia)
    {
        var total=new Dictionary<int,int>(item.ParamsFor(hq));
        foreach(var group in materia.GroupBy(entry=>entry.Param))
        {
            var current=total.GetValueOrDefault(group.Key);var added=group.Sum(entry=>entry.Value);
            if(item.Caps?.TryGetValue(group.Key,out var cap)==true)added=Math.Max(0,Math.Min(added,cap-current));
            total[group.Key]=current+added;
        }
        return total;
    }

    /// <summary>What a stat loses to its cap on this piece (0 when everything counts).</summary>
    public static int Wasted(GearItem item,bool hq,IReadOnlyList<(int Param,int Value)> materia,int param)
        =>materia.Where(entry=>entry.Param==param).Sum(entry=>entry.Value)-(PieceParams(item,hq,materia).GetValueOrDefault(param)-item.ParamsFor(hq).GetValueOrDefault(param));

    /// <summary>
    /// Grades VI, VIII, X and XII go only in the first advanced (overmeld) socket (0.338: the game data gives them a rate there and 0 %
    /// after it, MateriaJoinRate rows 5, 7, 9; the wiki says the same for XII; docs/evidence/0.338-overmeld-rates.md). Was « never » before 0.338.
    /// </summary>
    public static bool OvermeldAllowed(int grade,int advancedIndex)=>advancedIndex==0 || grade is not (5 or 7 or 9 or 11);

    /// <summary>
    /// 0.338 (user screenshot: XI and XII showed « rate unknown »): the game sheets have no row for grades XI and XII. FFXIV Teamcraft (MIT,
    /// apps/client/src/app/lazy-data/static-data.ts, dohdolMeldingRates, read 2026-11-02) gives XI the odd grades' rates and XII the
    /// even grades' rates; its grades I to X match the sheets exactly (docs/evidence/0.338-overmeld-rates.md). Not read in the game.
    /// </summary>
    public static readonly IReadOnlyDictionary<int,OvermeldRate> TeamcraftOvermeldRates=new Dictionary<int,OvermeldRate>
    {
        [10]=new([12,6,3,2],[17,10,7,5]),
        [11]=new([12,0,0,0],[17,0,0,0]),
    };

    /// <summary>Crafting and gathering stats (CP, GP, Craftsmanship, Control, Gathering, Perception): their materia use the crafting rates.</summary>
    public static bool CraftParam(int param)=>param is 10 or 11 or >= 70;

    /// <summary>Sum of the parameters of a set (HQ bonus and melded materia included, materia capped per piece).</summary>
    public static Dictionary<int,int> Totals(IEnumerable<(GearItem Item,bool Hq,IReadOnlyList<(int Param,int Value)> Materia)> pieces)
    {
        var total=new Dictionary<int,int>();
        foreach(var (item,hq,materia) in pieces)
            foreach(var (param,value) in PieceParams(item,hq,materia))total[param]=total.GetValueOrDefault(param)+value;
        return total;
    }

    /// <summary>
    /// Planned attributes: the real values read from the game, plus what the planned set adds, minus what the worn set gives.
    /// Keys are the game's PlayerAttribute names (as the character stats reader publishes them).
    /// </summary>
    public static Dictionary<string,int> Planned(IReadOnlyDictionary<string,int> real,IReadOnlyDictionary<int,int> worn,IReadOnlyDictionary<int,int> planned)
    {
        var result=new Dictionary<string,int>(real);
        foreach(var param in worn.Keys.Union(planned.Keys))
            if(AttributeOf(param) is { } key && result.TryGetValue(key,out var value))
                result[key]=value+planned.GetValueOrDefault(param)-worn.GetValueOrDefault(param);
        return result;
    }

    /// <summary>BaseParam row → PlayerAttribute name (docs/evidence/0.310-gear-sheets.md).</summary>
    public static string? AttributeOf(int param)=>param switch
    {
        1=>"Strength",2=>"Dexterity",3=>"Vitality",4=>"Intelligence",5=>"Mind",6=>"Piety",7=>"HealthPoints",8=>"MagicPoints",
        10=>"GatheringPoints",11=>"CraftingPoints",19=>"Tenacity",21=>"Defense",22=>"DirectHitRate",24=>"MagicDefense",27=>"CriticalHit",
        44=>"Determination",45=>"SkillSpeed",46=>"SpellSpeed",70=>"Craftsmanship",71=>"Control",72=>"Gathering",73=>"Perception",
        _=>null,
    };

    /// <summary>Average item level of a set, as the game counts it (a two-handed weapon counts twice; 12 slots).</summary>
    public static double AverageItemLevel(IReadOnlyDictionary<GearSlot,GearItem> set)
    {
        var sum=0;
        foreach(var slot in Enum.GetValues<GearSlot>())
        {
            if(set.TryGetValue(slot,out var item))sum+=item.ItemLevel;
            else if(slot==GearSlot.OffHand && set.TryGetValue(GearSlot.MainHand,out var weapon) && weapon.BlocksOffHand)sum+=weapon.ItemLevel;
        }
        return sum/12.0;
    }
}

/// <summary>
/// 0.310 (user request: share builds): a build as a short text code, « TENSHO-GEAR1: » + Base64 of « job;slot=item,… », to paste
/// in a chat; Import reads it back (unknown slots and malformed parts are ignored, null when nothing usable).
/// </summary>
public static class GearShare
{
    public const string Prefix="TENSHO-GEAR1:";

    public static string Export(uint job,IReadOnlyDictionary<GearSlot,uint> plan,IReadOnlyDictionary<GearSlot,IReadOnlyList<MeldedMateria>>? materia=null)
        =>Prefix+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{job};"+string.Join(",",plan.OrderBy(pair=>pair.Key).Select(pair=>
            $"{(int)pair.Key}={pair.Value}"+(materia?.TryGetValue(pair.Key,out var melds)==true && melds.Count>0 ? ":"+string.Join(".",melds.Select(meld=>meld.Code)):"")))));

    /// <summary>Plan and materia of a shared code (0.312: « slot=item:materia.materia »; older codes have no materia).</summary>
    public static (uint Job,Dictionary<GearSlot,uint> Plan,Dictionary<GearSlot,List<MeldedMateria>> Materia)? Import(string? text)
    {
        var start=text?.IndexOf(Prefix,StringComparison.Ordinal) ?? -1;
        if(start<0)return null;
        var code=new string(text![(start+Prefix.Length)..].TakeWhile(character=>char.IsLetterOrDigit(character) || character is '+' or '/' or '=').ToArray());
        string decoded;
        try{decoded=System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(code));}
        catch(FormatException){return null;}
        var parts=decoded.Split(';',2);
        if(parts.Length!=2 || !uint.TryParse(parts[0],out var job))return null;
        var plan=new Dictionary<GearSlot,uint>();var materia=new Dictionary<GearSlot,List<MeldedMateria>>();
        foreach(var entry in parts[1].Split(',',StringSplitOptions.RemoveEmptyEntries))
        {
            var pair=entry.Split('=');
            if(pair.Length!=2 || !int.TryParse(pair[0],out var slot) || !Enum.IsDefined((GearSlot)slot))continue;
            var value=pair[1].Split(':',2);
            if(!uint.TryParse(value[0],out var item) || item==0)continue;
            plan[(GearSlot)slot]=item;
            if(value.Length==2)
                materia[(GearSlot)slot]=value[1].Split('.',StringSplitOptions.RemoveEmptyEntries).Select(code=>uint.TryParse(code,out var number) ? MeldedMateria.From(number):default)
                    .Where(meld=>meld.Row!=0).Take(5).ToList();
        }
        return plan.Count==0 ? null:(job,plan,materia);
    }
}
