using TenshoAssistant.Gear;
using TenshoAssistant.UI;

// 0.593 (T25): several named plans for a job.
internal static class GearPlanSets593Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
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
        ("0.579: all jobs (glamour) is on the gear page itself, the same setting as in its filters",() =>
        {
            var gear=Read("MainWindow.Gear.cs");
            var page=gear[gear.IndexOf("private void DrawGearPlanner()")..gear.IndexOf("DrawGearWear(catalog);")];
            Check(page.Contains("BoolSetting(\"Gear.AllJobs\""),"on the page");
            Check(gear.Split("configuration.GearAllJobs=value").Length==3,"one setting, shown twice");
        }),
        ("0.580: the gear page plans another job: no worn piece and no read value is lent to it",() =>
        {
            var gear=Read("MainWindow.Gear.cs");
            Check(gear.Contains("Where(piece=>!otherJob && catalog.Find(piece.ItemId) is not null)"),"no worn piece");
            Check(gear.Contains("Values=new Dictionary<string,int>(),Category=0,Role=0,Modifiers=default"),"no read value");
            Check(gear.Contains("levels?.TryGetValue(gearJob,out var known)==true"),"a job whose level is known");
        }),
        ("0.585: job levels leave out the sheet's empty rows; the gear page's label comes before its list",() =>
        {
            Check(Read("Plugin.cs").Contains("Where(row=>row.ExpArrayIndex>=0 && !row.Name.IsEmpty)"),"rows 44 and 45 of ClassJob have no name, and share the first level slot");
            var gear=Read("MainWindow.Gear.cs");
            Check(gear.IndexOf("ImGui.TextUnformatted(localization.Get(\"Gear.PlanFor\"))")<gear.IndexOf("ImGui.BeginCombo(\"###GearJob\""),"label first");
        }),
    ];
}
