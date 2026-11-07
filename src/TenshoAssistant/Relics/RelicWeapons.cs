namespace TenshoAssistant.Relics;

/// <summary>W-03 (0.708): something a Zodiac book asks for. Needed: monsters to defeat; 0 for the others, done or not. docs/evidence/0.708-relic-weapons.md.</summary>
public sealed record RelicTarget(string Name,string Place,int Needed=0);
public sealed record RelicBook(uint Row,string Name,IReadOnlyList<RelicTarget> Monsters,IReadOnlyList<RelicTarget> Dungeons,IReadOnlyList<RelicTarget> Fates,IReadOnlyList<RelicTarget> Leves);
/// <summary>The weapons, one list per class, an item per stage in the game's order; and the books by their row. Families: the later relics, key then lines, in that order.</summary>
public sealed record RelicCatalog(IReadOnlyList<IReadOnlyList<uint>> Lines,IReadOnlyDictionary<uint,RelicBook> Books,IReadOnlyList<(string Key,IReadOnlyList<IReadOnlyList<uint>> Lines)>? Families=null,IReadOnlyDictionary<string,IReadOnlyList<RelicArmourSet>>? Armours=null);

/// <summary>W-13 (0.720): a stage of armour, as the English name says it. Prefix: how the name starts. Plus: the name ends with « +n », 0 for none. BaseOf: the name, without its « +n », follows this word on a row of the sheet: « Antiquated Chivalrous Circlet » makes « Chivalrous Circlet » a stage. docs/evidence/0.720-relic-armour.md.</summary>
public sealed record RelicArmourStage(int Level,string Prefix="",int Plus=0,string? BaseOf=null);
/// <summary>One job or role's armour, its pieces stage by stage. Name: the game's words for those jobs.</summary>
public sealed record RelicArmourSet(uint Category,string Name,IReadOnlyList<IReadOnlyList<uint>> Stages);

/// <summary>W-13 (0.716): a weapon as the sheet has it. Name: English, whatever the game's language, because stages are recognised by it. Category: its jobs, which stay the same from stage to stage.</summary>
public readonly record struct RelicItemRow(uint Id,string Name,int Level,int Rarity,uint Category);

/// <summary>A stage: its item level, a word in the English name if the level is not enough, and the lowest rarity. docs/evidence/0.716-relic-families.md.</summary>
public sealed record RelicStageRule(int Level,string? Word=null,int Rarity=3,string? Prefix=null,string? AlsoNamed=null);

public enum RelicReadStatus { Loaded,NotLoaded,Unavailable }
/// <summary>What the game holds of the book in progress, copied on the framework thread. Note 0: no book.</summary>
public sealed record RelicProgress(RelicReadStatus Status,uint Note,IReadOnlyList<int> Monsters,IReadOnlyList<bool> Dungeons,IReadOnlyList<bool> Fates,IReadOnlyList<bool> Leves)
{
    public static RelicProgress Of(RelicReadStatus status)=>new(status,0,[],[],[],[]);
}

public static class RelicBoard
{
    public static readonly IReadOnlyList<(string Key,IReadOnlyList<RelicStageRule> Stages)> Rules=
    [
        ("Anima",[new(170,"Animated"),new(200,"Awoken"),new(210,null,4),new(230,"Hyperconductive"),new(240,null,4),new(260,"Sharpened"),new(270,null,4),new(275,"Lux")]),
        ("Eureka",[new(290,"Antiquated"),new(355,"Anemos"),new(370,"Elemental"),new(375,"Elemental"),new(380,"Elemental"),new(385,"Pyros"),new(405,"Eureka"),new(405,"Physeos")]),
        ("Resistance",[new(485,null,4,null," Recollection"),new(500,null,3,"Augmented "," Recollection"),new(500,"Recollection"),new(510,"Law's Order"),new(515,"Law's Order"),new(535,"Blade's")]),
        ("Manderville",[new(615,"Manderville"),new(630,"Amazing Manderville"),new(645,"Majestic Manderville"),new(665,"Mandervillous")]),
        ("Phantom",[new(745,"Penumbrae"),new(760,"Umbrae"),new(775,"Obscurum"),new(790,"Eclipticum"),new(795,"Occultum")]),
    ];

