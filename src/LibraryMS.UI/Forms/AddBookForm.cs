using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using LibraryMS.Services;
using LibraryMS.Core.Entities;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Enterprise-grade Add Book form with Guna UI2 components and premium dark theme.
/// </summary>
public class AddBookForm : Form
{
    private readonly CategoryService _categoryService;
    private readonly BookService _bookService;
    private readonly int? _bookId;
    
    // UI Controls
    private Guna2TextBox _txtTitle = null!;
    private Guna2TextBox _txtAuthor = null!;
    private Guna2TextBox _txtISBN = null!;
    private Guna2ComboBox _cmbCategory = null!;
    private Guna2TextBox _txtQuantity = null!;
    private Guna2Button _btnSave = null!;
    private Guna2Button _btnCancel = null!;
    
    private readonly Dictionary<int, string> _categoryMap = new();

    public AddBookForm(CategoryService categoryService, int? bookId = null)
    {
        _categoryService = categoryService;
        _bookService = new BookService();
        _bookId = bookId;
        InitializeComponent();
        _ = LoadFormDataAsync();
    }

    private void InitializeComponent()
    {
        // Setup Form
        Text = _bookId.HasValue ? "Edit Book" : "Add New Book";
        Size = new Size(500, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextPrimary;
        Font = AppTheme.FontBody;

        var headerLabel = new Label
        {
            Text = _bookId.HasValue ? "✏️ Edit Book" : "📚 Add New Book",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(32, 24)
        };

        var mainPanel = new TableLayoutPanel
        {
            Location = new Point(32, 70),
            Size = new Size(420, 380),
            RowCount = 10,
            ColumnCount = 2,
            BackColor = AppTheme.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        // Title
        var lblTitle = CreateLabel("Title *");
        _txtTitle = CreateTextBox("Enter book title");
        mainPanel.Controls.Add(lblTitle, 0, 0);
        mainPanel.SetColumnSpan(_txtTitle, 2);
        mainPanel.Controls.Add(_txtTitle, 0, 1);

        // Author
        var lblAuthor = CreateLabel("Author *");
        _txtAuthor = CreateTextBox("Enter author name");
        mainPanel.Controls.Add(lblAuthor, 0, 2);
        mainPanel.SetColumnSpan(_txtAuthor, 2);
        mainPanel.Controls.Add(_txtAuthor, 0, 3);

        // ISBN & Category
        var lblISBN = CreateLabel("ISBN *");
        _txtISBN = CreateTextBox("Enter ISBN (e.g., 978-...)");
        _txtISBN.Size = new Size(200, 40);
        var lblCat = CreateLabel("Category *");
        _cmbCategory = new Guna2ComboBox { Size = new Size(200, 40) };
        ThemeManager.StyleComboBox(_cmbCategory);

        mainPanel.Controls.Add(lblISBN, 0, 4);
        mainPanel.Controls.Add(_txtISBN, 0, 5);
        mainPanel.Controls.Add(lblCat, 1, 4);
        mainPanel.Controls.Add(_cmbCategory, 1, 5);

        // Quantity
        var lblQuantity = CreateLabel("Quantity *");
        _txtQuantity = CreateTextBox("Enter quantity");
        _txtQuantity.Text = "1";
        _txtQuantity.Size = new Size(200, 40);
        mainPanel.Controls.Add(lblQuantity, 0, 6);
        mainPanel.Controls.Add(_txtQuantity, 0, 7);
        mainPanel.Controls.Add(new Label(), 1, 6);
        mainPanel.Controls.Add(new Label(), 1, 7);

        // Buttons
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 70,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(24, 15, 24, 15),
            BackColor = AppTheme.SurfaceLight
        };

        _btnSave = new Guna2Button { Text = "💾  Save", Size = new Size(130, 40) };
        ThemeManager.StylePrimaryButton(_btnSave);
        _btnSave.Click += BtnSave_Click;

        _btnCancel = new Guna2Button { Text = "Cancel", Size = new Size(100, 40) };
        ThemeManager.StyleSecondaryButton(_btnCancel);
        _btnCancel.Margin = new Padding(10, 0, 10, 0);
        _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        buttonPanel.Controls.AddRange(new Control[] { _btnSave, _btnCancel });

        Controls.Add(headerLabel);
        Controls.Add(mainPanel);
        Controls.Add(buttonPanel);
    }

    private Label CreateLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI Semibold", 10f),
        ForeColor = AppTheme.TextSecondary,
        AutoSize = true,
        Padding = new Padding(0, 8, 0, 2)
    };

