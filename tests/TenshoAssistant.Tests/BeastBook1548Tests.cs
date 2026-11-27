using TenshoAssistant.Beastmaster;

// 0.1548 (X-578, the user's request of 18/11 around 00:15: « first find the rank of every beast: inside the labyrinth, the labyrinth's
// bestiary shows the ranks of every beast one holds »). Real readings: the developer-mode probe of the bestiary's beast page
// (XBMMonsterBookDetail) in dalamud.log of 18/11 00:08 (Cu sith « (8); 5 », the user's capture: « RANG 5 (8) », Exp 12/100, PV 608/608) and
// of 17/11 10:25 (Pugil, not held: no numbers); the beasts' attributes of XBMPetParamGrow 42 (Cu sith) and 21 (Mouton), XbmProbe 18/11
// (docs/evidence/0.1548-beast-book.md).
internal static class BeastBook1548Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // XBMPetParamGrow rows 42 and 21, ranks 1 to 8 (Strength, Intelligence, Physical resistance, Magical resistance, Vitality).
    private static readonly BeastStats[] CuSith = [new(127, 78, 77, 73, 103), new(133, 82, 79, 76, 108), new(140, 87, 82, 78, 113), new(147, 91, 85, 81, 118),
        new(154, 95, 88, 83, 125), new(161, 100, 90, 86, 131), new(168, 104, 93, 89, 137), new(175, 108, 96, 91, 144)];
    private static readonly BeastStats[] Mouton = [new(50, 50, 91, 78, 113), new(55, 55, 94, 81, 119), new(60, 60, 97, 83, 125)];

    private static Beast Make(uint id, string name, BeastStats[] ranks) => new(id, name, 0, "", "", "", "", ranks, "", 0);
    private static readonly Beast[] Beasts = [Make(1, "Cu sith", CuSith), Make(3, "Mouton", Mouton), Make(4, "Pugil", CuSith)];

    private static string[] Texts(string line) => line.Split(';');

    // The probe's lines, texts only (the game's glyph before the attack type left out).
    private const string CuSithPage = "0/3;Satiété;83;Rés. magique;95;Intelligence;125;Vitalité;88;Rés. physique;154;Force;608/608;PV;12/100;Exp;(8);5;;Rang;"
        + "Zone d'apparition;Aire d'effet;Attaque physique tranchante;Griffure;Assaut (Nv 8);Cible unique;6 Attaques physiques tranchantes;Griffures multiples;"
        + "Salve (Nv 18);Cuir de thérien;Perforant;Attaque auto.;Thériens;Classe;L;M;S;Taille;Cu sith;N°1;Assimilation (Nv 22)";
    private const string MoutonPage = "0/4;Satiété;78;Rés. magique;50;Intelligence;113;Vitalité;91;Rés. physique;50;Force;550/550;PV;0/100;Exp;(1);1;Rang;"
        + "Zone d'apparition;Aire d'effet;Attaque physique contondante;Derrière laineux;Assaut (Nv 8);Aire d'effet;Berceuse;Salve (Nv 18);Cuir de thérien;"
        + "Contondant;Attaque auto.;Thériens;Classe;L;M;S;Taille;Mouton;N°3;Assimilation (Nv 22)";
    private const string PugilPage = "Satiété;PV;Exp;Rang;La nature a doté ce poisson d'une poche qu'il peut rempl;Noscea centrale;Zone d'apparition;Aire d'effet;"
        + "Attaque magique d'eau;Tournevis;Assaut (Nv 8);Aire d'effet;Mur hydraulique;Salve (Nv 18);Vague répressive;Eau;Attaque auto.;Hydrides;Classe;L;M;S;Taille;Pugil;N°4;Assimilation (Nv 22)";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast book 1548: the real pages: Cu sith own rank 8 synchronised at 5 (the capture), its attributes those of rank 5 in the sheet; Mouton rank 1", () =>
        {
            var cuSith = BeastTeams.ParseBook(Texts(CuSithPage), Beasts);
            Check(cuSith is { Beast: 1, Rank: 8, Synced: 5, Hp: 608, Exp: 12 }, $"Cu sith: {cuSith}");
            Check(cuSith!.Stats == CuSith[4], "the page's attributes are the sheet's at the synchronised rank (Force 154, Rés. physique 88, Vitalité 125, Intelligence 95, Rés. magique 83)");
            var mouton = BeastTeams.ParseBook(Texts(MoutonPage), Beasts);
            Check(mouton is { Beast: 3, Rank: 1, Synced: 1, Hp: 550, Exp: 0 } && mouton.Stats == Mouton[0], $"Mouton: {mouton}");
        }),
        ("Beast book 1548: a beast not held, a page out of shape, a rank beyond the beast's, two names: nothing read", () =>
        {
            Check(BeastTeams.ParseBook(Texts(PugilPage), Beasts) is null, "Pugil not held: no numbers");
            Check(BeastTeams.ParseBook(Texts(CuSithPage.Replace("154;Force", "?;Force")), Beasts) is null, "an attribute not a number");
            Check(BeastTeams.ParseBook(Texts(CuSithPage.Replace("608/608;PV", "608;PV")), Beasts) is null, "HP not « now/most »");
            Check(BeastTeams.ParseBook(Texts(CuSithPage.Replace("(8);5", "(9);5")), Beasts) is null, "rank 9 for a beast of 8 ranks");
            Check(BeastTeams.ParseBook(Texts(CuSithPage.Replace("(8);5", "(4);5")), Beasts) is null, "synchronised above its own rank");
            Check(BeastTeams.ParseBook(Texts(CuSithPage.Replace("Thériens", "Mouton")), Beasts) is null, "two beasts named");
            Check(BeastTeams.ParseBook(Texts(CuSithPage.Replace("0/3;Satiété;", "")), Beasts) is null, "texts missing before the rank");
        }),
        ("Beast book 1548: the bestiary's rank is the beast's own: a floor becomes exact, a rank known higher never goes down, nothing to write twice", () =>
        {
            var read = BeastTeams.ParseBook(Texts(CuSithPage), Beasts)!;
            var floor = new BeastTeams.SavedPlans([], [], new() { [1] = 5 }, null, [1]);
            var exact = BeastTeams.WithBook(floor, read)!;
            Check(exact.Ranks![1] == 8 && !exact.Floors!.Contains(1u) && exact.Book![1] == read, "« at least 5 » becomes 8, the page kept");
            var same = new BeastTeams.SavedPlans([], [], new() { [1] = 8 }, null, [1]);
            Check(BeastTeams.WithBook(same, read) is { } sure && sure.Ranks![1] == 8 && !sure.Floors!.Contains(1u), "« at least 8 » read 8: exact now");
            var higher = BeastTeams.WithBook(new BeastTeams.SavedPlans([], [], new() { [1] = 9 }), read)!;
            Check(higher.Ranks![1] == 9, "known 9: stays 9 (X-561)");
            Check(BeastTeams.WithBook(exact, read) is null, "the same page again: nothing to save");
            var older = exact with { Book = new() { [1] = read with { Rank = 9 } } };
            Check(BeastTeams.WithBook(older, read) is null || BeastTeams.WithBook(older, read)!.Book![1].Rank == 9, "a page of a higher rank is kept");
        }),
        ("Beast book 1548: the pages kept on disk come back; a file of before reads with none", () =>
        {
            var read = BeastTeams.ParseBook(Texts(CuSithPage), Beasts)!;
            var saved = BeastTeams.WithBook(new BeastTeams.SavedPlans([], []), read)!;
            var back = BeastTeams.Read(BeastTeams.Write(saved));
            Check(back is { Book: { } book } && book[1] == read && back.Ranks![1] == 8, "round trip");
            Check(BeastTeams.Read("{\"Plans\":{},\"Keys\":{}}") is { Book: null }, "older file: no pages");
        }),
        ("Beast book 1548: the reader reads the bestiary's beast page and the window merges it; both languages say where ranks are read", () =>
        {
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs"));
            Check(reader.Contains("gui.GetAddonByName(\"XBMMonsterBookDetail\")") && reader.Contains("BeastTeams.ParseBook(texts, known)") && reader.Contains("ReadBook();"), "reader");
            Check((File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"))+File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.Beastmaster.cs"))).Contains("mainWindow.BeastResultRead,mainWindow.BeastBookRead)"), "plugin wiring");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs")).Contains("BeastTeams.WithBook(LoadBeastPlans(), read)"), "window merge");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains(language == "en" ? "read in the game's bestiary" : "dans le bestiaire de dresseur du jeu"), language);
            }
        }),
    ];
}
