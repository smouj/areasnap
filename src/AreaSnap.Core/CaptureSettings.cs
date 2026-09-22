namespace AreaSnap.Core;

/// <summary>
/// Capture mode for a screenshot.
/// </summary>
public enum CaptureMode
{
    /// <summary>Capture the entire screen.</summary>
    FullScreen,

    /// <summary>Let the user select a rectangular region.</summary>
    Region,

    /// <summary>Capture a specific window.</summary>
    Window,
}

/// <summary>
/// Output format for the captured image.
/// </summary>
public enum OutputFormat
{
    Png,
    Jpg,
}

/// <summary>
/// Settings for a single capture operation.
/// </summary>
public sealed record CaptureSettings(
    CaptureMode Mode,
    OutputFormat Format,
    int JpgQuality,
    bool CopyToClipboard,
    bool SaveToFile,
    string SaveFolder,
    string FileNameTemplate)
{
    public const string DefaultFileNameTemplate = "AreaSnap-{yyyyMMdd-HHmmss}";
}