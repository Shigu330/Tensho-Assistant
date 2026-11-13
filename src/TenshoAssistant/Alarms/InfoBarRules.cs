namespace TenshoAssistant.Alarms;

/// <summary>X-217 (0.1226): the pure rules of the optional server bar entries (InfoDtrEntries).</summary>
public static class InfoBarRules
{
    /// <summary>The instance number to show: the game's public instance only while it is the current territory's, and only 1 to 9 (the
    /// game's own instance glyphs); anything else shows the zone alone, never a guessed number.</summary>
    public static int? Instance(uint instanceTerritory,uint instanceId,uint territory)
        => territory != 0 && instanceTerritory == territory && instanceId is >= 1 and <= 9 ? (int)instanceId : null;
}
