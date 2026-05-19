using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Repositories;

namespace LibraryMS.Services;

/// <summary>
/// Business logic service for Category operations.
/// </summary>
public class CategoryService
{
    private readonly ICategoryRepository _repository;

    public CategoryService()
    {
        _repository = new CategoryRepository();
    }

    public Task<IEnumerable<Category>> GetAllCategoriesAsync() => _repository.GetAllAsync();
    public Task<Category?> GetCategoryByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<(bool Success, string Message, int Id)> AddCategoryAsync(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            return (false, "Category name is required.", 0);

        var id = await _repository.AddAsync(category);
        return (true, "Category added successfully!", id);
    }

    public async Task<(bool Success, string Message)> UpdateCategoryAsync(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
            return (false, "Category name is required.");

        var result = await _repository.UpdateAsync(category);
        return result ? (true, "Category updated!") : (false, "Failed to update category.");
    }

    public async Task<(bool Success, string Message)> DeleteCategoryAsync(int id)
    {
        try
        {
            var result = await _repository.DeleteAsync(id);
            return result ? (true, "Category deleted!") : (false, "Category not found.");
        }
        catch (Exception ex)
        {
            return (false, $"Cannot delete: Category has associated books. {ex.Message}");
        }
    }
}
