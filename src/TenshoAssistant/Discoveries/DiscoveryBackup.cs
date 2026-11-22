namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-76 step 3 (0.945, user request): the zip copy of the Discoveries folder taken before the developer mode's « empty the base », in the
/// plugin's Backups folder; never the GitHub token nor its refresh token (X-175).
/// X-784 (0.1797, review of 0.1795, I1): since 0.1794 the plugin keeps <c>Discoveries.db</c> open for the whole session (SqliteKeeper): the
/// copy read the live file and failed every time (« nothing was removed »). The base now goes in as SQLite's own consistent copy (its journal's
/// saves included, <see cref="DiscoveryDatabase.Snapshot"/>), and the journal files beside it stay out: restored with a newer copy of the base,
/// an old journal could be replayed onto it.
/// X-794 (0.1800, review of 0.1799, M1): the copy is made off the game's thread (<see cref="Start"/>): VACUUM INTO of the whole base, then the
/// zip, took seconds with a big base, and the button made it in Draw (the game froze meanwhile).
/// </summary>
public static class DiscoveryBackup
{
    public const string Label="discoveries";

    /// <summary>The files never copied: both tokens, and with a database, its journal (-wal) and shared memory (-shm).</summary>
    public static IReadOnlySet<string> Skipped(bool database)=>database
        ? new HashSet<string>(GitHubExchange.SecretFiles,StringComparer.OrdinalIgnoreCase){DiscoveryDatabase.FileName+"-wal",DiscoveryDatabase.FileName+"-shm"}
        : GitHubExchange.SecretFiles;

    /// <summary>X-793 (0.1800): the Backups folder beside the Discoveries folder, where the copies and their passing files go.</summary>
    public static string Folder(string folder)=>Path.Combine(Path.GetDirectoryName(folder) ?? folder,"Backups");

    /// <summary>X-794 (0.1800): <see cref="Create"/> on a worker; the window reads the task with <see cref="Outcome"/>.</summary>
    public static Task<string?> Start(string folder,DiscoveryDatabase? database,DateTimeOffset at)=>Task.Run(()=>Create(folder,database,at));

    /// <summary>X-794 (0.1800): the copy started by the button as the window reads it: not ended (nothing removed yet); ended well (Empty: the
    /// base is emptied, also when there was no folder to copy, as before); failed (nothing removed, the error for the log).</summary>
    public static (bool Ended,bool Empty,Exception? Error) Outcome(Task<string?> copy)=>
        !copy.IsCompleted ? (false,false,null)
        : copy.IsCompletedSuccessfully ? (true,true,null)
        : (true,false,copy.Exception?.GetBaseException() ?? new OperationCanceledException("The copy was cancelled."));

    /// <summary>The copy (its path; null when the folder does not exist); throws when it cannot be made: the caller then removes nothing.</summary>
    public static string? Create(string folder,DiscoveryDatabase? database,DateTimeOffset at)
    {
        var backups=Folder(folder);
        return BackupZip.Create(folder,backups,Label,at,Skipped(database is not null),
            file=>database is not null && string.Equals(Path.GetFullPath(file),database.Path,StringComparison.OrdinalIgnoreCase) ? database.Snapshot(backups):null);
    }
}
