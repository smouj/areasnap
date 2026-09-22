using System.Drawing;
using System.Drawing.Imaging;
using AreaSnap.Capture;
using AreaSnap.Core;

namespace AreaSnap.App;

internal sealed class MainForm : Form
{
    private readonly ComboBox _mode = new();
    private readonly ComboBox _format = new();
    private readonly CheckBox _clipboard = new();
    private readonly CheckBox _saveFile = new();
    private readonly TextBox _saveFolder = new();
    private readonly Button _browseButton = new();
    private readonly Button _capture = new();
    private readonly Label _status = new();
    private readonly NotifyIcon _tray;
    private AppSettings _settings;
    private ScreenCapture _screenCapture = new();

    private const int HotkeyId = 0x4153; // "AS"
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint VirtualKeyS = 0x53;

    public MainForm()
    {
        Text = "AreaSnap";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(420, 340);
        _settings = AppSettings.Load();

        var title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            Location = new Point(24, 22),
            Text = "AreaSnap",
        };

        var description = new Label
        {
            AutoSize = true,
            Location = new Point(26, 64),
            Text = "Select an area. Capture it. Save or copy.",
        };

        // Capture mode
        var modeLabel = new Label { AutoSize = true, Location = new Point(26, 100), Text = "Capture" };
        _mode.DropDownStyle = ComboBoxStyle.DropDownList;
        _mode.Items.AddRange(Enum.GetNames<CaptureMode>());
        _mode.SelectedItem = _settings.DefaultMode.ToString();
        _mode.Location = new Point(100, 96);
        _mode.Size = new Size(140, 28);

        // Format
        var formatLabel = new Label { AutoSize = true, Location = new Point(260, 100), Text = "Format" };
        _format.DropDownStyle = ComboBoxStyle.DropDownList;
        _format.Items.AddRange(Enum.GetNames<OutputFormat>());
        _format.SelectedItem = _settings.Format.ToString();
        _format.Location = new Point(314, 96);
        _format.Size = new Size(80, 28);

        // Clipboard checkbox
        _clipboard.AutoSize = true;
        _clipboard.Checked = _settings.CopyToClipboard;
        _clipboard.Location = new Point(26, 140);
        _clipboard.Text = "Copy to clipboard";

        // Save file checkbox
        _saveFile.AutoSize = true;
        _saveFile.Checked = _settings.SaveToFile;
        _saveFile.Location = new Point(200, 140);
        _saveFile.Text = "Save to file";
        _saveFile.CheckedChanged += (_, _) =>
        {
            _saveFolder.Enabled = _saveFile.Checked;
            _browseButton.Enabled = _saveFile.Checked;
        };

        // Save folder
        var folderLabel = new Label { AutoSize = true, Location = new Point(26, 175), Text = "Save to" };
        _saveFolder.Location = new Point(26, 193);
        _saveFolder.Size = new Size(320, 28);
        _saveFolder.Font = new Font("Segoe UI", 9);
        _saveFolder.Text = _settings.SaveFolder;
        _saveFolder.Enabled = _settings.SaveToFile;

        _browseButton.Location = new Point(352, 193);
        _browseButton.Size = new Size(46, 28);
        _browseButton.Text = "…";
        _browseButton.Enabled = _settings.SaveToFile;
        _browseButton.Click += BrowseFolder;

        // Capture button
        _capture.Location = new Point(26, 240);
        _capture.Size = new Size(372, 42);
        _capture.Text = "Capture";
        _capture.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        _capture.BackColor = Color.FromArgb(46, 139, 87);
        _capture.ForeColor = Color.White;
        _capture.FlatStyle = FlatStyle.Flat;
        _capture.FlatAppearance.BorderSize = 0;
        _capture.Click += async (_, _) => await DoCaptureAsync();

        // Status
        _status.AutoSize = true;
        _status.Location = new Point(26, 295);
        _status.Text = "Ready · Ctrl+Shift+S to capture";
        _status.Font = new Font("Segoe UI", 9);

        Controls.AddRange(
        [
            title, description,
            modeLabel, _mode, formatLabel, _format,
            _clipboard, _saveFile,
            folderLabel, _saveFolder, _browseButton,
            _capture, _status
        ]);

        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Capture region", null, (_, _) => DoCaptureAsync());
        trayMenu.Items.Add("Capture fullscreen", null, (_, _) => DoCaptureFullscreenAsync());
        trayMenu.Items.Add("Exit", null, (_, _) => Close());
        _tray = new NotifyIcon
        {
            ContextMenuStrip = trayMenu,
            Icon = LoadApplicationIcon(),
            Text = "AreaSnap",
            Visible = true,
        };
        _tray.DoubleClick += (_, _) =>
        {
            Show();
            Activate();
        };

