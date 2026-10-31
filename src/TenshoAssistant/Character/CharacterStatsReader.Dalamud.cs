using System.Diagnostics;
using Dalamud.Game.Player;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

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
    private readonly IDataManager? data;
    private readonly IGameGui? gui;
    public CharacterStatsReader(CharacterStatsService service, IFramework framework, IPlayerState player, IClientState client, IPluginLog log,IObjectTable objects,
        IDataManager? data=null,IGameGui? gui=null)
    {
        this.service = service; this.framework = framework; this.player = player; this.client = client; this.log = log;
        this.objects=objects;this.data=data;this.gui=gui;
        framework.Update += OnUpdate;
    }
    private void OnUpdate(IFramework _)
    {
        lock (gate)
        {
            if (disposed || !framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading) return;
            ReadWindow();
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
                // Job category, role and level modifiers from the game sheets (ClassJob, ParamGrow; 0.182).
                var category=(int)job.Value.ClassJobCategory.RowId;var role=(int)job.Value.Role;
                var modifiers=default(Character.LevelModifiers);
                if(data is not null && data.GetExcelSheet<ParamGrow>().TryGetRow((uint)player.Level,out var grow))
                    modifiers=new(StatFormulas.MainOf(player.Level) ?? 0,grow.BaseSpeed,grow.LevelModifier);
                service.Publish(owner, player.CharacterName, job.RowId, job.Value.Name.ExtractText(), player.Level, values,currentGp,maximumGp,category,role,modifiers);
            }
            catch (Exception error)
            {
                service.Clear(); nextRead = now + 5 * Stopwatch.Frequency;
                log.Warning(error, "Tensho character stats unavailable; retry in five seconds.");
            }
        }
    }
    // The game's Character window, read every frame so the panel follows it when moved.
    private unsafe void ReadWindow()
    {
        if (gui is null) return;
        var handle = gui.GetAddonByName("Character");
        CharacterWindowRect? rect = null;
        if (!handle.IsNull && handle.IsReady && handle.IsVisible && !gui.GameUiHidden)
        {
            var unit = (AtkUnitBase*)handle.Address;
            if (unit->RootNode != null) rect = new(unit->X, unit->Y, unit->RootNode->Width * unit->Scale, unit->RootNode->Height * unit->Scale);
        }
        if (rect != service.Window) service.SetWindow(rect);
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
