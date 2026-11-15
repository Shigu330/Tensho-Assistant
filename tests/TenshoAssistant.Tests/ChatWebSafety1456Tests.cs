using TenshoAssistant.Chat;

// 0.1456 (X-451, security): a web address in Tensho's chat window never hides where it leads: a user part before the host
// (« https://www.mogstation.com@evil.org/ ») leaves the word as text; a host in letters of other alphabets is kept in the form the browser
// reaches (« xn--… »), shown as the site with how it reads.
internal static class ChatWebSafety1456Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Chat web 1456: a user part before the host, even empty, leaves the word as text", () =>
        {
            foreach (var text in new[] { "https://www.mogstation.com@evil.org/login", "https://@evil.org/", "https://user:pw@site.com/",
                                         "https://evil.org%2F@good.com/", "www.mogstation.com@evil.org", "http://a.b@c.org" })
            {
                Check(ChatWeb.Address(text) is null, "no address: " + text);
                Check(ChatWeb.Split("look " + text + " now").All(part => part.Url is null), "text only: " + text);
            }
            Check(ChatWeb.Address("https://site.org/contact@me") == "https://site.org/contact@me", "a « @ » in the path is no user part");
        }),
        ("Chat web 1456: a host in other alphabets is kept as the browser reaches it, and said with how it reads", () =>
        {
            var lookalike = "https://www.m\u043egstation.com/login?a=1#top";   // a Cyrillic « o »
            var url = ChatWeb.Address(lookalike);
            Check(url == "https://www.xn--mgstation-02h.com/login?a=1#top", "ASCII host: " + url);
            var site = ChatWeb.Site(url!);
            Check(site is { Foreign: true, Host: "www.xn--mgstation-02h.com" } && site.Reads == "www.m\u043egstation.com", "site: " + site);
            Check(ChatWeb.Split("buy gil at " + lookalike).Single(part => part.Url is not null).Url == url, "the window keeps the same address");
            var plain = ChatWeb.Site(ChatWeb.Address("https://www.mogstation.com/")!);
            Check(plain is { Foreign: false, Host: "www.mogstation.com", Reads: null }, "a plain site: " + plain);
            Check(ChatWeb.Site(ChatWeb.Address("https://xn--mgstation-07a.com/")!) is { Foreign: true }, "a « xn-- » name written as such is said too");
            Check(ChatWeb.Address("https://Site.COM:8443/p") == "https://site.com:8443/p" && ChatWeb.Address("https://site.com:443/p") == "https://site.com/p", "ports");
        }),
        ("Chat web 1456: the tooltip and the menu say the site; the tooltip is made only under the mouse", () =>
        {
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.ChatWindow.cs")).Replace("\r\n", "\n");
            Check(window.Contains("if(ImGui.IsItemHovered()){ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);TipText(WebLinkTip(link.Url));}", StringComparison.Ordinal), "tooltip only under the mouse");
            Check(!window.Contains("TipText(localization.Format(\"ChatWindow.WebLinkTip\"", StringComparison.Ordinal), "no tooltip made at each frame");
            var menu = window.IndexOf("ImGui.BeginPopup(\"###TenshoChatWeb\")", StringComparison.Ordinal);
            var site = window.IndexOf("localization.Format(\"ChatWindow.WebSite\",site.Host)", menu, StringComparison.Ordinal);
            Check(menu > 0 && site > menu && window.IndexOf("Dalamud.Utility.Util.OpenLink(address)", menu, StringComparison.Ordinal) > site, "the site before « open »");
            Check(window.Contains("if(site.Foreign)ImGui.TextColored(Error,localization.Format(\"ChatWindow.WebForeign\"", StringComparison.Ordinal), "the warning on a foreign name");
        }),
    ];
}
