using TenshoAssistant.Gear;

// 0.593 (T25): several named plans for a job.
internal static class GearPlanSets593Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gear plans: a new plan copies the current one, which is kept aside",() =>
        {
            GearPlanSet? set=null;
            var pieces=new Dictionary<GearSlot,uint>{[GearSlot.Head]=5};var materia=new Dictionary<GearSlot,List<uint>>{[GearSlot.Head]=[7]};
            Check(GearPlanSets.New(ref set,"Main","Levelling",pieces,materia),"created");
            Check(set is {Active:"Levelling",Others.Count:1} && set.Others[0].Name=="Main" && set.Others[0].Pieces[GearSlot.Head]==5,"the first one under the default name");
            pieces[GearSlot.Head]=9;materia[GearSlot.Head].Add(8);
            Check(set!.Others[0].Pieces[GearSlot.Head]==5 && set.Others[0].Materia[GearSlot.Head].Count==1,"a real copy: editing the new plan leaves it alone");
        }),
        ("Gear plans: names are refused when empty, too long or taken, capitals ignored",() =>
        {
            GearPlanSet? set=null;
            Check(GearPlanSets.New(ref set,"Main","Craft",[],[]),"first");
            Check(!GearPlanSets.New(ref set,"Main","  ",[],[]),"empty");
            Check(!GearPlanSets.New(ref set,"Main","craft",[],[]),"the current one's");
            Check(!GearPlanSets.New(ref set,"Main","MAIN",[],[]),"one kept aside");
            Check(!GearPlanSets.New(ref set,"Main",new string('x',GearPlanSets.MaxName+1),[],[]),"too long");
            Check(!GearPlanSets.New(ref set,"Main","A\nB",[],[]),"a control character");
            GearPlanSet? refused=null;
            Check(!GearPlanSets.New(ref refused,"Main","",[],[]) && refused is null,"a refused first name creates nothing");
        }),
        ("Gear plans: switching keeps the current plan and gives the other back",() =>
        {
            GearPlanSet? set=null;
            var main=new Dictionary<GearSlot,uint>{[GearSlot.Head]=1};
            GearPlanSets.New(ref set,"Main","Craft",main,[]);
            var craft=new Dictionary<GearSlot,uint>{[GearSlot.Body]=2};
            var taken=GearPlanSets.Switch(set!,"Main",craft,new(){[GearSlot.Body]=[3]});
            Check(taken is not null && taken.Pieces[GearSlot.Head]==1,"the other plan");
            Check(set!.Active=="Main" && set.Others is [{Name:"Craft"}] && set.Others[0].Pieces[GearSlot.Body]==2 && set.Others[0].Materia[GearSlot.Body][0]==3,"the current one kept, with its materia");
            Check(GearPlanSets.Switch(set,"Unknown",[],[]) is null && set.Active=="Main","an unknown name changes nothing");
        }),
        ("Gear plans: rename the current one; delete takes the next, never the last",() =>
        {
            GearPlanSet? set=null;
            GearPlanSets.New(ref set,"Main","Craft",[],[]);
            Check(GearPlanSets.Rename(set!,"CRAFT") && set!.Active=="CRAFT","its own capitals");
            Check(!GearPlanSets.Rename(set!,"main"),"taken");
            Check(GearPlanSets.Delete(set!)?.Name=="Main" && set!.Active=="Main" && set.Others.Count==0,"the next one is taken");
            Check(GearPlanSets.Delete(set!) is null && set!.Active=="Main","the only plan stays");
        }),
        ("Gear plans: a limit, a reset, and the picker before the plan is read",() =>
        {
            GearPlanSet? set=null;
            for(var index=0;index<GearPlanSets.MaxPlans-1;index++)Check(GearPlanSets.New(ref set,"Main",$"P{index}",[],[]),"under the limit");
            Check(!GearPlanSets.New(ref set,"Main","One too many",[],[]),"at the limit");
            Check(TenshoAssistant.ConfigurationReset.Kept.Contains("GearPlanSets"),"in the player's data reset");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Gear.cs"));
            Check(page.IndexOf("DrawGearPlanPicker(state.JobId);")<page.IndexOf("var plans=configuration.GearPlans??=[];"),"taken before the frame reads the plan");
        }),
    ];
}
