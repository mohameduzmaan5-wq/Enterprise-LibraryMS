using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Repositories;

namespace LibraryMS.Services;

/// <summary>
/// Business logic service for Book operations.
/// </summary>
public class BookService
{
    private readonly IBookRepository _repository;

    public BookService()
    {
        _repository = new BookRepository();
    }

    public Task<IEnumerable<Book>> GetAllBooksAsync() => _repository.GetAllAsync();
    public Task<Book?> GetBookByIdAsync(int id) => _repository.GetByIdAsync(id);
    public Task<IEnumerable<Book>> SearchBooksAsync(string term) => _repository.SearchAsync(term);
    public Task<IEnumerable<Book>> GetBooksByCategoryAsync(string category) => _repository.GetByCategoryAsync(category);
    public Task<int> GetTotalBooksAsync() => _repository.GetTotalCountAsync();
    public Task<int> GetBooksAddedThisMonthAsync() => _repository.GetAddedThisMonthCountAsync();

    /// <summary>
    /// Adds a new book with validation.
    /// </summary>
    public async Task<(bool Success, string Message, int Id)> AddBookAsync(Book book)
    {
        if (string.IsNullOrWhiteSpace(book.Title))
            return (false, "Book title is required.", 0);
        if (string.IsNullOrWhiteSpace(book.Author))
            return (false, "Author name is required.", 0);
        if (string.IsNullOrWhiteSpace(book.Category))
            return (false, "Please select a category.", 0);
        if (book.Quantity < 1)
            return (false, "Quantity must be at least 1.", 0);

        book.AvailableQuantity = book.Quantity;
        var id = await _repository.AddAsync(book);
        return (true, "Book added successfully!", id);
    }

    /// <summary>
    /// Updates an existing book with validation.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateBookAsync(Book book)
    {
        if (string.IsNullOrWhiteSpace(book.Title))
            return (false, "Book title is required.");
        if (string.IsNullOrWhiteSpace(book.Author))
            return (false, "Author name is required.");

        var result = await _repository.UpdateAsync(book);
        return result ? (true, "Book updated successfully!") : (false, "Failed to update book.");
    }

    /// <summary>
    /// Deletes a book by ID.
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteBookAsync(int id)
    {
        try
        {
            var result = await _repository.DeleteAsync(id);
            return result ? (true, "Book deleted successfully!") : (false, "Book not found.");
        }
        catch (Exception ex)
        {
            return (false, $"Cannot delete: {ex.Message}");
        }
    }
}
