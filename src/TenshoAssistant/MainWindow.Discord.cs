using Dalamud.Bindings.ImGui;
using TenshoAssistant.Alarms;

namespace TenshoAssistant;

/// <summary>
/// R-12 (0.319, user request 2026-11-02): Tensho alarms posted to a Discord channel through a webhook. Settings › Alarms › Discord:
/// switch (off by default), webhook address typed by the player (masked, checked against Discord's own hosts), sender name, message
/// template, categories, character name (off), role mention, test post and the last result. Posts leave on DiscordNotifier's worker;
/// clearing or changing the address, or turning sending off, drops what is still waiting.
/// </summary>
internal sealed partial class MainWindow
{
    internal string? AlarmItemName(uint item)=>item==0 ? null:itemCatalog.State.Catalog?.Find(item)?.Name;
    internal string? AlarmZoneName(uint territory)=>territory==0 ? null:maps.State.Catalog?.Entries.FirstOrDefault(map=>map.TerritoryId==territory)?.Zone;
    private DiscordNotifier? discordNotifier;
    internal void ConfigureDiscord(DiscordNotifier notifier)=>discordNotifier=notifier;
    private static readonly DiscordSettings DiscordDefaults=new();
    // Text being edited, saved when the field is left after an edit (never a disk write per key press); a refused value stays shown.
    private string? discordUrlEdit,discordNameEdit,discordTemplateEdit,discordRoleEdit;
    // Discord message of an alarm group being typed (Alarms page), by job section and group key (0.331: the same item can be in two sections).
    private readonly Dictionary<string,string> discordGroupEdits=[];
    // 0.598 (T24): a channel per category, typed like the main address.
    private readonly Dictionary<AlarmSource,string?> discordCategoryEdits=[];
    private static readonly (AlarmSource Source,string Key)[] DiscordCategories=[(AlarmSource.Gathering,"Discord.Gathering"),(AlarmSource.Fishing,"Discord.Fishing"),(AlarmSource.Reminder,"Discord.Reminders"),
        (AlarmSource.Voyage,"Discord.Voyages"),(AlarmSource.Other,"Discord.Other"),(AlarmSource.Currency,"Discord.Currencies"),(AlarmSource.DutyPop,"Discord.DutyPop")];
    private void DrawDiscordCategoryChannels()
    {
        if(!ImGui.TreeNode(localization.Get("Discord.Channels")+"###DiscordChannels"))return;
        try
        {
            ImGui.TextWrapped(localization.Get("Discord.ChannelsHelp"));
            foreach(var (source,key) in DiscordCategories)
            {
                ImGui.PushID((int)source);
                try
                {
                    var edit=discordCategoryEdits.GetValueOrDefault(source);
                    string Read()=>configuration.Discord.CategoryUrls?.GetValueOrDefault(source) ?? "";
                    var before=Read();
                    DiscordText(key,"Discord.ChannelTip",ref edit,Read,value=>
                    {
                        var urls=configuration.Discord.CategoryUrls??=[];
                        if(value.Length==0)urls.Remove(source);else urls[source]=value;
                    },"",300,localization.Get("Discord.ChannelHint"),typed=>typed.Trim() is var url && (url.Length==0 || DiscordWebhook.IsValidUrl(url)) ? url:null,"Discord.UrlInvalid",ImGuiInputTextFlags.Password,reset:false);
                    discordCategoryEdits[source]=edit;
                    // A new or cleared address: what was queued for the old one is not sent, as for the main one.
                    if(Read()!=before)discordNotifier?.Clear();
                    if(edit is null && Read().Length>0)ImGui.TextDisabled(DiscordWebhook.Redact(Read()));
                }
                finally{ImGui.PopID();}
            }
        }
        finally{ImGui.TreePop();}
    }

