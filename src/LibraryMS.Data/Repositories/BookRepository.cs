using Dapper;
using Microsoft.Data.SqlClient;
using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Database;

namespace LibraryMS.Data.Repositories;

/// <summary>
/// SQL Server repository implementation for Book entity.
/// Uses Dapper for high-performance data access.
/// </summary>
public class BookRepository : IBookRepository
{
    private SqlConnection GetConnection() => new(ConnectionString.Value);

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Book>(@"
            SELECT BookId AS Id, Title, Author, ISBN, Category, Quantity, AvailableQuantity, CreatedAt 
            FROM Books 
            ORDER BY Title");
    }

    public async Task<Book?> GetByIdAsync(int id)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Book>(@"
            SELECT BookId AS Id, Title, Author, ISBN, Category, Quantity, AvailableQuantity, CreatedAt 
            FROM Books 
            WHERE BookId = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Book>> SearchAsync(string searchTerm)
    {
        using var conn = GetConnection();
        var term = $"%{searchTerm}%";
        return await conn.QueryAsync<Book>(@"
            SELECT BookId AS Id, Title, Author, ISBN, Category, Quantity, AvailableQuantity, CreatedAt 
            FROM Books 
            WHERE Title LIKE @Term OR Author LIKE @Term OR ISBN LIKE @Term
            ORDER BY Title", new { Term = term });
    }

    public async Task<IEnumerable<Book>> GetByCategoryAsync(string category)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Book>(@"
            SELECT BookId AS Id, Title, Author, ISBN, Category, Quantity, AvailableQuantity, CreatedAt 
            FROM Books 
            WHERE Category = @Category
            ORDER BY Title", new { Category = category });
    }

    public async Task<int> AddAsync(Book book)
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Books (Title, Author, ISBN, Category, Quantity, AvailableQuantity, CreatedAt)
            VALUES (@Title, @Author, @ISBN, @Category, @Quantity, @AvailableQuantity, GETDATE());
            SELECT SCOPE_IDENTITY();", book);
    }

    public async Task<bool> UpdateAsync(Book book)
    {
        using var conn = GetConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Books SET 
                Title = @Title, Author = @Author, ISBN = @ISBN, Category = @Category,
                Quantity = @Quantity, AvailableQuantity = @AvailableQuantity
            WHERE BookId = @Id", book);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = GetConnection();
        
        // Check if there are any active loans for this book
        var activeLoans = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Loans WHERE BookId = @Id AND Status = 'Active'", new { Id = id });
            
        if (activeLoans > 0)
        {
            throw new Exception("Cannot delete book because it has active loans.");
        }
        
        // Delete loan history for this book first to avoid foreign key constraint violation
        await conn.ExecuteAsync("DELETE FROM Loans WHERE BookId = @Id", new { Id = id });
        
        // Now delete the book
        var rows = await conn.ExecuteAsync("DELETE FROM Books WHERE BookId = @Id", new { Id = id });
        return rows > 0;
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Books");
    }

    public async Task<int> GetAddedThisMonthCountAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Books 
            WHERE MONTH(CreatedAt) = MONTH(GETDATE()) AND YEAR(CreatedAt) = YEAR(GETDATE())");
    }
}
