using System.Reflection;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-01 C1.3 (0.1187, the root of the leak of unloaded instances): what the log says, ten seconds after a load, about the instances loaded
/// before in the same game process: their load context alive or collected, and which named parts of each (the plugin object, the window,
/// their fields that hold objects of the plugin's own code) are still alive. Weak references only, kept in a process-wide slot made of
/// runtime types only, so the watch itself never keeps an instance or its context alive; nothing is collected or forced.
/// Reading: a context that stays alive while its parts are collected costs little (code and static tables); parts that stay alive name what
/// still holds the instance. Roots found out of the game and readings: docs/evidence/0.1187-unload-roots.md.
/// </summary>
public static class UnloadWatch
{
    /// <summary>Process-wide slot (AppDomain data): the instances loaded in this process, the newest first.</summary>
    public const string Slot="TenshoAssistant.X01.Instances";
    /// <summary>How many instances are kept, this one included.</summary>
    public const int Kept=5;
    /// <summary>The name of the load context among an instance's parts.</summary>
    public const string Context="context";

    /// <summary>This instance first (its context noted weakly), then the earlier ones read from the slot, <see cref="Kept"/> at most.</summary>
    public static List<KeyValuePair<string,Dictionary<string,WeakReference>>> Push(object? slot,string label,object? context)
    {
        var parts=new Dictionary<string,WeakReference>(StringComparer.Ordinal);
        if(context is not null)parts[Context]=new WeakReference(context);
        var all=new List<KeyValuePair<string,Dictionary<string,WeakReference>>>{new(label,parts)};
        if(slot is List<KeyValuePair<string,Dictionary<string,WeakReference>>> earlier)all.AddRange(earlier.Take(Kept-1));
        return all;
    }

    /// <summary>Whether a type comes from the given assembly, by itself, as an array's element or as a generic argument.</summary>
    public static bool Ours(Type type,Assembly assembly)=>
        type.Assembly==assembly || (type.IsArray && type.GetElementType() is { } element && Ours(element,assembly)) ||
        (type.IsGenericType && type.GetGenericArguments().Any(argument=>Ours(argument,assembly)));

    /// <summary>Notes an owner and each of its fields that holds an object of the plugin's own code, weakly, under « name » and « name.field ».</summary>
    public static void Note(Dictionary<string,WeakReference> parts,string name,object? owner,Assembly assembly)
    {
        if(owner is null)return;
        parts[name]=new WeakReference(owner);
        foreach(var field in owner.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
        {
            if(field.FieldType.IsValueType || field.FieldType==typeof(string) || field.FieldType.IsPointer)continue;
            object? value;
            try{value=field.GetValue(owner);}
            catch(Exception error) when(error is NotSupportedException or FieldAccessException or TargetException){continue;}
            if(value is null || !Ours(value.GetType(),assembly))continue;
            parts[name+"."+field.Name]=new WeakReference(value);
        }
    }

    /// <summary>One line for an earlier instance: its context, and its parts still alive out of those noted (the first names, in order).</summary>
    public static string Describe(string label,IReadOnlyDictionary<string,WeakReference> parts,int names=24)
    {
        var context=parts.TryGetValue(Context,out var held) ? (held.IsAlive ? "alive":"collected"):"unknown";
        var noted=parts.Where(part=>part.Key!=Context).ToList();
        if(noted.Count==0)return $"{label}: context {context}, parts not noted";
        var alive=noted.Where(part=>part.Value.IsAlive).Select(part=>part.Key).Order(StringComparer.Ordinal).ToList();
        return $"{label}: context {context}, {alive.Count} of {noted.Count} parts alive"+
            (alive.Count==0 ? "":": "+string.Join(", ",alive.Take(names))+(alive.Count>names ? ", …":""));
    }
}
