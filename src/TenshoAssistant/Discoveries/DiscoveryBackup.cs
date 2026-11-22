namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-76 step 3 (0.945, user request): the zip copy of the Discoveries folder taken before the developer mode's « empty the base », in the
/// plugin's Backups folder; never the GitHub token nor its refresh token (X-175).
/// X-784 (0.1797, review of 0.1795, I1): since 0.1794 the plugin keeps <c>Discoveries.db</c> open for the whole session (SqliteKeeper): the
/// copy read the live file and failed every time (« nothing was removed »). The base now goes in as SQLite's own consistent copy (its journal's
/// saves included, <see cref="DiscoveryDatabase.Snapshot"/>), and the journal files beside it stay out: restored with a newer copy of the base,
/// an old journal could be replayed onto it.
/// </summary>
public static class DiscoveryBackup
{
    public const string Label="discoveries";

    /// <summary>The files never copied: both tokens, and with a database, its journal (-wal) and shared memory (-shm).</summary>
    public static IReadOnlySet<string> Skipped(bool database)=>database
        ? new HashSet<string>(GitHubExchange.SecretFiles,StringComparer.OrdinalIgnoreCase){DiscoveryDatabase.FileName+"-wal",DiscoveryDatabase.FileName+"-shm"}
        : GitHubExchange.SecretFiles;

    /// <summary>The copy (its path; null when the folder does not exist); throws when it cannot be made: the caller then removes nothing.</summary>
    public static string? Create(string folder,DiscoveryDatabase? database,DateTimeOffset at)
    {
        var backups=Path.Combine(Path.GetDirectoryName(folder) ?? folder,"Backups");
        return BackupZip.Create(folder,backups,Label,at,Skipped(database is not null),
            file=>database is not null && string.Equals(Path.GetFullPath(file),database.Path,StringComparison.OrdinalIgnoreCase) ? database.Snapshot(backups):null);
    }
}
