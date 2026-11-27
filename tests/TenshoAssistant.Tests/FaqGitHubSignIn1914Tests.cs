using System.Text.Json;
using TenshoAssistant.Help;

// X-893 (D-239, decision of 27/11): the known limit of the discoveries' GitHub sign-in is said in the FAQ, in English and French, and on
// docs/FAQ.md: a renewed sign-in the disk refused to write while the game crashed means connecting GitHub again, and nothing else is lost.
// The button names are read from the Discoveries page's own texts, so the answer cannot name a button that no longer exists.
internal static class FaqGitHubSignIn1914Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");
    private static string Text(string language, string section, string key)
    {
        using var document = JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").TrimStart('\uFEFF'));
        return document.RootElement.GetProperty(section).GetProperty(key).GetString() ?? "";
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("FAQ GitHub sign-in 1914 (X-893): the entry is in the FAQ list, opens the Discoveries page and says the known limit in English and French", () =>
        {
            Check(Faq.PageOf("GitHubSignIn") == "Discoveries", "the entry GitHubSignIn is in Faq.Entries with the Discoveries page");
            foreach (var (language, limit, crash, again, nothing) in new[]
            {
                ("en", "Known limit:", "the game crashes", "connect GitHub again", "Nothing else is lost"),
                ("fr", "Limite connue :", "le jeu plante", "connectez GitHub à nouveau", "Rien d'autre n'est perdu"),
            })
            {
                var question = Text(language, "Help", "QuestionGitHubSignIn");
                var answer = Text(language, "Help", "AnswerGitHubSignIn");
                Check(question.Length > 0, $"{language}: the question");
                foreach (var part in new[] { limit, crash, again, nothing })
                    Check(answer.Contains(part, StringComparison.Ordinal), $"{language}: the answer says « {part} »");
                Check(answer.IndexOf(limit, StringComparison.Ordinal) < answer.IndexOf(crash, StringComparison.Ordinal), $"{language}: the crash is part of the known limit");
                foreach (var button in new[] { "Connect", "Disconnect" })
                    Check(answer.Contains($"« {Text(language, "DiscoveriesUi", button)} »", StringComparison.Ordinal), $"{language}: the button {button} is named as the page names it");
            }
        }),
        ("FAQ GitHub sign-in 1914 (X-893): docs/FAQ.md carries the question and the answer in both languages", () =>
        {
            var page = Read("docs/FAQ.md");
            foreach (var language in new[] { "en", "fr" })
            {
                Check(page.Contains(Text(language, "Help", "QuestionGitHubSignIn"), StringComparison.Ordinal), $"{language}: the question is on docs/FAQ.md (python -I -X utf8 tools/faq.py write)");
                Check(page.Contains("\n" + Text(language, "Help", "AnswerGitHubSignIn") + "\n", StringComparison.Ordinal), $"{language}: docs/FAQ.md is out of date (python -I -X utf8 tools/faq.py write)");
            }
        }),
    ];
}
