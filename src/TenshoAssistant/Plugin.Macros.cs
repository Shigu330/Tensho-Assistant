using Dalamud.Plugin.Services;
using TenshoAssistant.Macros;

namespace TenshoAssistant;

// X-153 (0.1933, GPT review of 11/11, accepted by the user): the macros' family out of Plugin.cs, after the alarms' family (0.1932). The macro
// writer, the Macro Chain bridge and the macros' observation reader are each created in one install path and released in one release path of
// the plugin's registrations, in this file; Plugin.cs calls the two blocks where they stood, so the order of the registrations is the one of
// before. MacrosFamily1933Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The macro writer (the crafting macros written into the game's macro slots on the player's click), its outcome said in the chat.</summary>
    private void RegisterMacroWriter(IFramework framework,IChatGui chat)
    {
        Macros.MacroWriter? macroWriter=null;
        registrations.Register(()=>macroWriter=new Macros.MacroWriter(framework,log,outcome=>
        {
            var parts=outcome.Split(':');
            // 0.330 (audit): the exact slot list (« Done:first:last:book:list »); occupied slots are skipped, so « first to last » could
            // suggest they were overwritten.
            var text=parts[0]=="Done" ? localization.Format("CraftMacro.Written",parts[4].Replace(",",", ")):localization.Get("CraftMacro.Write"+parts[0]);
            if(parts[0]=="Done")chat.Print("[Tensho] "+text);else chat.PrintError("[Tensho] "+text);
        }),()=>macroWriter?.Dispose());
        mainWindow.ConfigureMacroWriter(macroWriter!);
    }

    /// <summary>The Macro Chain bridge and the macros' observation reader.</summary>
    private void RegisterMacroReaders(IFramework framework,IPlayerState player,IClientState client,ICommandManager commands,IGameInteropProvider interop)
    {
        MacroChainBridge? macroBridge = null;
        registrations.Register(() => macroBridge = new MacroChainBridge(macroChain, framework, player, client, commands, interop, log), () => macroBridge?.Dispose());
        MacroObservationReader? macroObserver = null;
        registrations.Register(() => macroObserver = new MacroObservationReader(macroObservations, framework, player, client, log), () => macroObserver?.Dispose());
    }
}
