using Guna.UI2.WinForms;
using LibraryMS.UI.Theme;
using LibraryMS.Services;
using LibraryMS.Core.Entities;
using LibraryMS.UI.Controls;

namespace LibraryMS.UI.Forms;

/// <summary>
/// Book management page with CRUD operations, search, and category filtering.
/// Premium dark theme with Guna UI2 components.
/// </summary>
public class BooksControl : UserControl
{
    private readonly BookService _bookService;
    private readonly CategoryService _categoryService;
    private DataGridView _booksGrid = null!;
    private Guna2TextBox _searchBox = null!;
    private Guna2ComboBox _categoryFilter = null!;
    private Guna2Button _addButton = null!;
    private Guna2Button _editButton = null!;
    private Guna2Button _deleteButton = null!;
    private Guna2Button _refreshButton = null!;
    private Label _totalLabel = null!;
    private Label _emptyStateLabel = null!;
    private List<Category> _categories = new();

    public BooksControl()
    {
        _bookService = new BookService();
        _categoryService = new CategoryService();
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
            Text = "📚  Book Management",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(0, 6)
        };

        _totalLabel = new Label
        {
            Text = "0 books",
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
            PlaceholderText = "🔍  Search books by title, author, or ISBN...",
            Size = new Size(350, 40),
            Location = new Point(0, 4),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        ThemeManager.StyleTextBox(_searchBox);
        _searchBox.TextChanged += async (s, e) => await SearchBooksAsync();

        _categoryFilter = new Guna2ComboBox
        {
            Size = new Size(180, 40),
            Location = new Point(360, 4),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        ThemeManager.StyleComboBox(_categoryFilter);
        _categoryFilter.Items.Add("All Categories");
        _categoryFilter.SelectedIndex = 0;
        _categoryFilter.SelectedIndexChanged += async (s, e) => await FilterByCategoryAsync();

        _addButton = new Guna2Button
        {
            Text = "+ Add Book",
            Size = new Size(120, 40),
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
            Text = "🗑️ Delete",
            Size = new Size(100, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StyleDangerButton(_deleteButton);
        _deleteButton.Click += DeleteButton_Click;

        _refreshButton = new Guna2Button
        {
            Text = "↻",
            Size = new Size(40, 40),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        ThemeManager.StyleSecondaryButton(_refreshButton);
        _refreshButton.Click += async (s, e) => await LoadDataAsync();

        toolbarPanel.Controls.AddRange(new Control[] { _searchBox, _categoryFilter, _refreshButton, _deleteButton, _editButton, _addButton });
        toolbarPanel.Resize += (s, e) => RepositionToolbarButtons(toolbarPanel);

        // ─── Data Grid ──────────────────────────────────────────
        _booksGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AppTheme.Surface
        };
        ThemeManager.StyleDataGridView(_booksGrid);
        _booksGrid.CellDoubleClick += (s, e) => EditButton_Click(s, e);

        // ─── Empty State Label ──────────────────────────────────
        _emptyStateLabel = new Label
        {
            Text = "No books found.",
            Font = AppTheme.FontSubtitle,
            ForeColor = AppTheme.TextSecondary,
            AutoSize = true,
            Visible = false
        };

        // ─── Grid Container ─────────────────────────────────────
        var gridContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(1)
        };
        gridContainer.Controls.Add(_emptyStateLabel);
        gridContainer.Controls.Add(_booksGrid);
        
        gridContainer.Resize += (s, e) => {
            _emptyStateLabel.Location = new Point(
                (gridContainer.Width - _emptyStateLabel.Width) / 2,
                (gridContainer.Height - _emptyStateLabel.Height) / 2
            );
        };

        // ─── Build Layout ───────────────────────────────────────
        Controls.Add(gridContainer);
        Controls.Add(toolbarPanel);
        Controls.Add(headerPanel);
        ResumeLayout();
    }

    private void RepositionToolbarButtons(Panel toolbar)
    {
        var right = toolbar.Width;
        _refreshButton.Location = new Point(right - 50, 4);
        _deleteButton.Location = new Point(right - 160, 4);
        _editButton.Location = new Point(right - 260, 4);
        _addButton.Location = new Point(right - 390, 4);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var books = await _bookService.GetAllBooksAsync();
            PopulateGrid(books);

            // Load categories for filter
            _categories = (await _categoryService.GetAllCategoriesAsync()).ToList();
            
            // Only update combo box if we haven't loaded categories yet
            if (_categoryFilter.Items.Count <= 1 && _categories.Any())
            {
                _categoryFilter.Items.Clear();
                _categoryFilter.Items.Add("All Categories");
                foreach (var cat in _categories)
                    _categoryFilter.Items.Add(cat.Name);
                _categoryFilter.SelectedIndex = 0;
            }
        }
        catch
        {
            LoadDemoData();
        }
    }

    private void PopulateGrid(IEnumerable<Book> books)
    {
        var bookList = books.ToList();
        _totalLabel.Text = $"{bookList.Count} books";
        
        _emptyStateLabel.Visible = bookList.Count == 0;
        _booksGrid.Visible = bookList.Count > 0;

        _booksGrid.DataSource = null;
        _booksGrid.Columns.Clear();

        var dt = new System.Data.DataTable();
        dt.Columns.AddRange(new[]
        {
            new System.Data.DataColumn("ID", typeof(int)),
            new System.Data.DataColumn("Title", typeof(string)),
            new System.Data.DataColumn("Author", typeof(string)),
            new System.Data.DataColumn("Category", typeof(string)),
            new System.Data.DataColumn("ISBN", typeof(string)),
            new System.Data.DataColumn("Quantity", typeof(int)),
            new System.Data.DataColumn("Available", typeof(int)),
            new System.Data.DataColumn("Status", typeof(string))
        });

        foreach (var book in bookList)
        {
            var status = book.AvailableQuantity > 0 ? "✅ Available" : "❌ Checked Out";
            dt.Rows.Add(book.Id, book.Title, book.Author, book.Category ?? "N/A",
                book.ISBN ?? "—", book.Quantity, book.AvailableQuantity, status);
        }

        _booksGrid.DataSource = dt;

        // Set column widths
        if (_booksGrid.Columns.Count > 0)
        {
            _booksGrid.Columns["ID"].Width = 50;
            _booksGrid.Columns["Title"].FillWeight = 30;
            _booksGrid.Columns["Author"].FillWeight = 20;
            _booksGrid.Columns["Category"].FillWeight = 15;
            _booksGrid.Columns["ISBN"].FillWeight = 15;
            _booksGrid.Columns["Quantity"].Width = 80;
            _booksGrid.Columns["Available"].Width = 80;
            _booksGrid.Columns["Status"].Width = 110;
        }
    }

    private void LoadDemoData()
    {
        var demoBooks = new List<Book>
        {
            new() { Id = 1, Title = "The Great Gatsby", Author = "F. Scott Fitzgerald", Category = "Fiction", ISBN = "978-0743273565", Quantity = 5, AvailableQuantity = 3 },
            new() { Id = 2, Title = "To Kill a Mockingbird", Author = "Harper Lee", Category = "Fiction", ISBN = "978-0446310789", Quantity = 4, AvailableQuantity = 2 },
            new() { Id = 3, Title = "1984", Author = "George Orwell", Category = "Fiction", ISBN = "978-0451524935", Quantity = 6, AvailableQuantity = 4 }
        };
        PopulateGrid(demoBooks);
    }

    private async Task SearchBooksAsync()
    {
        var term = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(term))
        {
            if (_categoryFilter.SelectedIndex > 0)
                await FilterByCategoryAsync();
            else
                await LoadDataAsync();
            return;
        }

        try
        {
            var books = await _bookService.SearchBooksAsync(term);
            
            // If category is selected, filter the search results too
            if (_categoryFilter.SelectedIndex > 0)
            {
                var selectedCategory = _categories[_categoryFilter.SelectedIndex - 1];
                books = books.Where(b => b.Category == selectedCategory.Name);
            }
            
            PopulateGrid(books);
        }
        catch { }
    }

