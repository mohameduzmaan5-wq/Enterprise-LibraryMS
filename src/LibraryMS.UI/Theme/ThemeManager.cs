using Guna.UI2.WinForms;

namespace LibraryMS.UI.Theme;

/// <summary>
/// Applies theme styles to Guna UI2 controls for consistent premium appearance.
/// </summary>
public static class ThemeManager
{
    /// <summary>
    /// Styles a Guna2Button as a primary action button.
    /// </summary>
    public static void StylePrimaryButton(Guna2Button btn)
    {
        btn.FillColor = AppTheme.Primary;
        btn.ForeColor = AppTheme.TextOnPrimary;
        btn.Font = AppTheme.FontButton;
        btn.BorderRadius = AppTheme.ButtonBorderRadius;
        btn.Animated = true;
        btn.HoverState.FillColor = AppTheme.PrimaryLight;
        btn.HoverState.ForeColor = AppTheme.TextOnPrimary;
        btn.PressedColor = AppTheme.PrimaryDark;
        btn.ShadowDecoration.Enabled = true;
        btn.ShadowDecoration.Color = AppTheme.Primary;
        btn.ShadowDecoration.Depth = 8;
        btn.ShadowDecoration.BorderRadius = AppTheme.ButtonBorderRadius;
    }

    /// <summary>
    /// Styles a Guna2Button as a secondary/outline button.
    /// </summary>
    public static void StyleSecondaryButton(Guna2Button btn)
    {
        btn.FillColor = Color.Transparent;
        btn.ForeColor = AppTheme.Primary;
        btn.Font = AppTheme.FontButton;
        btn.BorderRadius = AppTheme.ButtonBorderRadius;
        btn.BorderColor = AppTheme.Primary;
        btn.BorderThickness = 2;
        btn.Animated = true;
        btn.HoverState.FillColor = Color.FromArgb(20, AppTheme.Primary);
        btn.HoverState.ForeColor = AppTheme.PrimaryLight;
        btn.HoverState.BorderColor = AppTheme.PrimaryLight;
    }

    /// <summary>
    /// Styles a Guna2Button as a danger/delete button.
    /// </summary>
    public static void StyleDangerButton(Guna2Button btn)
    {
        btn.FillColor = AppTheme.Danger;
        btn.ForeColor = AppTheme.TextOnPrimary;
        btn.Font = AppTheme.FontButton;
        btn.BorderRadius = AppTheme.ButtonBorderRadius;
        btn.Animated = true;
        btn.HoverState.FillColor = AppTheme.DangerLight;
        btn.ShadowDecoration.Enabled = true;
        btn.ShadowDecoration.Color = AppTheme.Danger;
        btn.ShadowDecoration.Depth = 6;
        btn.ShadowDecoration.BorderRadius = AppTheme.ButtonBorderRadius;
    }

    /// <summary>
    /// Styles a Guna2Button as a success button.
    /// </summary>
    public static void StyleSuccessButton(Guna2Button btn)
    {
        btn.FillColor = AppTheme.Success;
        btn.ForeColor = Color.FromArgb(20, 20, 20);
        btn.Font = AppTheme.FontButton;
        btn.BorderRadius = AppTheme.ButtonBorderRadius;
        btn.Animated = true;
        btn.HoverState.FillColor = AppTheme.SuccessLight;
        btn.ShadowDecoration.Enabled = true;
        btn.ShadowDecoration.Color = AppTheme.Success;
        btn.ShadowDecoration.Depth = 6;
        btn.ShadowDecoration.BorderRadius = AppTheme.ButtonBorderRadius;
    }

    /// <summary>
    /// Styles a Guna2TextBox for dark theme input.
    /// </summary>
    public static void StyleTextBox(Guna2TextBox txt)
    {
        txt.FillColor = AppTheme.SurfaceLight;
        txt.ForeColor = AppTheme.TextPrimary;
        txt.Font = AppTheme.FontBody;
        txt.BorderRadius = AppTheme.InputBorderRadius;
        txt.BorderColor = AppTheme.Border;
        txt.BorderThickness = 1;
        txt.FocusedState.BorderColor = AppTheme.Primary;
        txt.FocusedState.FillColor = AppTheme.SurfaceElevated;
        txt.HoverState.BorderColor = AppTheme.BorderLight;
        txt.PlaceholderForeColor = AppTheme.TextMuted;
        txt.Cursor = Cursors.IBeam;
    }

