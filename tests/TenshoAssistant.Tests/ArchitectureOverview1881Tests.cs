using System.Text.RegularExpressions;

// X-874 (0.1881, review of 0.1879, M1): ARCHITECTURE.md's overview said « a single DLL, plus the native library of the optional Raphael
// solver », while the package also ships Microsoft.Data.Sqlite and SQLitePCLRaw (batteries_v2, core, provider.e_sqlite3) and the native
// e_sqlite3.dll. Every NuGet package of the plugin is now named in the overview of both halves, with the DLLs the package holds.
internal static class ArchitectureOverview1881Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");

    // The overview: from its heading to the table of files under it.
    private static string Overview(string text,string heading)
    {
        var from=text.IndexOf(heading,StringComparison.Ordinal);
        Check(from>=0,"the overview: "+heading);
        var to=text.IndexOf("\n| ",from,StringComparison.Ordinal);
        return text[from..to];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("X-874: the overview of ARCHITECTURE names every library the plugin's package ships, in both halves",() =>
        {
            var text=Read("docs/project/ARCHITECTURE.md");
            var half=text.IndexOf("\n---\n",StringComparison.Ordinal);
            var packages=Regex.Matches(Read("src/TenshoAssistant/TenshoAssistant.csproj"),@"<PackageReference Include=""([^""]+)""").Select(m=>m.Groups[1].Value).ToArray();
            Check(packages.Contains("Microsoft.Data.Sqlite") && packages.Contains("Raphael.Net"),"the packages are read: "+string.Join(", ",packages));
            // The bundle is a package of packages: the package holds its parts, named one by one.
            var dlls=new[]{"TenshoAssistant.dll","Raphael.Net.dll","raphael_bindings.dll","Microsoft.Data.Sqlite.dll","SQLitePCLRaw.batteries_v2.dll",
                "SQLitePCLRaw.core.dll","SQLitePCLRaw.provider.e_sqlite3.dll","e_sqlite3.dll"};
            foreach(var (overview,language) in new[]{(Overview(text[..half],"## Overview"),"English"),(Overview(text[half..],"## Vue d'ensemble"),"French")})
            {
                Check(!overview.Contains("a single DLL") && !overview.Contains("une seule DLL"),$"ARCHITECTURE ({language}): no « single DLL »");
                foreach(var dll in dlls)Check(overview.Contains($"`{dll}`"),$"ARCHITECTURE ({language}): the overview names {dll}");
                foreach(var package in packages.Where(p=>p!="SQLitePCLRaw.bundle_e_sqlite3"))
                    Check(overview.Contains($"`{package}.dll`"),$"ARCHITECTURE ({language}): the package {package}");
                Check(overview.Contains("src/TenshoAssistant.Desktop"),$"ARCHITECTURE ({language}): the desktop application in the overview");
            }
        }),
    ];
}
