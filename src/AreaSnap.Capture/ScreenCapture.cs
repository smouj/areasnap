using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AreaSnap.Core;

namespace AreaSnap.Capture;

/// <summary>
/// Captures screenshots using Windows GDI and Desktop Duplication.
/// Supports full screen, region selection, and window capture.
/// </summary>
public sealed class ScreenCapture : IDisposable
{
    private bool _disposed;

    /// <summary>
    /// Captures the entire primary screen.
    /// </summary>
    public Bitmap CaptureFullScreen()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var screen = System.Windows.Forms.Screen.PrimaryScreen!;
        var bounds = screen.Bounds;

        var bitmap = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

        return bitmap;
    }

    /// <summary>
    /// Captures a specific rectangular region of the screen.
    /// </summary>
    public Bitmap CaptureRegion(Rectangle region)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (region.Width <= 0 || region.Height <= 0)
            throw new ArgumentException("Region must have positive width and height.", nameof(region));

        var bitmap = new Bitmap(region.Width, region.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.CopyFromScreen(region.X, region.Y, 0, 0, region.Size, CopyPixelOperation.SourceCopy);

        return bitmap;
    }

    /// <summary>
    /// Captures a specific window by its handle.
    /// </summary>
    public Bitmap CaptureWindow(IntPtr hwnd)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        GetWindowRect(hwnd, out var rect);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException("Window has zero or negative dimensions.");

        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        // Use PrintWindow for windows that don't respond to CopyFromScreen
        var hdc = graphics.GetHdc();
        try
        {
            PrintWindow(hwnd, hdc, 2); // PW_RENDERFULLCONTENT
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }

        return bitmap;
    }

    /// <summary>
    /// Saves a bitmap to the specified file in the given format.
    /// </summary>
    public static string SaveBitmap(Bitmap bitmap, string filePath, OutputFormat format, int jpgQuality = 90)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        switch (format)
        {
            case OutputFormat.Jpg:
                var jpgEncoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                    .First(e => e.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                var jpgParams = new System.Drawing.Imaging.EncoderParameters(1);
                jpgParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality, jpgQuality);
                bitmap.Save(filePath, jpgEncoder, jpgParams);
                break;

            default: // PNG
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                break;
        }

        return filePath;
    }

    /// <summary>
    /// Copies a bitmap to the system clipboard.
    /// </summary>
    public static void CopyToClipboard(Bitmap bitmap)
    {
        System.Windows.Forms.Clipboard.SetImage(bitmap);
    }

    /// <summary>
    /// Generates a filename from a template using the current timestamp.
    /// </summary>
    public static string GenerateFileName(string template, OutputFormat format)
    {
        var extension = format == OutputFormat.Jpg ? ".jpg" : ".png";
        var now = DateTime.Now;
        var name = template
            .Replace("{yyyyMMdd}", now.ToString("yyyyMMdd"))
            .Replace("{yyyyMMdd-HHmmss}", now.ToString("yyyyMMdd-HHmmss"))
            .Replace("{HHmmss}", now.ToString("HHmmss"))
            .Replace("{date}", now.ToString("yyyy-MM-dd"))
            .Replace("{time}", now.ToString("HH-mm-ss"));

        return name + extension;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}