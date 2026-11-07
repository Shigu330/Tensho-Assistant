namespace TenshoAssistant.MyTools;

/// <summary>0.541 (issue #37): what is missing of a tool's item list, at the prices already known. An unknown price or stock is counted as unknown, never as free.</summary>
public static class ToolItemCost
{
    public const int MaximumAsked=32;

    /// <summary>The lowest known price of one piece, whatever its quality.</summary>
    public static decimal? Lowest(decimal? normal,decimal? high)=>normal is { } first && high is { } second ? Math.Min(first,second):normal ?? high;

    /// <param name="rows">Pieces missing (null: stock unknown) and the price of one (null: unknown).</param>
    public static (decimal Known,int Unknown) Missing(IEnumerable<(int? Missing,decimal? Price)> rows)
    {
        decimal known=0;var unknown=0;
        foreach(var (missing,price) in rows)
        {
            if(missing is 0)continue;
            if(missing is { } count && price is { } unit)known+=count*unit;
            else unknown++;
        }
        return (known,unknown);
    }
}
