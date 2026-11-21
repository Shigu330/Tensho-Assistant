namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>
    /// X-684 (0.1690, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of a player, wherever a
    /// player is named (the chat window's sender names, the Tensho map's party members, on the map and at its edge): name and world, then job
    /// and level when the game shows them (Chat.ChatCopy.SenderTip, tested by ChatTips1627Tests). Text: the chat and the map write their
    /// tooltips as text.
    /// </summary>
    private string PlayerTip(string name,string world,string? job,int level)
        =>Chat.ChatCopy.SenderTip(name,world,job,level,(who,where)=>localization.Format("ChatWindow.SenderTipWorld",who,where),
            (shown,at)=>localization.Format("ChatWindow.SenderTipJob",shown,at));

    // X-684 (0.1690): a party member's tooltip, built once per member state (name, world, job, level) and language, not every frame (the map
    // draws its party and its edge markers each frame).
    private readonly Dictionary<(string Name,ushort World,uint Job,int Level),string> partyTips=[];
    private int partyTipsRevision=-1;
    private string PartyMemberTip(Maps.MapPartyMarker member)
    {
        if(partyTipsRevision!=localization.Revision){partyTips.Clear();partyTipsRevision=localization.Revision;}
        var key=(member.Name,member.World,member.Job,member.Level);
        if(partyTips.TryGetValue(key,out var tip))return tip;
        if(partyTips.Count>=64)partyTips.Clear();
        tip=PlayerTip(member.Name,ChatPlayers?.World(member.World) ?? "",member.Job!=0 ? JobName(member.Job):null,member.Level);
        partyTips[key]=tip;
        return tip;
    }
}
