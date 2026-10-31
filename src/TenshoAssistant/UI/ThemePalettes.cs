using System.Numerics;

namespace TenshoAssistant.UI;

public enum PluginTheme { Dark, Light, ClassicFF, ClearBlue, ClearWhite, ClearGreen }

/// <summary>Plugin palettes modelled on the game's interface themes; colours are approximations, not extracted assets.</summary>
public static class ThemePalettes
{
    private static Vector4 C(float r,float g,float b,float a=1)=>new(r,g,b,a);
    public static readonly IReadOnlyDictionary<PluginTheme,ThemeColors> All=new Dictionary<PluginTheme,ThemeColors>
    {
        [PluginTheme.Dark]=new(C(0.075f,0.065f,0.055f),C(0.12f,0.105f,0.09f),C(0.34f,0.27f,0.17f),C(0.94f,0.77f,0.46f),C(0.91f,0.89f,0.83f),C(0.65f,0.69f,0.75f),
            C(0.52f,0.85f,0.66f),C(0.30f,0.23f,0.13f),C(0.25f,0.20f,0.14f),C(0.91f,0.69f,0.35f),C(0.88f,0.40f,0.35f)),
        [PluginTheme.Light]=new(C(0.93f,0.91f,0.86f),C(0.98f,0.97f,0.94f),C(0.62f,0.55f,0.43f),C(0.50f,0.33f,0.08f),C(0.12f,0.11f,0.09f),C(0.36f,0.34f,0.31f),
            C(0.10f,0.45f,0.26f),C(0.84f,0.77f,0.62f),C(0.89f,0.84f,0.73f),C(0.58f,0.37f,0.04f),C(0.66f,0.16f,0.12f)),
        [PluginTheme.ClassicFF]=new(C(0.05f,0.09f,0.26f),C(0.08f,0.14f,0.36f),C(0.62f,0.68f,0.82f),C(0.96f,0.84f,0.52f),C(0.96f,0.97f,1f),C(0.72f,0.78f,0.92f),
            C(0.55f,0.90f,0.65f),C(0.16f,0.27f,0.58f),C(0.12f,0.21f,0.47f),C(0.98f,0.78f,0.40f),C(0.98f,0.52f,0.46f)),
        [PluginTheme.ClearBlue]=new(C(0.04f,0.08f,0.13f),C(0.07f,0.13f,0.20f),C(0.27f,0.55f,0.75f),C(0.55f,0.85f,1f),C(0.92f,0.96f,1f),C(0.64f,0.74f,0.84f),
            C(0.52f,0.88f,0.72f),C(0.12f,0.30f,0.45f),C(0.10f,0.22f,0.34f),C(0.95f,0.78f,0.42f),C(0.95f,0.48f,0.44f)),
        [PluginTheme.ClearWhite]=new(C(0.86f,0.88f,0.90f),C(0.93f,0.94f,0.96f),C(0.55f,0.60f,0.66f),C(0.13f,0.34f,0.56f),C(0.10f,0.11f,0.13f),C(0.33f,0.36f,0.40f),
            C(0.10f,0.43f,0.27f),C(0.76f,0.82f,0.89f),C(0.82f,0.86f,0.91f),C(0.55f,0.36f,0.04f),C(0.64f,0.14f,0.12f)),
        [PluginTheme.ClearGreen]=new(C(0.04f,0.11f,0.08f),C(0.07f,0.16f,0.12f),C(0.30f,0.62f,0.45f),C(0.62f,0.92f,0.68f),C(0.93f,0.98f,0.94f),C(0.66f,0.80f,0.72f),
            C(0.60f,0.95f,0.70f),C(0.12f,0.33f,0.22f),C(0.10f,0.25f,0.17f),C(0.95f,0.80f,0.42f),C(0.95f,0.50f,0.46f)),
    };

    /// <summary>
    /// Game ColorThemeType → palette. 0–3 are the long-standing themes (dark, light, Classic FF, Clear Blue); 4 and 5 are
    /// assumed to be the later clear variants and unknown values fall back to dark. The settings show the raw value and allow
    /// an explicit choice when this mapping is wrong.
    /// </summary>
    public static PluginTheme FromGame(uint? value)=>value switch
    {
        1=>PluginTheme.Light,2=>PluginTheme.ClassicFF,3=>PluginTheme.ClearBlue,4=>PluginTheme.ClearWhite,5=>PluginTheme.ClearGreen,_=>PluginTheme.Dark,
    };
    /// <summary>Setting 0 follows the game; 1..6 force PluginTheme value-1.</summary>
    public static PluginTheme Resolve(int setting,uint? game)
        =>setting>=1 && setting<=All.Count && Enum.IsDefined((PluginTheme)(setting-1)) ? (PluginTheme)(setting-1):FromGame(game);

    public static double Contrast(Vector4 first,Vector4 second)
    {
        static double Luminance(Vector4 colour)
        {
            static double Channel(float value)=>value<=0.03928 ? value/12.92:Math.Pow((value+0.055)/1.055,2.4);
            return 0.2126*Channel(colour.X)+0.7152*Channel(colour.Y)+0.0722*Channel(colour.Z);
        }
        var (light,dark)=(Math.Max(Luminance(first),Luminance(second)),Math.Min(Luminance(first),Luminance(second)));
        return (light+0.05)/(dark+0.05);
    }
}

// Colours of one theme; Accent replaces the former fixed gold.
public sealed record ThemeColors(Vector4 Background,Vector4 Panel,Vector4 Border,Vector4 Accent,Vector4 Text,Vector4 TextMuted,
    Vector4 Success,Vector4 Selected,Vector4 Hovered,Vector4 Warning,Vector4 Error);
