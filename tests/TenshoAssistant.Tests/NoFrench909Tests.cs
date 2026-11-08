// 0.909 (X-76, pre-1.0 clean-up): no French in Tensho's own code. The crystal classifier gives the English keys of its language entries, and the
// comments quote Tensho's English labels (or their keys) and paraphrase user requests in English; French stays only as game data.
using TenshoAssistant.Items;
internal static class NoFrench909Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    // Tensho's French UI labels and French user quotes that comments used to hold.
    private static readonly string[] RemovedQuotes =
    [
        "Stocks importés","Craft lié","« consulté »","« Réduire »","Icônes de cette carte","Suivi du zoom","Échanges (12)","Tensho — objet","⇅ Trier",
        "« Objets »","« Rangements »","Comment l'obtenir","« Alarmes »","Téléportation","Légendes locales","Quêtes annexes","Seuils de butin par secteur",
        "plusieurs fois le même","un fichier par personnage","garde que auto","Connexion GitHub","automatique ferait aussi upload",
    ];
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("No French: the crystal classifier returns the English keys of CrystalElements.* and CrystalSizes.*",() =>
        {
            Check(CrystalClassifier.Elements.SequenceEqual(["Fire","Ice","Wind","Earth","Lightning","Water"]),"elements: "+string.Join(",",CrystalClassifier.Elements));
            Check(CrystalClassifier.Sizes.SequenceEqual(["Shards","Crystals","Clusters"]),"sizes: "+string.Join(",",CrystalClassifier.Sizes));
            Check(CrystalClassifier.Classify("Feuerscherbe","Fire Shard") is { Element: "Fire", Size: "Shards" },"English name");
            Check(CrystalClassifier.Classify("x","Water Cluster") is { Element: "Water", Size: "Clusters" },"English cluster");
            Check(CrystalClassifier.Classify("Éclat de glace") is { Element: "Ice", Size: "Shards" },"French fallback, ice shard");
            Check(CrystalClassifier.Classify("Cristal de terre","") is { Element: "Earth", Size: "Crystals" },"French fallback, earth crystal");
            Check(CrystalClassifier.Classify("Agrégat de vent") is { Element: "Wind", Size: "Clusters" },"French fallback, wind cluster");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Items.cs"));
            Check(page.Contains("\"CrystalElements.\"+element",StringComparison.Ordinal) && page.Contains("\"CrystalSizes.\"+size",StringComparison.Ordinal)
                && !page.Contains("CrystalElementKeys",StringComparison.Ordinal) && !page.Contains("CrystalSizeKeys",StringComparison.Ordinal),"the page uses the keys directly");
        }),
        ("No French: no comment in src quotes Tensho's French labels or the user's French words",() =>
        {
            var found=new List<string>();
            foreach(var file in Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories))
            {
                var parts=file.Split(Path.DirectorySeparatorChar);
                if(parts.Contains("bin") || parts.Contains("obj"))continue;
                var number=0;
                foreach(var line in File.ReadLines(file))
                {
                    number++;
                    var at=line.IndexOf("//",StringComparison.Ordinal);
                    if(at<0)continue;
                    var comment=line[at..];
                    foreach(var quote in RemovedQuotes)
                        if(comment.Contains(quote,StringComparison.Ordinal))found.Add($"{Path.GetFileName(file)}:{number}: {quote}");
                }
            }
            Check(found.Count==0,"French quotes in comments: "+string.Join(" | ",found));
        }),
    ];
}