        Load += (_, _) => RegisterGlobalHotkey();
        FormClosed += (_, _) =>
        {
            UnregisterHotKey(Handle, HotkeyId);
            _tray.Dispose();
            _screenCapture.Dispose();
        };
    }

    private static Icon LoadApplicationIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch (ArgumentException)
        {
            return SystemIcons.Application;
        }
    }

    private void BrowseFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select save folder",
            SelectedPath = _saveFolder.Text,
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _saveFolder.Text = dialog.SelectedPath;
    }

    private async Task DoCaptureAsync()
    {
        var mode = Enum.Parse<CaptureMode>((string)_mode.SelectedItem!);
        await DoCaptureAsync(mode);
    }

    private async Task DoCaptureFullscreenAsync()
    {
        await DoCaptureAsync(CaptureMode.FullScreen);
    }

    private async Task DoCaptureAsync(CaptureMode mode)
    {
        _status.Text = "Capturing…";

        // Save settings
        _settings = _settings with
        {
            DefaultMode = mode,
            Format = Enum.Parse<OutputFormat>((string)_format.SelectedItem!),
            CopyToClipboard = _clipboard.Checked,
            SaveToFile = _saveFile.Checked,
            SaveFolder = _saveFolder.Text,
        };
        AppSettings.Save(_settings);

        Hide();

        try
        {
            // Small delay to let the UI hide
            await Task.Delay(150);

            Rectangle region;
            Bitmap bitmap;

            switch (mode)
            {
                case CaptureMode.Region:
                    var regionResult = await SelectRegionAsync();
                    if (regionResult is null)
                    {
                        _status.Text = "Selection cancelled";
                        return;
                    }
                    region = regionResult.Value;
                    bitmap = _screenCapture.CaptureRegion(region);
                    break;

                case CaptureMode.Window:
                    _status.Text = "Click on a window to capture it";
                    Show();
                    return;

                case CaptureMode.FullScreen:
                default:
                    var screen = System.Windows.Forms.Screen.PrimaryScreen!;
                    region = screen.Bounds;
                    bitmap = _screenCapture.CaptureFullScreen();
                    break;
            }

            var format = _settings.Format;
            var fileName = ScreenCapture.GenerateFileName(_settings.FileNameTemplate, format);
            string? filePath = null;

            try
            {
                if (_settings.CopyToClipboard)
                {
                    ScreenCapture.CopyToClipboard(bitmap);
                    _status.Text = $"Copied to clipboard · {bitmap.Width}×{bitmap.Height}";
                }

                if (_settings.SaveToFile && !string.IsNullOrWhiteSpace(_settings.SaveFolder))
                {
                    if (!Directory.Exists(_settings.SaveFolder))
                        Directory.CreateDirectory(_settings.SaveFolder);

                    filePath = Path.Combine(_settings.SaveFolder, fileName);
                    ScreenCapture.SaveBitmap(bitmap, filePath, format, _settings.JpgQuality);

                    var size = new FileInfo(filePath).Length;
                    if (_settings.CopyToClipboard)
                        _status.Text = $"Copied + saved · {fileName} · {bitmap.Width}×{bitmap.Height} · {FormatSize(size)}";
                    else
                        _status.Text = $"Saved · {fileName} · {bitmap.Width}×{bitmap.Height} · {FormatSize(size)}";
                }
                else if (!_settings.CopyToClipboard)
                {
                    _status.Text = $"Captured · {bitmap.Width}×{bitmap.Height}";
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }
        catch (Exception ex)
        {
            _status.Text = $"Capture error: {DescribeException(ex)}";
        }
        finally
        {
            Show();
            Activate();
        }
    }

    private Task<Rectangle?> SelectRegionAsync()
    {
        var tcs = new TaskCompletionSource<Rectangle?>();

        // Run the selector on a new STA thread (required for WinForms)
        var thread = new Thread(() =>
        {
            Rectangle? result = null;
            using var selector = new RegionSelectorForm(
                region => result = region,
                () => { });

            selector.ShowDialog();
            tcs.SetResult(result);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.0} KB",
        _ => $"{bytes / (1024.0 * 1024.0):0.0} MB"
    };

    private static string DescribeException(Exception ex)
    {
        var messages = new List<string>();
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (!messages.Contains(current.Message, StringComparer.Ordinal))
                messages.Add(current.Message);
        }
        return string.Join(" → ", messages);
    }

    private void RegisterGlobalHotkey()
    {
        if (!RegisterHotKey(Handle, HotkeyId, ModControl | ModShift, VirtualKeyS))
            _status.Text = "Ready · Ctrl+Shift+S unavailable";
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr windowHandle, int id);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            _ = DoCaptureAsync();
            return;
        }
        base.WndProc(ref m);
    }
}