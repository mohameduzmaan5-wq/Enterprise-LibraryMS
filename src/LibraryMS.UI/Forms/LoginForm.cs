using System.Drawing.Drawing2D;
using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using LibraryMS.Services;
using LibraryMS.Data.Database;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Premium enterprise login form with BCrypt auth, lockout feedback,
/// attempt counter, and animated dark glassmorphism design.
/// </summary>
public class LoginForm : Form
{
    private readonly AuthService _auth = new();

    private Guna2TextBox  _txtUsername  = null!;
    private Guna2TextBox  _txtPassword  = null!;
    private Guna2Button   _btnLogin     = null!;
    private Guna2Button   _btnTogglePwd = null!;
    private Label         _lblStatus    = null!;
    private Label         _lblAttempts  = null!;
    private Label         _lblCapsLock  = null!;
    private Panel         _cardPanel    = null!;

    private int  _failCount    = 0;
    private bool _showPassword = false;

    public LoginForm()
    {
        InitializeComponent();
        _ = InitializeDatabaseOnLoadAsync();
    }

    /// <summary>
    /// Creates tables (including AppUsers) and seeds the default admin
    /// before the user can attempt to log in.
    /// </summary>
    private async Task InitializeDatabaseOnLoadAsync()
    {
        _btnLogin.Enabled = false;
        _lblStatus.ForeColor = AppTheme.TextMuted;
        _lblStatus.Text = "Connecting to database...";

        try
        {
            await DatabaseManager.InitializeDatabaseAsync();
            _lblStatus.Text = string.Empty;
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = AppTheme.Warning;
            _lblStatus.Text = "⚠ Database offline — check SQL Server connection.";
        }
        finally
        {
            _btnLogin.Enabled = true;
        }
    }

    private void InitializeComponent()
    {
        // ── Form settings ────────────────────────────────────────
        Text            = "LibraryMS — Sign In";
        Size            = new Size(480, 620);
        MinimumSize     = new Size(480, 620);
        MaximumSize     = new Size(480, 620);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = AppTheme.Background;
        DoubleBuffered  = true;

        // ── Background gradient ──────────────────────────────────
        this.Paint += (s, e) =>
        {
            using var brush = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(20, 10, 30),
                Color.FromArgb(12, 22, 40),
                LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillRectangle(brush, ClientRectangle);

            // Subtle radial glow top-centre
            using var glow = new PathGradientBrush(new PointF[]
            {
                new(0, 0), new(Width, 0), new(Width, 200), new(0, 200)
            })
            {
                CenterPoint    = new PointF(Width / 2f, 0),
                CenterColor    = Color.FromArgb(35, AppTheme.Primary),
                SurroundColors = new[] { Color.Transparent }
            };
            e.Graphics.FillRectangle(glow, 0, 0, Width, 200);
        };

        // ── Logo / brand panel ───────────────────────────────────
        var brandPanel = new Panel
        {
            Location  = new Point(0, 50),
            Size      = new Size(480, 100),
            BackColor = Color.Transparent
        };
        brandPanel.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.TextRenderingHint  = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Icon background circle
            using var circBrush = new SolidBrush(Color.FromArgb(40, AppTheme.Primary));
            g.FillEllipse(circBrush, 195, 0, 90, 90);
            using var circPen = new Pen(Color.FromArgb(80, AppTheme.Primary), 1.5f);
            g.DrawEllipse(circPen, 195, 0, 90, 90);

            // Book icon
            using var iconFont = new Font("Segoe UI Emoji", 30f);
            g.DrawString("📖", iconFont, Brushes.White, 208, 18);
        };

        var lblApp = new Label
        {
            Text      = "LibraryMS",
            Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            AutoSize  = true,
            BackColor = Color.Transparent
        };
        lblApp.Location = new Point((480 - lblApp.PreferredWidth) / 2, 98);

        var lblTagline = new Label
        {
            Text      = "Enterprise Library Management System",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            BackColor = Color.Transparent
        };
        lblTagline.Location = new Point(240 - lblTagline.PreferredWidth / 2, 130);

