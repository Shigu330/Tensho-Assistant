namespace TenshoAssistant.MiniGames;

public enum TriadCardSort { Number, Top, Right, Bottom, Left, Total, Weakest, Score }

/// <summary>
/// Card score of the Triple Triad notebook (0.277, user request). A Tensho estimate with a stated formula, not game data:
/// each side is worth its value (Inversion: 11 − value; Tueur d'as: a side worth 1 after Inversion counts as 6, it takes an A);
/// score = sum of the four sides + 2 × the weakest side (a weak side is the one the opponent captures through)
/// + Identique: 2 per pair of equal sides; Bonus (Ascension): +4 for a typed card; Malus (Descension): −4 for a typed card.
/// Plus and Ordre do not change a single card's worth.
/// </summary>
public static class TriadCardScore
{
    public static int SideValue(int side,TriadRules rules)
    {
        var value=(rules & TriadRules.Reverse)!=0 ? 11-side:side;
        return (rules & TriadRules.FallenAce)!=0 && value==1 ? 6:value;
    }

    public static int Score(TriadCardDefinition card,TriadRules rules)
    {
        int[] sides=[card.Top,card.Right,card.Bottom,card.Left];
        var values=sides.Select(side=>SideValue(side,rules)).ToArray();
        var score=values.Sum()+2*values.Min();
        if((rules & TriadRules.Same)!=0)
            for(var i=0;i<sides.Length;i++)for(var j=i+1;j<sides.Length;j++)if(sides[i]==sides[j])score+=2;
        if((rules & TriadRules.Ascension)!=0 && card.TypeId!=0)score+=4;
        if((rules & TriadRules.Descension)!=0 && card.TypeId!=0)score-=4;
        return score;
    }

    /// <summary>Sort key of a card; sides and total are the printed values (A = 10).</summary>
    public static int Key(TriadCardDefinition card,TriadCardSort sort,TriadRules rules)=>sort switch
    {
        TriadCardSort.Top=>card.Top,
        TriadCardSort.Right=>card.Right,
        TriadCardSort.Bottom=>card.Bottom,
        TriadCardSort.Left=>card.Left,
        TriadCardSort.Total=>card.Top+card.Right+card.Bottom+card.Left,
        TriadCardSort.Weakest=>Math.Min(Math.Min(card.Top,card.Right),Math.Min(card.Bottom,card.Left)),
        TriadCardSort.Score=>Score(card,rules),
        // Ex. cards are numbered apart: after the 460 ordinary cards, like the game's notebook.
        _=>(card.Extra ? 100_000:0)+card.Number,
    };
}
