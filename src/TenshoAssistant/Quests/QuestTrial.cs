namespace TenshoAssistant.Quests;

/// <summary>
/// X-606 (0.1710, R37): the trial quests of the desktop application's « Quests-details » picture, never in the game: a quest under way with
/// every line of its details (giver placed on map 25, territory 156, as the probe's quest 68142; end NPC, jobs, allied society, a quest before),
/// and the quest before it, done. The texts are English placeholders, not the game's; the hour is fixed so that the picture does not move.
/// </summary>
public static class QuestTrial
{
    public const ulong Character=1;
    public const uint Shown=68142;
    public static readonly DateTimeOffset ObservedAt=new(2026,11,21,9,0,0,TimeSpan.Zero);

    public static IReadOnlyList<QuestEntry> Entries()=>
    [
        new(Shown,"Trial quest","Trial genre",0,QuestProgress.Active,1)
        {
            Section="Trial section",SectionId=3,Level=65,Zone="Trial place",Expansion="Trial expansion",IssuerId=1,IssuerMapId=25,IssuerTerritoryId=156,
            IssuerName="trial giver",EndName="trial end NPC",Jobs="Trial jobs",Society="trial allied society",PreviousQuestIds=[Shown-1],PreviousQuestJoin=1,
        },
        new(Shown-1,"Trial quest before","Trial genre",0,QuestProgress.Complete,null){Section="Trial section",SectionId=3,Level=63},
    ];
}
