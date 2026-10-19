using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace AreaSnap.App;

internal sealed record CaptureWindowChoice(IntPtr Handle, string Title, Rectangle Bounds)
{
    public string DisplayText => $"{Title}  ·  {Bounds.Width}×{Bounds.Height}";
}

internal sealed class WindowPickerForm : Form
{
    private readonly ListBox _windows = new();
    private readonly Label _status = new();
    private readonly Button _capture = new();

    public CaptureWindowChoice? SelectedWindow => _windows.SelectedItem as CaptureWindowChoice;

    public WindowPickerForm()
    {
        Text = "Choose a window";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 390);

        var hint = new Label
        {
            AutoSize = false,
            Location = new Point(20, 16),
            Size = new Size(520, 42),
            Text = "Choose a visible window to capture. Some apps may block window capture.",
        };

        _windows.Location = new Point(20, 64);
        _windows.Size = new Size(520, 245);
        _windows.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _windows.DisplayMember = nameof(CaptureWindowChoice.DisplayText);
        _windows.SelectedIndexChanged += (_, _) => _capture.Enabled = SelectedWindow is not null;

        _status.AutoEllipsis = true;
        _status.Location = new Point(20, 320);
        _status.Size = new Size(300, 28);

        var refresh = new Button
        {
            Location = new Point(20, 354),
            Size = new Size(94, 28),
            Text = "Refresh",
        };
        refresh.Click += (_, _) => RefreshWindows();

        var cancel = new Button
        {
            Location = new Point(346, 354),
            Size = new Size(94, 28),
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
        };

        _capture.Location = new Point(446, 354);
        _capture.Size = new Size(94, 28);
        _capture.Text = "Capture";
        _capture.Enabled = false;
        _capture.Click += (_, _) =>
        {
            if (SelectedWindow is not null)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };

        Controls.AddRange([hint, _windows, _status, refresh, cancel, _capture]);
        AcceptButton = _capture;
        CancelButton = cancel;
        Load += (_, _) => RefreshWindows();
    }

    private void RefreshWindows()
    {
        var windows = WindowEnumerator.GetVisibleWindows();
        _windows.BeginUpdate();
        _windows.Items.Clear();
        _windows.Items.AddRange(windows.Cast<object>().ToArray());
        if (_windows.Items.Count > 0)
            _windows.SelectedIndex = 0;
        _windows.EndUpdate();

        _capture.Enabled = SelectedWindow is not null;
        _status.Text = windows.Count == 0
            ? "No eligible windows found. Open a window and refresh."
            : $"{windows.Count} visible windows · minimized windows are excluded";
    }
}

internal static class WindowEnumerator
{
    private delegate bool EnumWindowsCallback(IntPtr windowHandle, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr windowHandle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr windowHandle, StringBuilder text, int maximumCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr windowHandle, out NativeRect rectangle);

    public static IReadOnlyList<CaptureWindowChoice> GetVisibleWindows()
    {
        var currentProcessId = (uint)Environment.ProcessId;
        var result = new List<CaptureWindowChoice>();
        EnumWindowsCallback callback = (windowHandle, _) =>
        {
            if (!IsWindowVisible(windowHandle) || IsIconic(windowHandle))
                return true;

            GetWindowThreadProcessId(windowHandle, out var processId);
            if (processId == currentProcessId)
                return true;

            var titleBuffer = new StringBuilder(512);
            if (GetWindowText(windowHandle, titleBuffer, titleBuffer.Capacity) <= 0 ||
                !GetWindowRect(windowHandle, out var nativeRect))
            {
                return true;
            }

            var bounds = Rectangle.FromLTRB(nativeRect.Left, nativeRect.Top, nativeRect.Right, nativeRect.Bottom);
            if (bounds.Width < 160 || bounds.Height < 100)
                return true;

            result.Add(new CaptureWindowChoice(windowHandle, titleBuffer.ToString(), bounds));
            return true;
        };

        EnumWindows(callback, IntPtr.Zero);
        return result
            .OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
