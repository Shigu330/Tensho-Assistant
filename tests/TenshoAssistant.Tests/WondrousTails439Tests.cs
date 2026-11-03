using System.Numerics;
using TenshoAssistant.Reminders;

// N-03 (0.439): Wondrous Tails line odds, exact, now and after a shuffle.
internal static class WondrousTails439Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // Independent count: every 16-bit mask with 9 bits holding the placed ones, lines checked cell by cell.
    private static (double One,double Two,double Three) Brute(ushort placed)
    {
        int Lines(int mask)
        {
            bool Cell(int row,int column)=>(mask>>(row*4+column)&1)==1;
            var lines=0;
            for(var index=0;index<4;index++)
            {
                if(Enumerable.Range(0,4).All(column=>Cell(index,column)))lines++;
                if(Enumerable.Range(0,4).All(row=>Cell(row,index)))lines++;
            }
            if(Enumerable.Range(0,4).All(index=>Cell(index,index)))lines++;
            if(Enumerable.Range(0,4).All(index=>Cell(index,3-index)))lines++;
            return lines;
        }
        double total=0,one=0,two=0,three=0;
        for(var mask=0;mask<1<<16;mask++)
        {
            if(BitOperations.PopCount((uint)mask)!=9 || (mask & placed)!=placed)continue;
            total++;var lines=Lines(mask);
            if(lines>=1)one++;if(lines>=2)two++;if(lines>=3)three++;
        }
        return (one/total,two/total,three/total);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Wondrous Tails: 10 lines; odds now and after a shuffle agree with an independent count",() =>
        {
            Check(WondrousTailsLines.Count(0xFFFF)==10 && WondrousTailsLines.Count(0x000F)==1 && WondrousTailsLines.Count(0x8421)==1 && WondrousTailsLines.Count(0)==0,"lines");
            foreach(var placed in new ushort[]{0,0x0001,0x0007,0x8421 & 0x0421,0x0F00,0x1248,0x00F3})
            {
                var odds=WondrousTailsLines.Odds(placed)!;var brute=Brute(placed);
                Check(Math.Abs(odds.AtLeastOne-brute.One)<1e-12 && Math.Abs(odds.AtLeastTwo-brute.Two)<1e-12 && Math.Abs(odds.Three-brute.Three)<1e-12,$"placed {placed:X4}");
            }
            Check(WondrousTailsLines.Odds(0x0F00)!.AtLeastOne==1,"a full row: at least one line sure");
            Check(WondrousTailsLines.Odds(0x01FF) is null,"nine placed: nothing left to estimate");
            Check(WondrousTailsLines.Shuffled().Placed==0,"after a shuffle: all nine anywhere");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Reminders/GameTimers.Dalamud.cs"));
            Check(reader.Contains("(nint)PlayerState.MemberFunctionPointers.IsWeeklyBingoStickerPlaced!=0"),"address checked");
        }),
    ];
}
