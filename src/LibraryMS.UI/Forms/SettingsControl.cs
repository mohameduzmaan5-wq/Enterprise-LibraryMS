using System.Drawing.Drawing2D;
using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using LibraryMS.Data.Database;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Settings page with database connection config and theme options.
/// </summary>
public class SettingsControl : UserControl
{
    private Guna2TextBox _txtServer = null!;
    private Guna2TextBox _txtDatabase = null!;
    private Guna2Button _btnTest = null!;
    private Guna2Button _btnSave = null!;
    private Label _connectionStatus = null!;

    public SettingsControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        BackColor = AppTheme.Background;
        Dock = DockStyle.Fill;
        Padding = new Padding(32, 24, 32, 24);

        // Header
        var headerPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
        headerPanel.Controls.Add(new Label { Text = "⚙️  Settings", Font = AppTheme.FontSubtitle, ForeColor = AppTheme.TextPrimary, AutoSize = true, Location = new Point(0, 6) });

        // Database Settings Card
        var dbCard = new Panel { Dock = DockStyle.Top, Height = 300, BackColor = AppTheme.Surface, Padding = new Padding(28) };
        dbCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(AppTheme.Border, 1);
            var rect = new Rectangle(0, 0, dbCard.Width - 1, dbCard.Height - 1);
            using var path = CreateRoundedPath(rect, 16);
            e.Graphics.DrawPath(pen, path);
        };

        var dbTitle = new Label { Text = "🗄️  Database Connection", Font = AppTheme.FontHeading, ForeColor = AppTheme.TextPrimary, Dock = DockStyle.Top, Height = 35 };
        var dbDesc = new Label { Text = "Configure your SQL Server connection. Default uses LocalDB.", Font = AppTheme.FontBodySmall, ForeColor = AppTheme.TextSecondary, Dock = DockStyle.Top, Height = 25 };

        var fieldsPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 150, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0) };

        fieldsPanel.Controls.Add(new Label { Text = "Server", Font = new Font("Segoe UI Semibold", 10f), ForeColor = AppTheme.TextSecondary, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        _txtServer = new Guna2TextBox { Text = @"(localdb)\MSSQLLocalDB", Size = new Size(400, 40) };
        ThemeManager.StyleTextBox(_txtServer);
        fieldsPanel.Controls.Add(_txtServer);

        fieldsPanel.Controls.Add(new Label { Text = "Database", Font = new Font("Segoe UI Semibold", 10f), ForeColor = AppTheme.TextSecondary, AutoSize = true, Margin = new Padding(0, 8, 0, 4) });
        _txtDatabase = new Guna2TextBox { Text = "LibraryMS", Size = new Size(400, 40) };
        ThemeManager.StyleTextBox(_txtDatabase);
        fieldsPanel.Controls.Add(_txtDatabase);

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };

        _btnTest = new Guna2Button { Text = "🔌 Test Connection", Size = new Size(160, 40), Margin = new Padding(0, 0, 8, 0) };
        ThemeManager.StyleSecondaryButton(_btnTest);
        _btnTest.Click += BtnTest_Click;

        _btnSave = new Guna2Button { Text = "💾 Save & Apply", Size = new Size(140, 40) };
        ThemeManager.StylePrimaryButton(_btnSave);
        _btnSave.Click += BtnSave_Click;

        _connectionStatus = new Label { Text = "", Font = AppTheme.FontBodySmall, AutoSize = true, Margin = new Padding(12, 10, 0, 0) };

        buttonPanel.Controls.AddRange(new Control[] { _btnTest, _btnSave, _connectionStatus });

        dbCard.Controls.Add(buttonPanel);
        dbCard.Controls.Add(fieldsPanel);
        dbCard.Controls.Add(dbDesc);
        dbCard.Controls.Add(dbTitle);

        // About Card
        var aboutCard = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = AppTheme.Surface, Padding = new Padding(28), Margin = new Padding(0, 16, 0, 0) };
        aboutCard.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(AppTheme.Border, 1);
            using var path = CreateRoundedPath(new Rectangle(0, 0, aboutCard.Width - 1, aboutCard.Height - 1), 16);
            e.Graphics.DrawPath(pen, path);
        };

        var aboutTitle = new Label { Text = "ℹ️  About LibraryMS", Font = AppTheme.FontHeading, ForeColor = AppTheme.TextPrimary, Dock = DockStyle.Top, Height = 30 };
        var aboutText = new Label
        {
            Text = "LibraryMS Enterprise Edition v1.0.0\n.NET 8 · Windows Forms · SQL Server · Guna UI2\nBuilt with clean architecture and premium design.",
            Font = AppTheme.FontBody,
            ForeColor = AppTheme.TextSecondary,
            Dock = DockStyle.Fill
        };

        aboutCard.Controls.Add(aboutText);
        aboutCard.Controls.Add(aboutTitle);

        // Spacer between cards
        var spacer = new Panel { Dock = DockStyle.Top, Height = 16, BackColor = Color.Transparent };

        Controls.Add(aboutCard);
        Controls.Add(spacer);
        Controls.Add(dbCard);
        Controls.Add(headerPanel);
        ResumeLayout();
    }

    private async void BtnTest_Click(object? sender, EventArgs e)
    {
        _connectionStatus.Text = "Testing...";
        _connectionStatus.ForeColor = AppTheme.Warning;
        ConnectionString.Configure(_txtServer.Text.Trim(), _txtDatabase.Text.Trim());
        var ok = await DatabaseManager.TestConnectionAsync();
        _connectionStatus.Text = ok ? "✅ Connected successfully!" : "❌ Connection failed";
        _connectionStatus.ForeColor = ok ? AppTheme.Success : AppTheme.Danger;
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        ConnectionString.Configure(_txtServer.Text.Trim(), _txtDatabase.Text.Trim());
        try
        {
            await DatabaseManager.InitializeDatabaseAsync();
            _connectionStatus.Text = "✅ Saved & database initialized!";
            _connectionStatus.ForeColor = AppTheme.Success;
        }
        catch (Exception ex)
        {
            _connectionStatus.Text = $"❌ Error: {ex.Message}";
            _connectionStatus.ForeColor = AppTheme.Danger;
        }
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
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
}
