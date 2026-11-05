using System.Text.Json;
using System.Text.RegularExpressions;

namespace TenshoAssistant.Alarms;

/// <summary>Where an alarm comes from, for the Discord categories (R-12, 0.319).</summary>
// 0.344 (g, I-02): Currency added last, so saved alarms keep their category.
public enum AlarmSource { Other, Gathering, Fishing, Reminder, Voyage, Currency, DutyPop }

/// <summary>
/// Discord webhook settings (R-12, 0.319, user request 2026-11-02): off by default. The webhook address is a secret (anyone holding it
/// can post in the channel): it is stored in the plugin's configuration on this PC, never written to the logs nor to the diagnostic
/// report, and shown masked. Nothing is sent before the player sets an address and turns the setting on.
/// </summary>
public sealed class DiscordSettings
{
    public bool Enabled { get; set; }
    // 0.578 (user request): rows of the Reminders page whose alarms stay off Discord.
    public List<string>? ReminderRowsOff { get; set; }
    public string WebhookUrl { get; set; } = "";
    // 0.598 (T24): another channel for a category; without one, the address above. Secrets like it: stored here only, never logged nor reported.
    public Dictionary<AlarmSource,string>? CategoryUrls { get; set; }
    public string Username { get; set; } = "Tensho";
    // Empty = the localized default message. Placeholders: [label], [minutes], [time], [character].
    public string Template { get; set; } = "";
    public bool Gathering { get; set; } = true;
    public bool Fishing { get; set; } = true;
    public bool Reminders { get; set; } = true;
    public bool Voyages { get; set; } = true;
    public bool Other { get; set; } = true;
    // 0.344 (g, I-02): currency threshold alerts.
    public bool Currencies { get; set; } = true;
    // 0.366 (g, C-10): « duty ready » alerts (sent only when the alert itself is on, Alarms › Duty ready).
    public bool DutyPop { get; set; } = true;
    // The character's name is personal data: added only when the player asks for it.
    public bool IncludeCharacter { get; set; }
    // Discord role id (snowflake) mentioned at the start of each message; empty = no mention.
    public string MentionRoleId { get; set; } = "";
}

/// <summary>Pure rules of the Discord webhook: accepted addresses, message text and the JSON body Discord receives.</summary>
public static class DiscordWebhook
{
    public const int ContentMaximum = 2000, UsernameMaximum = 80;
    private static readonly HashSet<string> Hosts = new(StringComparer.OrdinalIgnoreCase) { "discord.com", "discordapp.com", "ptb.discord.com", "canary.discord.com" };
    // ASCII digits only: « \d » would also match full-width and other Unicode digits, which Discord refuses.
    private static readonly Regex WebhookPath = new(@"^/api/(?:v[0-9]{1,2}/)?webhooks/([0-9]{17,20})/([A-Za-z0-9_\-]{20,100})/?$", RegexOptions.CultureInvariant);
    private static readonly Regex Snowflake = new(@"^[0-9]{17,20}$", RegexOptions.CultureInvariant);
    private static readonly Regex RoleMention = new(@"^<@&([0-9]{17,20})>$", RegexOptions.CultureInvariant);

