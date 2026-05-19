using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using LibraryMS.Services;
using LibraryMS.Core.Entities;
using LibraryMS.UI.Controls;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Enterprise-grade Loan / Issue Management page.
/// Supports search, filter tabs, full CRUD with toast notifications.
/// </summary>
public class LoansControl : UserControl
{
    private readonly LoanService   _loanService;
    private readonly BookService   _bookService;
    private readonly MemberService _memberService;

    private DataGridView  _loansGrid    = null!;
    private Guna2TextBox  _searchBox    = null!;
    private Guna2Button   _issueButton  = null!;
    private Guna2Button   _returnButton = null!;
    private Guna2Button   _refreshButton = null!;
    private Guna2Button   _showAllBtn   = null!;
    private Guna2Button   _showActiveBtn  = null!;
    private Guna2Button   _showOverdueBtn = null!;
    private Label _totalLabel   = null!;
    private Label _activeLabel  = null!;
    private Label _overdueLabel = null!;
    private Label _emptyLabel   = null!;

    private string _currentFilter = "All";
    private List<Loan> _currentLoans = new();

    // ─── Constructor ────────────────────────────────────────────
    public LoansControl()
    {
        _loanService   = new LoanService();
        _bookService   = new BookService();
        _memberService = new MemberService();
        InitializeComponent();
        _ = LoadDataAsync();
    }

    // ─── Layout ─────────────────────────────────────────────────
    private void InitializeComponent()
    {
        SuspendLayout();
        BackColor = AppTheme.Background;
        Dock      = DockStyle.Fill;
        Padding   = new Padding(32, 24, 32, 24);

        // ── Header ──────────────────────────────────────────────
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 52, BackColor = Color.Transparent
        };
        var titleLabel = new Label
        {
            Text      = "🔄  Loan Management",
            Font      = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            AutoSize  = true,
            Location  = new Point(0, 8)
        };
        _totalLabel = new Label
        {
            Font      = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right
        };
        headerPanel.Controls.AddRange(new Control[] { titleLabel, _totalLabel });
        headerPanel.Resize += (s, e) =>
            _totalLabel.Location = new Point(headerPanel.Width - _totalLabel.Width - 10, 16);

