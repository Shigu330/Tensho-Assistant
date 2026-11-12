using System.Globalization;
using System.Text.Json.Nodes;

namespace TenshoAssistant.Discoveries;

/// <summary>
/// X-175 (1138, user choice of 12/11, option B): the two ways the players' exchange signs in to GitHub, in one place. Today the OAuth app of the
/// repository (0.883: scopes gist and public_repo). Once the maintainer has created and installed the GitHub App (least permissions: Issues read
/// and write on Shigu330/Tensho-Assistant only, account permission Gists read and write), its client id goes in <see cref="AppClientId"/> and the
/// exchange uses it instead. A GitHub App's device flow takes no scope (its permissions are the App's); its user token may expire (8 hours), with
/// a refresh token (6 months) that renews it before use, without any client secret (GitHub allows it for tokens of the device flow). Each refresh
/// gives a new refresh token and ends the old one. Pure: no request is sent from here (GitHubExchange sends them). Client ids are not secrets.
/// </summary>
public sealed record GitHubSignIn(string ClientId,string Scopes,bool App)
{
    public const string OAuthClientId="Ov23liBmk4RCUYw6pU6Z",OAuthScopes="gist public_repo";
    /// <summary>The GitHub App's client id: empty until the maintainer has created and installed it; the OAuth app is used meanwhile.</summary>
    public const string AppClientId="";
    /// <summary>A token is renewed this long before it expires (a request started just before the end is not refused halfway).</summary>
    public static readonly TimeSpan Margin=TimeSpan.FromMinutes(5);
    public static readonly GitHubSignIn OAuth=new(OAuthClientId,OAuthScopes,false);
    /// <summary>The sign-in the plugin uses: the GitHub App when its client id is set, else the OAuth app as before.</summary>
    public static GitHubSignIn Current=>For(AppClientId);
    public static GitHubSignIn For(string appClientId)=>appClientId.Trim() is { Length:>0 } id ? new(id,"",true):OAuth;

    /// <summary>The fields of « login/device/code »: the client id, and the scopes for the OAuth app only.</summary>
    public Dictionary<string,string> DeviceCodeFields()=>App ? new(){["client_id"]=ClientId}:new(){["client_id"]=ClientId,["scope"]=Scopes};
    public Dictionary<string,string> PollFields(string deviceCode)=>
        new(){["client_id"]=ClientId,["device_code"]=deviceCode,["grant_type"]="urn:ietf:params:oauth:grant-type:device_code"};
    public Dictionary<string,string> RefreshFields(string refreshToken)=>
        new(){["client_id"]=ClientId,["grant_type"]="refresh_token",["refresh_token"]=refreshToken};
    /// <summary>Whether a token got with <paramref name="tokenClientId"/> (github.json; empty: before 1138, the OAuth app) belongs to this sign-in.</summary>
    public bool Owns(string? tokenClientId)=>(string.IsNullOrEmpty(tokenClientId) ? OAuthClientId:tokenClientId)==ClientId;

    /// <summary>
    /// GitHub's token answer: access_token, and when the token expires, expires_in, refresh_token and refresh_token_expires_in (seconds). Null
    /// without an access token (an « error » answer: authorization_pending, bad_refresh_token…).
    /// </summary>
    public static GitHubToken? ReadToken(JsonObject answer,DateTimeOffset now)
    {
        if(Text(answer["access_token"]) is not { Length:>0 } access)return null;
        var refresh=Text(answer["refresh_token"]) is { Length:>0 } given ? given:null;
        return new(access,refresh,Seconds(answer["expires_in"]) is { } expires ? now.AddSeconds(expires):null,
            refresh is not null && Seconds(answer["refresh_token_expires_in"]) is { } lasts ? now.AddSeconds(lasts):null);
    }
    /// <summary>A token is to be renewed: it expires (null: never) within <see cref="Margin"/>.</summary>
    public static bool Due(DateTimeOffset? expires,DateTimeOffset now)=>expires is { } at && now>=at-Margin;
    /// <summary>A refresh token is past its end (null: no end given).</summary>
    public static bool Ended(DateTimeOffset? expires,DateTimeOffset now)=>expires is { } at && now>=at;

    private static string? Text(JsonNode? node)=>node is JsonValue value && value.TryGetValue<string>(out var text) ? text:null;
    private static long? Seconds(JsonNode? node)
    {
        if(node is not JsonValue value)return null;
        if(value.TryGetValue<long>(out var number))return number>0 ? number:null;
        if(value.TryGetValue<double>(out var real))return real>0 ? (long)real:null;
        return value.TryGetValue<string>(out var text) && long.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out var parsed) && parsed>0 ? parsed:null;
    }
}

/// <summary>X-175: a user token and, for a GitHub App whose tokens expire, its refresh token and both ends (UTC).</summary>
public sealed record GitHubToken(string Access,string? Refresh,DateTimeOffset? Expires,DateTimeOffset? RefreshExpires);
