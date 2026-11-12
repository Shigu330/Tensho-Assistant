using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TenshoAssistant.Desktop;

/// <summary>
/// X-150 (0.1120, GPT review of 11/11 and audit A15): how the desktop app reads the plugin's saved settings, read-only. The file is read
/// whole with a size bound, its secrets taken out (the Discord addresses: the app sends nothing), then given to Newtonsoft (the format
/// Dalamud writes, with type names) whose binder accepts only a closed list of types (AllowedType). Pure, linked into the tests.
/// </summary>
public static class SettingsGuard
{
    /// <summary>The largest settings file read (the user's is about 64 KB).</summary>
    public const long MaxBytes=8L*1024*1024;

    /// <summary>The properties never carried into the app: secrets of the plugin's settings (DiscordSettings), whatever object holds them.
    /// X-170 (0.1142, GPT audit): whatever their case (Newtonsoft fills a property from a name of any case).</summary>
    public static readonly IReadOnlySet<string> Secrets=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"WebhookUrl","CategoryUrls"};
    /// <summary>X-170 (0.1142): a Discord webhook address in any other text of the settings (a note, a copied line), whatever its case.</summary>
    public static readonly System.Text.RegularExpressions.Regex WebhookAddress=new(@"discord(?:app)?\.com/api/(?:v[0-9]{1,2}/)?webhooks/",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase|System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    private static bool HoldsWebhook(JsonNode? node)=>node is JsonValue value && value.TryGetValue<string>(out var text) && WebhookAddress.IsMatch(text);

    /// <summary>The file's text, read while the game may be writing it (shared), refused over MaxBytes.</summary>
    public static string ReadBounded(string path)
    {
        using var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        if(file.Length>MaxBytes)throw new InvalidDataException($"settings file larger than {MaxBytes} bytes");
        using var reader=new StreamReader(file);
        var buffer=new char[81920];
        var text=new System.Text.StringBuilder();
        int read;
        while((read=reader.Read(buffer,0,buffer.Length))>0)
        {
            text.Append(buffer,0,read);
            if(text.Length>MaxBytes)throw new InvalidDataException($"settings file larger than {MaxBytes} bytes");
        }
        return text.ToString();
    }

    /// <summary>
    /// The settings as the app may see them: the same JSON (order, numbers and type names kept) without the Secrets properties at any
    /// depth, and with any other text holding a Discord webhook address emptied (X-170). JsonException when it is not one JSON object.
    /// </summary>
    public static string Project(string json)
    {
        var root=JsonNode.Parse(json,documentOptions:new JsonDocumentOptions{MaxDepth=64}) as JsonObject ?? throw new JsonException("the settings are not a JSON object");
        var stack=new Stack<JsonNode>();
        stack.Push(root);
        while(stack.Count>0)
        {
            switch(stack.Pop())
            {
                case JsonObject node:
                    foreach(var secret in node.Select(pair=>pair.Key).Where(Secrets.Contains).ToArray())node.Remove(secret);
                    // X-170 (0.1142): a webhook address held by any other property is emptied (the app sends nothing).
                    foreach(var key in node.Where(pair=>HoldsWebhook(pair.Value)).Select(pair=>pair.Key).ToArray())node[key]="";
                    foreach(var (_,child) in node)if(child is not null)stack.Push(child);
                    break;
                case JsonArray list:
                    for(var index=0;index<list.Count;index++)if(HoldsWebhook(list[index]))list[index]="";
                    foreach(var child in list)if(child is not null)stack.Push(child);
                    break;
            }
        }
        return root.ToJsonString();
    }

    private static readonly HashSet<Type> Plain=[typeof(string),typeof(decimal),typeof(DateTime),typeof(DateTimeOffset),typeof(TimeSpan),typeof(Guid),
        typeof(Vector2),typeof(Vector3),typeof(Vector4),typeof(object)];
    private static readonly HashSet<Type> Containers=[typeof(List<>),typeof(Dictionary<,>),typeof(HashSet<>),typeof(Nullable<>),
        typeof(ValueTuple<>),typeof(ValueTuple<,>),typeof(ValueTuple<,,>),typeof(ValueTuple<,,,>),typeof(ValueTuple<,,,,>),typeof(ValueTuple<,,,,,>),typeof(ValueTuple<,,,,,,>),
        typeof(Tuple<>),typeof(Tuple<,>),typeof(Tuple<,,>)];

    /// <summary>
    /// The closed list of types a type name in the settings may give: the plugin's own types (its assembly, namespace TenshoAssistant),
    /// primitives, strings, dates, vectors, and lists, dictionaries, sets, tuples and nullables of those; anything else is refused.
    /// </summary>
    public static bool AllowedType(Type type,Assembly plugin)
    {
        if(type.IsArray)return type.GetArrayRank()==1 && AllowedType(type.GetElementType()!,plugin);
        if(type.IsGenericType)
            return (type.Assembly==plugin && type.Namespace?.StartsWith("TenshoAssistant",StringComparison.Ordinal)==true || Containers.Contains(type.GetGenericTypeDefinition()))
                && type.GetGenericArguments().All(argument=>AllowedType(argument,plugin));
        if(type.Assembly==plugin)return type.Namespace?.StartsWith("TenshoAssistant",StringComparison.Ordinal)==true;
        return type.IsPrimitive || type.IsEnum && type.Assembly==plugin || Plain.Contains(type);
    }
}

/// <summary>
/// X-150 (0.1120): one piece of work asked many times (settings notices from the link), run off the frame on a task: requests that arrive
/// while it waits or runs are grouped into one more run; the last result is taken once on the UI thread (Take).
/// </summary>
public sealed class Grouped<T>(Func<T> work,int delayMs=250)
{
    private readonly object gate=new();
    private bool running,again;
    private (T? Value,Exception? Error)? done;
    public int Runs{get{lock(gate)return runs;}}
    private int runs;

    /// <summary>Any thread: run the work soon (once for every request made before it starts).</summary>
    public void Request()
    {
        lock(gate)
        {
            if(running){again=true;return;}
            running=true;
        }
        _=Task.Run(Loop);
    }

    private async Task Loop()
    {
        while(true)
        {
            await Task.Delay(delayMs).ConfigureAwait(false);
            // Requests made during the wait are this run; those made while it reads ask one more.
            lock(gate)again=false;
            (T? Value,Exception? Error) result;
            try{result=(work(),null);}
            catch(Exception error){result=(default,error);}
            lock(gate)
            {
                done=result;runs++;
                if(!again){running=false;return;}
            }
        }
    }

    /// <summary>The newest result not taken yet (a value or the error that stopped it), or null.</summary>
    public (T? Value,Exception? Error)? Take(){lock(gate){var result=done;done=null;return result;}}

    public bool Busy{get{lock(gate)return running;}}
}
