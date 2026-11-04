using System.Globalization;

namespace TenshoAssistant.Gathering;

/// <summary>
/// 0.479 (user request, in-game campaign): what the game's Collectable window says for the next action, from its values (docs/evidence/0.479-collectable-window-values.md): the item, the current
/// collectability, and the gain of each action as the game gives it this turn. Null when a value is missing or of another type, so a patch that moves them gives no advice rather than a wrong one.
/// </summary>
public sealed record CollectableTurn(uint ItemId,int Current,int Maximum,int Scour,int BrazenMin,int BrazenMax,int Meticulous,bool Intuition,
    uint ScourAction,uint BrazenAction,uint MeticulousAction,uint CollectAction,uint ScrutinyAction)
{
    /// <summary>The gain of a Scour on later turns. An active Intuition adds the same amount to every action this turn; Brazen's range (50 to 150 % of the plain gain) stays the plain gain wide.</summary>
    public int LaterGain=>Intuition ? Math.Max(0,BrazenMax-BrazenMin):Scour;

    public static CollectableTurn? Parse(IReadOnlyList<string>? raw)
    {
        if(raw is null || raw.Count<55)return null;
        uint? Number(int index)
        {
            var parts=raw[index].Split(' ',2);
            return parts.Length==2 && parts[0]==$"{index}:uint" && uint.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out var value) ? value:null;
        }
        bool? Flag(int index)
        {
            var parts=raw[index].Split(' ',2);
            return parts.Length==2 && parts[0]==$"{index}:bool" && bool.TryParse(parts[1],out var value) ? value:null;
        }
        if(Number(2) is not { } item || item==0 || Number(13) is not { } current || Number(14) is not { } maximum || maximum is 0 or >100000 || current>maximum
            || Number(48) is not { } scour || Number(49) is not { } low || Number(50) is not { } high || Number(51) is not { } meticulous || Flag(54) is not { } intuition
            || scour==0 || low>high || high>maximum*4 || Number(26) is not { } scourAction || Number(29) is not { } brazenAction || Number(32) is not { } meticulousAction
            || Number(35) is not { } collectAction || Number(38) is not { } scrutinyAction)return null;
        return new(item,(int)current,(int)maximum,(int)scour,(int)low,(int)high,(int)meticulous,intuition,scourAction,brazenAction,meticulousAction,collectAction,scrutinyAction);
    }
}

public enum CollectableMove { Scour,Brazen,Meticulous,Collect }
/// <param name="Move">What to do now.</param>
/// <param name="Target">The tier aimed for; Below if even every attempt does not reach the low threshold.</param>
/// <param name="Selections">Actions still to raise collectability, this one included.</param>
/// <param name="Collections">Times to collect after them.</param>
/// <param name="Scrips">The scrips of the plan, if the tier is reached; 0 for an item reward.</param>
/// <param name="Impossible">Even the best rolls cannot reach the low threshold with the attempts left.</param>
public sealed record CollectableAdvice(CollectableMove Move,CollectableTier Target,int Selections,int Collections,int Scrips,bool Impossible=false);

/// <summary>
/// 0.479: the plan for the attempts left. Each one spent raising collectability is one less to collect, so every split is compared on what it earns: the tier reached times the items collected. The gains
/// are those the window gives now, read again each turn; a plan can change after a random gain. Nothing is executed.
/// </summary>
public static class CollectableAdvisor
{
    public static CollectableAdvice? Advise(CollectableTurn turn,int attempts,CollectableRule? rule)
    {
        ArgumentNullException.ThrowIfNull(turn);
        if(attempts<=0 || rule is null)return null;
        int Reach(int selections,int first)=>selections==0 ? turn.Current:Math.Min(turn.Maximum,turn.Current+first+(selections-1)*turn.LaterGain);
        var best=0;decimal bestValue=-1;
        for(var selections=0;selections<attempts;selections++)
        {
            var value=Worth(rule,Collectables.Tier(rule,Reach(selections,turn.Scour)))*(attempts-selections);
            if(value>bestValue){best=selections;bestValue=value;}
        }
        if(bestValue<=0)
        {
            // Nothing earns anything for sure. The best rolls say if the low threshold can still come: every attempt but one goes to it, or nothing can help.
            best=attempts-1;
            if(best==0 || turn.Current+turn.BrazenMax+(best-1)*turn.LaterGain*3/2<rule.Low)return new(CollectableMove.Collect,CollectableTier.Below,0,attempts,0,true);
        }
        var reached=Collectables.Tier(rule,Reach(best,turn.Scour));
        var scrips=rule.Scrips ? Reward(rule,reached)*(attempts-best):0;
        if(best==0)return new(CollectableMove.Collect,reached,0,attempts,scrips);
        return new(Choose(turn,rule,reached,best),reached,best,attempts-best,scrips);
    }
    private static decimal Worth(CollectableRule rule,CollectableTier tier)
        =>rule.Scrips && rule.RewardHigh>0 ? Reward(rule,tier):tier switch{CollectableTier.High=>3,CollectableTier.Mid=>2,CollectableTier.Low=>1,_=>0};
    private static int Reward(CollectableRule rule,CollectableTier tier)
        =>tier switch{CollectableTier.High=>rule.RewardHigh,CollectableTier.Mid=>rule.RewardMid,CollectableTier.Low=>rule.RewardLow,_=>0};
    private static int Threshold(CollectableRule rule,CollectableTier tier)
        =>tier switch{CollectableTier.High=>rule.High,CollectableTier.Mid=>rule.Mid,CollectableTier.Low=>rule.Low,_=>0};
    private static CollectableMove Choose(CollectableTurn turn,CollectableRule rule,CollectableTier target,int selections)
    {
        var later=(selections-1)*turn.LaterGain;
        var needed=Threshold(rule,target);
        // A better tier is possible with a good roll, and a bad one still keeps the tier aimed for.
        if(target!=CollectableTier.High)
        {
            var next=Threshold(rule,target+1);
            if(turn.Current+turn.BrazenMax+later>=next && turn.Current+turn.BrazenMin+later>=needed && turn.Current+turn.Scour+later<next)return CollectableMove.Brazen;
        }
        // Meticulous can cost no attempt: use it if it does as well as what the plan needs.
        if(turn.Meticulous>=turn.Scour || turn.Current+turn.Meticulous+later>=needed && target!=CollectableTier.Below)return CollectableMove.Meticulous;
        // Out of reach for sure: only a good roll can help.
        return target==CollectableTier.Below && turn.BrazenMax>turn.Scour ? CollectableMove.Brazen:CollectableMove.Scour;
    }
}
