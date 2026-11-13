using TenshoAssistant.Gear;

// 0.1242 (X-01 C5, Gear page): the planned set, its stats and the derived values are rebuilt only when an input changes, not in every
// frame. The plan and its materia are compared by their exact content (GearPlanKey.Signature: a change made in place anywhere is seen at
// once); the other inputs are the catalog, the character's state as read, the job and level, « assume HQ », the worn gear and the language.
internal static class GearModel1242Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Gear model 1242: the plan's signature is equal exactly when the plan and its materia are the same, changes in place included",() =>
        {
            var plan=new Dictionary<GearSlot,uint>{[GearSlot.Head]=10,[GearSlot.MainHand]=7};
            var sameOtherOrder=new Dictionary<GearSlot,uint>{[GearSlot.MainHand]=7,[GearSlot.Head]=10};
            var materia=new Dictionary<GearSlot,List<uint>>{[GearSlot.Head]=[5,6]};
            var before=GearPlanKey.Signature(plan,materia);
            Check(before==GearPlanKey.Signature(sameOtherOrder,new Dictionary<GearSlot,List<uint>>{[GearSlot.Head]=[5,6]}),"same content, other order: equal");
            plan[GearSlot.Head]=11;
            Check(GearPlanKey.Signature(plan,materia)!=before,"a piece picked in place: seen");
            plan[GearSlot.Head]=10;
            Check(GearPlanKey.Signature(plan,materia)==before,"back to the same: equal");
            materia[GearSlot.Head].Add(7);
            Check(GearPlanKey.Signature(plan,materia)!=before,"a materia added to the same list: seen");
            materia[GearSlot.Head].RemoveAt(2);materia[GearSlot.Head][1]=8;
            Check(GearPlanKey.Signature(plan,materia)!=before,"a materia changed in place: seen");
            Check(GearPlanKey.Signature(new Dictionary<GearSlot,uint>{[GearSlot.Head]=5},null)!=GearPlanKey.Signature(new Dictionary<GearSlot,uint>(),new Dictionary<GearSlot,List<uint>>{[GearSlot.Head]=[5]}),"a piece is not a materia");
            Check(GearPlanKey.Signature(new Dictionary<GearSlot,uint>{[GearSlot.Head]=12},null)!=GearPlanKey.Signature(new Dictionary<GearSlot,uint>{[GearSlot.Body]=12},null),"the slot counts");
            Check(GearPlanKey.Signature(new Dictionary<GearSlot,uint>(),null)==GearPlanKey.Signature(new Dictionary<GearSlot,uint>(),new Dictionary<GearSlot,List<uint>>()),"empty plan: one signature");
        }),
        ("Gear model 1242: the page builds its model once per change of an input; the stats read it, nothing computed in the frame",() =>
        {
            var gear=Read("src/TenshoAssistant/MainWindow.Gear.cs");
            Check(gear.Contains("var model=GearModelFor(catalog,state,worn,plan,otherJob);") && gear.Contains("DrawGearStats(state,model);"),"the page uses the model");
            Check(gear.Contains("var key=new GearModelKey(catalog,characterStats.State,state.JobId,state.Level,assumeHq,otherJob ? null:EquippedGear?.Invoke(),\n            GearPlanKey.Signature(plan,materiaPlan),localization.Revision);"),"every input in the key");
            Check(gear.Contains("if(gearModel is { } known && gearModelKey==key)return known;") && gear.Contains("gearModelKey=key;gearModel=model;"),"kept while the key is the same");
            var stats=gear[gear.IndexOf("private void DrawGearStats(",StringComparison.Ordinal)..gear.IndexOf("private void DrawMateriaSockets(",StringComparison.Ordinal)];
            Check(!stats.Contains("GearRules.Totals(") && !stats.Contains("GearRules.Planned(") && !stats.Contains("StatFormulas.Combat("),"the stats column computes nothing");
            var model=gear[gear.IndexOf("private GearModel GearModelFor(",StringComparison.Ordinal)..gear.IndexOf("private void DrawGearToolbar(",StringComparison.Ordinal)];
            Check(model.Contains("GearRules.Totals(") && model.Contains("StatFormulas.Combat(state.Values,") && model.Contains("StatFormulas.Combat(plannedValues,"),"all of it in the model");
            Check(gear.Split("StatFormulas.Combat(").Length-1==2 && gear.Split("GearRules.AverageItemLevel(").Length-1==2,"nowhere else on the page");
            Check(gear.Contains("gearLooks=model.Looks;gearMelds=model.Melds;") && !gear.Contains("gearLooks.Clear()"),"the looks and materia are the model's");
        }),
    ];
}
