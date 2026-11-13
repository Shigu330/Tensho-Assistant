namespace TenshoAssistant.MiniGames;

/// <summary>A racing chocobo rank of the game (ChocoboRaceRank: rating bounds and name from GoldSaucerTextData).</summary>
public sealed record RaceChocoboRank(uint Id,int RatingMin,int RatingMax,string Name);

/// <summary>A racing chocobo ability (ChocoboRaceAbility: name and description, in the client's language).</summary>
public sealed record RaceChocoboAbility(uint Id,string Name,string Description);

/// <summary>
/// The racing chocobo as the game had it loaded: rating, pedigree, sex, the five attributes (maximum speed, acceleration, endurance, stamina,
/// cunning) current and maximum, and the two abilities (hereditary and learned, ChocoboRaceAbility rows, 0 = none).
/// </summary>
public sealed record RaceChocoboSnapshot(int Rating,int Pedigree,bool Female,IReadOnlyList<int> Current,IReadOnlyList<int> Maximum,uint AbilityHereditary,uint AbilityLearned,DateTimeOffset ReadAt);

/// <summary>
/// X-204 (0.1235, user's request of 12/11, K-33): the racing chocobo sheet. Read only when the game itself loaded the chocobo (RaceChocoboManager in
/// state « Loaded »: after the player opened its window or raced), never asked of the server (RequestParameters is not called). The rank comes
/// from the game's ChocoboRaceRank bounds (docs/evidence/0.1235-racing-chocobo.md); a reading out of bounds is dropped, never shown.
/// </summary>
public static class RaceChocobo
{
    public const int Attributes=5,MaxValue=10_000;

    public static RaceChocoboRank? RankOf(int rating,IEnumerable<RaceChocoboRank> ranks)
        => ranks.Where(rank=>rating>=rank.RatingMin && rating<=rank.RatingMax).OrderBy(rank=>rank.Id).FirstOrDefault();

    public static bool Plausible(RaceChocoboSnapshot snapshot)
        => snapshot.Rating is >= 0 and <= MaxValue && snapshot.Pedigree is >= 0 and <= 99
           && snapshot.Current.Count==Attributes && snapshot.Maximum.Count==Attributes
           && snapshot.Current.All(value=>value is >= 0 and <= MaxValue) && snapshot.Maximum.All(value=>value is >= 0 and <= MaxValue)
           && snapshot.Maximum.Any(value=>value>0);
}