        // ── Glass card ────────────────────────────────────────────
        _cardPanel = new Panel
        {
            Location  = new Point(40, 165),
            Size      = new Size(400, 360),
            BackColor = Color.FromArgb(18, 28, 45)
        };
        _cardPanel.Paint += CardPanel_Paint;

        // Card title
        var lblTitle = new Label
        {
            Text      = "Sign in to your account",
            Font      = new Font("Segoe UI Semibold", 13f),
            ForeColor = AppTheme.TextPrimary,
            AutoSize  = true,
            Location  = new Point(28, 22),
            BackColor = Color.Transparent
        };

        // Username field
        var lblUser = MakeFieldLabel("Username", new Point(28, 60));
        _txtUsername = new Guna2TextBox
        {
            PlaceholderText = "Enter your username",
            Location        = new Point(28, 82),
            Size            = new Size(344, 44)
        };
        ThemeManager.StyleTextBox(_txtUsername);
        _txtUsername.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { _txtPassword.Focus(); e.SuppressKeyPress = true; } };

        // Password field
        var lblPwd = MakeFieldLabel("Password", new Point(28, 140));
        _txtPassword = new Guna2TextBox
        {
            PlaceholderText = "Enter your password",
            Location        = new Point(28, 162),
            Size            = new Size(304, 44),
            PasswordChar    = '●'
        };
        ThemeManager.StyleTextBox(_txtPassword);
        _txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { BtnLogin_Click(this, e); e.SuppressKeyPress = true; } };

        // Toggle password visibility
        _btnTogglePwd = new Guna2Button
        {
            Text     = "👁",
            Location = new Point(336, 162),
            Size     = new Size(36, 44),
            FillColor  = AppTheme.SurfaceLight,
            ForeColor  = AppTheme.TextSecondary,
            BorderRadius = 10,
            Font       = new Font("Segoe UI Emoji", 12f)
        };
        _btnTogglePwd.Click += (s, e) =>
        {
            _showPassword       = !_showPassword;
            _txtPassword.PasswordChar = _showPassword ? '\0' : '●';
            _btnTogglePwd.ForeColor   = _showPassword ? AppTheme.Primary : AppTheme.TextSecondary;
        };

        // Caps lock warning
        _lblCapsLock = new Label
        {
            Text      = "⚠️ Caps Lock is ON",
            Font      = AppTheme.FontCaption,
            ForeColor = AppTheme.Warning,
            AutoSize  = true,
            Location  = new Point(28, 210),
            BackColor = Color.Transparent,
            Visible   = false
        };

        // Status message
        _lblStatus = new Label
        {
            Text      = string.Empty,
            Font      = AppTheme.FontBodySmall,
            ForeColor = AppTheme.Danger,
            Size      = new Size(344, 44),
            Location  = new Point(28, 226),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Attempts indicator
        _lblAttempts = new Label
        {
            Text      = string.Empty,
            Font      = AppTheme.FontCaption,
            ForeColor = AppTheme.Warning,
            AutoSize  = true,
            Location  = new Point(28, 274),
            BackColor = Color.Transparent
        };

        // Login button
        _btnLogin = new Guna2Button
        {
            Text     = "Sign In",
            Location = new Point(28, 300),
            Size     = new Size(344, 46),
            Font     = new Font("Segoe UI Semibold", 11f),
        };
        ThemeManager.StylePrimaryButton(_btnLogin);
        _btnLogin.Click += BtnLogin_Click;

        _cardPanel.Controls.AddRange(new Control[]
        {
            lblTitle, lblUser, _txtUsername,
            lblPwd,  _txtPassword, _btnTogglePwd,
            _lblCapsLock, _lblStatus, _lblAttempts, _btnLogin
        });

        // ── Footer ────────────────────────────────────────────────
        var lblFooter = new Label
        {
            Text      = "LibraryMS v1.0.0 Enterprise Edition  •  Secured with BCrypt",
            Font      = AppTheme.FontCaption,
            ForeColor = AppTheme.TextMuted,
            AutoSize  = true,
            BackColor = Color.Transparent
        };
        lblFooter.Location = new Point(240 - lblFooter.PreferredWidth / 2, 545);

        // ── Default credentials hint ──────────────────────────────
        var lblHint = new Label
        {
            Text      = "Default: admin / Admin@123",
            Font      = AppTheme.FontCaption,
            ForeColor = AppTheme.TextMuted,
            AutoSize  = true,
            BackColor = Color.Transparent
        };
        lblHint.Location = new Point(240 - lblHint.PreferredWidth / 2, 530);

        Controls.AddRange(new Control[]
        {
            brandPanel, lblApp, lblTagline, _cardPanel, lblHint, lblFooter
        });

        // Monitor Caps Lock
        var capsTimer = new System.Windows.Forms.Timer { Interval = 300 };
        capsTimer.Tick += (s, e) =>
            _lblCapsLock.Visible = _txtPassword.Focused && Control.IsKeyLocked(Keys.CapsLock);
        capsTimer.Start();

        ActiveControl = _txtUsername;
    }

    // ── Card paint (glassmorphism border) ─────────────────────────
    private static void CardPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel p) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = RoundedRect(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 16);
        using var bg   = new SolidBrush(Color.FromArgb(18, 28, 45));
        e.Graphics.FillPath(bg, path);

        using var border = new Pen(Color.FromArgb(50, AppTheme.Primary), 1);
        e.Graphics.DrawPath(border, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int rad)
    {
        var p = new System.Drawing.Drawing2D.GraphicsPath();
        int d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    private static Label MakeFieldLabel(string text, Point loc) => new Label
    {
        Text      = text,
        Font      = new Font("Segoe UI Semibold", 9.5f),
        ForeColor = AppTheme.TextSecondary,
        AutoSize  = true,
        Location  = loc,
        BackColor = Color.Transparent
    };

    // ── Login handler ─────────────────────────────────────────────
    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        _btnLogin.Enabled = false;
        _btnLogin.Text    = "Signing in…";
        _lblStatus.Text   = string.Empty;

        try
        {
            var username = _txtUsername.Text.Trim();
            var password = _txtPassword.Text;

            var (success, message, user) = await _auth.LoginAsync(username, password);

            if (success)
            {
                // Fade-out and open MainForm
                _btnLogin.Text  = "✓ Success!";
                _btnLogin.FillColor = AppTheme.Success;
                await Task.Delay(500);

                var main = new MainForm();
                main.Show();
                Hide();
                main.FormClosed += (s2, e2) => Close();
            }
            else
            {
                _failCount++;
                ShowError(message);
                _txtPassword.Clear();
                _txtPassword.Focus();

                // Shake animation on card
                await ShakeAsync(_cardPanel);
            }
        }
        finally
        {
            if (_btnLogin != null && !IsDisposed)
            {
                _btnLogin.Enabled   = true;
                _btnLogin.Text      = "Sign In";
                _btnLogin.FillColor = AppTheme.Primary;
            }
        }
    }

    private void ShowError(string msg)
    {
        _lblStatus.ForeColor = msg.Contains("grace") || msg.Contains("within")
            ? AppTheme.Warning : AppTheme.Danger;
        _lblStatus.Text = msg;

        if (_failCount > 0 && _failCount < 5)
            _lblAttempts.Text = $"⚠️  {5 - _failCount} attempt(s) remaining before lockout";
        else
            _lblAttempts.Text = string.Empty;
    }

    private static async Task ShakeAsync(Control ctrl)
    {
        var orig = ctrl.Left;
        int[] offsets = { -8, 8, -6, 6, -4, 4, -2, 2, 0 };
        foreach (var dx in offsets)
        {
            ctrl.Left = orig + dx;
            await Task.Delay(28);
        }
        ctrl.Left = orig;
    }
}
