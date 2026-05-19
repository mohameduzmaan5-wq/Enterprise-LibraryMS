using System.Drawing.Drawing2D;
using LibraryMS.Core.Entities;
using LibraryMS.UI.Controls;
using LibraryMS.UI.Theme;
using LibraryMS.Services;
using LibraryMS.Core.DTOs;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Premium dashboard page with stats cards, recent activity, and quick actions.
/// Features glassmorphism cards and smooth data loading.
/// </summary>
public class DashboardControl : UserControl
{
    private readonly DashboardService _dashboardService;
    private readonly LoanService _loanService;
    private DashboardCard _cardBooks = null!;
    private DashboardCard _cardMembers = null!;
    private DashboardCard _cardLoans = null!;
    private DashboardCard _cardOverdue = null!;
    private DataGridView _recentLoansGrid = null!;
    private Label _welcomeLabel = null!;
    private Label _dateLabel = null!;
    private Panel _statsPanel = null!;
    private Panel _quickStatsPanel = null!;

    public DashboardControl()
    {
        _dashboardService = new DashboardService();
        _loanService = new LoanService();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        BackColor = AppTheme.Background;
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Padding = new Padding(32, 24, 32, 24);

        // ─── Header Section ─────────────────────────────────────
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 16)
        };

        var greetName = AppSession.CurrentUser?.FullName ?? "Admin";
        _welcomeLabel = new Label
        {
            Text = $"Welcome back, {greetName}! 👋",
            Font = AppTheme.FontTitle,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 0)
        };

        _dateLabel = new Label
        {
            Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy"),
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Location = new Point(0, 42)
        };

        headerPanel.Controls.AddRange(new Control[] { _dateLabel, _welcomeLabel });

        // ─── Stats Cards Section ────────────────────────────────
        _statsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 170,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0)
        };

        _cardBooks = CreateStatCard("Total Books", "—", "📚", "Loading...",
            AppTheme.GradientBooks.Start, AppTheme.GradientBooks.End);
        _cardMembers = CreateStatCard("Total Members", "—", "👥", "Loading...",
            AppTheme.GradientMembers.Start, AppTheme.GradientMembers.End);
        _cardLoans = CreateStatCard("Active Loans", "—", "🔄", "Loading...",
            AppTheme.GradientLoans.Start, AppTheme.GradientLoans.End);
        _cardOverdue = CreateStatCard("Overdue", "—", "⚠️", "Loading...",
            AppTheme.GradientOverdue.Start, AppTheme.GradientOverdue.End);

        _statsPanel.Controls.AddRange(new Control[] { _cardBooks, _cardMembers, _cardLoans, _cardOverdue });

        // ─── Quick Stats Row ────────────────────────────────────
        _quickStatsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 8)
        };

        var quickStats = new[]
        {
            ("Books This Month", "—", AppTheme.Primary),
            ("New Members", "—", AppTheme.Secondary),
            ("Loans Issued", "—", AppTheme.Success),
            ("Returns", "—", AppTheme.Warning)
        };

        foreach (var (label, value, color) in quickStats)
        {
            var chip = CreateQuickStatChip(label, value, color);
            _quickStatsPanel.Controls.Add(chip);
        }

        // ─── Section Label ──────────────────────────────────────
        var sectionLabel = new Label
        {
            Text = "📋  Recent Loan Activity",
            Font = AppTheme.FontHeading,
            ForeColor = AppTheme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(0, 12, 0, 4),
            BackColor = Color.Transparent
        };

        // ─── Recent Loans Grid ──────────────────────────────────
        _recentLoansGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppTheme.Surface,
            Margin = new Padding(0, 8, 0, 0)
        };
        ThemeManager.StyleDataGridView(_recentLoansGrid);

        // ─── Grid Container with rounded border ─────────────────
        var gridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(1)
        };
        gridContainer.Paint += (s, e) =>
        {
            using var pen = new Pen(AppTheme.Border, 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, gridContainer.Width - 1, gridContainer.Height - 1);
            e.Graphics.DrawRectangle(pen, rect);
        };
        gridContainer.Controls.Add(_recentLoansGrid);

        // ─── Build Layout ───────────────────────────────────────
        Controls.Add(gridContainer);
        Controls.Add(sectionLabel);
        Controls.Add(_quickStatsPanel);
        Controls.Add(_statsPanel);
        Controls.Add(headerPanel);

        ResumeLayout();

        // Handle resize to update card widths
        Resize += (s, e) => UpdateCardWidths();
    }

    private DashboardCard CreateStatCard(string title, string value, string icon, string subtitle, Color gradStart, Color gradEnd)
    {
        return new DashboardCard
        {
            Title = title,
            Value = value,
            IconText = icon,
            Subtitle = subtitle,
            GradientStart = gradStart,
            GradientEnd = gradEnd,
            Size = new Size(270, 150),
            Margin = new Padding(0, 0, 16, 16)
        };
    }

    private Panel CreateQuickStatChip(string label, string value, Color accentColor)
    {
        var chip = new Panel
        {
            Size = new Size(200, 40),
            Margin = new Padding(0, 0, 12, 0),
            BackColor = Color.FromArgb(15, accentColor)
        };

        chip.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Rounded background
            using var path = CreateRoundedRectPath(new Rectangle(0, 0, chip.Width - 1, chip.Height - 1), 10);
            using var fill = new SolidBrush(Color.FromArgb(15, accentColor));
            e.Graphics.FillPath(fill, path);

            // Border
            using var border = new Pen(Color.FromArgb(30, accentColor), 1);
            e.Graphics.DrawPath(border, path);

            // Dot indicator
            using var dotBrush = new SolidBrush(accentColor);
            e.Graphics.FillEllipse(dotBrush, 12, 14, 8, 8);

            // Value
            using var valBrush = new SolidBrush(AppTheme.TextPrimary);
            e.Graphics.DrawString(value, new Font("Segoe UI Semibold", 11f, FontStyle.Bold), valBrush, 28, 5);

            // Label
            var valSize = e.Graphics.MeasureString(value, new Font("Segoe UI Semibold", 11f, FontStyle.Bold));
            using var lblBrush = new SolidBrush(AppTheme.TextSecondary);
            e.Graphics.DrawString(label, AppTheme.FontCaption, lblBrush, 28 + valSize.Width + 4, 10);
        };

        chip.Tag = (label, value);
        return chip;
    }

    private void UpdateCardWidths()
    {
        if (_statsPanel == null) return;
        var availableWidth = _statsPanel.ClientSize.Width - _statsPanel.Padding.Horizontal;
        var cardWidth = Math.Max(220, (availableWidth - 48) / 4); // 4 cards with 16px gap each
        foreach (Control ctrl in _statsPanel.Controls)
        {
            if (ctrl is DashboardCard card)
                card.Width = cardWidth;
        }

        // Update quick stat chips
        if (_quickStatsPanel != null)
        {
            var chipWidth = Math.Max(160, (availableWidth - 36) / 4);
            foreach (Control ctrl in _quickStatsPanel.Controls)
                ctrl.Width = chipWidth;
        }
    }

    /// <summary>
    /// Loads live data from the database.
    /// </summary>
    public async Task LoadDataAsync()
    {
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();
            UpdateStatsUI(stats);

            // Load recent loans
            var loans = await _loanService.GetAllLoansAsync();
            var loanList = loans.Take(10).ToList();

            _recentLoansGrid.DataSource = null;
            _recentLoansGrid.Columns.Clear();

            var dt = new System.Data.DataTable();
            dt.Columns.AddRange(new[]
            {
                new System.Data.DataColumn("ID", typeof(int)),
                new System.Data.DataColumn("Book", typeof(string)),
                new System.Data.DataColumn("Member", typeof(string)),
                new System.Data.DataColumn("Borrow Date", typeof(string)),
                new System.Data.DataColumn("Due Date", typeof(string)),
                new System.Data.DataColumn("Status", typeof(string))
            });

            foreach (var loan in loanList)
            {
                var status = loan.IsOverdue ? "⚠ Overdue" : loan.Status == "Returned" ? "✅ Returned" : "📗 Active";
                dt.Rows.Add(loan.Id, loan.BookTitle, loan.MemberName,
                    loan.BorrowDate.ToString("MMM dd, yyyy"),
                    loan.DueDate.ToString("MMM dd, yyyy"),
                    status);
            }

            _recentLoansGrid.DataSource = dt;
        }
        catch (Exception ex)
        {
            LoadDemoData();
        }
    }

    private void UpdateStatsUI(DashboardStats stats)
    {
        _cardBooks.Value = stats.TotalBooks.ToString("N0");
        _cardBooks.Subtitle = $"+{stats.BooksAddedThisMonth} this month";

        _cardMembers.Value = stats.TotalMembers.ToString("N0");
        _cardMembers.Subtitle = $"+{stats.NewMembersThisMonth} new this month";

        _cardLoans.Value = stats.ActiveLoans.ToString("N0");
        _cardLoans.Subtitle = $"{stats.LoansThisMonth} issued this month";

        _cardOverdue.Value = stats.OverdueLoans.ToString("N0");
        _cardOverdue.Subtitle = stats.OverdueLoans > 0 ? "Action needed!" : "All on time ✓";

        // Update quick stats
        var quickData = new[]
        {
            (stats.BooksAddedThisMonth.ToString(), AppTheme.Primary),
            (stats.NewMembersThisMonth.ToString(), AppTheme.Secondary),
            (stats.LoansThisMonth.ToString(), AppTheme.Success),
            (stats.ReturnsThisMonth.ToString(), AppTheme.Warning)
        };

        for (int i = 0; i < Math.Min(quickData.Length, _quickStatsPanel.Controls.Count); i++)
        {
            var chip = _quickStatsPanel.Controls[i];
            if (chip.Tag is (string label, string _))
            {
                chip.Tag = (label, quickData[i].Item1);
                chip.Invalidate();
            }
        }
    }

    /// <summary>
    /// Loads demo data when database is unavailable.
    /// </summary>
    public void LoadDemoData()
    {
        var demoStats = new DashboardStats
        {
            TotalBooks = 1247,
            TotalMembers = 389,
            ActiveLoans = 67,
            OverdueLoans = 5,
            BooksAddedThisMonth = 23,
            NewMembersThisMonth = 12,
            LoansThisMonth = 45,
            ReturnsThisMonth = 38,
            TotalFinesCollected = 125.50m
        };
        UpdateStatsUI(demoStats);

        // Demo recent loans
        var dt = new System.Data.DataTable();
        dt.Columns.AddRange(new[]
        {
            new System.Data.DataColumn("ID", typeof(int)),
            new System.Data.DataColumn("Book", typeof(string)),
            new System.Data.DataColumn("Member", typeof(string)),
            new System.Data.DataColumn("Borrow Date", typeof(string)),
            new System.Data.DataColumn("Due Date", typeof(string)),
            new System.Data.DataColumn("Status", typeof(string))
        });

        dt.Rows.Add(1, "The Great Gatsby", "Arun Kumar", "May 08, 2026", "May 22, 2026", "📗 Active");
        dt.Rows.Add(2, "Clean Code", "Priya Sharma", "May 11, 2026", "May 25, 2026", "📗 Active");
        dt.Rows.Add(3, "A Brief History of Time", "Mohamed Ali", "Apr 28, 2026", "May 12, 2026", "⚠ Overdue");
        dt.Rows.Add(4, "Atomic Habits", "David Fernando", "May 03, 2026", "May 17, 2026", "📗 Active");
        dt.Rows.Add(5, "1984", "Lakshmi Nair", "Apr 18, 2026", "May 02, 2026", "✅ Returned");
        dt.Rows.Add(6, "Sapiens", "Kasun Perera", "May 15, 2026", "May 29, 2026", "📗 Active");
        dt.Rows.Add(7, "The Hobbit", "Nithya Raj", "May 15, 2026", "May 29, 2026", "📗 Active");
        dt.Rows.Add(8, "Thinking, Fast and Slow", "Amara Silva", "May 06, 2026", "May 20, 2026", "📗 Active");

        _recentLoansGrid.DataSource = dt;
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        UpdateCardWidths();
    }
}