    public static readonly IReadOnlyList<(string Key,IReadOnlyList<RelicArmourStage> Stages)> ArmourRules=
    [
        ("Eureka",[new(290,"Antiquated "),new(335,"",0,"Antiquated "),new(340,"",1,"Antiquated "),new(345,"",2,"Antiquated "),new(350,"Anemos "),new(380,"Elemental "),new(390,"Elemental ",1),new(390,"Elemental ",2)]),
        ("Resistance",[new(495,"Bozjan "),new(495,"Augmented Bozjan "),new(510,"Law's Order "),new(510,"Augmented Law's Order "),new(525,"Blade's ")]),
        ("Phantom",[new(745,"Arcanaut's "),new(745,"Arcanaut's ",1),new(745,"Arcanaut's ",2),new(780,"Phantom Vision "),new(780,"Phantom Vision ",1),new(780,"Phantom Vision ",2),new(780,"Phantom Vision ",3)]),
    ];
    public static IReadOnlySet<int> ArmourLevels()=>ArmourRules.SelectMany(family=>family.Stages).Select(stage=>stage.Level).ToHashSet();

    private static (string Bare,int Plus) WithoutPlus(string name)
    {
        var cut=name.LastIndexOf(" +",StringComparison.Ordinal);
        return cut>0 && int.TryParse(name.AsSpan(cut+2),out var plus) ? (name[..cut],plus):(name,0);
    }

    /// <summary>The rows of a stage. Names: every name of the armour rows, for BaseOf.</summary>
    public static IEnumerable<RelicItemRow> ArmourStage(IReadOnlyList<RelicItemRow> rows,RelicArmourStage stage,IReadOnlySet<string> names)=>rows.Where(row=>
    {
        if(row.Level!=stage.Level || !row.Name.StartsWith(stage.Prefix,StringComparison.Ordinal))return false;
        var (bare,plus)=WithoutPlus(row.Name);
        return plus==stage.Plus && (stage.BaseOf is null || names.Contains(stage.BaseOf+bare));
    });

    /// <summary>One set per job or role, a list of pieces per stage in the rules' order. A stage the set does not have stays empty: the Eureka armour and its Elemental armour have different owners.</summary>
    public static IReadOnlyList<RelicArmourSet> ArmourSets(IReadOnlyList<RelicItemRow> rows,IReadOnlyList<RelicArmourStage> stages,Func<uint,string> categoryName)
    {
        var names=rows.Select(row=>row.Name).ToHashSet(StringComparer.Ordinal);
        var sets=new Dictionary<uint,List<uint>[]>();var order=new List<uint>();
        for(var index=0;index<stages.Count;index++)
            foreach(var row in ArmourStage(rows,stages[index],names).OrderBy(row=>row.Id))
            {
                if(!sets.TryGetValue(row.Category,out var set))
                {
                    sets[row.Category]=set=Enumerable.Range(0,stages.Count).Select(_=>new List<uint>()).ToArray();
                    order.Add(row.Category);
                }
                set[index].Add(row.Id);
            }
        return order.Select(category=>new RelicArmourSet(category,categoryName(category),sets[category].Select(stage=>(IReadOnlyList<uint>)stage).ToArray())).ToArray();
    }

    /// <summary>The levels to read from the sheet, so that the rest of it is left alone.</summary>
    public static IReadOnlySet<int> Levels()=>Rules.SelectMany(family=>family.Stages).Select(stage=>stage.Level).ToHashSet();

