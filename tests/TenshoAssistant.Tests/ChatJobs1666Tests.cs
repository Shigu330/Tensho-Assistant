using TenshoAssistant.Chat;

// 0.1666 (X-690, the user's request of 20/11 around 22:40): in the Tensho chat window, a player's name in the colour of their role and their
// job's icon before it, when the game shows their job (party, alliance, nearby), memory only; the game's role colours; capture mode keeps both.
// docs/evidence/0.1666-chat-job-roles.md.
internal static class ChatJobs1666Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static readonly DateTimeOffset At=new(2026,11,21,1,30,0,TimeSpan.Zero);
    private static ChatRecord Said(ChatChannel channel)=>new(7,At,14,channel,"Aya Lune",[new ChatSegment("pull")]);
    // The ClassJob sheet's roles (XbmProbe of 21/11): Paladin 19 tank, White Mage 24 healer, Black Mage 25 and Monk 20 DPS, Miner 16 none.
    private static ChatJobs Jobs()
    {
        var jobs=new ChatJobs();
        jobs.SetRoles([(19,1),(24,4),(25,3),(20,2),(16,0)]);
        return jobs;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Chat jobs 1666: the sheet's roles, the game's role colours (its defaults when unread), the job icon 62100 + job",()=>
        {
            var jobs=Jobs();
            Check(jobs.Role(19)==JobRole.Tank && jobs.Role(24)==JobRole.Healer && jobs.Role(25)==JobRole.Dps && jobs.Role(20)==JobRole.Dps && jobs.Role(16)==JobRole.None && jobs.Role(99)==JobRole.None,"roles");
            Check(jobs.ColourOf(JobRole.Tank)==0xFF0080FF && jobs.ColourOf(JobRole.Healer)==0xFF80F860 && jobs.ColourOf(JobRole.Dps)==0xFFFF807F,"the game's defaults: blue, green, red");
            jobs.SetColours(0xFF112233,null,0x00445566);
            Check(jobs.ColourOf(JobRole.Tank)==0xFF112233 && jobs.ColourOf(JobRole.Healer)==ChatJobs.GameHealer && jobs.ColourOf(JobRole.Dps)==ChatJobs.GameDps,"the game's own colour read; unread or without alpha: default");
            var (r,g,b)=ChatJobs.Rgb(0xFF0080FF);
            Check(r==0 && Math.Abs(g-128/255f)<1e-6 && b==1,"0xAARRGGBB");
            Check(ChatJobs.Icon(19)==62119 && ChatJobs.Icon(0)==0,"icon");
        }),
        ("Chat jobs 1666: colour and icon only on spoken channels, with the job known and the setting on; never guessed",()=>
        {
            var jobs=Jobs();
            var on=new ChatSettings();
            Check(on.RoleColors && on.JobIcons,"both on by default");
            Check(jobs.ColourFor(on,ChatChannel.Party,19)==ChatJobs.GameTank && jobs.ColourFor(on,ChatChannel.Alliance,24)==ChatJobs.GameHealer && jobs.ColourFor(on,ChatChannel.Say,25)==ChatJobs.GameDps,"party, alliance, and another channel when the job is known");
            Check(jobs.ColourFor(on,ChatChannel.Party,0) is null && jobs.ColourFor(on,ChatChannel.Party,16) is null && jobs.ColourFor(on,ChatChannel.System,19) is null,"unknown job, a gatherer, a system line: none");
            Check(ChatJobs.IconFor(on,ChatChannel.Party,19)==62119 && ChatJobs.IconFor(on,ChatChannel.Yell,16)==62116 && ChatJobs.IconFor(on,ChatChannel.Party,0)==0 && ChatJobs.IconFor(on,ChatChannel.Echo,19)==0,"icon: any known job, spoken channels");
            var off=new ChatSettings{RoleColors=false,JobIcons=false};
            Check(jobs.ColourFor(off,ChatChannel.Party,19) is null && ChatJobs.IconFor(off,ChatChannel.Party,19)==0,"settings off");
        }),
        ("Chat jobs 1666: the jobs the game shows: the first given kept, the incomplete left out, bounded, cleared; the sender's job taken when the message arrives",()=>
        {
            var sources=new ChatSources();
            sources.Jobs.Replace([("Aya Lune",73,19),("Aya Lune",73,25),("No World",0,19),("",73,19),("No Job",73,0)]);
            Check(sources.Jobs.Count==1 && sources.Jobs.Find("Aya Lune",73)==19 && sources.Jobs.Find("Aya Lune",74)==0,"party first, the rest left out");
            sources.Expect(5,"Aya Lune",73,14);
            sources.Jobs.Replace([]);
            sources.Expect(6,"Aya Lune",73,14);
            Check(sources.For(5)?.Job==19 && sources.For(6)?.Job==0,"the job when the message arrived, kept with it; a later message, not known then: none");
            Check(sources.ResolveRead(1001,2001,73,14,()=>new ChatLogLine(14,"Aya Lune",73),9)==ChatSourceMatch.Given && sources.For(6)?.Job==0,"identifiers given keep the job");
            sources.Jobs.Replace(Enumerable.Range(0,ChatJobs.MaximumKept+50).Select(i=>($"P{i}",(ushort)73,19u)));
            Check(sources.Jobs.Count==ChatJobs.MaximumKept,"bounded");
            sources.Clear();
            Check(sources.Jobs.Count==0 && sources.For(5) is null,"cleared with the senders (logout, unload)");
        }),
        ("Chat jobs 1666: the icon before the sender's name, capture mode too, never in the copy; one block when wrapping; the layout key follows the setting",()=>
        {
            var settings=new ChatSettings{ShowTimestamps=false,ShowChannel=false};
            var runs=ChatFormat.WithJobIcon(ChatFormat.Runs(Said(ChatChannel.Party),settings,_=>""),62119);
            Check(runs[0].Role==ChatRole.JobIcon && runs[1].Role==ChatRole.Sender,"icon then name");
            var capture=new ChatSettings{ShowTimestamps=false,ShowChannel=false,CaptureNames=true};
            var hidden=ChatFormat.WithJobIcon(ChatFormat.Runs(Said(ChatChannel.Party),capture,_=>""),62119);
            Check(hidden[0].Role==ChatRole.JobIcon && !hidden[1].Text.Contains("Aya"),"capture mode: the icon stays, the name does not");
            Check(ChatFormat.WithJobIcon(ChatFormat.Runs(Said(ChatChannel.Party),settings,_=>""),0).All(run=>run.Role!=ChatRole.JobIcon)
                  && ChatFormat.WithJobIcon(ChatFormat.Runs(new ChatRecord(8,At,57,ChatChannel.System,"",[new ChatSegment("hi")]),settings,_=>""),62119).All(run=>run.Role!=ChatRole.JobIcon) && ChatFormat.Plain(Said(ChatChannel.Party),settings,_=>"")=="Aya Lune: pull","none without a job; the copy: the text alone");
            Check(ChatFormat.Wrap([new ChatRun("",ChatRole.JobIcon),new ChatRun("Aya: ",ChatRole.Sender)],2,text=>text.Length,0,3)[0][0].Role==ChatRole.JobIcon,"a block");
            Check(ChatFormat.LayoutKey(settings)!=ChatFormat.LayoutKey(new ChatSettings{ShowTimestamps=false,ShowChannel=false,JobIcons=false}),"layout key");
            var shared=ChatTheme.Share(new ChatSettings{RoleColors=false,JobIcons=false});
            var back=new ChatSettings();
            Check(ChatTheme.Apply(shared,back) && !back.RoleColors && !back.JobIcons,"in a look's code");
        }),
        ("Chat jobs 1666: wired: once a second, party, alliance and nearby, nothing during a cut, the game's role colours, memory only; drawn and coloured",()=>
        {
            var actions=Read("src/TenshoAssistant/Chat/ChatPlayerActions.Dalamud.cs");
            Check(actions.Contains("CompetitiveGate.Add(framework,OnJobsTick);") && actions.Contains("CompetitiveGate.Remove(framework,OnJobsTick);") && actions.Contains("if(now-jobsAt<1000)return;") && actions.Contains("private long jobsAt=-100_000;"),"once a second, unsubscribed");
            Check(actions.Contains("TenshoAssistant.Competitive.CompetitiveGate.Cut || !client.IsLoggedIn") && actions.Contains("manager->MainGroup.AllianceMembers") && actions.Contains("UiConfigOption.LogColorRoleTank"),"cut, alliance, colours");
            var sources=Read("src/TenshoAssistant/Chat/ChatSources.cs");
            Check(sources.Contains("sources[id]=new(name,world,Job:Jobs.Find(name,world));"),"taken at arrival");
            var store=Read("src/TenshoAssistant/Chat/ChatStore.cs")+Read("src/TenshoAssistant/Chat/ChatHistory.cs");
            Check(!store.Contains("Job"),"never in the chat history on disk");
            var window=Read("src/TenshoAssistant/MainWindow.ChatWindow.cs");
            Check(window.Contains("if(run.Role==ChatRole.JobIcon)") && window.Contains("ChatFormat.WithJobIcon(ChatFormat.Runs(view.RecordAt(index)") && window.Contains("if(ChatRoleColour(record,options) is var (red,green,blue))"),"drawn and coloured");
        }),
    ];
}
