using TenshoAssistant.FreeCompany;

// 0.694 (user request): several workshop projects as one list, and all the parts of a vessel.
internal static class WorkshopSets694Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static WorkshopProject Project(uint id,uint draft,uint order,params (uint Item,int Per,int Sets)[] materials)=>
        new(id,1000+id,"t",[new WorkshopPart("p",[new WorkshopPhase(1,materials.Select(row=>new WorkshopMaterial(row.Item,row.Per,row.Sets)).ToArray())])],draft,order);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Workshop sets: the same prototype and order are one set, and one prototype holds two classes",() =>
        {
            WorkshopProject[] all=[Project(552,30,7),Project(553,30,7),Project(564,30,10),Project(532,25,2)];
            var set=WorkshopStock.SameSet(all,all[0]);
            Check(set.Count==2 && set.All(project=>project.Order==7),"Unkiu II only");
        }),
        ("Workshop sets: a project with no prototype is alone",() =>
        {
            var alone=Project(1,0,0);
            Check(WorkshopStock.SameSet([alone,Project(2,0,0)],alone).Count==1,"alone");
        }),
        ("Workshop sets: followed projects add up, each item once",() =>
        {
            var totals=WorkshopStock.Totals([Project(1,1,1,(10,2,3),(11,1,1)),Project(2,1,1,(10,1,4))]);
            Check(totals.Count==2 && totals.Single(row=>row.Item==10).Total==10 && totals.Single(row=>row.Item==11).Total==1,"sums");
        }),
    ];
}
