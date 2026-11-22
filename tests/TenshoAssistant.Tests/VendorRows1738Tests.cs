using TenshoAssistant.Npcs;

// X-748 (0.1738, idea I-022 chosen by the user on 21/11): an item's vendors on no map folded into one row, hidden by default.
internal static class VendorRows1738Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Vendor rows 1738: behaviour: the vendors on a map first, in their order; the others apart; one row while folded", () =>
        {
            string[] offers = ["Ann (map)", "Housing 1", "Bob (map)", "Housing 2", "Housing 3", "Cid (map)"];
            var (placed, unplaced) = NpcVendorRows.Split(offers, offer => offer.EndsWith("(map)", StringComparison.Ordinal));
            Check(string.Join("|", placed) == "Ann (map)|Bob (map)|Cid (map)" && string.Join("|", unplaced) == "Housing 1|Housing 2|Housing 3", "split, order kept");
            Check(NpcVendorRows.Rows(3, 3, open: false) == 4 && NpcVendorRows.Rows(3, 3, open: true) == 7, "one row folded; every vendor unfolded");
            Check(NpcVendorRows.Rows(3, 0, open: true) == 3 && NpcVendorRows.Rows(0, 20, open: false) == 1, "no fold row without unplaced vendors; twenty in one row (the capture of 17/11)");
            var (none, all) = NpcVendorRows.Split(Array.Empty<string>(), _ => true);
            Check(none.Count == 0 && all.Count == 0, "no offer");
        }),
        ("Vendor rows 1738: wiring: split by the vendor's places, folded at first, a click unfolds; its tooltip; en and fr", () =>
        {
            var page = Read("MainWindow.NpcVendors.cs");
            Check(page.Contains("NpcVendorRows.Split(offers,offer=>VendorLocations(offer.Vendor).Count>0);", StringComparison.Ordinal) && page.Contains("private bool npcVendorsUnplacedOpen;", StringComparison.Ordinal), "split, folded by default");
            Check(page.Contains("npcVendorsUnplacedOpen=!npcVendorsUnplacedOpen;", StringComparison.Ordinal) && page.Contains("Tip(\"NpcUi.UnplacedTip\");", StringComparison.Ordinal)
                && page.Contains("if(npcVendorsUnplacedOpen)for(var index=0;index<unplaced.Count;index++)DrawNpcOfferRow(unplaced[index],placed.Count+index);", StringComparison.Ordinal), "the row, its tooltip, unfolded on a click");
            var fr = Read("Localization/Languages/fr.json"); var en = Read("Localization/Languages/en.json");
            Check(fr.Contains("\"UnplacedClosed\": \"Marchands sans position : {0} (cliquez pour les afficher)\"", StringComparison.Ordinal) && en.Contains("\"UnplacedClosed\": \"Vendors without a position: {0} (click to show them)\"", StringComparison.Ordinal), "en and fr");
        }),
    ];
}
