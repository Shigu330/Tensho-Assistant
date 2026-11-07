// 0.784 (X-26, user report): the next move on a recipe where HQ is impossible.
internal static class NoHqLive784Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("No-HQ synthesis: the game's quality bar against Tensho's 0 is accepted only for a recipe with no quality to seek",() =>
        {
            var projection=Read("Craft/CraftLiveProjection.cs");
            Check(projection.Contains("var qualityAgrees=native.MaxQuality==p.Quality || p.Quality==0 && !p.CanHq && !p.Collectable && p.RequiredQuality==0;"),"rule");
            Check(projection.Contains("if(native.MaxProgress!=p.Progress || !qualityAgrees || native.MaxDurability!=p.Durability"),"used, the other checks kept");
            Check(TenshoAssistant.Craft.CraftRecipeQuality.Maximum(false,false,0,2500,100)==0 && TenshoAssistant.Craft.CraftRecipeQuality.Maximum(true,false,0,2500,100)==2500,"why the gap: 0 kept on purpose");
        }),
    ];
}
