using System.Drawing.Drawing2D;
using LibraryMS.Core.Entities;
using LibraryMS.Services;
using LibraryMS.UI.Controls;
using LibraryMS.UI.Theme;
using LibraryMS.Data.Database;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Main application shell with sidebar navigation and content panel.
/// Premium dark theme with glassmorphism sidebar.
/// </summary>
public class MainForm : Form
{
    private Panel _sidebarPanel   = null!;
    private Panel _contentPanel   = null!;
    private Panel _brandPanel     = null!;
    private Label _statusLabel    = null!;
    private Label _lblUserName    = null!;
    private Label _lblUserRole    = null!;
    private readonly AuthService _authService = new();
    private readonly List<SidebarButton> _navButtons = new();
    private UserControl? _currentPage;
    private readonly Dictionary<string, Func<UserControl>> _pages = new();

    public MainForm()
    {
        InitializeComponent();
        RegisterPages();
        NavigateTo("Dashboard");
        _ = InitializeDatabaseAsync();
    }

    private void InitializeComponent()
    {
        // ─── Form Settings ──────────────────────────────────────
        Text = "LibraryMS — Enterprise Library Management System";
        Size = new Size(1440, 900);
        MinimumSize = new Size(1200, 750);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.TextPrimary;
        Font = AppTheme.FontBody;
        FormBorderStyle = FormBorderStyle.Sizable;
        DoubleBuffered = true;

        // ─── Sidebar Panel ──────────────────────────────────────
        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = AppTheme.SidebarWidth,
            BackColor = AppTheme.SidebarBackground,
            Padding = new Padding(0)
        };
        _sidebarPanel.Paint += SidebarPanel_Paint;

