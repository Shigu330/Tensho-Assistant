using TenshoAssistant.FreeCompany;

// T26 (0.919): the airship's last route from the voyage log: sector names, longest first, order of first appearance, unknown text ignored.
internal static class AirshipLog919Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Airship log 919: sectors in order, « secteur 1 » not inside « secteur 10 », control bytes and unknown names ignored, saved and compared",() =>
        {
            var sectors=new (uint,string)[]{(0,"Mer de nuages - secteur 1"),(9,"Mer de nuages - secteur 10"),(3,"Mer de nuages - secteur 4"),(5,""),};
            var lines=new[]{
                "Montagnes d'Abalathia franchies. Mettons le cap vers \u201cMer de nuages - secteur 1\u201d.",
                "\u201cMer de nuages - secteur 1\u201d atteint. Commençons exploration préliminaire.",
                "H\u0002%I\u0003Cristaux de ventIH découverts.",
                "Cap vers \u201cMer de nuages - secteur 10\u201d.",
                "Cap vers \u201cMer de nuages - secteur 4\u201d puis \u201cMer de nuages - secteur 10\u201d.",
                "",
            };
            var route=AirshipLogRoute.Sectors(lines,sectors);
            Check(route.SequenceEqual(new uint[]{0,9,3}),"route: "+string.Join(",",route));
            Check(AirshipLogRoute.Sectors([],sectors).Count==0 && AirshipLogRoute.Sectors(["Rien"],sectors).Count==0,"nothing found");
            var a=new VoyageVessel{Kind=VesselKind.Airship,LastRoute=["A"]};var b=a with {LastRoute=["B"]};
            Check(!Voyages.SameAs([a],[b]) && Voyages.SameAs([a],[a with {}]),"last route compared");
            var watcher=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/VoyageWatcher.Dalamud.cs"));
            Check(watcher.Contains("if(airRead.Count==1)") && watcher.Contains("var lastRouteRead=AirshipLastRoute(workshop,houseId);"),"single airship only");
        }),
    ];
}
