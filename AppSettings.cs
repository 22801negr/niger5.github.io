using System.IO;
using System.Windows;
using System.Windows.Media;

namespace WarehousePro.Wpf;

public static class AppSettings
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WarehousePro.Wpf",
        "settings.json");

    public static string Theme { get; private set; } = "Dark";
    public static string Language { get; private set; } = "ru";

    static AppSettings()
    {
        LoadSettings();
    }

    public static void SetTheme(string theme)
    {
        if (theme == "Light" || theme == "Dark" || theme == "Blue")
            Theme = theme;
    }

    public static void SetLanguage(string language)
    {
        if (language == "ru" || language == "en")
            Language = language;
    }

    public static void LoadSettings()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Theme = "Dark";
                Language = "ru";
                return;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = System.Text.Json.JsonSerializer.Deserialize<SettingsData>(json);

            if (settings == null)
                return;

            Theme = settings.Theme is "Light" or "Dark" or "Blue" ? settings.Theme : "Dark";
            Language = settings.Language is "ru" or "en" ? settings.Language : "ru";
        }
        catch
        {
            Theme = "Dark";
            Language = "ru";
        }
    }

    public static void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var settings = new SettingsData { Theme = Theme, Language = Language };
            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
            // ignore file IO issues silently
        }
    }

    public static void ApplyThemeToWindow(Window window) => ApplyThemeResources();

    public static void ApplyThemeToAllOpenWindows() => ApplyThemeResources();

    public static void ApplyThemeResources()
    {
        if (Application.Current == null)
            return;

        var palette = GetPalette();
        SetBrush("BgMain", palette.BgMain);
        SetBrush("BgPanel", palette.BgPanel);
        SetBrush("BgPanelAlt", palette.BgPanelAlt);
        SetBrush("BgInput", palette.BgInput);
        SetBrush("BgRow", palette.BgRow);
        SetBrush("BgRowAlt", palette.BgRowAlt);
        SetBrush("BgHover", palette.BgHover);
        SetBrush("BorderSoft", palette.BorderSoft);
        SetBrush("TextPrimary", palette.TextPrimary);
        SetBrush("TextSecondary", palette.TextSecondary);
        SetBrush("TextMuted", palette.TextMuted);
        SetBrush("AccentBlue", palette.AccentBlue);
        SetBrush("AccentGreen", palette.AccentGreen);
        SetBrush("AccentRed", palette.AccentRed);
        SetBrush("AccentGold", palette.AccentGold);
        SetBrush("AccentPurple", palette.AccentPurple);
    }

    private static void SetBrush(string key, Color color)
    {
        Application.Current.Resources[key] = new SolidColorBrush(color);
    }

    private static Palette GetPalette()
    {
        return Theme switch
        {
            "Light" => new Palette(
                Color.FromRgb(240, 244, 249),
                Color.FromRgb(255, 255, 255),
                Color.FromRgb(246, 249, 252),
                Color.FromRgb(242, 246, 250),
                Color.FromRgb(255, 255, 255),
                Color.FromRgb(236, 242, 248),
                Color.FromRgb(226, 233, 241),
                Color.FromRgb(214, 220, 232),
                Color.FromRgb(19, 31, 45),
                Color.FromRgb(80, 95, 112),
                Color.FromRgb(107, 119, 137),
                Color.FromRgb(59, 130, 246),
                Color.FromRgb(22, 163, 74),
                Color.FromRgb(220, 38, 38),
                Color.FromRgb(217, 119, 6),
                Color.FromRgb(124, 58, 237)),
            "Blue" => new Palette(
                Color.FromRgb(11, 25, 42),
                Color.FromRgb(19, 38, 62),
                Color.FromRgb(23, 45, 71),
                Color.FromRgb(20, 37, 56),
                Color.FromRgb(24, 48, 76),
                Color.FromRgb(16, 32, 54),
                Color.FromRgb(36, 64, 100),
                Color.FromRgb(45, 74, 113),
                Color.FromRgb(240, 246, 255),
                Color.FromRgb(156, 182, 220),
                Color.FromRgb(124, 147, 180),
                Color.FromRgb(59, 130, 246),
                Color.FromRgb(34, 197, 94),
                Color.FromRgb(248, 113, 113),
                Color.FromRgb(245, 158, 11),
                Color.FromRgb(139, 92, 246)),
            _ => new Palette(
                Color.FromRgb(11, 18, 32),
                Color.FromRgb(16, 26, 43),
                Color.FromRgb(18, 31, 50),
                Color.FromRgb(23, 37, 55),
                Color.FromRgb(22, 37, 54),
                Color.FromRgb(16, 26, 43),
                Color.FromRgb(35, 54, 80),
                Color.FromRgb(31, 43, 61),
                Color.FromRgb(243, 247, 255),
                Color.FromRgb(154, 176, 200),
                Color.FromRgb(125, 147, 173),
                Color.FromRgb(59, 130, 246),
                Color.FromRgb(34, 197, 94),
                Color.FromRgb(248, 113, 113),
                Color.FromRgb(245, 158, 11),
                Color.FromRgb(139, 92, 246))
        };
    }

    private sealed class SettingsData
    {
        public string Theme { get; set; } = "Dark";
        public string Language { get; set; } = "ru";
    }

    private readonly record struct Palette(
        Color BgMain,
        Color BgPanel,
        Color BgPanelAlt,
        Color BgInput,
        Color BgRow,
        Color BgRowAlt,
        Color BgHover,
        Color BorderSoft,
        Color TextPrimary,
        Color TextSecondary,
        Color TextMuted,
        Color AccentBlue,
        Color AccentGreen,
        Color AccentRed,
        Color AccentGold,
        Color AccentPurple);
}
