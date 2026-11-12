using TenshoAssistant.Desktop;
using TenshoAssistant.Inventory;
using TenshoAssistant.Link;

// 0.1142 (X-170, GPT audit of 12/11, for a single client: bank and link parts). Behaviour tests (R28): the bank's lock held by another program at
// start (two game clients), then released; the link's notices after a plugin restart while the app was away; the settings read by the desktop
// application with Discord addresses in any case.
internal static class AuditFixes1142Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static LinkSnapshot Snapshot(long sequence,params (string Category,long Generation)[] changes)
        => new(LocalLinkProtocol.Version,"0.1142.0",true,false,132,"New Gridania",sequence,null,changes.Select(change=>new LinkChange(change.Category,change.Generation)).ToArray());

    private static BankEntry Entry(ulong character)
    {
        var owner=new InventoryOwnerId(character);
        var containers=BankStorageScope.ContainerIds(BankStorageKind.Saddlebag).Select(id=>new InventoryContainerSnapshot(new(owner,new(id)),InventoryCoverage.Complete,
            id==BankStorageScope.ContainerIds(BankStorageKind.Saddlebag)[0] ? [new InventoryStack(new(new(5057),false),3,0)] : [])).ToArray();
        return new(new(character,BankStorageKind.Saddlebag,0),"Name (World)",new(DateTimeOffset.UtcNow,"test",containers));
    }
    private static bool Wait(Func<bool> done,int milliseconds=20_000)
    {
        var until=Environment.TickCount64+milliseconds;
        while(Environment.TickCount64<until){if(done())return true;Thread.Sleep(25);}
        return done();
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Bank 1142: the lock held by another program at start: the bank waits « loading », nothing can change, then loads once it is free",() =>
        {
            var root=Path.Combine(Path.GetTempPath(),"tensho-bank-1142-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            try
            {
                var folder=Path.Combine(root,"InventoryBank");var legacy=Path.Combine(root,"InventoryBank.json");
                using(var first=new InventoryBankService(new PerCharacterBankStore(folder,legacy)))
                {
                    first.Ready.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
                    Check(first.ImportAsync(Entry(1)).WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult().Success,"a saved storage");
                }
                var store=new PerCharacterBankStore(folder,legacy,lockWaitMs:100);
                var held=new FileStream(store.LockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
                InventoryBankService bank;
                try
                {
                    var busy=false;try{store.Load();}catch(BankLockBusyException){busy=true;}
                    Check(busy,"the store says the lock is busy (its own exception)");
                    bank=new InventoryBankService(store,TimeSpan.FromMilliseconds(100));
                    Check(Wait(()=>bank.State.Error?.Contains("waits for another program",StringComparison.Ordinal)==true),"said: "+bank.State.Error);
                    Check(bank.State.Loading && !bank.Ready.IsCompleted,"still loading while the lock is held");
                    var refused=bank.ImportAsync(Entry(2)).WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
                    Check(!refused.Success,"nothing changed meanwhile: "+refused.Message);
                    Thread.Sleep(400);
                    Check(bank.State.Loading,"still waiting after several tries");
                }
                finally{held.Dispose();}
                using(bank)
                {
                    Check(bank.Ready.WaitAsync(TimeSpan.FromSeconds(30)).Wait(TimeSpan.FromSeconds(30)),"loaded once released");
                    Check(bank.State.Error is null && !bank.State.Loading && bank.State.Entries.Count==1,"the saved storage is there: "+bank.State.Error);
                    var saved=bank.ImportAsync(Entry(2)).WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
                    Check(saved.Success,"changes work again (not blocked for the session): "+saved.Message);
                }
                File.WriteAllText(Path.Combine(folder,"3.json"),"{ damaged");
                using var blocked=new InventoryBankService(new PerCharacterBankStore(folder,legacy),TimeSpan.FromMilliseconds(100));
                blocked.Ready.WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
                Check(blocked.State.Error?.StartsWith("The bank could not be loaded",StringComparison.Ordinal)==true && !blocked.State.Loading,"a damaged file still blocks, as before: "+blocked.State.Error);
            }
            finally{try{Directory.Delete(root,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
        }),
        ("Link 1142: a plugin started again while the app was away, its generations back up to the one seen: its first notices are read",() =>
        {
            var reader=new LinkChangeReader();
            Check(reader.Take(Snapshot(1,("bank",1),("voyages",2),("settings",3))).Count==3,"first notices");
            Check(reader.Take(Snapshot(2,("bank",1),("voyages",2),("settings",3))).Count==0,"the same again: nothing new");
            Check(reader.Take(null).Count==0,"the link lost");
            var after=reader.Take(Snapshot(1,("craft-lists",3)));
            Check(after.SequenceEqual(["craft-lists"]),"the restarted plugin's notice with an equal generation is read: "+string.Join(",",after));
            Check(reader.Take(Snapshot(9,("bank",5))).SequenceEqual(["bank"]) && reader.Take(Snapshot(10,("bank",5))).Count==0,"then as usual");
            var again=reader.Take(Snapshot(2,("my-tools",5)));
            Check(again.SequenceEqual(["my-tools"]),"snapshots counting again from the start (no gap seen): read too");
        }),
        ("Link 1142: the settings read by the desktop app lose the Discord addresses whatever their case, and any other text holding one",() =>
        {
            const string json="{\"$type\":\"TenshoAssistant.Configuration, TenshoAssistant\",\"Discord\":{\"webhookurl\":\"https://discord.com/api/webhooks/123456789012345678/abcdefghijklmnopqrstuvwxyz\","
                +"\"CATEGORYURLS\":{\"a\":\"x\"},\"Enabled\":true},\"Note\":\"see HTTPS://DISCORDAPP.COM/API/V10/WEBHOOKS/1/abc\",\"Lines\":[\"keep\",\"https://Discord.com/api/webhooks/9/z\"],\"Name\":\"discord.com is fine\"}";
            var projected=SettingsGuard.Project(json);
            Check(!projected.Contains("webhook",StringComparison.OrdinalIgnoreCase) && !projected.Contains("CATEGORYURLS",StringComparison.OrdinalIgnoreCase),"no address left: "+projected);
            Check(projected.Contains("\"Enabled\":true",StringComparison.Ordinal) && projected.Contains("\"keep\"",StringComparison.Ordinal)
                && projected.Contains("discord.com is fine",StringComparison.Ordinal) && projected.Contains("TenshoAssistant.Configuration",StringComparison.Ordinal),"the rest kept: "+projected);
        }),
    ];
}
