using System.Drawing.Drawing2D;
using LibraryMS.UI.Theme;

namespace LibraryMS.UI.Controls;

/// <summary>
/// Custom sidebar navigation button with smooth hover/active animations
/// and a modern pill-shaped active indicator.
/// </summary>
public class SidebarButton : UserControl
{
    private string _text = "Menu Item";
    private string _iconText = "📁";
    private bool _isActive = false;
    private float _hoverProgress = 0f;
    private float _activeIndicator = 0f;
    private readonly System.Windows.Forms.Timer _animTimer;
    private bool _isHovered = false;

    public event EventHandler? Clicked;

    public string ButtonText
    {
        get => _text;
        set { _text = value; Invalidate(); }
    }

    public string IconText
    {
        get => _iconText;
        set { _iconText = value; Invalidate(); }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            _animTimer.Start();
            Invalidate();
        }
    }

    public SidebarButton()
    {
       SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Size = new Size(AppTheme.SidebarWidth - 20, 48);
        Cursor = Cursors.Hand;

        _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animTimer.Tick += (s, e) =>
        {
            bool changed = false;

            // Hover animation
            if (_isHovered && _hoverProgress < 1f) { _hoverProgress = Math.Min(1f, _hoverProgress + 0.1f); changed = true; }
            else if (!_isHovered && _hoverProgress > 0f) { _hoverProgress = Math.Max(0f, _hoverProgress - 0.08f); changed = true; }

            // Active indicator animation
            if (_isActive && _activeIndicator < 1f) { _activeIndicator = Math.Min(1f, _activeIndicator + 0.12f); changed = true; }
            else if (!_isActive && _activeIndicator > 0f) { _activeIndicator = Math.Max(0f, _activeIndicator - 0.1f); changed = true; }

            if (!changed) _animTimer.Stop();
            Invalidate();
        };
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        _animTimer.Start();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        _animTimer.Start();
        base.OnMouseLeave(e);
    }

    protected override void OnClick(EventArgs e)
    {
        Clicked?.Invoke(this, e);
        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = 12;

        // ─── Background ─────────────────────────────────────────
        if (_isActive || _activeIndicator > 0)
        {
            // Active state - primary fill with transparency
            var alpha = (int)(25 * _activeIndicator);
            using var activePath = CreateRoundedRectPath(rect, radius);
            using var activeBrush = new SolidBrush(Color.FromArgb(alpha, AppTheme.Primary));
            g.FillPath(activeBrush, activePath);

            // Active left indicator bar
            var barHeight = (int)(28 * _activeIndicator);
            var barY = (Height - barHeight) / 2;
            using var barPath = CreateRoundedRectPath(new Rectangle(0, barY, 3, barHeight), 2);
            using var barBrush = new SolidBrush(Color.FromArgb((int)(255 * _activeIndicator), AppTheme.Primary));
            g.FillPath(barBrush, barPath);
        }

        if (_hoverProgress > 0 && !_isActive)
        {
            // Hover state
            var alpha = (int)(15 * _hoverProgress);
            using var hoverPath = CreateRoundedRectPath(rect, radius);
            using var hoverBrush = new SolidBrush(Color.FromArgb(alpha, AppTheme.TextPrimary));
            g.FillPath(hoverBrush, hoverPath);
        }

        // ─── Icon ───────────────────────────────────────
        var iconColor = _isActive
            ? Color.FromArgb((int)(255 * Math.Max(0.6f, _activeIndicator)), AppTheme.Primary)
            : Color.FromArgb((int)(255 * Math.Max(0.6f, _hoverProgress * 0.4f + 0.6f)), AppTheme.SidebarText);

        using var iconFont  = new Font("Segoe UI Emoji", 16f);
        using var iconBrush = new SolidBrush(iconColor);
        g.DrawString(_iconText, iconFont, iconBrush, 16, (Height - 24) / 2);

        // ─── Text ───────────────────────────────────────────────
        var textColor = _isActive
            ? Color.FromArgb((int)(255 * Math.Max(0.8f, _activeIndicator)), AppTheme.SidebarTextActive)
            : Color.FromArgb((int)(255 * Math.Max(0.6f, _hoverProgress * 0.3f + 0.6f)), AppTheme.SidebarText);

        using var textBrush = new SolidBrush(textColor);
        var textY = (Height - g.MeasureString(_text, AppTheme.FontSidebar).Height) / 2;
        g.DrawString(_text, AppTheme.FontSidebar, textBrush, 50, textY);
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
