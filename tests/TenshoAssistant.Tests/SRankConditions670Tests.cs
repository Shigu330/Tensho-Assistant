using System.Text.Json;
using TenshoAssistant.Hunting;

// N-12 (0.670): the S rank conditions: rows, texts and the arguments each text waits for.
internal static class SRankConditions670Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static JsonElement Section(string language)
        =>JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"))).RootElement.GetProperty("SRank");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("S rank conditions: one per mark and zone, sane windows, by expansion",() =>
        {
            var marks=SRankConditions.Marks;
            Check(marks.Count==50 && marks.Select(mark=>mark.Name).Distinct().Count()==marks.Count,"each mark once");
            Check(marks.Where(mark=>mark.Territory!=0).Select(mark=>mark.Territory).Distinct().Count()==marks.Count(mark=>mark.Territory!=0),"one mark per zone");
            Check(marks.Count(mark=>mark.Territory==0)==3 && marks.Where(mark=>mark.Territory==0).All(mark=>mark.Condition=="Extraordinary"),"the three that follow an S rank");
            Check(marks.GroupBy(mark=>mark.Expansion).OrderBy(group=>group.Key).Select(group=>group.Count()).SequenceEqual([17,6,6,7,7,7]),"by expansion");
            Check(marks.All(mark=>mark.Minimum>0 && mark.Maximum>=mark.Minimum && mark.MaintenanceMinimum>0 && mark.MaintenanceMaximum>=mark.MaintenanceMinimum && mark.MaintenanceMaximum<=mark.Maximum),"windows");
            Check(marks.All(mark=>mark.Refs.All(item=>item.Id!=0)),"every reference is a row");
        }),
        ("S rank conditions: every text exists in both languages and gets its arguments",() =>
        {
            foreach(var language in new[]{"en","fr"})
            {
                var section=Section(language);
                foreach(var mark in SRankConditions.Marks)
                {
                    Check(section.TryGetProperty(mark.Condition,out var text),$"{language}: {mark.Condition}");
                    var arguments=SRankConditions.Arguments(mark,item=>"x");
                    var waited=System.Text.RegularExpressions.Regex.Matches(text.GetString()!,"\\{(\\d)\\}").Select(match=>int.Parse(match.Groups[1].Value)).DefaultIfEmpty(-1).Max()+1;
                    Check(waited==arguments.Length,$"{language}: {mark.Condition} waits for {waited}, gets {arguments.Length}");
                }
            }
        }),
        ("S rank conditions: a list of counts is one argument, the place comes last, the search reads names and text",() =>
        {
            var kill=new SRankMark(0,1,1,"Kill",[new(SRankRefKind.Mob,1,100),new(SRankRefKind.Mob,2,50)],null,SRankOdds.Unknown,1,1,1,1);
            Check(SRankConditions.Arguments(kill,item=>"m"+item.Id).SequenceEqual(["100 m1, 50 m2"]),"one list");
            var fate=new SRankMark(0,1,1,"FateThree",[new(SRankRefKind.Fate,9)],"14, 23.5",SRankOdds.Unknown,1,1,1,1);
            Check(SRankConditions.Arguments(fate,item=>"f").SequenceEqual(["f","14, 23.5"]),"the place last");
            var row=new SRankRow(kill,"Ixtab","Rak'tika","","Kill dolls");
            Check(SRankConditions.Matches(row,"") && SRankConditions.Matches(row,"ixt") && SRankConditions.Matches(row,"RAK") && SRankConditions.Matches(row,"doll") && !SRankConditions.Matches(row,"zzz"),"search");
        }),
    ];
}
