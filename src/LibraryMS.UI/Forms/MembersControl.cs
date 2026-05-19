using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using LibraryMS.Services;
using LibraryMS.Core.Entities;
using LibraryMS.UI.Controls;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Member management page with CRUD operations and search.
/// </summary>
public class MembersControl : UserControl
{
    private readonly MemberService _memberService;
    private DataGridView _membersGrid = null!;
    private Guna2TextBox _searchBox = null!;
    private Guna2Button _addButton = null!;
    private Guna2Button _editButton = null!;
    private Guna2Button _deleteButton = null!;
    private Label _totalLabel = null!;

    public MembersControl()
    {
        _memberService = new MemberService();
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        BackColor = AppTheme.Background;
        Dock = DockStyle.Fill;
        Padding = new Padding(32, 24, 32, 24);

        // ─── Header ─────────────────────────────────────────────
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.Transparent
        };

        var titleLabel = new Label
        {
            Text = "👥  Member Management",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 6)
        };

        _totalLabel = new Label
        {
            Text = "0 members",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(headerPanel.Width - 120, 14)
        };

        headerPanel.Controls.AddRange(new Control[] { titleLabel, _totalLabel });
        headerPanel.Resize += (s, e) => _totalLabel.Location = new Point(headerPanel.Width - _totalLabel.Width - 10, 14);

        // ─── Toolbar ────────────────────────────────────────────
        var toolbarPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 8)
        };

        _searchBox = new Guna2TextBox
        {
            PlaceholderText = "🔍  Search members by name, email, or phone...",
            Size = new Size(380, 40),
            Location = new Point(0, 4)
        };
        ThemeManager.StyleTextBox(_searchBox);
        _searchBox.TextChanged += async (s, e) => await SearchMembersAsync();

        _addButton = new Guna2Button
        {
            Text = "+ Add Member",
            Size = new Size(140, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StylePrimaryButton(_addButton);
        _addButton.Click += AddButton_Click;

        _editButton = new Guna2Button
        {
            Text = "✏️ Edit",
            Size = new Size(90, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StyleSecondaryButton(_editButton);
        _editButton.Click += EditButton_Click;

        _deleteButton = new Guna2Button
        {
            Text = "🗑️ Remove",
            Size = new Size(110, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StyleDangerButton(_deleteButton);
        _deleteButton.Click += DeleteButton_Click;

        toolbarPanel.Controls.AddRange(new Control[] { _searchBox, _deleteButton, _editButton, _addButton });
        toolbarPanel.Resize += (s, e) =>
        {
            var right = toolbarPanel.Width;
            _deleteButton.Location = new Point(right - 120, 4);
            _editButton.Location = new Point(right - 220, 4);
            _addButton.Location = new Point(right - 370, 4);
        };

        // ─── Data Grid ──────────────────────────────────────────
        _membersGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppTheme.Surface
        };
        ThemeManager.StyleDataGridView(_membersGrid);
        _membersGrid.CellDoubleClick += (s, e) => EditButton_Click(s, e);

        var gridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(1)
        };
        gridContainer.Controls.Add(_membersGrid);

        // ─── Build Layout ───────────────────────────────────────
        Controls.Add(gridContainer);
        Controls.Add(toolbarPanel);
        Controls.Add(headerPanel);
        ResumeLayout();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var members = await _memberService.GetAllMembersAsync();
            PopulateGrid(members);
        }
        catch
        {
            LoadDemoData();
        }
    }

    private void PopulateGrid(IEnumerable<Member> members)
    {
        var memberList = members.ToList();
        _totalLabel.Text = $"{memberList.Count} members";

        _membersGrid.DataSource = null;
        _membersGrid.Columns.Clear();

        var dt = new System.Data.DataTable();
        dt.Columns.AddRange(new[]
        {
            new System.Data.DataColumn("ID", typeof(int)),
            new System.Data.DataColumn("Name", typeof(string)),
            new System.Data.DataColumn("Email", typeof(string)),
            new System.Data.DataColumn("Phone", typeof(string)),
            new System.Data.DataColumn("Membership", typeof(string)),
            new System.Data.DataColumn("Join Date", typeof(string)),
            new System.Data.DataColumn("Active Loans", typeof(int)),
            new System.Data.DataColumn("Status", typeof(string))
        });

        foreach (var m in memberList)
        {
            var membershipIcon = m.MembershipType switch
            {
                "Premium" => "⭐ Premium",
                "Student" => "🎓 Student",
                _ => "📋 Standard"
            };
            var status = m.IsActive ? "✅ Active" : "❌ Inactive";
            dt.Rows.Add(m.Id, m.FullName, m.Email ?? "—", m.Phone ?? "—",
                membershipIcon, m.JoinDate.ToString("MMM dd, yyyy"),
                m.ActiveLoans, status);
        }

        _membersGrid.DataSource = dt;

        if (_membersGrid.Columns.Count > 0)
        {
            _membersGrid.Columns["ID"].Width = 50;
            _membersGrid.Columns["Name"].FillWeight = 25;
            _membersGrid.Columns["Email"].FillWeight = 25;
            _membersGrid.Columns["Phone"].FillWeight = 15;
            _membersGrid.Columns["Membership"].Width = 110;
            _membersGrid.Columns["Join Date"].Width = 110;
            _membersGrid.Columns["Active Loans"].Width = 90;
            _membersGrid.Columns["Status"].Width = 90;
        }
    }

    private void LoadDemoData()
    {
        var demo = new List<Member>
        {
            new() { Id = 1, FirstName = "Arun", LastName = "Kumar", Email = "arun.kumar@email.com", Phone = "+94-771-234567", MembershipType = "Premium", JoinDate = new DateTime(2024, 1, 15), IsActive = true, ActiveLoans = 2 },
            new() { Id = 2, FirstName = "Priya", LastName = "Sharma", Email = "priya.s@email.com", Phone = "+94-772-345678", MembershipType = "Standard", JoinDate = new DateTime(2024, 2, 20), IsActive = true, ActiveLoans = 1 },
            new() { Id = 3, FirstName = "Mohamed", LastName = "Ali", Email = "mali@email.com", Phone = "+94-773-456789", MembershipType = "Student", JoinDate = new DateTime(2024, 3, 10), IsActive = true, ActiveLoans = 1 },
            new() { Id = 4, FirstName = "Lakshmi", LastName = "Nair", Email = "lakshmi.n@email.com", Phone = "+94-774-567890", MembershipType = "Premium", JoinDate = new DateTime(2024, 4, 5), IsActive = true, ActiveLoans = 0 },
            new() { Id = 5, FirstName = "David", LastName = "Fernando", Email = "david.f@email.com", Phone = "+94-775-678901", MembershipType = "Standard", JoinDate = new DateTime(2024, 5, 12), IsActive = true, ActiveLoans = 1 },
            new() { Id = 6, FirstName = "Nithya", LastName = "Raj", Email = "nithya.r@email.com", Phone = "+94-776-789012", MembershipType = "Student", JoinDate = new DateTime(2024, 6, 18), IsActive = true, ActiveLoans = 1 },
            new() { Id = 7, FirstName = "Kasun", LastName = "Perera", Email = "kasun.p@email.com", Phone = "+94-777-890123", MembershipType = "Standard", JoinDate = new DateTime(2024, 7, 22), IsActive = true, ActiveLoans = 1 },
            new() { Id = 8, FirstName = "Amara", LastName = "Silva", Email = "amara.s@email.com", Phone = "+94-778-901234", MembershipType = "Premium", JoinDate = new DateTime(2024, 8, 30), IsActive = false, ActiveLoans = 0 }
        };
        PopulateGrid(demo);
    }

    private async Task SearchMembersAsync()
    {
        var term = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(term)) { await LoadDataAsync(); return; }
        try
        {
            var members = await _memberService.SearchMembersAsync(term);
            PopulateGrid(members);
        }
        catch { }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new MemberDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ToastNotification.Show(this.FindForm()!, "Member added successfully!", ToastType.Success);
            await LoadDataAsync();
        }
    }

    private async void EditButton_Click(object? sender, EventArgs e)
    {
        if (_membersGrid.SelectedRows.Count == 0)
        {
            ToastNotification.Show(this.FindForm()!, "Please select a member to edit.", ToastType.Info);
            return;
        }
        var memberId = (int)_membersGrid.SelectedRows[0].Cells["ID"].Value;
        using var dialog = new MemberDialog(memberId);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ToastNotification.Show(this.FindForm()!, "Member updated successfully!", ToastType.Success);
            await LoadDataAsync();
        }
    }

    private async void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (_membersGrid.SelectedRows.Count == 0)
        {
            ToastNotification.Show(this.FindForm()!, "Please select a member to remove.", ToastType.Info);
            return;
        }
        var memberName = _membersGrid.SelectedRows[0].Cells["Name"].Value.ToString();
        var confirmDialog = new Guna2MessageDialog
        {
            Caption = "Confirm Remove",
            Text = $"Remove '{memberName}' from the system?\nThis action cannot be undone.",
            Buttons = MessageDialogButtons.YesNo,
            Icon = MessageDialogIcon.Warning,
            Style = MessageDialogStyle.Dark,
            Parent = this.FindForm()
        };

        if (confirmDialog.Show() == DialogResult.Yes)
        {
            var memberId = (int)_membersGrid.SelectedRows[0].Cells["ID"].Value;
            var (success, message) = await _memberService.DeleteMemberAsync(memberId);
            
            if (success)
            {
                ToastNotification.Show(this.FindForm()!, message, ToastType.Success);
                await LoadDataAsync();
            }
            else
            {
                ToastNotification.Show(this.FindForm()!, message, ToastType.Error);
            }
        }
    }
}

