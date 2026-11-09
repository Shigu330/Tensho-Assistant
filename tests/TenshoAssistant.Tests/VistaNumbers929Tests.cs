using TenshoAssistant.Maps;

// X-76 P5 (0.929): sightseeing log numbers from the game's AdventureExPhase rows (read 2026-11-08).
internal static class VistaNumbers929Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Vista numbers 929: A Realm Reborn 1-80, numbering per expansion, Dawntrail's two phases continue",() =>
        {
            var phases=new (uint,uint,int)[]{(2162830,2162891,2),(2162892,2162936,3),(2162937,2162982,4),(2162983,2163010,5),(2163011,2163027,5)};
            Check(VistaLogNumbers.Of(2162688,phases)==(0,1) && VistaLogNumbers.Of(2162767,phases)==(0,80),"A Realm Reborn");
            Check(VistaLogNumbers.Of(2162830,phases)==(2,1) && VistaLogNumbers.Of(2162891,phases)==(2,62),"Stormblood");
            Check(VistaLogNumbers.Of(2162892,phases)==(3,1),"Shadowbringers restarts");
            Check(VistaLogNumbers.Of(2163010,phases)==(5,28) && VistaLogNumbers.Of(2163011,phases)==(5,29) && VistaLogNumbers.Of(2163027,phases)==(5,45),"Dawntrail continues");
            Check(VistaLogNumbers.Of(2162768,phases) is null && VistaLogNumbers.Of(1,phases) is null,"outside: no number");
        }),
    ];
}
