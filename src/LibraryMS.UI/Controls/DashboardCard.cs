using System.Drawing.Drawing2D;
using LibraryMS.UI.Theme;

namespace LibraryMS.UI.Controls;

/// <summary>
/// Premium glassmorphism-inspired dashboard stat card with gradient background,
/// glow effects, and smooth animations.
/// </summary>
public class DashboardCard : UserControl
{
    private string _title = "Title";
    private string _value = "0";
    private string _subtitle = "";
    private string _iconText = "📊";
    private Color _gradientStart = AppTheme.Primary;
    private Color _gradientEnd = AppTheme.PrimaryLight;
    private float _animationProgress = 0f;
    private readonly System.Windows.Forms.Timer _animTimer;
    private bool _isHovered = false;

    public string Title
    {
        get => _title;
        set { _title = value; Invalidate(); }
    }

    public string Value
    {
        get => _value;
        set { _value = value; Invalidate(); }
    }

    public string Subtitle
    {
        get => _subtitle;
        set { _subtitle = value; Invalidate(); }
    }

    public string IconText
    {
        get => _iconText;
        set { _iconText = value; Invalidate(); }
    }

    public Color GradientStart
    {
        get => _gradientStart;
        set { _gradientStart = value; Invalidate(); }
    }

    public Color GradientEnd
    {
        get => _gradientEnd;
        set { _gradientEnd = value; Invalidate(); }
    }

    public DashboardCard()
    {
       SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Hand;

        _animTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60fps
        _animTimer.Tick += (s, e) =>
        {
            if (_isHovered && _animationProgress < 1f)
                _animationProgress = Math.Min(1f, _animationProgress + 0.08f);
            else if (!_isHovered && _animationProgress > 0f)
                _animationProgress = Math.Max(0f, _animationProgress - 0.06f);
            else
                _animTimer.Stop();

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

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        var radius = AppTheme.CardBorderRadius;

        // Calculate scale for hover animation
        var scale = 1f + (_animationProgress * 0.02f);
        var glowAlpha = (int)(40 * _animationProgress);

        // ─── Outer Glow (on hover) ─────────────────────────────
        if (_animationProgress > 0)
        {
            var glowRect = new Rectangle(-4, -4, Width + 7, Height + 7);
            using var glowPath = CreateRoundedRectPath(glowRect, radius + 4);
            using var glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, _gradientStart));
            g.FillPath(glowBrush, glowPath);
        }

        // ─── Main Card Background ──────────────────────────────
        using var cardPath = CreateRoundedRectPath(rect, radius);
        using var gradientBrush = new LinearGradientBrush(rect, 
            Color.FromArgb(35, _gradientStart.R, _gradientStart.G, _gradientStart.B),
            Color.FromArgb(15, _gradientEnd.R, _gradientEnd.G, _gradientEnd.B),
            135f);
        g.FillPath(gradientBrush, cardPath);

        // ─── Surface fill ───────────────────────────────────────
        using var surfaceBrush = new SolidBrush(Color.FromArgb(180, AppTheme.Surface.R, AppTheme.Surface.G, AppTheme.Surface.B));
        g.FillPath(surfaceBrush, cardPath);

        // ─── Gradient accent bar (left) ─────────────────────────
        var barRect = new Rectangle(0, 0, 4, Height);
        using var barPath = CreateRoundedRectPath(new Rectangle(0, 12, 4, Height - 24), 2);
        using var barBrush = new LinearGradientBrush(barRect, _gradientStart, _gradientEnd, 90f);
        g.FillPath(barBrush, barPath);

        // ─── Top-right gradient circle (decorative) ─────────────
        var circleSize = 80 + (int)(10 * _animationProgress);
        var circleRect = new Rectangle(Width - circleSize + 10, -20, circleSize, circleSize);
        using var circleBrush = new SolidBrush(Color.FromArgb(15 + (int)(10 * _animationProgress), _gradientStart));
        using var circlePath = new GraphicsPath();
        circlePath.AddEllipse(circleRect);
        g.FillPath(circleBrush, circlePath);

        // ─── Second smaller circle ──────────────────────────────
        var circle2Rect = new Rectangle(Width - 50, Height - 50, 60, 60);
        using var circle2Brush = new SolidBrush(Color.FromArgb(8, _gradientEnd));
        using var circle2Path = new GraphicsPath();
        circle2Path.AddEllipse(circle2Rect);
        g.FillPath(circle2Brush, circle2Path);

        // ─── Border ─────────────────────────────────────────────
        var borderAlpha = 40 + (int)(20 * _animationProgress);
        using var borderPen = new Pen(Color.FromArgb(borderAlpha, _gradientStart), 1f);
        g.DrawPath(borderPen, cardPath);

        // ─── Glass highlight line at top ────────────────────────
        var highlightRect = new Rectangle(radius, 1, Width - radius * 2, 1);
        using var highlightBrush = new SolidBrush(Color.FromArgb(25, 255, 255, 255));
        g.FillRectangle(highlightBrush, highlightRect);

        // ─── Icon ───────────────────────────────────────
        using var iconFont  = new Font("Segoe UI Emoji", 24f);
        using var iconBrush = new SolidBrush(Color.FromArgb(200, _gradientStart));
        var iconSize = g.MeasureString(_iconText, iconFont);
        g.DrawString(_iconText, iconFont, iconBrush,
            Width - iconSize.Width - 20, 18);

        // ─── Title Text ─────────────────────────────────────────
        using var titleBrush = new SolidBrush(AppTheme.TextSecondary);
        g.DrawString(_title, AppTheme.FontCardLabel, titleBrush, 24, 22);

        // ─── Value Text ─────────────────────────────────────────
        using var valueBrush = new SolidBrush(AppTheme.TextPrimary);
        g.DrawString(_value, AppTheme.FontCardValue, valueBrush, 24, 48);

        // ─── Subtitle Text ──────────────────────────────────────
        if (!string.IsNullOrEmpty(_subtitle))
        {
            using var subtitleBrush = new SolidBrush(Color.FromArgb(180, _gradientStart));
            g.DrawString(_subtitle, AppTheme.FontBodySmall, subtitleBrush, 24, Height - 35);
        }
    }

    /// <summary>
    /// Creates a rounded rectangle GraphicsPath.
    /// </summary>
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