/// <summary>
/// Member add/edit dialog with styled inputs.
/// </summary>
public class MemberDialog : Form
{
    private readonly MemberService _memberService;
    private readonly int? _memberId;
    private Guna2TextBox _txtFirstName = null!;
    private Guna2TextBox _txtLastName = null!;
    private Guna2TextBox _txtEmail = null!;
    private Guna2TextBox _txtPhone = null!;
    private Guna2TextBox _txtAddress = null!;
    private Guna2ComboBox _cmbMembership = null!;
    private Guna2Button _btnSave = null!;

    public MemberDialog(int? memberId = null)
    {
        _memberService = new MemberService();
        _memberId = memberId;
        InitializeComponent();
        if (_memberId.HasValue) _ = LoadMemberAsync();
    }

    private void InitializeComponent()
    {
        Text = _memberId.HasValue ? "Edit Member" : "Register New Member";
        Size = new Size(480, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextPrimary;

        var mainPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(24),
            WrapContents = false,
            AutoScroll = true,
            BackColor = AppTheme.Surface
        };

        _txtFirstName = AddField(mainPanel, "First Name *", "Enter first name");
        _txtLastName = AddField(mainPanel, "Last Name *", "Enter last name");
        _txtEmail = AddField(mainPanel, "Email", "email@example.com");
        _txtPhone = AddField(mainPanel, "Phone", "+94-7XX-XXXXXX");
        _txtAddress = AddField(mainPanel, "Address", "Full address");

        var lblMembership = new Label
        {
            Text = "Membership Type",
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 4)
        };
        mainPanel.Controls.Add(lblMembership);

