namespace TenshoAssistant.Alarms;

/// <summary>
/// X-724 (0.1730, the user's request of 21/11): Tensho's entries in the game's server bar, by the setting that shows each: all of them in
/// Settings › Tracking › Server bar (SettingsIndex), with Tensho's tooltip over them. Pure (tests).
/// </summary>
public static class ServerBarEntries
{
    /// <summary>The label keys of the settings that show an entry (alarms, reminders, finished reminders, currencies, gil of the day, bags,
    /// gear condition, zone and instance, chat alerts, Tensho's state).</summary>
    public static readonly IReadOnlyList<string> LabelKeys=["Advanced.AlarmServerBar","Advanced.ReminderServerBar","Advanced.ReminderFinishedServerBar",
        "Currencies.ServerBar","GilJournal.ServerBar","Advanced.BagsServerBar","Advanced.WearServerBar","Advanced.PlaceServerBar","ChatWindow.ServerBarAlerts","Advanced.StateServerBar"];

    /// <summary>At least one entry is on: Tensho's tooltip over the entries has something to show (its setting is greyed otherwise).</summary>
    public static bool AnyOn(Configuration configuration)
        =>configuration.AlarmServerBar || configuration.ReminderServerBar || configuration.ReminderFinishedServerBar || configuration.CurrencyServerBar
          || configuration.GilServerBar || configuration.BagsServerBar || configuration.WearServerBar || configuration.PlaceServerBar
          || configuration.Chat.ServerBarAlerts || configuration.StateServerBar;
}