        // ── Stats strip ─────────────────────────────────────────
        var statsStrip = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            Height        = 44,
            BackColor     = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            Padding       = new Padding(0, 4, 0, 4)
        };
        _activeLabel  = CreateStatChip("📗 Active", "—", AppTheme.Success);
        _overdueLabel = CreateStatChip("⚠️ Overdue", "—", AppTheme.Warning);
        statsStrip.Controls.AddRange(new Control[] { _activeLabel, _overdueLabel });

        // ── Filter / toolbar ────────────────────────────────────
        var filterPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = Color.Transparent
        };

        _showAllBtn = CreateFilterButton("📋 All", true);
        _showAllBtn.Location = new Point(0, 6);
        _showAllBtn.Click   += async (s, e) => { _currentFilter = "All"; UpdateFilterButtons(); await LoadDataAsync(); };

        _showActiveBtn = CreateFilterButton("📗 Active", false);
        _showActiveBtn.Location = new Point(118, 6);
        _showActiveBtn.Click   += async (s, e) => { _currentFilter = "Active"; UpdateFilterButtons(); await LoadActiveAsync(); };

        _showOverdueBtn = CreateFilterButton("⚠️ Overdue", false);
        _showOverdueBtn.Location = new Point(236, 6);
        _showOverdueBtn.Click   += async (s, e) => { _currentFilter = "Overdue"; UpdateFilterButtons(); await LoadOverdueAsync(); };

        filterPanel.Controls.AddRange(new Control[] { _showAllBtn, _showActiveBtn, _showOverdueBtn });
        filterPanel.Resize += (s, e) => RepositionFilterRightButtons(filterPanel);

        // ── Search box ──────────────────────────────────────────
        var toolbarPanel = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = Color.Transparent
        };
        _searchBox = new Guna2TextBox
        {
            PlaceholderText = "🔍  Search by book title, member name, or ISBN...",
            Size            = new Size(380, 40),
            Location        = new Point(0, 6),
            Anchor          = AnchorStyles.Top | AnchorStyles.Left
        };
        ThemeManager.StyleTextBox(_searchBox);
        _searchBox.TextChanged += async (s, e) => await SearchLoansAsync();

        _issueButton = new Guna2Button
        {
            Text   = "📖 Issue Book",
            Size   = new Size(130, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StylePrimaryButton(_issueButton);
        _issueButton.Click += IssueButton_Click;

        _returnButton = new Guna2Button
        {
            Text   = "↩️ Return",
            Size   = new Size(110, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StyleSuccessButton(_returnButton);
        _returnButton.Click += ReturnButton_Click;

        _refreshButton = new Guna2Button
        {
            Text   = "↻",
            Size   = new Size(40, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StyleSecondaryButton(_refreshButton);
        _refreshButton.Click += async (s, e) =>
        {
            _searchBox.Text = string.Empty;
            await LoadDataAsync();
        };

        toolbarPanel.Controls.AddRange(new Control[]
            { _searchBox, _refreshButton, _returnButton, _issueButton });
        toolbarPanel.Resize += (s, e) => RepositionToolbarButtons(toolbarPanel);

        // ── Grid ────────────────────────────────────────────────
        _loansGrid = new DataGridView
        {
            Dock            = DockStyle.Fill,
            BackgroundColor = AppTheme.Surface
        };
        ThemeManager.StyleDataGridView(_loansGrid);
        _loansGrid.CellDoubleClick += (s, e) => ReturnButton_Click(s, e);

        _emptyLabel = new Label
        {
            Text      = "No loans found.",
            Font      = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextSecondary,
            AutoSize  = true,
            Visible   = false
        };

        var gridContainer = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding   = new Padding(1)
        };
        gridContainer.Controls.Add(_emptyLabel);
        gridContainer.Controls.Add(_loansGrid);
        gridContainer.Resize += (s, e) =>
        {
            _emptyLabel.Location = new Point(
                (gridContainer.Width  - _emptyLabel.Width)  / 2,
                (gridContainer.Height - _emptyLabel.Height) / 2);
        };

        // ── Build layout (reverse order for DockStyle.Top) ──────
        Controls.Add(gridContainer);
        Controls.Add(toolbarPanel);
        Controls.Add(filterPanel);
        Controls.Add(statsStrip);
        Controls.Add(headerPanel);
        ResumeLayout();
    }

    // ─── Helpers ────────────────────────────────────────────────
    private void RepositionToolbarButtons(Panel toolbar)
    {
        var right = toolbar.Width;
        _refreshButton.Location = new Point(right - 48, 6);
        _returnButton.Location  = new Point(right - 168, 6);
        _issueButton.Location   = new Point(right - 308, 6);
    }

    private void RepositionFilterRightButtons(Panel panel)
    {
        // right-side buttons are in toolbar; nothing extra needed here
    }

    private Guna2Button CreateFilterButton(string text, bool active)
    {
        var btn = new Guna2Button
        {
            Text          = text,
            Size          = new Size(110, 40),
            Font          = AppTheme.FontButton,
            BorderRadius  = 12,
            Animated      = true,
            FillColor     = active ? Color.FromArgb(30, AppTheme.Primary) : Color.Transparent,
            ForeColor     = active ? AppTheme.Primary : AppTheme.TextSecondary,
            BorderColor   = active ? AppTheme.Primary : AppTheme.Border,
            BorderThickness = 1
        };
        btn.HoverState.FillColor = Color.FromArgb(20, AppTheme.Primary);
        return btn;
    }

    private Label CreateStatChip(string label, string value, Color color)
    {
        return new Label
        {
            Text      = $"{label}:  {value}",
            Font      = AppTheme.FontBodySmall,
            ForeColor = color,
            AutoSize  = true,
            Margin    = new Padding(0, 0, 20, 0),
            Padding   = new Padding(0, 6, 0, 0)
        };
    }

    private void UpdateFilterButtons()
    {
        foreach (var (key, btn) in new[]
        {
            ("All",     _showAllBtn),
            ("Active",  _showActiveBtn),
            ("Overdue", _showOverdueBtn)
        })
        {
            btn.FillColor   = key == _currentFilter ? Color.FromArgb(30, AppTheme.Primary) : Color.Transparent;
            btn.ForeColor   = key == _currentFilter ? AppTheme.Primary : AppTheme.TextSecondary;
            btn.BorderColor = key == _currentFilter ? AppTheme.Primary : AppTheme.Border;
        }
    }

    // ─── Data loading ────────────────────────────────────────────
    private async Task LoadDataAsync()
    {
        try   { _currentLoans = (await _loanService.GetAllLoansAsync()).ToList();     PopulateGrid(_currentLoans); }
        catch { LoadDemoData(); }
        await RefreshStatChipsAsync();
    }

    private async Task LoadActiveAsync()
    {
        try   { _currentLoans = (await _loanService.GetActiveLoansAsync()).ToList();  PopulateGrid(_currentLoans); }
        catch { }
    }

    private async Task LoadOverdueAsync()
    {
        try   { _currentLoans = (await _loanService.GetOverdueLoansAsync()).ToList(); PopulateGrid(_currentLoans); }
        catch { }
    }

    private async Task SearchLoansAsync()
    {
        var term = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(term)) { await LoadDataAsync(); return; }
        try
        {
            var results = await _loanService.SearchLoansAsync(term);
            PopulateGrid(results.ToList());
        }
        catch { }
    }

    private async Task RefreshStatChipsAsync()
    {
        try
        {
            var active  = await _loanService.GetActiveCountAsync();
            var overdue = await _loanService.GetOverdueCountAsync();
            _activeLabel.Text  = $"📗 Active:  {active}";
            _overdueLabel.Text = $"⚠️ Overdue:  {overdue}";
        }
        catch { }
    }

    // ─── Grid population ────────────────────────────────────────
    private void PopulateGrid(List<Loan> loans)
    {
        _totalLabel.Text = $"{loans.Count} loan{(loans.Count != 1 ? "s" : "")}";
        _emptyLabel.Visible = loans.Count == 0;
        _loansGrid.Visible  = loans.Count > 0;

        _loansGrid.DataSource = null;
        _loansGrid.Columns.Clear();

        var dt = new System.Data.DataTable();
        dt.Columns.AddRange(new[]
        {
            new System.Data.DataColumn("ID",       typeof(int)),
            new System.Data.DataColumn("Book",     typeof(string)),
            new System.Data.DataColumn("Member",   typeof(string)),
            new System.Data.DataColumn("Issued",   typeof(string)),
            new System.Data.DataColumn("Due Date", typeof(string)),
            new System.Data.DataColumn("Returned", typeof(string)),
            new System.Data.DataColumn("Fine",     typeof(string)),
            new System.Data.DataColumn("Status",   typeof(string))
        });

        foreach (var l in loans)
        {
            var status = l.Status == "Returned"
                ? "✅ Returned"
                : l.IsOverdue
                    ? $"⚠️ Overdue ({Math.Abs(l.DaysRemaining)}d)"
                    : $"📗 Active ({l.DaysRemaining}d left)";

            dt.Rows.Add(
                l.Id,
                l.BookTitle   ?? "—",
                l.MemberName  ?? "—",
                l.BorrowDate.ToString("MMM dd, yyyy"),
                l.DueDate.ToString("MMM dd, yyyy"),
                l.ReturnDate?.ToString("MMM dd, yyyy") ?? "—",
                l.FineAmount > 0 ? $"${l.FineAmount:F2}" : "—",
                status);
        }

        _loansGrid.DataSource = dt;

        if (_loansGrid.Columns.Count > 0)
        {
            _loansGrid.Columns["ID"].Width      = 50;
            _loansGrid.Columns["Book"].FillWeight   = 30;
            _loansGrid.Columns["Member"].FillWeight = 22;
            _loansGrid.Columns["Issued"].Width  = 110;
            _loansGrid.Columns["Due Date"].Width = 110;
            _loansGrid.Columns["Returned"].Width = 110;
            _loansGrid.Columns["Fine"].Width     = 70;
            _loansGrid.Columns["Status"].Width   = 155;
        }
    }

    private void LoadDemoData()
    {
        PopulateGrid(new List<Loan>
        {
            new() { Id=1, BookTitle="The Great Gatsby",         MemberName="Arun Kumar",     BorrowDate=DateTime.Now.AddDays(-10), DueDate=DateTime.Now.AddDays(4),   Status="Active" },
            new() { Id=2, BookTitle="Clean Code",               MemberName="Priya Sharma",   BorrowDate=DateTime.Now.AddDays(-7),  DueDate=DateTime.Now.AddDays(7),   Status="Active" },
            new() { Id=3, BookTitle="A Brief History of Time",  MemberName="Mohamed Ali",    BorrowDate=DateTime.Now.AddDays(-20), DueDate=DateTime.Now.AddDays(-6),  Status="Active" },
            new() { Id=4, BookTitle="1984",                     MemberName="Lakshmi Nair",   BorrowDate=DateTime.Now.AddDays(-30), DueDate=DateTime.Now.AddDays(-16), Status="Returned", ReturnDate=DateTime.Now.AddDays(-14) },
            new() { Id=5, BookTitle="Sapiens",                  MemberName="David Fernando", BorrowDate=DateTime.Now.AddDays(-3),  DueDate=DateTime.Now.AddDays(11),  Status="Active" }
        });
    }

    // ─── Button handlers ─────────────────────────────────────────
    private async void IssueButton_Click(object? sender, EventArgs e)
    {
        using var dlg = new IssueLoanDialog(_bookService, _memberService);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var (ok, msg) = await _loanService.IssueBookAsync(dlg.SelectedBookId, dlg.SelectedMemberId, dlg.LoanDays);

        if (ok)
        {
            ToastNotification.Show(this.FindForm()!, msg, ToastType.Success);
            await LoadDataAsync();
        }
        else
        {
            ToastNotification.Show(this.FindForm()!, msg, ToastType.Error);
        }
    }

    private async void ReturnButton_Click(object? sender, EventArgs e)
    {
        if (_loansGrid.SelectedRows.Count == 0)
        {
            ToastNotification.Show(this.FindForm()!, "Please select a loan record to return.", ToastType.Info);
            return;
        }

        var statusCell = _loansGrid.SelectedRows[0].Cells["Status"].Value?.ToString();
        if (statusCell?.Contains("Returned") == true)
        {
            ToastNotification.Show(this.FindForm()!, "This book has already been returned.", ToastType.Warning);
            return;
        }

        var id         = (int)_loansGrid.SelectedRows[0].Cells["ID"].Value;
        var bookName   = _loansGrid.SelectedRows[0].Cells["Book"].Value?.ToString() ?? "this book";
        var memberName = _loansGrid.SelectedRows[0].Cells["Member"].Value?.ToString() ?? "member";

        var confirm = new Guna2MessageDialog
        {
            Caption = "Confirm Return",
            Text    = $"Return '{bookName}' for {memberName}?\nAny applicable fine will be calculated automatically.",
            Buttons = MessageDialogButtons.YesNo,
            Icon    = MessageDialogIcon.Question,
            Style   = MessageDialogStyle.Dark,
            Parent  = this.FindForm()
        };

        if (confirm.Show() != DialogResult.Yes) return;

        var (ok, msg) = await _loanService.ReturnBookAsync(id);

        if (ok)
        {
            ToastNotification.Show(this.FindForm()!, msg, ToastType.Success);
            await LoadDataAsync();
        }
        else
        {
            ToastNotification.Show(this.FindForm()!, msg, ToastType.Error);
        }
    }
}

// ─────────────────────────────────────────────────────────────────
/// <summary>
/// Premium Issue Book dialog with real-time stock preview,
/// auto issue/due date display, and enterprise validation.
/// </summary>
public class IssueLoanDialog : Form
{
    private Guna2ComboBox _cmbBook   = null!;
    private Guna2ComboBox _cmbMember = null!;
    private Guna2TextBox  _txtDays   = null!;
    private Label _lblIssuedDate = null!;
    private Label _lblDueDate    = null!;
    private Label _lblStock      = null!;
    private Panel _previewPanel  = null!;

    private readonly List<Book>   _books   = new();
    private readonly List<Member> _members = new();

    public int SelectedBookId   { get; private set; }
    public int SelectedMemberId { get; private set; }
    public int LoanDays         { get; private set; } = 14;

    public IssueLoanDialog(BookService bs, MemberService ms)
    {
        Text                = "📖  Issue Book to Member";
        Size                = new Size(520, 520);
        StartPosition       = FormStartPosition.CenterParent;
        FormBorderStyle     = FormBorderStyle.FixedDialog;
        MaximizeBox         = false;
        MinimizeBox         = false;
        BackColor           = AppTheme.Surface;
        ForeColor           = AppTheme.TextPrimary;

        // ── Main form panel ─────────────────────────────────────
        var mp = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding       = new Padding(28, 20, 28, 8),
            WrapContents  = false,
            AutoScroll    = false,
            BackColor     = AppTheme.Surface
        };

        // Book selector
        mp.Controls.Add(MakeLabel("Select Book *"));
        _cmbBook = new Guna2ComboBox { Size = new Size(436, 42) };
        ThemeManager.StyleComboBox(_cmbBook);
        _cmbBook.SelectedIndexChanged += OnBookChanged;
        mp.Controls.Add(_cmbBook);

        // Member selector
        mp.Controls.Add(MakeLabel("Select Member *"));
        _cmbMember = new Guna2ComboBox { Size = new Size(436, 42) };
        ThemeManager.StyleComboBox(_cmbMember);
        mp.Controls.Add(_cmbMember);

        // Loan duration
        mp.Controls.Add(MakeLabel("Loan Duration (days) *"));
        _txtDays = new Guna2TextBox { Text = "14", Size = new Size(436, 42) };
        ThemeManager.StyleTextBox(_txtDays);
        _txtDays.TextChanged += OnDaysChanged;
        mp.Controls.Add(_txtDays);

        // ── Preview card ────────────────────────────────────────
        _previewPanel = new Panel
        {
            Size      = new Size(436, 80),
            BackColor = Color.FromArgb(20, AppTheme.Primary),
            Margin    = new Padding(0, 12, 0, 0)
        };
        _previewPanel.Paint += PreviewPanel_Paint;

        _lblIssuedDate = MakePreviewLabel(new Point(16, 12));
        _lblDueDate    = MakePreviewLabel(new Point(16, 34));
        _lblStock      = MakePreviewLabel(new Point(16, 56));
        _previewPanel.Controls.AddRange(new Control[] { _lblIssuedDate, _lblDueDate, _lblStock });

        mp.Controls.Add(_previewPanel);

        // ── Button row ──────────────────────────────────────────
        var bp = new FlowLayoutPanel
        {
            Dock          = DockStyle.Bottom,
            Height        = 65,
            FlowDirection = FlowDirection.RightToLeft,
            Padding       = new Padding(20, 12, 20, 12),
            BackColor     = AppTheme.SurfaceLight
        };
        var btnIssue = new Guna2Button { Text = "📖  Issue Book", Size = new Size(140, 40) };
        ThemeManager.StylePrimaryButton(btnIssue);
        btnIssue.Click += BtnIssue_Click;

        var btnCancel = new Guna2Button { Text = "Cancel", Size = new Size(100, 40) };
        ThemeManager.StyleSecondaryButton(btnCancel);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        bp.Controls.AddRange(new Control[] { btnIssue, btnCancel });

        Controls.Add(mp);
        Controls.Add(bp);

        UpdatePreview();

        // Load data in background
        _ = Task.Run(async () =>
        {
            try
            {
                var books   = (await bs.GetAllBooksAsync()).Where(b => b.AvailableQuantity > 0).ToList();
                var members = (await ms.GetActiveMembersAsync()).ToList();

                Invoke(() =>
                {
                    _books.AddRange(books);
                    foreach (var b in books)
                        _cmbBook.Items.Add($"{b.Title}  —  {b.AvailableQuantity} copy(ies) available");

                    _members.AddRange(members);
                    foreach (var m in members)
                        _cmbMember.Items.Add($"{m.FullName}  [{m.MembershipType}]");

                    if (_cmbBook.Items.Count   > 0) _cmbBook.SelectedIndex   = 0;
                    if (_cmbMember.Items.Count > 0) _cmbMember.SelectedIndex = 0;

                    UpdatePreview();
                });
            }
            catch { }
        });
    }

    // ─── Preview helpers ─────────────────────────────────────────
    private static Label MakeLabel(string text) => new Label
    {
        Text      = text,
        Font      = new Font("Segoe UI Semibold", 10f),
        ForeColor = AppTheme.TextSecondary,
        AutoSize  = true,
        Margin    = new Padding(0, 10, 0, 4)
    };

    private static Label MakePreviewLabel(Point location) => new Label
    {
        AutoSize  = true,
        Font      = AppTheme.FontBodySmall,
        ForeColor = AppTheme.TextSecondary,
        Location  = location,
        BackColor = Color.Transparent
    };

    private void PreviewPanel_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(60, AppTheme.Primary), 1);
        var rect = new Rectangle(0, 0, _previewPanel.Width - 1, _previewPanel.Height - 1);
        // Draw rounded border
        using var path = CreateRoundedPath(rect, 10);
        e.Graphics.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedPath(Rectangle rect, int r)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = r * 2;
        path.AddArc(rect.X,             rect.Y,              d, d, 180, 90);
        path.AddArc(rect.Right - d,     rect.Y,              d, d, 270, 90);
        path.AddArc(rect.Right - d,     rect.Bottom - d,     d, d,   0, 90);
        path.AddArc(rect.X,             rect.Bottom - d,     d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    private void UpdatePreview()
    {
        if (_lblIssuedDate == null) return;

        var issueDate = DateTime.Now;
        var days      = int.TryParse(_txtDays?.Text, out var d) ? d : 14;
        var dueDate   = issueDate.AddDays(Math.Max(1, days));

        _lblIssuedDate.Text = $"📅  Issue Date : {issueDate:dddd, MMMM dd, yyyy}";
        _lblDueDate.Text    = $"📆  Due Date   : {dueDate:dddd, MMMM dd, yyyy}  (+{Math.Max(1, days)} days)";

        if (_books.Count > 0 && _cmbBook?.SelectedIndex >= 0 && _cmbBook.SelectedIndex < _books.Count)
        {
            var book = _books[_cmbBook.SelectedIndex];
            _lblStock.Text      = $"📦  Stock      : {book.AvailableQuantity} of {book.Quantity} cop{(book.Quantity == 1 ? "y" : "ies")} available";
            _lblStock.ForeColor = book.AvailableQuantity > 2
                ? AppTheme.Success
                : book.AvailableQuantity > 0 ? AppTheme.Warning : AppTheme.Danger;
        }
        else
        {
            _lblStock.Text      = "📦  Stock      : —";
            _lblStock.ForeColor = AppTheme.TextSecondary;
        }
    }

    private void OnBookChanged(object? sender, EventArgs e) => UpdatePreview();
    private void OnDaysChanged(object? sender, EventArgs e) => UpdatePreview();

    // ─── Issue validation ────────────────────────────────────────
    private void BtnIssue_Click(object? sender, EventArgs e)
    {
        if (_cmbBook.SelectedIndex < 0)
        {
            ToastNotification.Show(this, "Please select a book.", ToastType.Warning);
            return;
        }
        if (_cmbMember.SelectedIndex < 0)
        {
            ToastNotification.Show(this, "Please select a member.", ToastType.Warning);
            return;
        }
        if (!int.TryParse(_txtDays.Text.Trim(), out var days) || days < 1 || days > 180)
        {
            ToastNotification.Show(this, "Loan duration must be between 1 and 180 days.", ToastType.Warning);
            return;
        }

        SelectedBookId   = _books[_cmbBook.SelectedIndex].Id;
        SelectedMemberId = _members[_cmbMember.SelectedIndex].Id;
        LoanDays         = days;
        DialogResult     = DialogResult.OK;
        Close();
    }
}
