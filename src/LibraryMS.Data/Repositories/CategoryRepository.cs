using Dapper;
using Microsoft.Data.SqlClient;
using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Database;

namespace LibraryMS.Data.Repositories;

/// <summary>
/// SQL Server repository implementation for Category entity.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private SqlConnection GetConnection() => new(ConnectionString.Value);

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Category>(@"
            SELECT c.*, 
                (SELECT COUNT(*) FROM Books b WHERE b.Category = c.Name) AS BookCount
            FROM Categories c 
            ORDER BY c.Name");
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Category>(@"
            SELECT c.*, 
                (SELECT COUNT(*) FROM Books b WHERE b.Category = c.Name) AS BookCount
            FROM Categories c 
            WHERE c.Id = @Id", new { Id = id });
    }

    public async Task<int> AddAsync(Category category)
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Categories (Name, Description)
            VALUES (@Name, @Description);
            SELECT SCOPE_IDENTITY();", category);
    }

    public async Task<bool> UpdateAsync(Category category)
    {
        using var conn = GetConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Categories SET 
                Name = @Name, Description = @Description, UpdatedAt = GETDATE()
            WHERE Id = @Id", category);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = GetConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Categories WHERE Id = @Id", new { Id = id });
        return rows > 0;
    }
}
