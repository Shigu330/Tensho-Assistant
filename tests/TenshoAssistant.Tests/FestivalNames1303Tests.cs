// 0.1303 (X-293, the user's decision of 13/11 after LuminaSupplemental 7.0.1 « Added additional Festival Names »): the embedded festival
// names copied again from the file 7.0.1 changed (its ManualData, the generator's input: the published Generated file did not have the
// new names yet), values kept as published, with the source, the commit, the original file's SHA-256 and the GPL-3.0 notice (R05).
internal static class FestivalNames1303Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Festival names 1303: copied again from LuminaSupplemental 7.0.1, notice kept, the new names read", () =>
        {
            var lines = File.ReadAllLines(Path.GetFullPath("src/TenshoAssistant/Events/Data/festival-names.csv"));
            var notice = string.Join("\n", lines.TakeWhile(line => line.StartsWith('#')));
            Check(notice.Contains("# Source: LuminaSupplemental, src/LuminaSupplemental.SpaghettiGenerator/ManualData/FestivalName.csv, commit ef4372ebdc78684dae430705e6c619b287661ded", StringComparison.Ordinal), "source and commit");
            Check(notice.Contains("original file SHA-256 50113f38849ec74e576a8e814131f0046b43ac3b2d958bdbf0b9f7dff1fa6ef7", StringComparison.Ordinal), "the original file's hash");
            Check(notice.Contains("GNU General Public License v3.0 (GPL-3.0), Copyright (C) Critical-Impact and LuminaSupplemental contributors.", StringComparison.Ordinal)
                  && notice.Contains("There is no warranty", StringComparison.Ordinal), "the licence notice (R05)");
            var rows = lines.SkipWhile(line => line.StartsWith('#')).ToArray();
            Check(rows[0] == "FestivalId,Name", "the header as published");
            var names = rows.Skip(1).Select(line => line.Split(',', 2)).ToDictionary(cells => int.Parse(cells[0], System.Globalization.CultureInfo.InvariantCulture), cells => cells[1]);
            Check(names[256] == "Unmoored Isle" && names[257] == "Keybound Brawler", "the names added in 7.0.1");
            Check(names.Count == 134 && names[258] == "Starlight Celebration (2026)" && names[0] == "None", "the other rows kept");
        }),
    ];
}
