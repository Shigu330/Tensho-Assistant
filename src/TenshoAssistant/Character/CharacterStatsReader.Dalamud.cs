using System.Diagnostics;
using Dalamud.Game.Player;
using Dalamud.Plugin.Services;

namespace TenshoAssistant.Character;

internal sealed class CharacterStatsReader : IDisposable
{
    private readonly CharacterStatsService service;
    private readonly IFramework framework;
    private readonly IPlayerState player;
    private readonly IClientState client;
    private readonly IPluginLog log;
    private readonly IObjectTable objects;
    private readonly object gate = new();
    private bool disposed;
    private long nextRead;
    private ulong owner;
    private static readonly PlayerAttribute[] Attributes =
    [PlayerAttribute.Strength, PlayerAttribute.Dexterity, PlayerAttribute.Vitality, PlayerAttribute.Intelligence,
        PlayerAttribute.Mind, PlayerAttribute.Piety, PlayerAttribute.HealthPoints, PlayerAttribute.MagicPoints,
        PlayerAttribute.Defense, PlayerAttribute.MagicDefense, PlayerAttribute.CriticalHit,
        PlayerAttribute.DirectHitRate, PlayerAttribute.Determination, PlayerAttribute.Tenacity,
        PlayerAttribute.SkillSpeed, PlayerAttribute.SpellSpeed, PlayerAttribute.Craftsmanship,
        PlayerAttribute.Control, PlayerAttribute.CraftingPoints, PlayerAttribute.Gathering,
        PlayerAttribute.Perception, PlayerAttribute.GatheringPoints];
    public CharacterStatsReader(CharacterStatsService service, IFramework framework, IPlayerState player, IClientState client, IPluginLog log,IObjectTable objects)
    {
        this.service = service; this.framework = framework; this.player = player; this.client = client; this.log = log;
        this.objects=objects;
        framework.Update += OnUpdate;
    }
    private void OnUpdate(IFramework _)
    {
        lock (gate)
        {
            if (disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            if (!client.IsLoggedIn || !player.IsLoaded || player.ContentId == 0)
            { if (owner != 0) { owner = 0; service.Clear(); } return; }
            var now = Stopwatch.GetTimestamp();
            if (owner != player.ContentId) { owner = player.ContentId; nextRead = 0; service.Clear(); }
            if (now < nextRead) return;
            nextRead = now + Stopwatch.Frequency;
            try
            {
                var job = player.ClassJob;
                var values = Attributes.ToDictionary(attribute => attribute.ToString(), attribute => player.GetAttribute(attribute));
                uint? currentGp=null,maximumGp=null;
                var local=objects.LocalPlayer;
                if(local is not null && local.ClassJob.RowId==job.RowId)
                {
                    var current=local.CurrentGp; var maximum=local.MaxGp;
                    if(current<=maximum) { currentGp=current; maximumGp=maximum; }
                }
                if (!client.IsLoggedIn || !player.IsLoaded || player.ContentId != owner) { service.Clear(); return; }
                service.Publish(owner, player.CharacterName, job.RowId, job.Value.Name.ExtractText(), player.Level, values,currentGp,maximumGp);
            }
            catch (Exception error)
            {
                service.Clear(); nextRead = now + 5 * Stopwatch.Frequency;
                log.Warning(error, "Tensho character stats unavailable; retry in five seconds.");
            }
        }
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true; framework.Update -= OnUpdate; service.Clear();
        }
    }
}
