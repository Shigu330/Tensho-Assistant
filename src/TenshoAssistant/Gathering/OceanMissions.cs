using System.Text.RegularExpressions;

namespace TenshoAssistant.Gathering;

/// <summary>What a voyage mission counts: species (IKDContentBonus rows), a bite strength (Weak, Strong, Legendary as GatherBuddy names them) or a minimum of stars.</summary>
public sealed record OceanMissionCriterion(IReadOnlySet<uint> Bonuses,string? Bite,int MinStars);

/// <summary>
/// X-85 V3 (0.961): the voyage missions read as criteria from the game's own English texts, without a hand-written table. IKDPlayerMissionCondition gives
/// only a text (« Catch sharks », « Catch shrimp or squid », « Catch fish with a ferocious bite (!!!) », « Catch fish rated ★★★ or higher »); the species
/// words are matched to IKDContentBonus rows through their English requirement (« … catch 200 or more sharks. »), and IKDFishParam lists each fish's
/// species rows. Stars (FishParameter.OceanStars) are not used yet: 143 of 259 ocean fish read 4 stars, not proven (docs/evidence/0.961-ocean-missions.md).
/// </summary>
public static partial class OceanMissionRules
{
    [GeneratedRegex(@"catch \d+ or more ([a-z ]+?)\.?$",RegexOptions.IgnoreCase)]
    private static partial Regex BonusWords();

    /// <summary>Species word (lower case, « mantis shrimp ») to its IKDContentBonus row, from the bonuses' English requirements.</summary>
    public static IReadOnlyDictionary<string,uint> SpeciesWords(IEnumerable<(uint Id,string Requirement)> bonuses)
    {
        var words=new Dictionary<string,uint>(StringComparer.OrdinalIgnoreCase);
        foreach(var (id,requirement) in bonuses)
            if(BonusWords().Match(requirement.Trim()) is { Success:true } match)words.TryAdd(match.Groups[1].Value.Trim(),id);
        return words;
    }

    public static OceanMissionCriterion? Parse(string english,IReadOnlyDictionary<string,uint> words)
    {
        var text=english.Trim().TrimEnd('.');
        if(text.Contains("bite (!!!)",StringComparison.Ordinal))return new(new HashSet<uint>(),"Legendary",0);
        if(text.Contains("bite (!!)",StringComparison.Ordinal))return new(new HashSet<uint>(),"Strong",0);
        if(text.Contains("bite (!)",StringComparison.Ordinal))return new(new HashSet<uint>(),"Weak",0);
        if(text.Contains("rated ★★★ or higher",StringComparison.Ordinal))return new(new HashSet<uint>(),null,3);
        if(!text.StartsWith("Catch ",StringComparison.Ordinal))return null;
        var found=new HashSet<uint>();
        foreach(var word in text["Catch ".Length..].Split(" or ",StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries))
        {
            if(!words.TryGetValue(word,out var id))return null;
            found.Add(id);
        }
        return found.Count>0 ? new(found,null,0):null;
    }

    /// <summary>The missions not done yet that a fish counts for (species from the game, bite from GatherBuddy when known); stars are left out.</summary>
    public static IReadOnlyList<string> CountsFor(uint fish,IReadOnlyDictionary<uint,uint[]> fishBonuses,string? bite,
        IEnumerable<(string Text,int Progress,int Target,OceanMissionCriterion? Criterion)> missions)
        =>missions.Where(mission=>mission.Criterion is { } criterion && (mission.Target<=0 || mission.Progress<mission.Target)
            && (criterion.Bonuses.Count>0 && fishBonuses.TryGetValue(fish,out var own) && own.Any(criterion.Bonuses.Contains)
                || criterion.Bite is { } wanted && bite==wanted))
            .Select(mission=>mission.Text).ToArray();
}
