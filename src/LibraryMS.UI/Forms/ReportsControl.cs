using System.Drawing.Drawing2D;
using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using LibraryMS.Services;
using LibraryMS.Core.DTOs;
using LibraryMS.UI.Controls;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Enterprise Reports & Analytics page with 6 report types,
/// date filtering, bar-chart trend view, CSV export, and print.
/// </summary>
public class ReportsControl : UserControl
{
    private readonly ReportService _svc = new();

    // toolbar
    private Guna2Button _btnBorrowHistory = null!;
    private Guna2Button _btnOverdue       = null!;
    private Guna2Button _btnTopBooks      = null!;
    private Guna2Button _btnMemberAct     = null!;
    private Guna2Button _btnFines         = null!;
    private Guna2Button _btnInventory     = null!;
    private Guna2Button _btnExportCsv     = null!;
    private Guna2Button _btnPrint         = null!;
    private Guna2Button _btnRefresh       = null!;
    private Guna2TextBox  _searchBox      = null!;
    private Guna2DateTimePicker _dtFrom   = null!;
    private Guna2DateTimePicker _dtTo     = null!;

    // grid + chart panel
    private DataGridView _grid       = null!;
    private Panel        _chartPanel = null!;
    private Label        _titleLabel = null!;
    private Label        _countLabel = null!;
    private Label        _emptyLabel = null!;

    // stats strip
    private Label _stat1 = null!, _stat2 = null!, _stat3 = null!, _stat4 = null!;

    private string _currentReport = "BorrowHistory";
    // cached raw data for search filtering
    private System.Data.DataTable? _fullTable;
    private List<MonthlyTrendRow> _trends = new();

    public ReportsControl()
    {
        InitializeComponent();
        _ = LoadReportAsync("BorrowHistory");
    }

    // ─── Layout ────────────────────────────────────────────────────
    private void InitializeComponent()
    {
        SuspendLayout();
        BackColor = AppTheme.Background;
        Dock = DockStyle.Fill;
        Padding = new Padding(28, 20, 28, 20);

        // ── Header ──────────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
        _titleLabel = new Label
        {
            Text = "📊  Reports & Analytics",
            Font = AppTheme.FontSubtitle, ForeColor = AppTheme.TextPrimary,
            AutoSize = true, Location = new Point(0, 8)
        };
        _countLabel = new Label
        {
            Font = AppTheme.FontBody, ForeColor = AppTheme.TextSecondary,
            AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        header.Controls.AddRange(new Control[] { _titleLabel, _countLabel });
        header.Resize += (s, e) => _countLabel.Location = new Point(header.Width - _countLabel.Width - 4, 16);

        // ── Stats strip ──────────────────────────────────────────
        var statsBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 4)
        };
        _stat1 = StatChip("—", AppTheme.Primary);
        _stat2 = StatChip("—", AppTheme.Secondary);
        _stat3 = StatChip("—", AppTheme.Success);
        _stat4 = StatChip("—", AppTheme.Warning);
        statsBar.Controls.AddRange(new Control[] { _stat1, _stat2, _stat3, _stat4 });