    /// <summary>Which rows a stage holds. AlsoNamed: a row whose name, with these words after it, is a weapon too: the base of a line with no word of its own. Prefix: the name must start with it, and is compared without it: « Augmented Honorbound » has its « Honorbound Recollection », a crafted « Augmented Crystarium Rod » has none.</summary>
    public static IEnumerable<RelicItemRow> Stage(IReadOnlyList<RelicItemRow> rows,RelicStageRule rule)
    {
        var names=rule.AlsoNamed is null ? null:rows.Select(row=>row.Name).ToHashSet(StringComparer.Ordinal);
        return rows.Where(row=>row.Level==rule.Level && row.Rarity>=rule.Rarity
            && (rule.Word is null || row.Name.Contains(rule.Word,StringComparison.Ordinal))
            && (rule.Prefix is null || row.Name.StartsWith(rule.Prefix,StringComparison.Ordinal))
            && (rule.AlsoNamed is null || names!.Contains((rule.Prefix is null ? row.Name:row.Name[rule.Prefix.Length..])+rule.AlsoNamed)));
    }

    /// <summary>One line per set of jobs, an item per stage in the rules' order; a line missing a stage keeps the others.</summary>
    public static IReadOnlyList<IReadOnlyList<uint>> FamilyLines(IReadOnlyList<RelicItemRow> rows,IReadOnlyList<RelicStageRule> stages)
    {
        var lines=new Dictionary<uint,List<uint>>();var order=new List<uint>();
        foreach(var stage in stages)
            foreach(var row in Stage(rows,stage).OrderBy(row=>row.Id))
            {
                if(!lines.TryGetValue(row.Category,out var line)){lines[row.Category]=line=[];order.Add(row.Category);}
                if(!line.Contains(row.Id))line.Add(row.Id);
            }
        return order.Select(category=>(IReadOnlyList<uint>)lines[category]).ToArray();
    }

    /// <summary>Stages as rows, classes as columns, into one list per class. An item 0 is no weapon at that stage; a class with none is left out.</summary>
    public static IReadOnlyList<IReadOnlyList<uint>> Lines(IReadOnlyList<IReadOnlyList<uint>> stages)
    {
        var classes=stages.Count==0 ? 0:stages.Max(stage=>stage.Count);
        return Enumerable.Range(0,classes).Select(index=>(IReadOnlyList<uint>)stages.Where(stage=>index<stage.Count && stage[index]!=0).Select(stage=>stage[index]).ToArray()).Where(line=>line.Count>0).ToArray();
    }

    /// <summary>The furthest stage held, -1 for none; null while the stocks of a stage have not been read. Stocks count: a weapon sold or dyed away is not seen.</summary>
    public static int? Reached(IReadOnlyList<uint> line,Func<uint,long?> held)
    {
        var reached=-1;var unknown=false;
        for(var index=0;index<line.Count;index++)
            switch(held(line[index]))
            {
                case null:unknown=true;break;
                case >0:reached=index;break;
            }
        return reached<0 && unknown ? null:reached;
    }

    /// <summary>Pieces seen in the stocks, and pieces whose stocks are known: unknown is not « none ».</summary>
    public static (int Held,int Known) PiecesHeld(IReadOnlyList<uint> pieces,Func<uint,long?> held)
    {
        var counts=pieces.Select(held).ToArray();
        return (counts.Count(count=>count>0),counts.Count(count=>count is not null));
    }

    /// <summary>How much of a book is done, target by target. A list shorter than the book counts the rest as not done.</summary>
    public static (int Done,int Total) Done(RelicBook book,RelicProgress progress)
    {
        var done=book.Monsters.Select((target,index)=>index<progress.Monsters.Count && target.Needed>0 && progress.Monsters[index]>=target.Needed).Count(value=>value);
        done+=book.Dungeons.Where((_,index)=>index<progress.Dungeons.Count && progress.Dungeons[index]).Count();
        done+=book.Fates.Where((_,index)=>index<progress.Fates.Count && progress.Fates[index]).Count();
        done+=book.Leves.Where((_,index)=>index<progress.Leves.Count && progress.Leves[index]).Count();
        return (done,book.Monsters.Count+book.Dungeons.Count+book.Fates.Count+book.Leves.Count);
    }
}
