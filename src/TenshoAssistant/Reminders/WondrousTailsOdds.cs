using System.Numerics;

namespace TenshoAssistant.Reminders;

/// <summary>
/// N-03 (0.439): Wondrous Tails line odds. The journal is a 4 × 4 grid (cell i = bit i); its 10 lines are the 4 rows, 4 columns and 2
/// diagonals; lines count once 9 stickers are placed. « Now »: the stickers left land on the empty cells, every placement equally
/// likely; « after a shuffle »: all 9 stickers anywhere. Exact counts over every placement (at most C(16,9) = 11 440). Row-major or
/// column-major order gives the same odds (a transposition maps lines to lines).
/// </summary>
public sealed record WondrousTailsOdds(int Placed,double AtLeastOne,double AtLeastTwo,double Three);

public static class WondrousTailsLines
{
    public const int Cells=16,Stickers=9;
    private static readonly ushort[] Lines=BuildLines();

    private static ushort[] BuildLines()
    {
        var lines=new List<ushort>();
        for(var row=0;row<4;row++)lines.Add((ushort)(0xF<<(row*4)));
        for(var column=0;column<4;column++)lines.Add((ushort)(0x1111<<column));
        lines.Add(0x8421);lines.Add(0x1248);
        return lines.ToArray();
    }

    public static int Count(ushort mask)=>Lines.Count(line=>(mask & line)==line);

    /// <summary>Odds of at least one, two and three lines with 9 stickers, from the placed cells; null when 9 or more are placed or the mask is wrong.</summary>
    public static WondrousTailsOdds? Odds(ushort placed)
    {
        var count=BitOperations.PopCount(placed);
        if(count>=Stickers)return null;
        var empty=Enumerable.Range(0,Cells).Where(cell=>(placed & (1<<cell))==0).ToArray();
        long total=0,one=0,two=0,three=0;
        void Visit(int start,int left,ushort mask)
        {
            if(left==0)
            {
                total++;var lines=Count(mask);
                if(lines>=1)one++;if(lines>=2)two++;if(lines>=3)three++;
                return;
            }
            for(var index=start;index<=empty.Length-left;index++)Visit(index+1,left-1,(ushort)(mask | (1<<empty[index])));
        }
        Visit(0,Stickers-count,placed);
        return total==0 ? null:new(count,(double)one/total,(double)two/total,(double)three/total);
    }

    /// <summary>The odds after a shuffle: the 9 stickers anywhere (the placed ones moved at random).</summary>
    public static WondrousTailsOdds Shuffled()=>Odds(0)!;
}
