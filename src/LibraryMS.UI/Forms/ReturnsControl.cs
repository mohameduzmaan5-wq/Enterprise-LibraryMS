using System.Drawing.Drawing2D;
using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using LibraryMS.Services;
using LibraryMS.Core.Entities;
using LibraryMS.UI.Controls;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Enterprise Return Book & Fine Management page.
/// Features: real-time fine preview, 7-day grace rule, return history,
/// overdue detection, search, and toast notifications.
/// </summary>
public class ReturnsControl : UserControl
{
    private readonly LoanService _loanService;

    // ── Toolbar controls ────────────────────────────────────────────
    private Guna2TextBox _searchBox      = null!;
    private Guna2Button  _returnButton   = null!;
    private Guna2Button  _refreshButton  = null!;
    private Guna2Button  _showActiveBtn  = null!;
    private Guna2Button  _showHistoryBtn = null!;
    private Guna2Button  _showOverdueBtn = null!;

    // ── Stats strip ─────────────────────────────────────────────────
    private Label _lblTotalActive    = null!;
    private Label _lblTotalOverdue   = null!;
    private Label _lblFinesCollected = null!;
    private Label _lblOutstanding    = null!;
    private Label _totalRecordsLabel = null!;

    // ── Grid ────────────────────────────────────────────────────────
    private DataGridView _grid      = null!;
    private Label        _emptyLabel = null!;

    // ── Fine preview panel ──────────────────────────────────────────
    private Panel  _finePanel     = null!;
    private Label  _lblFineTitle  = null!;
    private Label  _lblBorrowDate = null!;
    private Label  _lblDueDate    = null!;
    private Label  _lblDaysOver   = null!;
    private Label  _lblGrace      = null!;
    private Label  _lblFineAmt    = null!;
    private Label  _lblFineStatus = null!;

    private string _currentFilter = "Active";

    // ─── Constructor ────────────────────────────────────────────────
    public ReturnsControl()
    {
        _loanService = new LoanService();
        InitializeComponent();
        _ = LoadDataAsync();
    }

