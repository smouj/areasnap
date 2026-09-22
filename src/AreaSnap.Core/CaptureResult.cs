namespace AreaSnap.Core;

/// <summary>
/// Result of a screenshot capture.
/// </summary>
public sealed record CaptureResult(
    string? FilePath,
    System.Drawing.Rectangle Region,
    int Width,
    int Height,
    long FileSizeBytes)
{
    public string DisplayText => FilePath is not null
        ? $"Saved · {Path.GetFileName(FilePath)} · {Width}×{Height} · {FormatSize(FileSizeBytes)}"
        : $"Copied · {Width}×{Height}";

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.0} KB",
        _ => $"{bytes / (1024.0 * 1024.0):0.0} MB"
    };
}