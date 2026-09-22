using System.Drawing;
using System.Windows.Forms;
using AreaSnap.Core;

namespace AreaSnap.Capture;

/// <summary>
/// Overlay form that darkens the screen and lets the user select a rectangular region.
/// </summary>
public sealed class RegionSelectorForm : Form
{
    private readonly Action<Rectangle> _onRegionSelected;
    private readonly Action _onCancelled;

    private Point _startPoint;
    private Rectangle _selection;
    private bool _dragging;
    private Bitmap? _screenSnapshot;
    private Pen _borderPen = new(Color.FromArgb(46, 139, 87), 2);
    private SolidBrush _overlayBrush = new(Color.FromArgb(100, 0, 0, 0));

    public RegionSelectorForm(Action<Rectangle> onRegionSelected, Action onCancelled)
    {
        _onRegionSelected = onRegionSelected;
        _onCancelled = onCancelled;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;

        // Cover all screens
        var bounds = System.Windows.Forms.Screen.AllScreens
            .Aggregate(Rectangle.Empty, (current, screen) =>
                Rectangle.Union(current, screen.Bounds));
        Bounds = bounds;
        Location = bounds.Location;
        Size = bounds.Size;

        DoubleBuffered = true;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Capture the screen before we overlay it
        _screenSnapshot = new Bitmap(Bounds.Width, Bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(_screenSnapshot);
        g.CopyFromScreen(Bounds.X, Bounds.Y, 0, 0, Bounds.Size, CopyPixelOperation.SourceCopy);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Draw the screen snapshot as background
        if (_screenSnapshot is not null)
            e.Graphics.DrawImage(_screenSnapshot, 0, 0);

        // Dark overlay on everything
        e.Graphics.FillRectangle(_overlayBrush, ClientRectangle);

        // Clear overlay inside selection to show original content
        if (_selection.Width > 0 && _selection.Height > 0)
        {
            // Draw the original screen content inside the selection
            if (_screenSnapshot is not null)
                e.Graphics.DrawImage(_screenSnapshot, _selection, _selection, GraphicsUnit.Pixel);

            // Green border around selection
            e.Graphics.DrawRectangle(_borderPen, _selection);

            // Size label
            var label = $"{_selection.Width} × {_selection.Height}";
            var labelPos = new Point(_selection.X, _selection.Y - 22);
            if (labelPos.Y < 0)
                labelPos.Y = _selection.Bottom + 4;
            e.Graphics.DrawString(label, new Font("Segoe UI", 10), Brushes.White, labelPos);
        }

        // Instructions
        if (!_dragging)
        {
            var hintFont = new Font("Segoe UI", 14, FontStyle.Bold);
            var hintText = "Click and drag to select an area · Esc to cancel";
            var hintSize = e.Graphics.MeasureString(hintText, hintFont);
            var hintX = (ClientSize.Width - hintSize.Width) / 2;
            var hintY = (ClientSize.Height - hintSize.Height) / 2;
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(180, 0, 0, 0)), hintX - 20, hintY - 10, hintSize.Width + 40, hintSize.Height + 20);
            e.Graphics.DrawString(hintText, hintFont, Brushes.White, hintX, hintY);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            _startPoint = e.Location;
            _selection = new Rectangle(e.Location, Size.Empty);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging) return;

        var x = Math.Min(_startPoint.X, e.X);
        var y = Math.Min(_startPoint.Y, e.Y);
        var w = Math.Abs(e.X - _startPoint.X);
        var h = Math.Abs(e.Y - _startPoint.Y);
        _selection = new Rectangle(x, y, w, h);
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging) return;

        _dragging = false;

        if (_selection.Width > 5 && _selection.Height > 5)
        {
            // Convert to screen coordinates
            var screenRect = new Rectangle(
                _selection.X + Bounds.X,
                _selection.Y + Bounds.Y,
                _selection.Width,
                _selection.Height);
            _onRegionSelected(screenRect);
        }
        else
        {
            _onCancelled();
        }

        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            _dragging = false;
            _onCancelled();
            Close();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _screenSnapshot?.Dispose();
            _borderPen.Dispose();
            _overlayBrush.Dispose();
        }
        base.Dispose(disposing);
    }
}