using LibraryMS.Core.Entities;

namespace LibraryMS.Core.Interfaces;

/// <summary>
/// Repository interface for Book entity operations.
/// </summary>
public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();
    Task<Book?> GetByIdAsync(int id);
    Task<IEnumerable<Book>> SearchAsync(string searchTerm);
    Task<IEnumerable<Book>> GetByCategoryAsync(string category);
    Task<int> AddAsync(Book book);
    Task<bool> UpdateAsync(Book book);
    Task<bool> DeleteAsync(int id);
    Task<int> GetTotalCountAsync();
    Task<int> GetAddedThisMonthCountAsync();
}
