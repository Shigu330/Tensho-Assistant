using System.Text.Json;
using TenshoAssistant.Chat;

// X-517 (review of 0.1494): the public answer about the chat history says what the history does now: the defaults of ChatHistory and
// the « No limit » boxes of the chat window, in both languages. The numbers and the box's name are read from the code and from the
// window's own texts, so the answer cannot say again a default or a name that is no longer true.
internal static class FaqChatHistory1495Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static string Text(string language, string section, string key)
    {
        using var document = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").TrimStart('﻿'));
        return document.RootElement.GetProperty(section).GetProperty(key).GetString() ?? "";
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("FAQ chat history 1495 (X-517): the answer gives the defaults the history has and says « No limit » exists, in English and French", () =>
        {
            var thousands = ChatHistory.DefaultMessages.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
            foreach (var (language, messages, days) in new[] { ("en", thousands, "days"), ("fr", thousands.Replace(",", " ", StringComparison.Ordinal), "jours") })
            {
                var answer = Text(language, "Help", "AnswerChatHistory");
                var unlimited = Text(language, "ChatWindow", "HistoryMessagesUnlimited");
                Check(answer.Contains($"{messages} ", StringComparison.Ordinal), $"{language}: the default number of messages ({messages})");
                Check(answer.Contains($"{ChatHistory.DefaultDays} {days}", StringComparison.Ordinal), $"{language}: the default number of days ({ChatHistory.DefaultDays})");
                Check(!answer.Contains($"7 {days}", StringComparison.Ordinal), $"{language}: the old default of 7 days is gone");
                Check(answer.Contains($"« {unlimited} »", StringComparison.Ordinal), $"{language}: the box « {unlimited} » of the chat window is named");
                Check(answer.Contains(Text(language, "ChatWindow", "KeepHistory"), StringComparison.Ordinal), $"{language}: the setting that turns the history on is still named as the window names it");
            }
        }),
        ("FAQ chat history 1495 (X-517): docs/FAQ.md carries the same answer", () =>
        {
            var page = Read("docs/FAQ.md");
            foreach (var language in new[] { "en", "fr" })
                Check(page.Contains("\n" + Text(language, "Help", "AnswerChatHistory") + "\n", StringComparison.Ordinal), $"{language}: docs/FAQ.md is out of date (python -I -X utf8 tools/faq.py write)");
        }),
    ];
}
