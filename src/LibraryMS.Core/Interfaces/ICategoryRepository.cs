using LibraryMS.Core.Entities;

namespace LibraryMS.Core.Interfaces;

/// <summary>
/// Repository interface for Category entity operations.
/// </summary>
public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<int> AddAsync(Category category);
    Task<bool> UpdateAsync(Category category);
    Task<bool> DeleteAsync(int id);
}
