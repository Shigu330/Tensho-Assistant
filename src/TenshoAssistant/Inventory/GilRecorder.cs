namespace TenshoAssistant.Inventory;

/// <summary>
/// X-658 (0.1604, the user's report of 20/11: « I received gil from the market and it wasn't counted »): the balances the gil watcher compares
/// and how each change becomes journal entries, apart from the game (GilWatcher reads, this decides). Two causes found in the user's journal
/// (docs/evidence/0.1604-gil-retainer-sales.md):
/// <list type="bullet">
/// <item>The first reading after a load was a starting point, never a change: the plugin reloaded (a new version) while a retainer was open
/// with 198 760 gil of sales in its purse, the new instance took them as its starting point, and the withdrawal that followed was counted as a
/// transfer only. Now the retainers' total starts from the last balance the journal recorded (<see cref="Seed"/>), when the character's gil
/// did not change meanwhile (X-662 M1, 0.1606): what rose meanwhile is their market sales. The character's and the company chest's first
/// readings stay starting points (no category can be told for them).</item>
/// <item>For one reading, when another retainer is opened, its purse may be read before the game filled it: the total fell then rose back
/// (« withdrawal » −1 122 then « market » +1 122 a second later, 3 555 gil in seven days, the transfer's gap; X-663: amounts invented, of the shape of
/// the user's journal, docs/evidence/0.1604-gil-retainer-sales.md). A new retainers' total now counts
/// only once read the same twice in a row (<see cref="Settle"/>).</item>
/// </list>
/// 0.769 (X-19): a rise of the retainers' total is a deposit as far as the character's deposits since then cover it, the rest their market
/// sales; a fall is a withdrawal.
/// </summary>
public sealed class GilRecorder
{
    private long? gil,retainers,company,retainersRead;
    private long retainerDeposits;
    // X-662 M1 (0.1606): the journal's last balances, kept until the character's first reading says whether they can be the start.
    private long? seedGil,seedRetainers,firstGil;

    public long? Gil=>gil;
    public long? Retainers=>retainers;
    public long? Company=>company;

    /// <summary>Another character (or none): nothing known.</summary>
    public void Reset(){gil=null;retainers=null;company=null;retainersRead=null;retainerDeposits=0;seedGil=null;seedRetainers=null;firstGil=null;}

    /// <summary>
    /// The journal of the character is loaded: the retainers' total starts from its last recorded balance, but only if the character's first
    /// reading is the journal's last balance of the character too (X-662 M1, review of 0.1605, R06): then no deposit nor withdrawal was made
    /// while Tensho was not watching, and what the retainers gained meanwhile is their market sales. If the character's gil changed meanwhile
    /// (a deposit made without Tensho, a withdrawal, any other change), or either balance was never recorded: a starting point, as before
    /// 0.1604 (a rise not seen is not a proven sale).
    /// </summary>
    public void Seed(IEnumerable<GilEntry> entries)
    {
        if(retainers is not null)return;
        var (lastGil,lastRetainers,_)=GilJournal.LastBalances(entries);
        if(lastGil is null || lastRetainers is null)return;
        seedGil=lastGil;seedRetainers=lastRetainers;
        Decide();
    }

    // The seed is taken or dropped once the character's first reading is known; until then the retainers' readings wait.
    private void Decide()
    {
        if(seedRetainers is not { } start || firstGil is not { } first)return;
        if(retainers is null && first==seedGil)retainers=start;
        seedGil=null;seedRetainers=null;
    }

    /// <summary>
    /// One reading (null: unknown, changes nothing). <paramref name="classify"/> gives the category of a change of the character's gil;
    /// <paramref name="changed"/> is told each entry added with the change read. True when a known balance changed.
    /// </summary>
    public bool Read(long? newGil,long? newRetainers,long? newCompany,DateTimeOffset at,List<GilEntry> entries,Func<long,GilCategory> classify,Action<GilCategory,long>? changed=null)
    {
        firstGil??=newGil;
        Decide();
        if(seedRetainers is not null)newRetainers=null;
        return Record(ref gil,newGil,GilSource.Character,at,entries,classify,changed) | Record(ref retainers,Settle(newRetainers),GilSource.Retainers,at,entries,classify,changed)
            | Record(ref company,newCompany,GilSource.Company,at,entries,classify,changed);
    }

    /// <summary>A new retainers' total counts once read the same twice in a row; until then, unknown.</summary>
    private long? Settle(long? read)
    {
        if(read is null || read==retainers){retainersRead=null;return read;}
        if(retainersRead==read){retainersRead=null;return read;}
        retainersRead=read;
        return null;
    }

    // A known balance that changed becomes an entry; the first known value is only a starting point. An unknown value changes nothing.
    private bool Record(ref long? last,long? now,GilSource source,DateTimeOffset at,List<GilEntry> entries,Func<long,GilCategory> classify,Action<GilCategory,long>? changed)
    {
        if(now is not { } value || last==value)return false;
        if(last is { } before)
        {
            var delta=value-before;
            var category=source switch{GilSource.Retainers=>GilCategory.Retainer,GilSource.Company=>GilCategory.Company,_=>classify(delta)};
            // 0.769 (X-19): a deposit to the retainers waits for their rise; a rise beyond the deposits is their market sales.
            if(source==GilSource.Character && category==GilCategory.Retainer && delta<0)retainerDeposits+=-delta;
            if(source==GilSource.Retainers && delta>0)
            {
                var (transfer,sales,left)=GilJournal.SplitRetainerRise(delta,retainerDeposits);
                retainerDeposits=left;
                if(sales>0)entries.Add(new GilEntry(at,sales,value-transfer,source,GilCategory.Market));
                delta=transfer;
            }
            if(delta!=0)entries.Add(new GilEntry(at,delta,value,source,category));
            changed?.Invoke(category,value-before);
        }
        last=value;
        return true;
    }
}
