namespace TenshoAssistant.Desktop;

/// <summary>
/// X-285 (0.1300, user's look at the gallery on 13/11): a reference picture must show the page, never an error the desktop application
/// causes (« the sheets of the allied societies could not be read »: the application had not given the page the sheets it reads). While a
/// picture is taken, the keys of the texts the page asks are listened to (LocalizationManager.KeyRead); a key of an error or « unavailable »
/// text means the picture protects an error state: images.py refuses the publication. Pure, tested (ReferenceErrors1300Tests).
/// </summary>
public static class ReferenceErrors
{
    /// <summary>The last part of a key that names an error or unavailable state: « Allied.SheetsUnavailable », « Weather.Error »,
    /// « Squadron.ReadFailed », « Common.StateFailed ». Tooltips and settings that only name an error (« …ErrorTip ») are not states.</summary>
    public static bool IsError(string key)
    {
        var last=key[(key.LastIndexOf('.')+1)..];
        return last.EndsWith("Unavailable",StringComparison.Ordinal) || last.EndsWith("Error",StringComparison.Ordinal)
            || last.EndsWith("Failed",StringComparison.Ordinal) || last=="StateFailed";
    }

    /// <summary>The last part of a key that names a wait (« DeepDungeon.Loading », « Common.StateWaiting »): in a still picture taken after
    /// the catalogues loaded, a wait that never ends (the application did not give the page what it waits for).</summary>
    public static bool IsWaiting(string key)
    {
        var last=key[(key.LastIndexOf('.')+1)..];
        return last.EndsWith("Loading",StringComparison.Ordinal) || last=="StateWaiting";
    }

    /// <summary>The error keys among those asked by the pictured frame (and its waiting keys, unless it is a loading picture), sorted,
    /// each once.</summary>
    public static string[] Shown(IEnumerable<string> keys,bool loadingPicture=false)=>
        keys.Where(key=>IsError(key) || (!loadingPicture && IsWaiting(key))).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
}
