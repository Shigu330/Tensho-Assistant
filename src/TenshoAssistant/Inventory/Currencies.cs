namespace TenshoAssistant.Inventory;

/// <summary>How a currency's balance and cap are read: Grand Company seals (cap by rank), a tomestone, or any other currency item.</summary>
public enum CurrencyKind { Seals, Tomestone, Other }

/// <summary>A tracked currency: its item, name and icon from the game data, its kind and its item stack size (the cap the game shows).</summary>
public sealed record CurrencyDefinition(uint ItemId,string Name,uint IconId,CurrencyKind Kind,uint StackCap);

/// <summary>One balance read from the game: amount and cap (null = not read, unknown — never 0).</summary>
public sealed record CurrencyBalance(CurrencyDefinition Currency,long? Amount,long? Cap)
{
    public double? Percent=>Amount is { } amount && Cap is { } cap && cap>0 ? amount*100.0/cap:null;
}

/// <summary>All balances of one read, for one character, with this week's limited tomestones (null = not read).</summary>
public sealed record CurrencyReading(ulong Character,DateTimeOffset ReadAt,IReadOnlyList<CurrencyBalance> Balances,int? WeeklyTomestones,int? WeeklyLimit);

/// <summary>
/// g, I-02 (0.344): threshold alerts, original code (ideas from CurrencyAlert, MIT, and Currency Watchdog, read only; docs/evidence/
/// 0.344-currencies.md). A threshold is a percentage of the cap (it follows the Grand Company rank's cap), 80 % by default, 0 = off.
/// One alert when a balance crosses its threshold upwards; re-armed when it falls 2 points under it (under the threshold itself for
/// thresholds of 2 % or less). The first read of a character arms silently: a currency already over its threshold is listed but not
/// announced; a changed threshold re-arms that currency only, silently (0.344 review). A single 0 does not arm: during login the game
/// answers 0 before the real balance arrives, which would turn the real balance into a false crossing; two 0 reads in a row (5 s apart)
/// are a real 0 and arm, so a later jump over the threshold is announced (0.344 fix check). A balance or cap
/// not read changes nothing. Caps of 1 or less and of a million or more are not percentage caps (gil, MGP): never alerted.
/// </summary>
public sealed class CurrencyAlertTracker
{
    public const int DefaultPercent=80,Hysteresis=2;
    private readonly Dictionary<uint,bool> armed=[];
    private readonly Dictionary<uint,int> percentSeen=[];
    private readonly HashSet<uint> zeroOnce=[];
    private ulong character;

    public static bool Percentable(long cap)=>cap>1 && cap<1_000_000;
    public static int PercentFor(uint item,IReadOnlyDictionary<uint,int>? thresholds)
        =>thresholds is not null && thresholds.TryGetValue(item,out var percent) ? Math.Clamp(percent,0,100):DefaultPercent;

    /// <summary>The balances that just crossed their threshold (to announce) and all those at or over it (to list).</summary>
    public (IReadOnlyList<CurrencyBalance> Crossed,IReadOnlyList<CurrencyBalance> Over) Evaluate(CurrencyReading reading,IReadOnlyDictionary<uint,int>? thresholds)
    {
        if(reading.Character!=character){armed.Clear();percentSeen.Clear();zeroOnce.Clear();character=reading.Character;}
        var crossed=new List<CurrencyBalance>();var over=new List<CurrencyBalance>();
        foreach(var balance in reading.Balances)
        {
            var id=balance.Currency.ItemId;
            if(balance.Amount is not { } amount || balance.Cap is not { } cap || !Percentable(cap))continue;
            var percent=PercentFor(id,thresholds);
            if(percentSeen.TryGetValue(id,out var before) && before!=percent)armed.Remove(id);
            percentSeen[id]=percent;
            if(percent==0){armed.Remove(id);continue;}
            var isOver=amount>0 && amount>=cap*percent/100.0;
            if(isOver)over.Add(balance);
            if(!armed.TryGetValue(id,out var ready))
            {
                if(amount>0){armed[id]=!isOver;zeroOnce.Remove(id);}
                else if(!zeroOnce.Add(id)){armed[id]=true;zeroOnce.Remove(id);}
                continue;
            }
            var rearm=cap*(percent>Hysteresis ? percent-Hysteresis:percent)/100.0;
            if(ready && isOver){crossed.Add(balance);armed[id]=false;}
            else if(!ready && amount<rearm)armed[id]=true;
        }
        return (crossed,over);
    }

    public void Reset(){armed.Clear();percentSeen.Clear();zeroOnce.Clear();character=0;}
}

/// <summary>Which currencies are tracked by default (docs/evidence/0.344-currencies.md).</summary>
public static class CurrencyDefaults
{
    /// <summary>
    /// Wolf marks, trophy crystals, allied seals, Centurio seals, sacks of nuts, bicolor gemstones, Skybuilders' scrips, crafters' and
    /// gatherers' purple and orange scrips. Grand Company seals and tomestones are added from the game (current company, TomestonesItem).
    /// </summary>
    public static readonly IReadOnlyList<uint> Items=[25,36656,27,10307,26533,26807,28063,33913,33914,41784,41785];

    /// <summary>Seal item of a Grand Company (1 Maelstrom → 20, 2 Twin Adders → 21, 3 Immortal Flames → 22); 0 without one.</summary>
    public static uint SealItem(uint grandCompany)=>grandCompany is >= 1 and <= 3 ? 19+grandCompany:0;
}
