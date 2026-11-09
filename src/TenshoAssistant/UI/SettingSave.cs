namespace TenshoAssistant.UI;

/// <summary>
/// X-76 H3 (0.914): the core of every setting save, kept out of ImGui so it can be tested. The save runs; when it throws, the rollback
/// puts the previous value back in memory (so what is shown matches the file) and the error is returned for the caller to log and show.
/// </summary>
public static class SettingSave
{
    /// <summary>A rollback that keeps the value in memory: it is written by the next save that works.</summary>
    public static readonly Action Keep=()=>{};

    /// <summary>Runs <paramref name="save"/>; on failure runs <paramref name="rollback"/> and returns the error, else null.</summary>
    public static Exception? Run(Action save,Action rollback)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(rollback);
        try{save();return null;}
        catch(Exception error){rollback();return error;}
    }
}