    // ─── Layout ─────────────────────────────────────────────────────
    private void InitializeComponent()
    {
        SuspendLayout();
        BackColor = AppTheme.Background;
        Dock      = DockStyle.Fill;
        Padding   = new Padding(32, 24, 32, 24);

        // ── Header ──────────────────────────────────────────────────
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent
        };
        var titleLabel = new Label
        {
            Text = "↩️  Return Book & Fine Management",
            Font = AppTheme.FontSubtitle, ForeColor = AppTheme.TextPrimary,
            AutoSize = true, Location = new Point(0, 8)
        };
        _totalRecordsLabel = new Label
        {
            Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary,
            AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        headerPanel.Controls.AddRange(new Control[] { titleLabel, _totalRecordsLabel });
        headerPanel.Resize += (s, e) =>
            _totalRecordsLabel.Location = new Point(headerPanel.Width - _totalRecordsLabel.Width - 10, 16);

        // ── Stats strip ─────────────────────────────────────────────
        var statsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 44, BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 4)
        };
        _lblTotalActive    = MakeStatLabel("📗 Active:", "—", AppTheme.Success);
        _lblTotalOverdue   = MakeStatLabel("⚠️ Overdue:", "—", AppTheme.Warning);
        _lblFinesCollected = MakeStatLabel("💰 Collected:", "—", AppTheme.Primary);
        _lblOutstanding    = MakeStatLabel("🔴 Outstanding:", "—", AppTheme.Danger);
        statsPanel.Controls.AddRange(new Control[]
            { _lblTotalActive, _lblTotalOverdue, _lblFinesCollected, _lblOutstanding });

        // ── Filter tab bar ───────────────────────────────────────────
        var filterPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent
        };
        _showActiveBtn  = CreateTab("📗 Active Loans", true);
        _showActiveBtn.Location = new Point(0, 6);
        _showActiveBtn.Click   += async (s, e) => { _currentFilter = "Active";  UpdateTabs(); await LoadDataAsync(); };

        _showOverdueBtn = CreateTab("⚠️ Overdue", false);
        _showOverdueBtn.Location = new Point(130, 6);
        _showOverdueBtn.Click   += async (s, e) => { _currentFilter = "Overdue"; UpdateTabs(); await LoadDataAsync(); };

        _showHistoryBtn = CreateTab("🕓 Return History", false);
        _showHistoryBtn.Location = new Point(248, 6);
        _showHistoryBtn.Click   += async (s, e) => { _currentFilter = "History"; UpdateTabs(); await LoadDataAsync(); };

        filterPanel.Controls.AddRange(new Control[] { _showActiveBtn, _showOverdueBtn, _showHistoryBtn });

        // ── Toolbar ──────────────────────────────────────────────────
        var toolbarPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent
        };
        _searchBox = new Guna2TextBox
        {
            PlaceholderText = "🔍  Search by book title or member name...",
            Size = new Size(380, 40), Location = new Point(0, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        ThemeManager.StyleTextBox(_searchBox);
        _searchBox.TextChanged += async (s, e) => await SearchAsync();

        _returnButton = new Guna2Button
        {
            Text = "↩️  Return Selected", Size = new Size(160, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StyleSuccessButton(_returnButton);
        _returnButton.Click += ReturnButton_Click;

        _refreshButton = new Guna2Button
        {
            Text = "↻", Size = new Size(40, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StyleSecondaryButton(_refreshButton);
        _refreshButton.Click += async (s, e) =>
        {
            _searchBox.Text = string.Empty;
            ClearFinePreview();
            await LoadDataAsync();
        };

        toolbarPanel.Controls.AddRange(new Control[] { _searchBox, _refreshButton, _returnButton });
        toolbarPanel.Resize += (s, e) =>
        {
            var r = toolbarPanel.Width;
            _refreshButton.Location = new Point(r - 48,  6);
            _returnButton.Location  = new Point(r - 216, 6);
        };

        // ── Main split area ──────────────────────────────────────────
        // Fine preview on right, grid on left
        _finePanel = BuildFinePreviewPanel();

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill, BackgroundColor = AppTheme.Surface
        };
        ThemeManager.StyleDataGridView(_grid);
        _grid.SelectionChanged += Grid_SelectionChanged;

        _emptyLabel = new Label
        {
            Text = "No records found.", Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextSecondary, AutoSize = true, Visible = false
        };

        var gridContainer = new Panel
        {
            Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(1)
        };
        gridContainer.Controls.Add(_emptyLabel);
        gridContainer.Controls.Add(_grid);
        gridContainer.Resize += (s, e) =>
        {
            _emptyLabel.Location = new Point(
                (gridContainer.Width  - _emptyLabel.Width)  / 2,
                (gridContainer.Height - _emptyLabel.Height) / 2);
        };

        // Main content area
        var contentArea = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Background };
        contentArea.Controls.Add(gridContainer);
        contentArea.Controls.Add(_finePanel);

        // ── Build layout (reverse DockStyle.Top order) ───────────────
        Controls.Add(contentArea);
        Controls.Add(toolbarPanel);
        Controls.Add(filterPanel);
        Controls.Add(statsPanel);
        Controls.Add(headerPanel);
        ResumeLayout();
    }

    // ─── Fine preview panel ──────────────────────────────────────────
    private Panel BuildFinePreviewPanel()
    {
        var panel = new Panel
        {
            Dock      = DockStyle.Right,
            Width     = 280,
            BackColor = AppTheme.SurfaceLight,
            Padding   = new Padding(18, 20, 18, 20)
        };
        panel.Paint += FinePanelPaint;

        _lblFineTitle = new Label
        {
            Text = "Fine Preview", Dock = DockStyle.Top, Height = 28,
            Font = AppTheme.FontHeading, ForeColor = AppTheme.Primary, BackColor = Color.Transparent
        };

        var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppTheme.Border, Margin = new Padding(0, 4, 0, 4) };

        _lblBorrowDate = MakeDetailLabel("📅 Issued:", "—");
        _lblDueDate    = MakeDetailLabel("📆 Due Date:", "—");
        _lblDaysOver   = MakeDetailLabel("⏰ Days Overdue:", "—");
        _lblGrace      = MakeDetailLabel("🛡️ Grace Period:", "7 days");

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = AppTheme.Separator, Margin = new Padding(0, 10, 0, 10) };

        _lblFineAmt = new Label
        {
            Text = "$ —", Dock = DockStyle.Top, Height = 50,
            Font = new Font("Segoe UI Semibold", 26f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary, TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        _lblFineStatus = new Label
        {
            Text = "Select an active loan to preview fine.", Dock = DockStyle.Top, Height = 44,
            Font = AppTheme.FontBodySmall, ForeColor = AppTheme.TextSecondary,
            BackColor = Color.Transparent
        };

        // Add in reverse dock order
        panel.Controls.Add(_lblFineStatus);
        panel.Controls.Add(_lblFineAmt);
        panel.Controls.Add(divider);
        panel.Controls.Add(_lblGrace);
        panel.Controls.Add(_lblDaysOver);
        panel.Controls.Add(_lblDueDate);
        panel.Controls.Add(_lblBorrowDate);
        panel.Controls.Add(sep);
        panel.Controls.Add(_lblFineTitle);

        return panel;
    }

    private static void FinePanelPaint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel p) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(AppTheme.Border, 1);
        e.Graphics.DrawLine(pen, 0, 0, 0, p.Height);
    }

    private Label MakeDetailLabel(string heading, string value)
    {
        var lbl = new Label
        {
            Dock = DockStyle.Top, Height = 24, BackColor = Color.Transparent,
            Font = AppTheme.FontBodySmall, ForeColor = AppTheme.TextSecondary,
            Text = $"{heading}  {value}"
        };
        return lbl;
    }

    // ─── Helpers ─────────────────────────────────────────────────────
    private static Label MakeStatLabel(string heading, string value, Color color) => new Label
    {
        Text = $"{heading}  {value}", Font = AppTheme.FontBodySmall,
        ForeColor = color, AutoSize = true,
        Margin = new Padding(0, 0, 22, 0), Padding = new Padding(0, 6, 0, 0)
    };

    private Guna2Button CreateTab(string text, bool active)
    {
        var btn = new Guna2Button
        {
            Text = text, Size = new Size(122, 40),
            Font = AppTheme.FontButton, BorderRadius = 12, Animated = true,
            FillColor     = active ? Color.FromArgb(30, AppTheme.Secondary) : Color.Transparent,
            ForeColor     = active ? AppTheme.Secondary : AppTheme.TextSecondary,
            BorderColor   = active ? AppTheme.Secondary : AppTheme.Border,
            BorderThickness = 1
        };
        btn.HoverState.FillColor = Color.FromArgb(20, AppTheme.Secondary);
        return btn;
    }

    private void UpdateTabs()
    {
        foreach (var (key, btn, color) in new[]
        {
            ("Active",  _showActiveBtn,  AppTheme.Success),
            ("Overdue", _showOverdueBtn, AppTheme.Warning),
            ("History", _showHistoryBtn, AppTheme.Secondary)
        })
        {
            var isActive     = key == _currentFilter;
            btn.FillColor    = isActive ? Color.FromArgb(30, color) : Color.Transparent;
            btn.ForeColor    = isActive ? color : AppTheme.TextSecondary;
            btn.BorderColor  = isActive ? color : AppTheme.Border;
        }
        ClearFinePreview();
    }

    // ─── Data loading ────────────────────────────────────────────────
    private async Task LoadDataAsync()
    {
        try
        {
            IEnumerable<Loan> loans = _currentFilter switch
            {
                "Active"  => await _loanService.GetActiveLoansAsync(),
                "Overdue" => await _loanService.GetOverdueLoansAsync(),
                "History" => await _loanService.GetReturnHistoryAsync(),
                _         => await _loanService.GetAllLoansAsync()
            };
            PopulateGrid(loans.ToList());
        }
        catch { LoadDemoData(); }

        await RefreshStatsAsync();
    }

    private async Task SearchAsync()
    {
        var term = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(term)) { await LoadDataAsync(); return; }
        try
        {
            var results = (await _loanService.SearchLoansAsync(term)).ToList();
            // Apply filter if set
            if (_currentFilter == "Active")
                results = results.Where(l => l.Status == "Active").ToList();
            else if (_currentFilter == "Overdue")
                results = results.Where(l => l.IsOverdue).ToList();
            else if (_currentFilter == "History")
                results = results.Where(l => l.Status == "Returned").ToList();
            PopulateGrid(results);
        }
        catch { }
    }

    private async Task RefreshStatsAsync()
    {
        try
        {
            var active    = await _loanService.GetActiveCountAsync();
            var overdue   = await _loanService.GetOverdueCountAsync();
            var collected = await _loanService.GetTotalFinesCollectedAsync();
            var outstanding = await _loanService.GetOutstandingFinesAsync();

            _lblTotalActive.Text    = $"📗 Active:  {active}";
            _lblTotalOverdue.Text   = $"⚠️ Overdue:  {overdue}";
            _lblFinesCollected.Text = $"💰 Collected:  ${collected:F2}";
            _lblOutstanding.Text    = $"🔴 Outstanding:  ${outstanding:F2}";
        }
        catch { }
    }

    // ─── Grid ────────────────────────────────────────────────────────
    private void PopulateGrid(List<Loan> loans)
    {
        _totalRecordsLabel.Text  = $"{loans.Count} record{(loans.Count != 1 ? "s" : "")}";
        _emptyLabel.Visible      = loans.Count == 0;
        _grid.Visible            = loans.Count > 0;

        _grid.DataSource = null;
        _grid.Columns.Clear();

        var dt = new System.Data.DataTable();
        dt.Columns.AddRange(new[]
        {
            new System.Data.DataColumn("ID",       typeof(int)),
            new System.Data.DataColumn("Book",     typeof(string)),
            new System.Data.DataColumn("Member",   typeof(string)),
            new System.Data.DataColumn("Issued",   typeof(string)),
            new System.Data.DataColumn("Due Date", typeof(string)),
            new System.Data.DataColumn("Overdue",  typeof(string)),
            new System.Data.DataColumn("Fine",     typeof(string)),
            new System.Data.DataColumn("Status",   typeof(string))
        });

        foreach (var l in loans)
        {
            var overdueDays   = l.Status == "Active" ? Math.Max(0, (int)(DateTime.Now - l.DueDate).TotalDays) : 0;
            var previewFine   = l.Status == "Active" ? LoanService.ComputeFine(l.DueDate) : l.FineAmount;
            var overdueText   = overdueDays > 0 ? $"{overdueDays}d" : "—";
            var fineText      = previewFine > 0 ? $"${previewFine:F2}" : (l.FineAmount > 0 ? $"${l.FineAmount:F2}" : "None");
            var status        = l.Status == "Returned"
                ? "✅ Returned"
                : l.IsOverdue
                    ? $"⚠️ Overdue ({overdueDays}d)"
                    : $"📗 Active ({Math.Max(0, l.DaysRemaining)}d left)";

            dt.Rows.Add(
                l.Id,
                l.BookTitle  ?? "—",
                l.MemberName ?? "—",
                l.BorrowDate.ToString("MMM dd, yyyy"),
                l.DueDate.ToString("MMM dd, yyyy"),
                overdueText,
                fineText,
                status);
        }

        _grid.DataSource = dt;
        if (_grid.Columns.Count > 0)
        {
            _grid.Columns["ID"].Width       = 48;
            _grid.Columns["Book"].FillWeight = 28;
            _grid.Columns["Member"].FillWeight = 22;
            _grid.Columns["Issued"].Width   = 105;
            _grid.Columns["Due Date"].Width = 105;
            _grid.Columns["Overdue"].Width  = 72;
            _grid.Columns["Fine"].Width     = 70;
            _grid.Columns["Status"].Width   = 155;
        }
    }

    private void LoadDemoData()
    {
        PopulateGrid(new List<Loan>
        {
            new() { Id=1, BookTitle="Clean Code",    MemberName="Arun Kumar",   BorrowDate=DateTime.Now.AddDays(-22), DueDate=DateTime.Now.AddDays(-8),  Status="Active" },
            new() { Id=2, BookTitle="1984",           MemberName="Priya Sharma", BorrowDate=DateTime.Now.AddDays(-15), DueDate=DateTime.Now.AddDays(-1),  Status="Active" },
            new() { Id=3, BookTitle="Sapiens",        MemberName="David F.",     BorrowDate=DateTime.Now.AddDays(-5),  DueDate=DateTime.Now.AddDays(9),   Status="Active" },
            new() { Id=4, BookTitle="The Hobbit",     MemberName="Lakshmi N.",   BorrowDate=DateTime.Now.AddDays(-30), DueDate=DateTime.Now.AddDays(-16), Status="Returned", ReturnDate=DateTime.Now.AddDays(-14), FineAmount=9.00m },
            new() { Id=5, BookTitle="Atomic Habits",  MemberName="Kasun P.",     BorrowDate=DateTime.Now.AddDays(-40), DueDate=DateTime.Now.AddDays(-26), Status="Active" }
        });
    }

    // ─── Fine preview logic ──────────────────────────────────────────
    private void Grid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0) { ClearFinePreview(); return; }

        try
        {
            var row    = _grid.SelectedRows[0];
            var status = row.Cells["Status"].Value?.ToString() ?? "";

            if (status.Contains("Returned"))
            {
                // Show paid fine info
                var fineText  = row.Cells["Fine"].Value?.ToString() ?? "None";
                var issueDate = row.Cells["Issued"].Value?.ToString() ?? "—";
                var dueDate   = row.Cells["Due Date"].Value?.ToString() ?? "—";

                _lblBorrowDate.Text = $"📅 Issued:    {issueDate}";
                _lblDueDate.Text    = $"📆 Due Date:  {dueDate}";
                _lblDaysOver.Text   = $"⏰ Days Overdue:  {row.Cells["Overdue"].Value}";
                _lblGrace.Text      = "🛡️ Grace Period:  7 days";
                _lblFineAmt.Text    = fineText == "None" ? "$ 0.00" : fineText;
                _lblFineAmt.ForeColor = AppTheme.Success;
                _lblFineStatus.Text = "✅ Book returned — fine recorded.";
                _lblFineStatus.ForeColor = AppTheme.TextSecondary;
            }
            else
            {
                // Real-time fine preview for active loans
                var overdueStr  = row.Cells["Overdue"].Value?.ToString() ?? "—";
                var issueDate   = row.Cells["Issued"].Value?.ToString()   ?? "—";
                var dueDateStr  = row.Cells["Due Date"].Value?.ToString() ?? "—";
                var overdueDays = overdueStr != "—" && overdueStr.EndsWith("d")
                    ? int.Parse(overdueStr.TrimEnd('d'))
                    : 0;

                _lblBorrowDate.Text   = $"📅 Issued:    {issueDate}";
                _lblDueDate.Text      = $"📆 Due Date:  {dueDateStr}";
                _lblDaysOver.Text     = $"⏰ Days Overdue:  {(overdueDays > 0 ? $"{overdueDays} days" : "Not overdue")}";
                _lblGrace.Text        = "🛡️ Grace Period:  7 days";

                // Fine from grid (precomputed)
                var fineText = row.Cells["Fine"].Value?.ToString() ?? "None";
                var hasFine  = fineText != "None";

                _lblFineAmt.Text      = hasFine ? fineText : "$ 0.00";
                _lblFineAmt.ForeColor = hasFine ? AppTheme.Danger : AppTheme.Success;
                _lblFineStatus.ForeColor = hasFine ? AppTheme.Warning : AppTheme.Success;
                _lblFineStatus.Text   = hasFine
                    ? $"⚠️  Fine will be charged on return.\n    ($1.00/day after 7-day grace)"
                    : overdueDays > 0
                        ? $"🛡️  Within grace period — no fine yet."
                        : "✓  On time — no fine applicable.";
            }
        }
        catch { ClearFinePreview(); }
    }

    private void ClearFinePreview()
    {
        _lblBorrowDate.Text   = "📅 Issued:    —";
        _lblDueDate.Text      = "📆 Due Date:  —";
        _lblDaysOver.Text     = "⏰ Days Overdue:  —";
        _lblGrace.Text        = "🛡️ Grace Period:  7 days";
        _lblFineAmt.Text      = "$ —";
        _lblFineAmt.ForeColor = AppTheme.TextPrimary;
        _lblFineStatus.Text   = "Select an active loan to preview fine.";
        _lblFineStatus.ForeColor = AppTheme.TextSecondary;
    }

    // ─── Return action ────────────────────────────────────────────────
    private async void ReturnButton_Click(object? sender, EventArgs e)
    {
        if (_grid.SelectedRows.Count == 0)
        {
            ToastNotification.Show(this.FindForm()!, "Please select a loan record to return.", ToastType.Info);
            return;
        }

        var statusCell = _grid.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";
        if (statusCell.Contains("Returned"))
        {
            ToastNotification.Show(this.FindForm()!, "This book has already been returned.", ToastType.Warning);
            return;
        }

        var loanId   = (int)_grid.SelectedRows[0].Cells["ID"].Value;
        var bookName = _grid.SelectedRows[0].Cells["Book"].Value?.ToString() ?? "this book";
        var member   = _grid.SelectedRows[0].Cells["Member"].Value?.ToString() ?? "member";
        var fineCell = _grid.SelectedRows[0].Cells["Fine"].Value?.ToString() ?? "None";
        var fineInfo = fineCell != "None" ? $"\n\n⚠️  Fine: {fineCell}  (7-day grace applied)" : "\n\n✓  No fine applicable.";

        var confirm = new Guna2MessageDialog
        {
            Caption = "Confirm Book Return",
            Text    = $"Return '{bookName}' for {member}?{fineInfo}",
            Buttons = MessageDialogButtons.YesNo,
            Icon    = MessageDialogIcon.Question,
            Style   = MessageDialogStyle.Dark,
            Parent  = this.FindForm()
        };

        if (confirm.Show() != DialogResult.Yes) return;

        var (ok, msg) = await _loanService.ReturnBookAsync(loanId);
        if (ok)
        {
            ToastNotification.Show(this.FindForm()!, msg, ToastType.Success);
            ClearFinePreview();
            await LoadDataAsync();
        }
        else
        {
            ToastNotification.Show(this.FindForm()!, msg, ToastType.Error);
        }
    }
}
