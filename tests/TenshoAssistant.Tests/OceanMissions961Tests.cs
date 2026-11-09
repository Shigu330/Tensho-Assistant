using TenshoAssistant.Gathering;

// X-85 V3 (0.961): mission criteria from the game's English texts; a fish counts for a mission not done yet by species or bite.
internal static class OceanMissions961Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ocean missions 961: species words from the bonuses, missions parsed, fish counted until the mission is done",() =>
        {
            var words=OceanMissionRules.SpeciesWords([(14,"Requirement: As a party, catch 200 or more sharks."),(42,"Requirement: As an individual, catch 50 or more shrimp."),
                (41,"Requirement: As a party, catch 400 or more squid."),(56,"Requirement: As an individual, catch 50 or more mantis shrimp."),(55,"Requirement: As a party, catch 300 or more prehistoric wavekin."),(1,"Requirement: Score 2,500 points or higher.")]);
            Check(words["sharks"]==14 && words["mantis shrimp"]==56 && words["prehistoric wavekin"]==55 && !words.ContainsKey("points"),"words");
            var squid=OceanMissionRules.Parse("Catch shrimp or squid",words)!;
            Check(squid.Bonuses.SetEquals([42u,41u]) && squid.Bite is null,"species");
            Check(OceanMissionRules.Parse("Catch mantis shrimp or prehistoric wavekin",words)!.Bonuses.SetEquals([56u,55u]),"two-word species");
            Check(OceanMissionRules.Parse("Catch fish with a ferocious bite (!!!)",words)!.Bite=="Legendary" && OceanMissionRules.Parse("Catch fish with a strong bite (!!)",words)!.Bite=="Strong"
                && OceanMissionRules.Parse("Catch fish with a weak bite (!)",words)!.Bite=="Weak","bites");
            Check(OceanMissionRules.Parse("Catch fish rated ★★★ or higher",words)!.MinStars==3 && OceanMissionRules.Parse("●★★★★を3匹釣る。",words) is null && OceanMissionRules.Parse("Catch dragons",words) is null,"stars and unknown");
            var bonuses=new Dictionary<uint,uint[]>{[100]=[41],[101]=[14]};
            var missions=new[]{("Squid",3,19,(OceanMissionCriterion?)squid),("Ferocious",2,7,OceanMissionRules.Parse("Catch fish with a ferocious bite (!!!)",words)),("Stars",1,5,OceanMissionRules.Parse("Catch fish rated ★★★ or higher",words))};
            Check(OceanMissionRules.CountsFor(100,bonuses,"Weak",missions).SequenceEqual(["Squid"]),"species counts");
            Check(OceanMissionRules.CountsFor(101,bonuses,"Legendary",missions).SequenceEqual(["Ferocious"]),"bite counts, stars left out");
            var done=new[]{("Squid",19,19,(OceanMissionCriterion?)squid)};
            Check(OceanMissionRules.CountsFor(100,bonuses,null,done).Count==0,"done mission not marked");
        }),
    ];
}
