using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Data.Sqlite;

namespace TenshoAssistant.Diagnostics;

/// <summary>
/// X-685 (0.1752, the memory that grows with each reload: every copy of Tensho unloaded before kept its load context, « context alive, 0
/// of N parts alive », after 0.1741 and 0.1745 had taken System.Text.Json and Newtonsoft off the path): Microsoft.Data.Sqlite is loaded in
/// Tensho's own load context (a private copy, as every dependency of a plugin), and its connection factory, made at the first Open whatever
/// the pooling, starts a prune timer and adds two handlers to the process events DomainUnload and ProcessExit that nothing ever takes off
/// (Microsoft.Data.Sqlite 10.0.14, SqliteConnectionFactory: _pruneTimer, &lt;.ctor&gt;b__6_0 and b__6_1). The timer queue and the process
/// events are the runtime's, shared by the game process: they held the copy's factory, so its assembly, so Tensho's whole load context, for
/// good. At unload, once the data files are closed, the copy's own factory is released: its timer stopped, its handlers taken off (the
/// public « -= » with the same delegate). Only the members of Tensho's private copy are touched, never a cache of the runtime or of the game
/// process (D-206). A factory used after that still opens connections (Tensho's connections are not pooled: nothing to prune or clear).
/// Evidence: docs/evidence/0.1752-unload-sqlite.md.
/// </summary>
public static class UnloadRelease
{
    /// <summary>The factory's type in Microsoft.Data.Sqlite (internal).</summary>
    public const string FactoryType="Microsoft.Data.Sqlite.SqliteConnectionFactory";

    /// <summary>What a release did: timers stopped and handlers taken off; Found false when the factory or its members were not where
    /// this version of Microsoft.Data.Sqlite keeps them (a later version of the library: the line says so, nothing else is tried).</summary>
    public readonly record struct Released(bool Found,int Timers,int Handlers)
    {
        public string Text=>Found ? $"SQLite connection factory released: {Timers} timer(s) stopped, {Handlers} process event handler(s) taken off"
            :"SQLite connection factory not found where Microsoft.Data.Sqlite kept it (a later version of the library?): nothing released";
    }

    /// <summary>Releases the connection factory of the Microsoft.Data.Sqlite this code was compiled with (Tensho's private copy).</summary>
    public static Released Sqlite()
    {
        const BindingFlags instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        try
        {
            var type=typeof(SqliteConnection).Assembly.GetType(FactoryType,false);
            if(type?.GetField("Instance",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(null) is not { } factory)return new(false,0,0);
            var timers=0;
            foreach(var field in type.GetFields(instance))
                if(field.FieldType==typeof(Timer) && field.GetValue(factory) is Timer timer){timer.Dispose();timers++;}
            // The handlers are the constructor's lambdas over « this »: instance methods of the factory, (object, EventArgs). Taken off both
            // events by the public « -= » with an equal delegate (same target, same method); a handler not there is left as it is.
            var handlers=0;
            foreach(var method in type.GetMethods(instance|BindingFlags.DeclaredOnly))
            {
                if(method.ReturnType!=typeof(void) || method.GetParameters() is not [{ ParameterType:var first },{ ParameterType:var second }] || first!=typeof(object) || second!=typeof(EventArgs))continue;
                var handler=(EventHandler)Delegate.CreateDelegate(typeof(EventHandler),factory,method);
                AppDomain.CurrentDomain.DomainUnload-=handler;
                AppDomain.CurrentDomain.ProcessExit-=handler;
                handlers++;
            }
            return new(timers>0 || handlers>0,timers,handlers);
        }
        catch(Exception error) when(error is TargetInvocationException or TypeInitializationException or FieldAccessException or MethodAccessException or ArgumentException or NotSupportedException or InvalidOperationException)
        {
            return new(false,0,0);
        }
    }

    /// <summary>
    /// The handlers of the process events (DomainUnload, ProcessExit, UnhandledException, FirstChanceException, AssemblyLoad, AssemblyResolve,
    /// TypeResolve, ResourceResolve) whose code comes from the given load context: one name each (« event: Type.Method »). Read only, by the
    /// runtime's fields behind the events (read, never written); null when this runtime keeps them elsewhere (the log says « unknown »).
    /// For the developer mode's line at unload: what still holds this copy among the process's events.
    /// </summary>
    public static List<string>? ProcessEventsFrom(AssemblyLoadContext? context)
    {
        if(context is null)return null;
        const BindingFlags statics=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        const BindingFlags instances=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var places=new (Type Owner,object? Target,string Field)[]
        {
            (typeof(AppDomain),AppDomain.CurrentDomain,nameof(AppDomain.DomainUnload)),
            (typeof(AppDomain),AppDomain.CurrentDomain,nameof(AppDomain.ProcessExit)),
            (typeof(AppContext),null,"UnhandledException"),
            (typeof(AppContext),null,"FirstChanceException"),
            (typeof(AssemblyLoadContext),null,"AssemblyLoad"),
            (typeof(AssemblyLoadContext),null,"AssemblyResolve"),
            (typeof(AssemblyLoadContext),null,"TypeResolve"),
            (typeof(AssemblyLoadContext),null,"ResourceResolve"),
        };
        var found=new List<string>();
        var read=0;
        foreach(var (owner,target,name) in places)
        {
            FieldInfo? field;
            try{field=owner.GetField(name,target is null ? statics:instances);}
            catch(AmbiguousMatchException){continue;}
            if(field is null || !typeof(Delegate).IsAssignableFrom(field.FieldType))continue;
            read++;
            object? value;
            try{value=field.GetValue(target);}
            catch(Exception error) when(error is FieldAccessException or TargetException or NotSupportedException or ArgumentException){continue;}
            if(value is not Delegate hooks)continue;
            foreach(var hook in hooks.GetInvocationList())
                if(hook.Method.DeclaringType is { } declaring && AssemblyLoadContext.GetLoadContext(declaring.Assembly)==context)
                    found.Add($"{name}: {declaring.FullName}.{hook.Method.Name}");
        }
        return read==0 ? null:found;
    }
}
