using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace TenshoAssistant.Items;

/// <summary>What happened to a request to open a game log on an entry.</summary>
public enum GameLogOpen { Opened, Closing, Unavailable }

/// <summary>
/// 0.405 (user request: « open the recipe or the resource in the game's log, at the right place »): opens the game's Crafting log on a
/// recipe (AgentRecipeNote.OpenRecipeByRecipeId) or its Gathering log on an item (AgentGatheringNote.OpenGatherableByItemId). Called on
/// the player's click only (from the window, on the game's thread, like Try On); each game function is called only once resolved. They
/// open a game window: nothing is sent to the server.
/// 0.429 (user report: with the log already open, some recipes give the game's red « cannot display a recipe… »): an open log is first
/// closed (the agent's own Hide); the caller asks again on the next frames and the log opens on the entry once closed.
/// </summary>
public static unsafe class GameLogOpener
{
    public static GameLogOpen Recipe(uint recipeId,Action<Exception> warn)
    {
        try
        {
            var agent=AgentRecipeNote.Instance();
            if(agent==null || recipeId==0 || (nint)AgentRecipeNote.MemberFunctionPointers.OpenRecipeByRecipeIdInternal==0)return GameLogOpen.Unavailable;
            if(agent->AgentInterface.IsAgentActive()){agent->AgentInterface.Hide();return GameLogOpen.Closing;}
            agent->OpenRecipeByRecipeId(recipeId);
            return GameLogOpen.Opened;
        }
        catch(Exception error){warn(error);return GameLogOpen.Unavailable;}
    }

    public static GameLogOpen Gatherable(uint itemId,Action<Exception> warn)
    {
        try
        {
            var agent=AgentGatheringNote.Instance();
            if(agent==null || itemId is 0 or > ushort.MaxValue || (nint)AgentGatheringNote.MemberFunctionPointers.OpenGatherableByItemId==0)return GameLogOpen.Unavailable;
            if(agent->AgentInterface.IsAgentActive()){agent->AgentInterface.Hide();return GameLogOpen.Closing;}
            agent->OpenGatherableByItemId((ushort)itemId);
            return GameLogOpen.Opened;
        }
        catch(Exception error){warn(error);return GameLogOpen.Unavailable;}
    }
}
