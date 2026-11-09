// 0.1020 (system plan 4.9, the rest of the release chain): a release always runs every check again, a new major number needs
// -Major (the maintainer's decision), repo.json is read back before it is committed, and the installer's screenshots
// (images/image1..5.png, 730x380 at most) are listed as ImageUrls when they exist.
internal static class ReleaseGuards1020Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static int At(string text,string part)
    {
        var index=text.IndexOf(part,StringComparison.Ordinal);
        Check(index>=0,$"missing: {part}");
        return index;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Release: a new major number stops before the tag unless -Major is given",() =>
        {
            var release=Read("scripts/Release.ps1");
            Check(release.Contains("[switch]$Major",StringComparison.Ordinal),"Release.ps1 takes -Major");
            Check(release.Contains("if ($Major -and $Mode -ne 'Release') { throw",StringComparison.Ordinal),"not with -Commit");
            Check(At(release,"-and -not $Major) { Stop-Release")<At(release,"Step \"git tag -a $tag\""),"major number checked before the tag");
            var verify=Read("scripts/Verify.ps1");
            Check(verify.Contains("[switch]$Major",StringComparison.Ordinal) && verify.Contains("$taskReleaseArgs.Major = $true",StringComparison.Ordinal),"Verify.ps1 passes -Major on");
            Check(verify.Contains("-Major goes with -Release only.",StringComparison.Ordinal),"Verify.ps1 refuses -Major without -Release");
        }),
        ("Release: repo.json is checked before it is written and read back before it is committed",() =>
        {
            var release=Read("scripts/Release.ps1");
            Check(At(release,"Assert-RepoJson $json $version $download")<At(release,"Step \"write repo.json"),"checked in memory first");
            var readBack=At(release,"Assert-RepoJson ([IO.File]::ReadAllText((Join-Path $root 'repo.json'))) $version $download");
            Check(readBack<At(release,"Step \"git commit repo.json\""),"read back before the commit");
            foreach (var key in new[] { "InternalName", "AssemblyVersion", "DownloadLinkInstall", "IconUrl", "ImageUrls" })
                Check(release.Contains(key,StringComparison.Ordinal),$"repo.json check covers {key}");
        }),
        ("Release: screenshots images/image1..5.png, PNG of 730x380 at most, become ImageUrls",() =>
        {
            var release=Read("scripts/Release.ps1");
            Check(release.Contains("$imageWidth = 730; $imageHeight = 380",StringComparison.Ordinal),"installer size");
            Check(release.Contains("foreach ($index in 1..5)",StringComparison.Ordinal) && release.Contains("\"image$index.png\"",StringComparison.Ordinal),"five screenshots at most");
            Check(release.Contains("$size[0] -gt $imageWidth -or $size[1] -gt $imageHeight",StringComparison.Ordinal),"a larger screenshot stops the release");
            Check(release.Contains("if ($imageUrls.Count) { $entry.ImageUrls = $imageUrls }",StringComparison.Ordinal),"listed only when some exist");
            Check(Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant/images"),"image*.png").Length<=5,"no more than five screenshots in the folder");
        }),
        ("Verify: -Release never reuses the stamp of an earlier green run",() =>
        {
            var verify=Read("scripts/Verify.ps1");
            Check(verify.Contains("if (-not $Force -and -not $Release -and (Test-Path -LiteralPath $taskStampFile)",StringComparison.Ordinal),"a release builds and tests again");
        }),
    ];
}