        // ── Report selector tabs ─────────────────────────────────
        var tabBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 4, 0, 4)
        };
        _btnBorrowHistory = MakeTab("📋 Borrow History", "BorrowHistory", true);
        _btnOverdue       = MakeTab("⚠️ Overdue",        "Overdue",       false);
        _btnTopBooks      = MakeTab("🏆 Top Books",      "TopBooks",      false);
        _btnMemberAct     = MakeTab("👥 Member Activity","MemberActivity",false);
        _btnFines         = MakeTab("💰 Fines",          "Fines",         false);
        _btnInventory     = MakeTab("📦 Inventory",      "Inventory",     false);
        tabBar.Controls.AddRange(new Control[]
            { _btnBorrowHistory, _btnOverdue, _btnTopBooks, _btnMemberAct, _btnFines, _btnInventory });

        // ── Filter / toolbar ─────────────────────────────────────
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };

        _searchBox = new Guna2TextBox
        {
            PlaceholderText = "🔍  Filter results...",
            Size = new Size(240, 38), Location = new Point(0, 6),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        ThemeManager.StyleTextBox(_searchBox);
        _searchBox.TextChanged += FilterGrid;

        var lblFrom = new Label { Text = "From:", Font = AppTheme.FontBodySmall, ForeColor = AppTheme.TextSecondary, AutoSize = true, Location = new Point(250, 16) };
        _dtFrom = new Guna2DateTimePicker { Size = new Size(130, 38), Location = new Point(290, 6), Value = DateTime.Today.AddMonths(-3) };
        ThemeManager.StyleDatePicker(_dtFrom);

        var lblTo = new Label { Text = "To:", Font = AppTheme.FontBodySmall, ForeColor = AppTheme.TextSecondary, AutoSize = true, Location = new Point(432, 16) };
        _dtTo = new Guna2DateTimePicker { Size = new Size(130, 38), Location = new Point(455, 6), Value = DateTime.Today };
        ThemeManager.StyleDatePicker(_dtTo);

        _btnRefresh = ActionButton("↻", AppTheme.Border, AppTheme.TextSecondary, new Size(38, 38));
        _btnRefresh.Click += async (s, e) => await LoadReportAsync(_currentReport);

        _btnExportCsv = ActionButton("⬇ CSV", AppTheme.Secondary, AppTheme.Background, new Size(80, 38));
        _btnExportCsv.Click += ExportCsv_Click;

        _btnPrint = ActionButton("🖨 Print", AppTheme.Primary, AppTheme.TextOnPrimary, new Size(80, 38));
        _btnPrint.Click += Print_Click;

        toolbar.Controls.AddRange(new Control[]
            { _searchBox, lblFrom, _dtFrom, lblTo, _dtTo, _btnRefresh, _btnExportCsv, _btnPrint });
        toolbar.Resize += (s, e) =>
        {
            var r = toolbar.Width;
            _btnPrint.Location      = new Point(r - 88,  6);
            _btnExportCsv.Location  = new Point(r - 176, 6);
            _btnRefresh.Location    = new Point(r - 222, 6);
        };

        // ── Chart panel ─────────────────────────────────────────
        _chartPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 160,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8)
        };
        _chartPanel.Paint += ChartPanel_Paint;

        // ── Grid ────────────────────────────────────────────────
        _grid = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = AppTheme.Surface };
        ThemeManager.StyleDataGridView(_grid);

        _emptyLabel = new Label
        {
            Text = "No data for selected range.", Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextSecondary, AutoSize = true, Visible = false
        };

        var gridWrap = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(1) };
        gridWrap.Controls.Add(_emptyLabel);
        gridWrap.Controls.Add(_grid);
        gridWrap.Resize += (s, e) => _emptyLabel.Location = new Point(
            (gridWrap.Width  - _emptyLabel.Width)  / 2,
            (gridWrap.Height - _emptyLabel.Height) / 2);

        // ── Build layout (reverse dock order) ────────────────────
        Controls.Add(gridWrap);
        Controls.Add(_chartPanel);
        Controls.Add(toolbar);
        Controls.Add(tabBar);
        Controls.Add(statsBar);
        Controls.Add(header);
        ResumeLayout();
    }

    // ─── Tab helpers ────────────────────────────────────────────────
    private Guna2Button MakeTab(string text, string key, bool active)
    {
        var btn = new Guna2Button
        {
            Text = text, Size = new Size(142, 38),
            Font = AppTheme.FontButton, BorderRadius = 10, Animated = true,
            Margin = new Padding(0, 0, 6, 0),
            FillColor    = active ? Color.FromArgb(30, AppTheme.Primary) : Color.Transparent,
            ForeColor    = active ? AppTheme.Primary : AppTheme.TextSecondary,
            BorderColor  = active ? AppTheme.Primary : AppTheme.Border,
            BorderThickness = 1
        };
        btn.HoverState.FillColor = Color.FromArgb(20, AppTheme.Primary);
        btn.Click += async (s, e) => { _currentReport = key; UpdateTabs(); await LoadReportAsync(key); };
        return btn;
    }

    private Guna2Button ActionButton(string text, Color fill, Color fore, Size size)
    {
        var btn = new Guna2Button { Text = text, Size = size, FillColor = fill, ForeColor = fore, Font = AppTheme.FontButton, BorderRadius = 10, Animated = true };
        return btn;
    }

    private static Label StatChip(string text, Color color) => new Label
    {
        Text = text, Font = AppTheme.FontBodySmall, ForeColor = color,
        AutoSize = true, Margin = new Padding(0, 0, 24, 0), Padding = new Padding(0, 4, 0, 0)
    };

    private void UpdateTabs()
    {
        var all = new[] { ("BorrowHistory", _btnBorrowHistory), ("Overdue", _btnOverdue),
            ("TopBooks", _btnTopBooks), ("MemberActivity", _btnMemberAct),
            ("Fines", _btnFines), ("Inventory", _btnInventory) };
        foreach (var (key, btn) in all)
        {
            var on = key == _currentReport;
            btn.FillColor   = on ? Color.FromArgb(30, AppTheme.Primary) : Color.Transparent;
            btn.ForeColor   = on ? AppTheme.Primary : AppTheme.TextSecondary;
            btn.BorderColor = on ? AppTheme.Primary : AppTheme.Border;
        }
        _searchBox.Text = string.Empty;
    }

    // ─── Load reports ───────────────────────────────────────────────
    private async Task LoadReportAsync(string key)
    {
        var from = _dtFrom.Value.Date;
        var to   = _dtTo.Value.Date;

        try
        {
            switch (key)
            {
                case "BorrowHistory":
                    var bh = (await _svc.GetBorrowHistoryAsync(from, to)).ToList();
                    _fullTable = BuildTable(bh,
                        new[] { "ID", "Book", "Author", "Member", "Borrowed", "Due", "Returned", "Fine", "Status" },
                        r => new[] { r.LoanId.ToString(), r.BookTitle, r.Author, r.MemberName,
                            r.BorrowDate.ToString("MMM dd yy"), r.DueDate.ToString("MMM dd yy"),
                            r.ReturnDate?.ToString("MMM dd yy") ?? "—",
                            r.FineAmount > 0 ? $"${r.FineAmount:F2}" : "—", r.Status });
                    SetStats($"📋 {bh.Count} transactions", $"✅ {bh.Count(x=>x.Status=="Returned")} returned",
                        $"📗 {bh.Count(x=>x.Status=="Active")} active", $"💰 ${bh.Sum(x=>x.FineAmount):F2} fines");
                    break;

                case "Overdue":
                    var ov = (await _svc.GetOverdueReportAsync()).ToList();
                    _fullTable = BuildTable(ov,
                        new[] { "ID", "Book", "Member", "Email", "Borrowed", "Due", "Days Overdue", "Accrued Fine" },
                        r => new[] { r.LoanId.ToString(), r.BookTitle, r.MemberName, r.MemberEmail,
                            r.BorrowDate.ToString("MMM dd yy"), r.DueDate.ToString("MMM dd yy"),
                            $"{r.DaysOverdue}d", $"${r.AccruedFine:F2}" });
                    SetStats($"⚠️ {ov.Count} overdue", $"📅 Oldest: {(ov.Any() ? ov.Max(x=>x.DaysOverdue)+"d" : "—")}",
                        $"💰 ${ov.Sum(x=>x.AccruedFine):F2} outstanding", $"🛡️ 7-day grace applied");
                    break;

                case "TopBooks":
                    var tb = (await _svc.GetTopBorrowedBooksAsync(20)).ToList();
                    _fullTable = BuildTable(tb,
                        new[] { "Rank", "Title", "Author", "Category", "Total Qty", "Available", "Borrows" },
                        (r, i) => new[] { (i+1).ToString(), r.Title, r.Author, r.Category,
                            r.Quantity.ToString(), r.Available.ToString(), r.BorrowCount.ToString() });
                    SetStats($"🏆 Top {tb.Count} books", $"📚 Most: {(tb.Any() ? tb[0].BorrowCount+" borrows":"-")}",
                        $"📖 {tb.Sum(x=>x.BorrowCount)} total borrows", $"🔖 {tb.Select(x=>x.Category).Distinct().Count()} categories");
                    break;

                case "MemberActivity":
                    var ma = (await _svc.GetMemberActivityAsync(from, to)).ToList();
                    _fullTable = BuildTable(ma,
                        new[] { "ID", "Member", "Type", "Total Loans", "Active", "Returned", "Fines", "Last Active" },
                        r => new[] { r.MemberId.ToString(), r.MemberName, r.MembershipType,
                            r.TotalLoans.ToString(), r.ActiveLoans.ToString(), r.ReturnedLoans.ToString(),
                            $"${r.TotalFines:F2}", r.LastActivity.ToString("MMM dd yy") });
                    SetStats($"👥 {ma.Count} members", $"📗 {ma.Sum(x=>x.ActiveLoans)} active loans",
                        $"💰 ${ma.Sum(x=>x.TotalFines):F2} fines", $"🏆 Top: {(ma.Any() ? ma[0].MemberName : "—")}");
                    break;

                case "Fines":
                    var fi = (await _svc.GetFineReportAsync(from, to)).ToList();
                    _fullTable = BuildTable(fi,
                        new[] { "ID", "Book", "Member", "Due Date", "Returned", "Days Over", "Fine", "Status" },
                        r => new[] { r.LoanId.ToString(), r.BookTitle, r.MemberName,
                            r.DueDate.ToString("MMM dd yy"), r.ReturnDate.ToString("MMM dd yy"),
                            $"{r.OverdueDays}d", $"${r.FineAmount:F2}", r.Status });
                    SetStats($"💰 {fi.Count} fines", $"💵 Total: ${fi.Sum(x=>x.FineAmount):F2}",
                        $"📊 Avg: ${(fi.Any() ? fi.Average(x=>x.FineAmount) : 0):F2}",
                        $"⏰ Max: ${(fi.Any() ? fi.Max(x=>x.FineAmount) : 0):F2}");
                    break;

                case "Inventory":
                    var inv = (await _svc.GetInventoryReportAsync()).ToList();
                    _fullTable = BuildTable(inv,
                        new[] { "ID", "Title", "Author", "Category", "ISBN", "Qty", "Available", "Checked Out", "Status" },
                        r => new[] { r.BookId.ToString(), r.Title, r.Author, r.Category, r.ISBN,
                            r.Quantity.ToString(), r.Available.ToString(), r.CheckedOut.ToString(), r.Status });
                    SetStats($"📦 {inv.Count} titles", $"✅ {inv.Sum(x=>x.Available)} available",
                        $"🔄 {inv.Sum(x=>x.CheckedOut)} checked out",
                        $"🏷 {inv.Select(x=>x.Category).Distinct().Count()} categories");
                    break;
            }

            // Bind grid
            BindGrid(_fullTable);

            // Load trend data for chart
            _trends = (await _svc.GetMonthlyTrendsAsync(6)).ToList();
            _chartPanel.Invalidate();
        }
        catch
        {
            ToastNotification.Show(this.FindForm()!, "Database unavailable — showing offline view.", ToastType.Warning);
            LoadDemoData(key);
        }
    }

    // ─── Table builder (with rank overload) ─────────────────────────
    private static System.Data.DataTable BuildTable<T>(
        List<T> rows, string[] cols, Func<T, string[]> selector)
    {
        var dt = new System.Data.DataTable();
        foreach (var c in cols) dt.Columns.Add(c, typeof(string));
        foreach (var r in rows)
        {
            var vals = selector(r);
            dt.Rows.Add(vals.Cast<object>().ToArray());
        }
        return dt;
    }

    private static System.Data.DataTable BuildTable<T>(
        List<T> rows, string[] cols, Func<T, int, string[]> selector)
    {
        var dt = new System.Data.DataTable();
        foreach (var c in cols) dt.Columns.Add(c, typeof(string));
        for (int i = 0; i < rows.Count; i++)
            dt.Rows.Add(selector(rows[i], i).Cast<object>().ToArray());
        return dt;
    }

    private void BindGrid(System.Data.DataTable? dt)
    {
        _grid.DataSource = null;
        _grid.Columns.Clear();
        if (dt == null) return;
        _grid.DataSource = dt;

        _countLabel.Text = $"{dt.Rows.Count} record{(dt.Rows.Count != 1 ? "s" : "")}";
        _emptyLabel.Visible = dt.Rows.Count == 0;
        _grid.Visible       = dt.Rows.Count > 0;
    }

    private void SetStats(string s1, string s2, string s3, string s4)
    {
        _stat1.Text = s1; _stat2.Text = s2; _stat3.Text = s3; _stat4.Text = s4;
    }

    // ─── Live search filter ──────────────────────────────────────────
    private void FilterGrid(object? sender, EventArgs e)
    {
        if (_fullTable == null) return;
        var term = _searchBox.Text.Trim().ToLower();
        if (string.IsNullOrEmpty(term)) { BindGrid(_fullTable); return; }

        var filtered = _fullTable.Clone();
        foreach (System.Data.DataRow row in _fullTable.Rows)
        {
            if (row.ItemArray.Any(v => v?.ToString()?.ToLower().Contains(term) == true))
                filtered.ImportRow(row);
        }
        BindGrid(filtered);
    }

    // ─── Bar chart ───────────────────────────────────────────────────
    private void ChartPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = _chartPanel.ClientRectangle;
        g.Clear(AppTheme.Surface);

        // border
        using var borderPen = new Pen(AppTheme.Border, 1);
        g.DrawRectangle(borderPen, 0, 0, rect.Width - 1, rect.Height - 1);

        if (_trends.Count == 0)
        {
            using var nb = new SolidBrush(AppTheme.TextMuted);
            g.DrawString("Monthly Trend — awaiting data", AppTheme.FontCaption, nb, 12, 8);
            return;
        }

        // Title
        using var titleBrush = new SolidBrush(AppTheme.TextSecondary);
        g.DrawString("📈  Monthly Loan & Return Trend", AppTheme.FontBodySmall, titleBrush, 12, 6);

        int padL = 50, padR = 20, padT = 30, padB = 30;
        int chartW = rect.Width  - padL - padR;
        int chartH = rect.Height - padT - padB;
        int maxVal = _trends.Max(t => Math.Max(t.Loans, t.Returns)) + 1;
        int barGroupW = chartW / _trends.Count;
        int barW = Math.Max(4, barGroupW / 3);

        for (int i = 0; i < _trends.Count; i++)
        {
            var t  = _trends[i];
            int x0 = padL + i * barGroupW;

            // Loans bar
            int lH = (int)((double)t.Loans / maxVal * chartH);
            using var lb = new SolidBrush(Color.FromArgb(180, AppTheme.Primary));
            g.FillRectangle(lb, x0, padT + chartH - lH, barW, lH);

            // Returns bar
            int rH = (int)((double)t.Returns / maxVal * chartH);
            using var rb = new SolidBrush(Color.FromArgb(180, AppTheme.Success));
            g.FillRectangle(rb, x0 + barW + 2, padT + chartH - rH, barW, rH);

            // X label
            using var xb = new SolidBrush(AppTheme.TextMuted);
            g.DrawString(t.Label, AppTheme.FontCaption, xb, x0, padT + chartH + 4);
        }

        // Y axis
        using var axisPen = new Pen(AppTheme.Border, 1);
        g.DrawLine(axisPen, padL, padT, padL, padT + chartH);
        g.DrawLine(axisPen, padL, padT + chartH, padL + chartW, padT + chartH);

        // Legend
        using var legBrush1 = new SolidBrush(AppTheme.Primary);
        using var legBrush2 = new SolidBrush(AppTheme.Success);
        using var legTxt    = new SolidBrush(AppTheme.TextSecondary);
        int lx = rect.Width - 130;
        g.FillRectangle(legBrush1, lx, 8, 12, 10);
        g.DrawString("Loans",   AppTheme.FontCaption, legTxt, lx + 16, 6);
        g.FillRectangle(legBrush2, lx + 65, 8, 12, 10);
        g.DrawString("Returns", AppTheme.FontCaption, legTxt, lx + 81, 6);
    }

    // ─── Export & Print ──────────────────────────────────────────────
    private async void ExportCsv_Click(object? sender, EventArgs e)
    {
        try
        {
            string csv = _currentReport switch
            {
                "BorrowHistory"  => ReportService.ToCsv(await _svc.GetBorrowHistoryAsync(_dtFrom.Value, _dtTo.Value),  "Borrow History"),
                "Overdue"        => ReportService.ToCsv(await _svc.GetOverdueReportAsync(),                             "Overdue Report"),
                "TopBooks"       => ReportService.ToCsv(await _svc.GetTopBorrowedBooksAsync(20),                        "Top Borrowed Books"),
                "MemberActivity" => ReportService.ToCsv(await _svc.GetMemberActivityAsync(_dtFrom.Value, _dtTo.Value),  "Member Activity"),
                "Fines"          => ReportService.ToCsv(await _svc.GetFineReportAsync(_dtFrom.Value, _dtTo.Value),      "Fine Report"),
                "Inventory"      => ReportService.ToCsv(await _svc.GetInventoryReportAsync(),                           "Inventory"),
                _ => string.Empty
            };
            if (string.IsNullOrEmpty(csv)) { ToastNotification.Show(this.FindForm()!, "No data to export.", ToastType.Warning); return; }
            if (SaveCsvToFile(csv, _currentReport, out var path))
                ToastNotification.Show(this.FindForm()!, $"Exported: {Path.GetFileName(path)}", ToastType.Success);
        }
        catch (Exception ex)
        {
            ToastNotification.Show(this.FindForm()!, $"Export failed: {ex.Message}", ToastType.Error);
        }
    }

    private void Print_Click(object? sender, EventArgs e)
    {
        if (_fullTable == null || _fullTable.Rows.Count == 0)
        {
            ToastNotification.Show(this.FindForm()!, "No data to print.", ToastType.Warning);
            return;
        }
        var cols = _fullTable.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToArray();
        var text = ReportService.ToTextReport(
            _fullTable.Rows.Cast<System.Data.DataRow>().ToList(),
            _currentReport,
            cols,
            row => cols.Select((c, i) => row[i]?.ToString() ?? "").ToArray());
        PrintReportText(text, _currentReport);
    }

    // ── WinForms export / print helpers (UI layer only) ──────────
    private static bool SaveCsvToFile(string csv, string name, out string savedPath)
    {
        savedPath = string.Empty;
        using var dlg = new SaveFileDialog
        {
            Title      = "Export Report as CSV",
            FileName   = $"{name}_{DateTime.Now:yyyyMMdd_HHmm}.csv",
            Filter     = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = "csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dlg.ShowDialog() != DialogResult.OK) return false;
        File.WriteAllText(dlg.FileName, csv, System.Text.Encoding.UTF8);
        savedPath = dlg.FileName;
        return true;
    }

    private static void PrintReportText(string text, string title)
    {
        var pd      = new System.Drawing.Printing.PrintDocument { DocumentName = $"LibraryMS — {title}" };
        var lines   = text.Split('\n');
        var lineIdx = 0;
        pd.PrintPage += (s, e) =>
        {
            if (e.Graphics == null) return;
            var font  = new Font("Courier New", 8f);
            float y   = e.MarginBounds.Top;
            float lineH = font.GetHeight(e.Graphics);
            while (lineIdx < lines.Length && y + lineH <= e.MarginBounds.Bottom)
            {
                e.Graphics.DrawString(lines[lineIdx++].TrimEnd('\r'), font, Brushes.Black, e.MarginBounds.Left, y);
                y += lineH;
            }
            e.HasMorePages = lineIdx < lines.Length;
            font.Dispose();
        };
        using var preview = new PrintPreviewDialog
        {
            Document = pd, Width = 900, Height = 700,
            StartPosition = FormStartPosition.CenterScreen
        };
        preview.ShowDialog();
    }

    // ─── Demo data fallback ──────────────────────────────────────────
    private void LoadDemoData(string key)
    {
        var dt = new System.Data.DataTable();
        dt.Columns.Add("Status", typeof(string));
        dt.Columns.Add("Info",   typeof(string));
        dt.Rows.Add("Demo Mode", $"{key} report — connect to SQL Server for live data");
        _fullTable = dt;
        BindGrid(_fullTable);
        SetStats("📊 Demo Mode", "🔌 No DB connection", "📋 Sample data", "⚙️ Configure DB");
    }
}
