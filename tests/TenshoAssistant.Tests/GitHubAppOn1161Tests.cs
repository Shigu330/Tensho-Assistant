using TenshoAssistant.Discoveries;

// 0.1161 (X-175): the maintainer created and installed Tensho's GitHub App on 12/11 (permissions checked: Issues and Gists read and write,
// Metadata read). The exchange now signs in through it: no scope asked, its own client id, and a token got through the former OAuth app is
// not taken as one of the App (the exchange forgets it and asks to connect again: GitHubApp1138Tests drives that path).
internal static class GitHubAppOn1161Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("GitHub App 1161: the exchange signs in through the App, without scopes; an OAuth token is not the App's",() =>
        {
            var current=GitHubSignIn.Current;
            Check(current.App && current.ClientId=="Iv23lizJ9lSmYOfu12ML" && current.ClientId==GitHubSignIn.AppClientId,"the App's client id");
            Check(current.Scopes.Length==0 && !current.DeviceCodeFields().ContainsKey("scope") && current.DeviceCodeFields()["client_id"]==GitHubSignIn.AppClientId,"no scope asked");
            Check(!current.Owns(null) && !current.Owns("") && !current.Owns(GitHubSignIn.OAuthClientId),"a token of the OAuth app (or of before 1138) is not the App's");
            Check(current.Owns(GitHubSignIn.AppClientId),"the App's own token");
            Check(current.RefreshFields("r")["client_id"]==GitHubSignIn.AppClientId && !current.RefreshFields("r").ContainsKey("client_secret"),"renewed without any client secret");
            Check(current.ClientId.StartsWith("Iv",StringComparison.Ordinal) && GitHubSignIn.OAuthClientId.StartsWith("Ov",StringComparison.Ordinal),"an App's id (Iv…), not an OAuth app's (Ov…)");
        }),
    ];
}