    /// <summary>
    /// A Discord webhook address: HTTPS on Discord's own hosts, default port, no user info, query or fragment, path
    /// /api/[vN/]webhooks/&lt;id&gt;/&lt;token&gt;. Anything else is refused, so Tensho never posts to another server.
    /// </summary>
    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps && uri.IsDefaultPort && Hosts.Contains(uri.Host) && uri.UserInfo.Length == 0
            && uri.Query.Length == 0 && uri.Fragment.Length == 0 && WebhookPath.IsMatch(uri.AbsolutePath);
    }

    /// <summary>The address with its token hidden (« …/webhooks/1234…/*** »), for display; empty when not a webhook address.</summary>
    public static string Redact(string? url)
    {
        if (!IsValidUrl(url)) return "";
        var uri = new Uri(url!.Trim());
        var id = WebhookPath.Match(uri.AbsolutePath).Groups[1].Value;
        return $"https://{uri.Host}/api/webhooks/{id[..4]}…/***";
    }

    /// <summary>0.598 (T24): where a category posts: its own address when it has a valid one, else the main one.</summary>
    public static string UrlFor(DiscordSettings settings, AlarmSource source)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.CategoryUrls?.GetValueOrDefault(source) is { } own && IsValidUrl(own) ? own : settings.WebhookUrl;
    }

    public static bool IsValidRoleId(string? id) => id is not null && Snowflake.IsMatch(id.Trim());

    /// <summary>A role id typed or pasted (the bare id, or the mention « &lt;@&amp;id&gt; » Discord copies); "" to remove; null when not a role id.</summary>
    public static string? RoleIdFrom(string? text)
    {
        var trimmed = (text ?? "").Trim();
        if (trimmed.Length == 0) return "";
        if (Snowflake.IsMatch(trimmed)) return trimmed;
        var mention = RoleMention.Match(trimmed);
        return mention.Success ? mention.Groups[1].Value : null;
    }

    /// <summary>
    /// Source of an alarm made before 0.319 (no stored source): a calendar reminder or a vessel return is recognized by its group,
    /// the localized title of its tool; anything else stays as it is. Returns true when the alarm was changed.
    /// </summary>
    public static bool AssignLegacySource(TenshoAlarm alarm, string remindersTitle, string voyagesTitle)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        if (alarm.Source != AlarmSource.Other || alarm.JobId != 0 || alarm.ItemId != 0 || alarm.Group.Length == 0) return false;
        if (alarm.Group == remindersTitle) alarm.Source = AlarmSource.Reminder;
        else if (alarm.Group == voyagesTitle) alarm.Source = AlarmSource.Voyage;
        else return false;
        return true;
    }

    /// <summary>Category of an alarm: its stored source, else its gathering job (16 miner, 17 botanist, 18 fisher), else other.</summary>
    public static AlarmSource SourceOf(TenshoAlarm alarm)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        if (alarm.Source != AlarmSource.Other) return alarm.Source;
        return alarm.JobId switch { 16 or 17 => AlarmSource.Gathering, 18 => AlarmSource.Fishing, _ => AlarmSource.Other };
    }

    public static bool CategoryOn(DiscordSettings settings, AlarmSource source) => source switch
    {
        AlarmSource.Gathering => settings.Gathering, AlarmSource.Fishing => settings.Fishing, AlarmSource.Reminder => settings.Reminders,
        AlarmSource.Voyage => settings.Voyages, AlarmSource.Currency => settings.Currencies,
        AlarmSource.DutyPop => settings.DutyPop, _ => settings.Other,
    };

    /// <summary>0.344: whether an alert that is not a Tensho alarm (currency thresholds) goes to Discord: setting on, valid address, category on.</summary>
    public static bool ShouldSend(DiscordSettings? settings, AlarmSource source) =>
        settings is { Enabled: true } && IsValidUrl(UrlFor(settings, source)) && CategoryOn(settings, source);

    /// <summary>0.578: the Reminders row of an alarm: the one that made it, or the deadline an automatic one follows. Empty: none.</summary>
    public static string ReminderRowOf(TenshoAlarm alarm)
    {
        ArgumentNullException.ThrowIfNull(alarm);
        if (alarm.ReminderRow.Length > 0) return alarm.ReminderRow;
        if (alarm.AutoKey == "squadron:mission") return "SquadronMission";
        if (alarm.AutoKey == "squadron:training") return "SquadronTraining";
        if (alarm.AutoKey.StartsWith("venture:", StringComparison.Ordinal)) return "Ventures";
        if (alarm.AutoKey.StartsWith("vessel:", StringComparison.Ordinal)) return "Voyages";
        return "";
    }
    public static bool ReminderRowOn(DiscordSettings settings, TenshoAlarm alarm) => settings.ReminderRowsOff?.Contains(ReminderRowOf(alarm)) != true;

    /// <summary>Whether a ringing alarm goes to Discord: setting on, valid address, its category on, and its switch: for a reminder, the box of its row in Reminders (0.584: the only one), else the alarm's own.</summary>
    public static bool ShouldSend(DiscordSettings? settings, TenshoAlarm alarm) =>
        settings is { Enabled: true } && IsValidUrl(UrlFor(settings, SourceOf(alarm))) && (ReminderRowOf(alarm).Length > 0 ? ReminderRowOn(settings, alarm) : alarm.Discord) && CategoryOn(settings, SourceOf(alarm));

    /// <summary>
    /// The message text: the alarm's own message, else the template, else the default; placeholders replaced, [character] only when
    /// allowed (else removed), the role mention first when set, cut to Discord's 2000 characters.
    /// </summary>
    /// <summary>Eorzea time at this moment, HH:mm (one Eorzean hour lasts 175 seconds).</summary>
    public static string EorzeaClock(DateTimeOffset moment)
    {
        var minutes=moment.ToUnixTimeMilliseconds()*3600/175/1000/60%1440;
        return $"{minutes/60:00}:{minutes%60:00}";
    }

    public static string Format(DiscordSettings settings, string defaultTemplate, string? alarmMessage, string label, int minutes, DateTimeOffset localTime, string? character, string? zone = null, string? item = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var template = !string.IsNullOrWhiteSpace(alarmMessage) ? alarmMessage! : !string.IsNullOrWhiteSpace(settings.Template) ? settings.Template : defaultTemplate;
        var who = settings.IncludeCharacter ? character ?? "" : "";
        var text = template.Replace("[label]", label, StringComparison.Ordinal).Replace("[minutes]", Math.Max(0, minutes).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("[time]", localTime.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("[character]", who, StringComparison.Ordinal)
            // 0.564 (T18): Discord writes these two in each reader's own time zone; the Eorzea time and the names of the zone and the item.
            .Replace("[discordtime]", $"<t:{localTime.ToUnixTimeSeconds()}:t>", StringComparison.Ordinal)
            .Replace("[relative]", $"<t:{localTime.ToUnixTimeSeconds()}:R>", StringComparison.Ordinal)
            .Replace("[et]", EorzeaClock(localTime), StringComparison.Ordinal)
            .Replace("[zone]", zone ?? "", StringComparison.Ordinal)
            .Replace("[item]", item ?? "", StringComparison.Ordinal).Trim();
        if (settings.IncludeCharacter && who.Length > 0 && !template.Contains("[character]", StringComparison.Ordinal)) text = $"{text} — {who}";
        if (IsValidRoleId(settings.MentionRoleId)) text = $"<@&{settings.MentionRoleId.Trim()}> {text}";
        return text.Length <= ContentMaximum ? text : text[..(ContentMaximum - 1)] + "…";
    }

    /// <summary>Sender name shown by Discord: 1–80 characters, and never one Discord refuses (containing « discord » or « clyde »).</summary>
    public static string Username(string? name)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0 || trimmed.Contains("discord", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("clyde", StringComparison.OrdinalIgnoreCase)) return "Tensho";
        return trimmed.Length <= UsernameMaximum ? trimmed : trimmed[..UsernameMaximum];
    }

    /// <summary>
    /// The JSON body: content and sender name; « allowed_mentions » parses nothing, so a label containing @everyone, @here or a user
    /// mention never pings anyone; only the configured role can be mentioned.
    /// </summary>
    public static string Payload(DiscordSettings settings, string content)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var roles = IsValidRoleId(settings.MentionRoleId) ? new[] { settings.MentionRoleId.Trim() } : [];
        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["content"] = content, ["username"] = Username(settings.Username),
            ["allowed_mentions"] = new Dictionary<string, object> { ["parse"] = Array.Empty<string>(), ["roles"] = roles },
        });
    }
}
