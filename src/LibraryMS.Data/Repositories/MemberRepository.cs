using Dapper;
using Microsoft.Data.SqlClient;
using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Database;

namespace LibraryMS.Data.Repositories;

/// <summary>
/// SQL Server repository implementation for Member entity.
/// </summary>
public class MemberRepository : IMemberRepository
{
    private SqlConnection GetConnection() => new(ConnectionString.Value);

    public async Task<IEnumerable<Member>> GetAllAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Member>(@"
            SELECT m.*, 
                (SELECT COUNT(*) FROM Loans l WHERE l.MemberId = m.Id AND l.Status = 'Active') AS ActiveLoans
            FROM Members m 
            ORDER BY m.LastName, m.FirstName");
    }

    public async Task<Member?> GetByIdAsync(int id)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Member>(@"
            SELECT m.*, 
                (SELECT COUNT(*) FROM Loans l WHERE l.MemberId = m.Id AND l.Status = 'Active') AS ActiveLoans
            FROM Members m 
            WHERE m.Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Member>> SearchAsync(string searchTerm)
    {
        using var conn = GetConnection();
        var term = $"%{searchTerm}%";
        return await conn.QueryAsync<Member>(@"
            SELECT m.*, 
                (SELECT COUNT(*) FROM Loans l WHERE l.MemberId = m.Id AND l.Status = 'Active') AS ActiveLoans
            FROM Members m 
            WHERE m.FirstName LIKE @Term OR m.LastName LIKE @Term OR m.Email LIKE @Term OR m.Phone LIKE @Term
            ORDER BY m.LastName, m.FirstName", new { Term = term });
    }

    public async Task<int> AddAsync(Member member)
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Members (FirstName, LastName, Email, Phone, Address, MembershipType, JoinDate, IsActive)
            VALUES (@FirstName, @LastName, @Email, @Phone, @Address, @MembershipType, @JoinDate, @IsActive);
            SELECT SCOPE_IDENTITY();", member);
    }

    public async Task<bool> UpdateAsync(Member member)
    {
        using var conn = GetConnection();
        var rows = await conn.ExecuteAsync(@"
            UPDATE Members SET 
                FirstName = @FirstName, LastName = @LastName, Email = @Email,
                Phone = @Phone, Address = @Address, MembershipType = @MembershipType,
                IsActive = @IsActive, UpdatedAt = GETDATE()
            WHERE Id = @Id", member);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = GetConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Members WHERE Id = @Id", new { Id = id });
        return rows > 0;
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Members");
    }

    public async Task<int> GetNewThisMonthCountAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Members 
            WHERE MONTH(JoinDate) = MONTH(GETDATE()) AND YEAR(JoinDate) = YEAR(GETDATE())");
    }

    public async Task<IEnumerable<Member>> GetActiveMembersAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Member>(@"
            SELECT * FROM Members WHERE IsActive = 1 ORDER BY LastName, FirstName");
    }
}
