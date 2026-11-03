using TenshoAssistant.Craft;

// A-02 (0.420): crafting food and medicine added to the simulator's attributes.
internal static class CraftConsumables420Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft consumables: capped percent of the attribute without them, NQ or HQ, food and medicine added",() =>
        {
            // Pan-fried Mahi-mahi (docs/evidence/0.420-craft-food.md): Control 8 % max 25, HQ 10 % max 31.
            var mahi=new CraftConsumable(4672,"Pan-fried Mahi-mahi",false,[new(CraftConsumables.Control,true,8,25,10,31)]);
            Check(CraftConsumables.Bonus(mahi,false,CraftConsumables.Control,200)==16 && CraftConsumables.Bonus(mahi,true,CraftConsumables.Control,200)==20,"percent below the cap");
            Check(CraftConsumables.Bonus(mahi,false,CraftConsumables.Control,1000)==25 && CraftConsumables.Bonus(mahi,true,CraftConsumables.Control,1000)==31,"capped");
            Check(CraftConsumables.Bonus(mahi,true,CraftConsumables.Craftsmanship,1000)==0 && CraftConsumables.Bonus(null,true,CraftConsumables.Control,1000)==0,"other attribute, none");
            var medicine=new CraftConsumable(9,"Draught",true,[new(CraftConsumables.Cp,true,6,21,6,21),new(CraftConsumables.Control,false,5,0,7,0)]);
            Check(CraftConsumables.Apply(4000,1000,500,mahi,true,medicine,false)==(4000,1000+31+5,500+21),"each from the base attributes, then added; flat bonus");
            Check(CraftConsumables.Apply(4000,1000,500,null,true,null,true)==(4000,1000,500),"nothing chosen");
        }),
        ("Craft consumables: the simulation and the search use the attributes with them",() =>
        {
            var simulation=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftSimulation.cs"));
            var solver=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CraftSolver.cs"));
            Check(simulation.Contains("CraftSimulation.Run(recipe,EffectiveCraftStats(),") && solver.Contains("var effective=EffectiveCraftStats();"),"both");
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("(uint)action.Data[0] is not (Craft.CraftConsumables.WellFed or Craft.CraftConsumables.Medicated)"),"food and medicine only");
        }),
    ];
}