    private Guna2TextBox CreateTextBox(string placeholder)
    {
        var txt = new Guna2TextBox
        {
            PlaceholderText = placeholder,
            Size = new Size(420, 40) // Full width span default
        };
        ThemeManager.StyleTextBox(txt);
        return txt;
    }

    private async Task LoadFormDataAsync()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            _cmbCategory.Items.Clear();
            foreach (var cat in categories)
            {
                _categoryMap[_cmbCategory.Items.Count] = cat.Id.ToString();
                _cmbCategory.Items.Add(cat.Name);
            }
            if (_cmbCategory.Items.Count > 0) _cmbCategory.SelectedIndex = 0;

            if (_bookId.HasValue)
            {
                var book = await _bookService.GetBookByIdAsync(_bookId.Value);
                if (book != null)
                {
                    _txtTitle.Text = book.Title;
                    _txtAuthor.Text = book.Author;
                    _txtISBN.Text = book.ISBN ?? "";
                    _txtQuantity.Text = book.Quantity.ToString();

                    // Select correct category
                    for (int i = 0; i < _cmbCategory.Items.Count; i++)
                    {
                        if (_cmbCategory.Items[i].ToString() == book.Category)
                        {
                            _cmbCategory.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast("Error loading data: " + ex.Message, "Error", MessageDialogIcon.Error);
        }
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(_txtTitle.Text))
        {
            ShowToast("Title is required.", "Validation Error", MessageDialogIcon.Warning);
            _txtTitle.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtAuthor.Text))
        {
            ShowToast("Author is required.", "Validation Error", MessageDialogIcon.Warning);
            _txtAuthor.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(_txtISBN.Text) || _txtISBN.Text.Length < 10)
        {
            ShowToast("Please enter a valid ISBN.", "Validation Error", MessageDialogIcon.Warning);
            _txtISBN.Focus();
            return false;
        }

        if (!int.TryParse(_txtQuantity.Text, out var qty) || qty <= 0)
        {
            ShowToast("Quantity must be a valid positive number.", "Validation Error", MessageDialogIcon.Warning);
            _txtQuantity.Focus();
            return false;
        }

        if (_cmbCategory.SelectedIndex < 0)
        {
            ShowToast("Please select a category.", "Validation Error", MessageDialogIcon.Warning);
            _cmbCategory.Focus();
            return false;
        }

        return true;
    }

    private void ShowToast(string text, string caption, MessageDialogIcon icon)
    {
        var dialog = new Guna2MessageDialog
        {
            Caption = caption,
            Text = text,
            Icon = icon,
            Buttons = MessageDialogButtons.OK,
            Style = MessageDialogStyle.Dark,
            Parent = this
        };
        dialog.Show();
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateInputs()) return;

        _btnSave.Enabled = false;

        try
        {
            var newTotalCopies = int.Parse(_txtQuantity.Text);

            if (_bookId.HasValue)
            {
                var book = await _bookService.GetBookByIdAsync(_bookId.Value);
                if (book == null)
                {
                    ShowToast("Book not found.", "Error", MessageDialogIcon.Error);
                    return;
                }

                var checkedOut = book.Quantity - book.AvailableQuantity;
                if (newTotalCopies < checkedOut)
                {
                    ShowToast($"Cannot reduce quantity below {checkedOut} (currently checked out).", "Validation Error", MessageDialogIcon.Warning);
                    return;
                }

                book.Title = _txtTitle.Text.Trim();
                book.Author = _txtAuthor.Text.Trim();
                book.ISBN = _txtISBN.Text.Trim();
                book.Category = _cmbCategory.Text;
                book.Quantity = newTotalCopies;
                book.AvailableQuantity = newTotalCopies - checkedOut;

                var (success, message) = await _bookService.UpdateBookAsync(book);
                if (success) { DialogResult = DialogResult.OK; Close(); }
                else ShowToast(message, "Error", MessageDialogIcon.Error);
            }
            else
            {
                var book = new Book
                {
                    Title = _txtTitle.Text.Trim(),
                    Author = _txtAuthor.Text.Trim(),
                    ISBN = _txtISBN.Text.Trim(),
                    Category = _cmbCategory.Text,
                    Quantity = newTotalCopies,
                    AvailableQuantity = newTotalCopies
                };

                var (success, message, id) = await _bookService.AddBookAsync(book);
                if (success) { DialogResult = DialogResult.OK; Close(); }
                else ShowToast(message, "Error", MessageDialogIcon.Error);
            }
        }
        catch (Exception ex)
        {
            ShowToast("An unexpected error occurred: " + ex.Message, "Error", MessageDialogIcon.Error);
        }
        finally
        {
            _btnSave.Enabled = true;
        }
    }
}