    private void DrawDiscordSettings()
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("Discord.Title")+"###DiscordSettings"))return;
        var settings=configuration.Discord??=new();
        ImGui.TextWrapped(localization.Get("Discord.Help"));
        BoolSetting("Discord.Enabled","Discord.EnabledTip",()=>configuration.Discord.Enabled,value=>{configuration.Discord.Enabled=value;if(!value)discordNotifier?.Clear();},c=>c.Discord.Enabled);
        var before=settings.WebhookUrl;
        DiscordText("Discord.Url","Discord.UrlTip",ref discordUrlEdit,()=>configuration.Discord.WebhookUrl,value=>configuration.Discord.WebhookUrl=value,"",300,
            localization.Get("Discord.UrlHint"),typed=>typed.Trim() is var url && (url.Length==0 || DiscordWebhook.IsValidUrl(url)) ? url:null,"Discord.UrlInvalid",ImGuiInputTextFlags.Password,reset:false);
        if(settings.WebhookUrl.Length>0)
        {
            ImGui.SameLine();
            if(DeleteButton("Discord.Clear")){SaveSetting("Discord.Url",()=>configuration.Discord.WebhookUrl,value=>configuration.Discord.WebhookUrl=value,"");discordUrlEdit=null;}
            Tip("Discord.ClearTip");
        }
        // A new or cleared address: what was queued for the old one is not sent.
        if(settings.WebhookUrl!=before)discordNotifier?.Clear();
        if(discordUrlEdit is null)
        {
            if(settings.WebhookUrl.Length==0)ImGui.TextDisabled(localization.Get("Discord.UrlEmpty"));
            else ImGui.TextColored(Success,localization.Format("Discord.UrlSaved",DiscordWebhook.Redact(settings.WebhookUrl)));
        }
        // X-76 step 3 (0.952): every post goes through ShouldSend, which needs « Post alarms to Discord »: what shapes a post is indented
        // under it and greyed while it is off. The address and the test post (sent without that switch) stay outside.
        ImGui.Indent();
        ImGui.BeginDisabled(!configuration.Discord.Enabled);
        try
        {
            DiscordText("Discord.Username","Discord.UsernameTip",ref discordNameEdit,()=>configuration.Discord.Username,value=>configuration.Discord.Username=value,DiscordDefaults.Username,80,"",
                typed=>DiscordWebhook.Username(typed));
            DiscordText("Discord.Template","Discord.TemplateTip",ref discordTemplateEdit,()=>configuration.Discord.Template,value=>configuration.Discord.Template=value,DiscordDefaults.Template,300,
                localization.Get("Discord.DefaultTemplate"),typed=>typed.Trim());
            ImGui.TextDisabled(localization.Get("Discord.Categories"));
            ImGui.Indent();
            try
            {
                BoolSetting("Discord.Gathering","Discord.CategoryTip",()=>configuration.Discord.Gathering,value=>configuration.Discord.Gathering=value,c=>c.Discord.Gathering);
                BoolSetting("Discord.Fishing","Discord.CategoryTip",()=>configuration.Discord.Fishing,value=>configuration.Discord.Fishing=value,c=>c.Discord.Fishing);
                BoolSetting("Discord.Reminders","Discord.CategoryTip",()=>configuration.Discord.Reminders,value=>configuration.Discord.Reminders=value,c=>c.Discord.Reminders);
                BoolSetting("Discord.Voyages","Discord.CategoryTip",()=>configuration.Discord.Voyages,value=>configuration.Discord.Voyages=value,c=>c.Discord.Voyages);
                BoolSetting("Discord.Other","Discord.CategoryTip",()=>configuration.Discord.Other,value=>configuration.Discord.Other=value,c=>c.Discord.Other);
                // X-76 step 3 (0.952): these two are sent only when their alert is on elsewhere (Storage › Currencies « Cap alerts »,
                // Alarms « Alert when a duty is ready »): greyed while it is off, their tips name it.
                ImGui.BeginDisabled(!configuration.CurrencyAlerts);
                try{BoolSetting("Discord.Currencies","Discord.CurrenciesTip",()=>configuration.Discord.Currencies,value=>configuration.Discord.Currencies=value,c=>c.Discord.Currencies);}
                finally{ImGui.EndDisabled();}
                ImGui.BeginDisabled(configuration.AlarmOptions?.DutyPopAlert!=true);
                try{BoolSetting("Discord.DutyPop","Discord.DutyPopTip",()=>configuration.Discord.DutyPop,value=>configuration.Discord.DutyPop=value,c=>c.Discord.DutyPop);}
                finally{ImGui.EndDisabled();}
            }
            finally{ImGui.Unindent();}
            DrawDiscordCategoryChannels();
            BoolSetting("Discord.Character","Discord.CharacterTip",()=>configuration.Discord.IncludeCharacter,value=>configuration.Discord.IncludeCharacter=value,c=>c.Discord.IncludeCharacter);
            DiscordText("Discord.Mention","Discord.MentionTip",ref discordRoleEdit,()=>configuration.Discord.MentionRoleId,value=>configuration.Discord.MentionRoleId=value,DiscordDefaults.MentionRoleId,32,"",
                DiscordWebhook.RoleIdFrom,"Discord.MentionInvalid");
        }
        finally{ImGui.EndDisabled();ImGui.Unindent();}
        // Test post: only with a saved valid address, on the player's click; one at a time.
        var state=discordNotifier?.State;
        var ready=DiscordWebhook.IsValidUrl(settings.WebhookUrl) && state is not { Pending:>0 };
        if(!ready)ImGui.BeginDisabled();
        try
        {
            if(ImGui.SmallButton(localization.Get("Discord.Test")+"###DiscordTest") && discordNotifier is { } notifier)
                notifier.Enqueue(new(settings.WebhookUrl,DiscordWebhook.Payload(settings,DiscordWebhook.Format(settings,localization.Get("Discord.DefaultTemplate"),
                    localization.Get("Discord.TestMessage"),localization.Get("Discord.TestLabel"),5,DateTimeOffset.Now.AddMinutes(5),null))));
        }
        finally{if(!ready)ImGui.EndDisabled();}
        Tip("Discord.TestTip");
        if(state is { } shown && shown.Sent+shown.Failed+shown.Pending>0)
        {
            ImGui.SameLine();
            var line=shown.Pending>0 ? localization.Format("Discord.Pending",shown.Pending):localization.Format("Discord.Status"+shown.Status,shown.Code);
            ImGui.TextColored(shown.Pending>0 || shown.Status is DiscordStatus.Sent or DiscordStatus.Idle ? Success:UI.FfxivTheme.Error,line);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("Discord.Counts",shown.Sent,shown.Failed,shown.Dropped));
        }
        if(discordNotifier?.Block is { TotalSeconds:>0 } block)ImGui.TextColored(Accent,localization.Format("Discord.Blocked",Math.Ceiling(block.TotalSeconds)));
        if(settings.Enabled && !DiscordWebhook.IsValidUrl(settings.WebhookUrl) && settings.CategoryUrls is not {Count:>0})ImGui.TextColored(Accent,localization.Get("Discord.NeedsUrl"));
    }

    // For the diagnostic report: only whether the saved address has a webhook's shape, never the address.
    private string DiscordWebhookShape()=>DiscordWebhook.IsValidUrl(configuration.Discord?.WebhookUrl) ? "valid":"invalid";

    /// <summary>
    /// A text setting saved when its field is left after an edit (or when it is no longer edited, e.g. its header was folded), with its
    /// tooltip and the « ↺ » button. <paramref name="accept"/> gives the value to save, or null to refuse it: the typed text then stays in
    /// the field with the <paramref name="invalidKey"/> message, and the saved value is not touched.
    /// </summary>
    private void DiscordText(string labelKey,string tipKey,ref string? edit,Func<string> read,Action<string> write,string fallback,int maximum,string hint,
        Func<string,string?> accept,string? invalidKey=null,ImGuiInputTextFlags flags=ImGuiInputTextFlags.None,bool reset=true)
    {
        var text=edit ?? read();
        ImGui.SetNextItemWidth(Math.Min(420,ImGui.GetContentRegionAvail().X));
        if(ImGui.InputTextWithHint(localization.Get(labelKey)+"###"+labelKey,hint,ref text,maximum,flags))edit=text;
        var active=ImGui.IsItemActive();
        if(edit is { } typed && (ImGui.IsItemDeactivatedAfterEdit() || !active))
        {
            if(accept(typed) is { } value){if(value!=read())SaveSetting(labelKey,read,write,value);edit=null;}
        }
        Tip(tipKey);
        if(reset && DefaultButton(labelKey,read()!=fallback)){SaveSetting(labelKey,read,write,fallback);edit=null;}
        if(edit is not null && invalidKey is not null && !active && accept(edit) is null)ImGui.TextColored(UI.FfxivTheme.Error,localization.Get(invalidKey));
    }
}