        // ─── Brand Section ──────────────────────────────────────
        _brandPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.Transparent,
            Padding = new Padding(20, 0, 20, 0)
        };
        _brandPanel.Paint += BrandPanel_Paint;

        // ─── Sidebar separator ──────────────────────────────────
        var separator = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = AppTheme.Separator,
            Margin = new Padding(20, 0, 20, 0)
        };

        // ─── Navigation Label ───────────────────────────────────
        var navLabel = new Label
        {
            Text = "NAVIGATION",
            Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
            ForeColor = AppTheme.TextMuted,
            Dock = DockStyle.Top,
            Height = 40,
            Padding = new Padding(24, 16, 0, 0),
            BackColor = Color.Transparent
        };

        // ─── Navigation Buttons ─────────────────────────────────
        var navContainer = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(10, 4, 10, 4),
            BackColor = Color.Transparent
        };

        var navItems = new (string text, string icon, string key)[]
        {
            ("Dashboard", "📊", "Dashboard"),
            ("Books",     "📚", "Books"),
            ("Members",   "👥", "Members"),
            ("Loans",     "🔄", "Loans"),
            ("Returns",   "↩️", "Returns"),
            ("Reports",   "📈", "Reports"),
            ("Settings",  "⚙️", "Settings")
        };

        foreach (var (text, icon, key) in navItems)
        {
            var btn = new SidebarButton
            {
                ButtonText = text,
                IconText = icon,
                Tag = key,
                Margin = new Padding(0, 2, 0, 2)
            };
            btn.Clicked += NavButton_Clicked;
            _navButtons.Add(btn);
            navContainer.Controls.Add(btn);
        }

        // ─── Session user badge ─────────────────────────────────
        var userBadge = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 70,
            BackColor = Color.FromArgb(12, 255, 255, 255),
            Padding   = new Padding(14, 8, 14, 8),
            Cursor    = Cursors.Default
        };
        userBadge.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.Separator, 1);
            e.Graphics.DrawLine(pen, 0, 0, userBadge.Width, 0);
        };

        // Avatar circle
        var avatarPanel = new Panel
        {
            Size      = new Size(36, 36),
            Location  = new Point(14, 17),
            BackColor = Color.Transparent
        };
        avatarPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var b = new SolidBrush(AppTheme.Primary);
            e.Graphics.FillEllipse(b, 0, 0, 35, 35);
            var initial = AppSession.CurrentUser?.FullName.FirstOrDefault() ?? 'U';
            using var f = new Font("Segoe UI Semibold", 14f, FontStyle.Bold);
            using var tb = new SolidBrush(Color.White);
            var sz = e.Graphics.MeasureString(initial.ToString(), f);
            e.Graphics.DrawString(initial.ToString(), f, tb,
                (36 - sz.Width) / 2, (36 - sz.Height) / 2);
        };

        _lblUserName = new Label
        {
            Text      = AppSession.CurrentUser?.FullName ?? "User",
            Font      = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = AppTheme.TextPrimary,
            Location  = new Point(58, 14),
            Size      = new Size(120, 18),
            BackColor = Color.Transparent
        };
        _lblUserRole = new Label
        {
            Text      = AppSession.CurrentUser?.RoleDisplay ?? "",
            Font      = AppTheme.FontCaption,
            ForeColor = AppTheme.TextMuted,
            Location  = new Point(58, 32),
            Size      = new Size(130, 16),
            BackColor = Color.Transparent
        };

        var btnLogout = new Guna.UI2.WinForms.Guna2Button
        {
            Text         = "↪",
            Size         = new Size(28, 28),
            Location     = new Point(170, 21),
            FillColor    = Color.Transparent,
            ForeColor    = AppTheme.TextSecondary,
            BorderRadius = 8,
            Font         = new Font("Segoe UI", 13f),
            Animated     = true
        };
        btnLogout.HoverState.FillColor = Color.FromArgb(25, AppTheme.Danger);
        btnLogout.HoverState.ForeColor = AppTheme.Danger;
        btnLogout.Click += async (s, e) => await LogoutAsync();

        userBadge.Controls.AddRange(new Control[] { avatarPanel, _lblUserName, _lblUserRole, btnLogout });

        // ─── Bottom Status ──────────────────────────────────────
        var bottomPanel = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 42,
            BackColor = Color.Transparent,
            Padding   = new Padding(20, 0, 20, 8)
        };

        _statusLabel = new Label
        {
            Text      = "● Connecting...",
            Font      = AppTheme.FontCaption,
            ForeColor = AppTheme.Warning,
            Dock      = DockStyle.Bottom,
            Height    = 18,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var versionLabel = new Label
        {
            Text      = "v1.0.0 — Enterprise Edition",
            Font      = AppTheme.FontCaption,
            ForeColor = AppTheme.TextMuted,
            Dock      = DockStyle.Bottom,
            Height    = 16,
            TextAlign = ContentAlignment.MiddleLeft
        };

        bottomPanel.Controls.AddRange(new Control[] { _statusLabel, versionLabel });

        // ─── Build Sidebar ──────────────────────────────────────
        _sidebarPanel.Controls.Add(userBadge);
        _sidebarPanel.Controls.Add(bottomPanel);
        _sidebarPanel.Controls.Add(navContainer);
        _sidebarPanel.Controls.Add(navLabel);
        _sidebarPanel.Controls.Add(separator);
        _sidebarPanel.Controls.Add(_brandPanel);

        // ─── Sidebar border (right edge) ────────────────────────
        var sidebarBorder = new Panel
        {
            Dock = DockStyle.Left,
            Width = 1,
            BackColor = AppTheme.Separator
        };

        // ─── Content Panel ──────────────────────────────────────
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Background,
            Padding = new Padding(0)
        };

        // ─── Add to Form ────────────────────────────────────────
        Controls.Add(_contentPanel);
        Controls.Add(sidebarBorder);
        Controls.Add(_sidebarPanel);
    }

   private void SidebarPanel_Paint(object? sender, PaintEventArgs e)
{
    if (_sidebarPanel.Width <= 0 || _sidebarPanel.Height <= 0)
        return;

    // Draw subtle gradient overlay on sidebar
    var g = e.Graphics;

    using var brush = new LinearGradientBrush(
        _sidebarPanel.ClientRectangle,
        Color.FromArgb(5, 108, 99, 255),
        Color.Transparent,
        90f);

    g.FillRectangle(brush, _sidebarPanel.ClientRectangle);
}

    private void BrandPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Brand icon
        var iconFont = new Font("Segoe UI Emoji", 22f);
        g.DrawString("📖", iconFont, Brushes.White, 18, 24);
        iconFont.Dispose();

        // Brand text
        using var brandBrush = new SolidBrush(AppTheme.TextPrimary);
        g.DrawString("LibraryMS", AppTheme.FontSidebarBrand, brandBrush, 55, 24);

        // Subtitle
        using var subBrush = new SolidBrush(AppTheme.TextMuted);
        g.DrawString("Enterprise Edition", AppTheme.FontCaption, subBrush, 56, 48);
    }

    private void RegisterPages()
    {
        _pages["Dashboard"] = () => new DashboardControl();
        _pages["Books"]     = () => new BooksControl();
        _pages["Members"]   = () => new MembersControl();
        _pages["Loans"]     = () => new LoansControl();
        _pages["Returns"]   = () => new ReturnsControl();
        _pages["Reports"]   = () => new ReportsControl();
        _pages["Settings"]  = () => new SettingsControl();
    }

    private void NavButton_Clicked(object? sender, EventArgs e)
    {
        if (sender is SidebarButton btn && btn.Tag is string key)
        {
            NavigateTo(key);
        }
    }

    public void NavigateTo(string pageKey)
    {
        // Update active states
        foreach (var btn in _navButtons)
            btn.IsActive = btn.Tag?.ToString() == pageKey;

        // Load page
        if (_pages.TryGetValue(pageKey, out var factory))
        {
            UserControl newPage;

try
{
    newPage = factory();
}
catch (Exception ex)
{
    MessageBox.Show(
        ex.ToString(),
        "Module Load Error",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error);

    return;
}
            newPage.Dock = DockStyle.Fill;

            _contentPanel.SuspendLayout();
            if (_currentPage != null)
            {
                _contentPanel.Controls.Remove(_currentPage);
                _currentPage.Dispose();
            }
            _contentPanel.Controls.Add(newPage);
            _contentPanel.ResumeLayout();

            _currentPage = newPage;

            if (newPage is DashboardControl dashboard)
{
    _ = dashboard.LoadDataAsync();
}
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        try
        {
            _statusLabel.Text = "● Initializing database...";
            _statusLabel.ForeColor = AppTheme.Warning;

            await DatabaseManager.InitializeDatabaseAsync();

            _statusLabel.Text = "● Connected to database";
            _statusLabel.ForeColor = AppTheme.Success;

            // Refresh dashboard if it's the current page
            if (_currentPage is DashboardControl dashboard)
            {
                await dashboard.LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "● Offline mode (demo data)";
            _statusLabel.ForeColor = AppTheme.Danger;

            // Load demo data into the current page
            if (_currentPage is DashboardControl dashboard)
            {
                dashboard.LoadDemoData();
            }
        }
    }

    private async Task LogoutAsync()
    {
        var confirm = new Guna.UI2.WinForms.Guna2MessageDialog
        {
            Caption = "Sign Out",
            Text    = "Are you sure you want to sign out?",
            Buttons = Guna.UI2.WinForms.MessageDialogButtons.YesNo,
            Icon    = Guna.UI2.WinForms.MessageDialogIcon.Question,
            Style   = Guna.UI2.WinForms.MessageDialogStyle.Dark,
            Parent  = this
        };
        if (confirm.Show() != DialogResult.Yes) return;

        await _authService.LogoutAsync();

        // Re-show login form
        var login = new LoginForm();
        login.Show();
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _currentPage?.Dispose();
        base.OnFormClosing(e);
    }
}
