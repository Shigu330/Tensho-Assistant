namespace TenshoAssistant.Relics;

/// <summary>W-03 (0.708): something a Zodiac book asks for. Needed: monsters to defeat; 0 for the others, done or not. docs/evidence/0.708-relic-weapons.md.</summary>
public sealed record RelicTarget(string Name,string Place,int Needed=0);
public sealed record RelicBook(uint Row,string Name,IReadOnlyList<RelicTarget> Monsters,IReadOnlyList<RelicTarget> Dungeons,IReadOnlyList<RelicTarget> Fates,IReadOnlyList<RelicTarget> Leves);
/// <summary>The weapons, one list per class, an item per stage in the game's order; and the books by their row.</summary>
public sealed record RelicCatalog(IReadOnlyList<IReadOnlyList<uint>> Lines,IReadOnlyDictionary<uint,RelicBook> Books);

public enum RelicReadStatus { Loaded,NotLoaded,Unavailable }
/// <summary>What the game holds of the book in progress, copied on the framework thread. Note 0: no book.</summary>
public sealed record RelicProgress(RelicReadStatus Status,uint Note,IReadOnlyList<int> Monsters,IReadOnlyList<bool> Dungeons,IReadOnlyList<bool> Fates,IReadOnlyList<bool> Leves)
{
    public static RelicProgress Of(RelicReadStatus status)=>new(status,0,[],[],[],[]);
}

public static class RelicBoard
{
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