        _cmbMembership = new Guna2ComboBox { Size = new Size(400, 40) };
        ThemeManager.StyleComboBox(_cmbMembership);
        _cmbMembership.Items.AddRange(new object[] { "Standard", "Premium", "Student" });
        _cmbMembership.SelectedIndex = 0;
        mainPanel.Controls.Add(_cmbMembership);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(20, 10, 20, 10),
            BackColor = AppTheme.SurfaceLight
        };

        _btnSave = new Guna2Button { Text = "💾  Save", Size = new Size(120, 40) };
        ThemeManager.StylePrimaryButton(_btnSave);
        _btnSave.Click += BtnSave_Click;

        var btnCancel = new Guna2Button { Text = "Cancel", Size = new Size(100, 40) };
        ThemeManager.StyleSecondaryButton(btnCancel);
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        buttonPanel.Controls.AddRange(new Control[] { _btnSave, btnCancel });

        Controls.Add(mainPanel);
        Controls.Add(buttonPanel);
    }

    private Guna2TextBox AddField(FlowLayoutPanel parent, string label, string placeholder)
    {
        var lbl = new Label
        {
            Text = label,
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 4)
        };
        parent.Controls.Add(lbl);

        var txt = new Guna2TextBox
        {
            PlaceholderText = placeholder,
            Size = new Size(400, 40)
        };
        ThemeManager.StyleTextBox(txt);
        parent.Controls.Add(txt);
        return txt;
    }

    private async Task LoadMemberAsync()
    {
        try
        {
            var member = await _memberService.GetMemberByIdAsync(_memberId!.Value);
            if (member != null)
            {
                _txtFirstName.Text = member.FirstName;
                _txtLastName.Text = member.LastName;
                _txtEmail.Text = member.Email ?? "";
                _txtPhone.Text = member.Phone ?? "";
                _txtAddress.Text = member.Address ?? "";
                _cmbMembership.SelectedItem = member.MembershipType;
            }
        }
        catch { }
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        var member = new Member
        {
            FirstName = _txtFirstName.Text.Trim(),
            LastName = _txtLastName.Text.Trim(),
            Email = _txtEmail.Text.Trim(),
            Phone = _txtPhone.Text.Trim(),
            Address = _txtAddress.Text.Trim(),
            MembershipType = _cmbMembership.SelectedItem?.ToString() ?? "Standard"
        };

        if (_memberId.HasValue)
        {
            member.Id = _memberId.Value;
            var (success, message) = await _memberService.UpdateMemberAsync(member);
            if (success) { DialogResult = DialogResult.OK; Close(); }
            else ToastNotification.Show(this, message, ToastType.Warning);
        }
        else
        {
            var (success, message, id) = await _memberService.AddMemberAsync(member);
            if (success) { DialogResult = DialogResult.OK; Close(); }
            else ToastNotification.Show(this, message, ToastType.Warning);
        }
    }
}
