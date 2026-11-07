using TenshoAssistant.FreeCompany;

// 0.694 (user request): several workshop projects as one list, and all the parts of a vessel.
internal static class WorkshopSets694Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static WorkshopProject Project(uint id,uint draft,uint order,params (uint Item,int Per,int Sets)[] materials)=>
        new(id,1000+id,"t",[new WorkshopPart("p",[new WorkshopPhase(1,materials.Select(row=>new WorkshopMaterial(row.Item,row.Per,row.Sets)).ToArray())])],draft,order,7);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Workshop sets: the same prototype and order are one set, and one prototype holds two classes",() =>
        {
            WorkshopProject[] all=[Project(552,30,7),Project(553,30,7),Project(564,30,10),Project(532,25,2)];
            var set=WorkshopStock.SameSet(all,all[0]);
            Check(set.Count==2 && set.All(project=>project.Order==7),"Unkiu II only");
        }),
        ("Workshop sets: without a category a project is alone",() =>
        {
            var alone=Project(1,0,0) with {Category=0};
            Check(WorkshopStock.SameSet([alone,Project(2,0,0) with {Category=0}],alone).Count==1,"alone");
        }),
        ("Workshop sets (0.727, user report): the Bronco is its four parts, no tower base and no housing wall; the first prototype's row counts",() =>
        {
            // CompanyCraftSequence as read on 2026-11-07: rows 1, 500, 506, 512, 518 and 1000 all have no prototype (-1) and order 1, with categories 1, 5, 5, 5, 5 and 2.
            const uint none=uint.MaxValue;
            WorkshopProject[] all=[Project(1,none,1) with {Category=1},Project(500,none,1) with {Category=5},Project(506,none,1) with {Category=5},Project(512,none,1) with {Category=5},Project(518,none,1) with {Category=5},Project(1000,none,1) with {Category=2},Project(501,0,2) with {Category=5},Project(507,0,2) with {Category=5}];
            Check(WorkshopStock.SameSet(all,all[1]).Select(project=>project.Id).SequenceEqual([500u,506u,512u,518u]),"the Bronco");
            Check(WorkshopStock.SameSet(all,all[0]).Count==1 && WorkshopStock.SameSet(all,all[5]).Count==1,"the tower base and the wall are alone");
            Check(WorkshopStock.SameSet(all,all[6]).Count==2,"row 0 is a real prototype");
            // A submarine set of the same pair stays apart from an airship's: categories 7 and 5.
            Check(WorkshopStock.SameSet([Project(528,24,1) with {Category=7},Project(1,24,1) with {Category=5}],Project(528,24,1) with {Category=7}).Count==1,"another vessel");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/WorkshopProjectsLoader.Dalamud.cs")).Contains("sequence.Order,sequence.Category"),"read from the sheet");
        }),
        ("Workshop sets: followed projects add up, each item once",() =>
        {
            var totals=WorkshopStock.Totals([Project(1,1,1,(10,2,3),(11,1,1)),Project(2,1,1,(10,1,4))]);
            Check(totals.Count==2 && totals.Single(row=>row.Item==10).Total==10 && totals.Single(row=>row.Item==11).Total==1,"sums");
        }),
    ];
}