    private async Task FilterByCategoryAsync()
    {
        // If there's an active search, apply search with filter
        if (!string.IsNullOrEmpty(_searchBox.Text.Trim()))
        {
            await SearchBooksAsync();
            return;
        }

        if (_categoryFilter.SelectedIndex <= 0)
        {
            await LoadDataAsync();
            return;
        }

        try
        {
            var selectedCategory = _categories[_categoryFilter.SelectedIndex - 1];
            var books = await _bookService.GetBooksByCategoryAsync(selectedCategory.Name);
            PopulateGrid(books);
        }
        catch { }
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new AddBookForm(_categoryService);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ToastNotification.Show(this.FindForm()!, "Book added successfully!", ToastType.Success);
            await LoadDataAsync();
        }
    }

    private async void EditButton_Click(object? sender, EventArgs e)
    {
        if (_booksGrid.SelectedRows.Count == 0)
        {
            ToastNotification.Show(this.FindForm()!, "Please select a book to edit.", ToastType.Info);
            return;
        }

        var bookId = (int)_booksGrid.SelectedRows[0].Cells["ID"].Value;
        using var dialog = new AddBookForm(_categoryService, bookId);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            ToastNotification.Show(this.FindForm()!, "Book updated successfully!", ToastType.Success);
            await LoadDataAsync();
        }
    }

    private async void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (_booksGrid.SelectedRows.Count == 0)
        {
            ToastNotification.Show(this.FindForm()!, "Please select a book to delete.", ToastType.Info);
            return;
        }

        var bookTitle = _booksGrid.SelectedRows[0].Cells["Title"].Value.ToString();
        var confirmDialog = new Guna2MessageDialog
        {
            Caption = "Confirm Delete",
            Text = $"Are you sure you want to delete '{bookTitle}'?\nThis action cannot be undone.",
            Buttons = MessageDialogButtons.YesNo,
            Icon = MessageDialogIcon.Warning,
            Style = MessageDialogStyle.Dark,
            Parent = this.FindForm()
        };

        if (confirmDialog.Show() == DialogResult.Yes)
        {
            var bookId = (int)_booksGrid.SelectedRows[0].Cells["ID"].Value;
            var (success, message) = await _bookService.DeleteBookAsync(bookId);
            
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