    /// <summary>
    /// Styles a Guna2ComboBox for dark theme.
    /// </summary>
    public static void StyleComboBox(Guna2ComboBox cmb)
    {
        cmb.FillColor = AppTheme.SurfaceLight;
        cmb.ForeColor = AppTheme.TextPrimary;
        cmb.Font = AppTheme.FontBody;
        cmb.BorderRadius = AppTheme.InputBorderRadius;
        cmb.BorderColor = AppTheme.Border;
        cmb.BorderThickness = 1;
        cmb.FocusedState.BorderColor = AppTheme.Primary;
        cmb.HoverState.BorderColor = AppTheme.BorderLight;
        cmb.ItemsAppearance.ForeColor = AppTheme.TextPrimary;
        cmb.ItemsAppearance.BackColor = AppTheme.SurfaceLight;
        cmb.ItemsAppearance.SelectedBackColor = AppTheme.Primary;
        cmb.DropDownStyle = ComboBoxStyle.DropDownList;
    }

    /// <summary>
    /// Styles a Guna2Panel as a glassmorphism card.
    /// </summary>
    public static void StyleGlassCard(Guna2Panel panel)
    {
        panel.FillColor = AppTheme.Surface;
        panel.BorderRadius = AppTheme.CardBorderRadius;
        panel.BorderColor = AppTheme.Border;
        panel.BorderThickness = 1;
        panel.ShadowDecoration.Enabled = true;
        panel.ShadowDecoration.Color = Color.FromArgb(30, 0, 0, 0);
        panel.ShadowDecoration.Depth = 12;
        panel.ShadowDecoration.BorderRadius = AppTheme.CardBorderRadius;
    }

    /// <summary>
    /// Styles a DataGridView for the dark premium theme.
    /// </summary>
    public static void StyleDataGridView(DataGridView dgv)
    {
        // Enable double buffering to fix rendering and overlapping issues
        typeof(DataGridView).InvokeMember("DoubleBuffered", 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.SetProperty, 
            null, dgv, new object[] { true });

        dgv.BackgroundColor = AppTheme.Surface;
        dgv.GridColor = AppTheme.Border;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.EnableHeadersVisualStyles = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.RowHeadersVisible = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.AllowUserToResizeRows = false;
        dgv.ReadOnly = true;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.RowTemplate.Height = AppTheme.GridRowHeight;
        dgv.ScrollBars = ScrollBars.Vertical;
        dgv.Cursor = Cursors.Default;

        // Row hover cursor
        dgv.CellMouseEnter += (s, e) => { if (e.RowIndex >= 0) dgv.Cursor = Cursors.Hand; };
        dgv.CellMouseLeave += (s, e) => dgv.Cursor = Cursors.Default;

        // Column headers
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.SurfaceLight,
            ForeColor = AppTheme.TextSecondary,
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
            SelectionBackColor = AppTheme.SurfaceLight,
            SelectionForeColor = AppTheme.TextSecondary,
            Padding = new Padding(12, 8, 12, 8),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        dgv.ColumnHeadersHeight = 45;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

        // Default cell style
        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontBody,
            SelectionBackColor = Color.FromArgb(40, AppTheme.Primary.R, AppTheme.Primary.G, AppTheme.Primary.B),
            SelectionForeColor = AppTheme.TextPrimary,
            Padding = new Padding(12, 4, 12, 4)
        };

        // Alternating rows
        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = AppTheme.SurfaceElevated,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.FontBody,
            SelectionBackColor = Color.FromArgb(40, AppTheme.Primary.R, AppTheme.Primary.G, AppTheme.Primary.B),
            SelectionForeColor = AppTheme.TextPrimary,
            Padding = new Padding(12, 4, 12, 4)
        };
    }

    /// <summary>
    /// Styles a Guna2DateTimePicker for dark theme.
    /// </summary>
    public static void StyleDatePicker(Guna2DateTimePicker dtp)
    {
        dtp.FillColor = AppTheme.SurfaceLight;
        dtp.ForeColor = AppTheme.TextPrimary;
        dtp.Font = AppTheme.FontBody;
        dtp.BorderRadius = AppTheme.InputBorderRadius;
        dtp.BorderColor = AppTheme.Border;
        dtp.BorderThickness = 1;
    }

    /// <summary>
    /// Creates a translucent loading overlay label that can be added on top of any control.
    /// Call <c>overlay.Visible = true/false</c> to toggle.
    /// </summary>
    public static Label CreateLoadingOverlay(Control parent)
    {
        var lbl = new Label
        {
            Text      = "⏳  Loading...",
            Font      = AppTheme.FontHeading,
            ForeColor = AppTheme.TextMuted,
            BackColor = Color.FromArgb(200, AppTheme.Background.R, AppTheme.Background.G, AppTheme.Background.B),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.Fill,
            Visible   = false
        };
        parent.Controls.Add(lbl);
        lbl.BringToFront();
        return lbl;
    }
}
