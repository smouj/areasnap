using System.Drawing;
using System.Text.Json;
using AreaSnap.Capture;
using AreaSnap.Core;

namespace AreaSnap.App;

/// <summary>
/// Persisted application settings stored in %APPDATA%/AreaSnap/settings.json.
/// </summary>
public sealed record AppSettings(
    string SaveFolder,
    string FileNameTemplate,
    OutputFormat Format,
    int JpgQuality,
    bool CopyToClipboard,
    bool SaveToFile,
    CaptureMode DefaultMode)
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AreaSnap", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? Default();
            }
        }
        catch
        {
            // Corrupted settings — start fresh
        }
        return Default();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Best effort
        }
    }

    public static AppSettings Default() => new(
        SaveFolder: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures", "AreaSnap"),
        FileNameTemplate: CaptureSettings.DefaultFileNameTemplate,
        Format: OutputFormat.Png,
        JpgQuality: 90,
        CopyToClipboard: true,
        SaveToFile: true,
        DefaultMode: CaptureMode.Region
    );
}