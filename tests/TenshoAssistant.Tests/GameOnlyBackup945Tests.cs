using System.IO.Compression;
using TenshoAssistant;

// X-76 step 3 (0.945): « game and observed data only » keeps and gives back each source's choice; a zip copy before emptying, token never copied, 5 kept.
internal static class GameOnlyBackup945Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game data only 945: all off, choices kept, given back",() =>
        {
            var c=new Configuration{CommunityLumina=true,CommunityGarland=false,CommunityWiki=true,CommunityGatherBuddy=true,CommunityTeamcraftBites=false,CommunityFashion=true,CommunityUniversalis=true};
            var bits=CommunitySources.Bits(c);
            CommunitySources.ApplyGameDataOnly(c,true);
            Check(c.GameDataOnly && CommunitySources.Bits(c)==0 && c.CommunityBeforeGameOnly==bits,"all off, kept");
            CommunitySources.ApplyGameDataOnly(c,true);
            Check(c.CommunityBeforeGameOnly==bits,"twice changes nothing");
            CommunitySources.ApplyGameDataOnly(c,false);
            Check(!c.GameDataOnly && CommunitySources.Bits(c)==bits && c.CommunityBeforeGameOnly is null && !c.CommunityGarland && c.CommunityWiki,"given back");
            Check(TenshoAssistant.ConfigurationReset.Reset.Contains("GameDataOnly") && TenshoAssistant.ConfigurationReset.Reset.Contains("CommunityBeforeGameOnly"),"reset list");
        }),
        ("Backup zip 945: copy before emptying, token never copied, five kept",() =>
        {
            var folder=Path.Combine(Path.GetTempPath(),"tensho-backup-"+Guid.NewGuid().ToString("N"));
            try
            {
                var source=Path.Combine(folder,"Discoveries");Directory.CreateDirectory(Path.Combine(source,"Shared"));
                File.WriteAllText(Path.Combine(source,"Discoveries.json"),"{}");File.WriteAllText(Path.Combine(source,"Shared","kind-10.json.gz"),"x");File.WriteAllText(Path.Combine(source,"github.token"),"secret");
                var backups=Path.Combine(folder,"Backups");var at=new DateTimeOffset(2026,11,8,20,0,0,TimeSpan.Zero);
                var path=BackupZip.Create(source,backups,"discoveries",at,new HashSet<string>{"github.token"})!;
                using(var zip=ZipFile.OpenRead(path))
                {
                    var names=zip.Entries.Select(entry=>entry.FullName).ToArray();
                    Check(names.Contains("Discoveries.json") && names.Contains("Shared/kind-10.json.gz") && !names.Any(name=>name.Contains("token")),"content: "+string.Join(",",names));
                }
                for(var index=1;index<=7;index++)BackupZip.Create(source,backups,"discoveries",at.AddMinutes(index));
                Check(Directory.GetFiles(backups,"backup-*-discoveries.zip").Length==BackupZip.Keep && !Directory.GetFiles(backups,"*.tmp").Any(),"five kept");
                Check(BackupZip.Create(Path.Combine(folder,"missing"),backups,"x",at) is null,"missing folder");
            }
            finally{if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }),
    ];
}
