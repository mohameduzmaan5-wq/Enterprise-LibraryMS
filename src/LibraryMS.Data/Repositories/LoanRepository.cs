using Dapper;
using Microsoft.Data.SqlClient;
using LibraryMS.Core.DTOs;
using LibraryMS.Core.Entities;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Database;

namespace LibraryMS.Data.Repositories;

/// <summary>
/// SQL Server repository implementation for Loan entity.
/// </summary>
public class LoanRepository : ILoanRepository
{
    private SqlConnection GetConnection() => new(ConnectionString.Value);

    public async Task<IEnumerable<Loan>> GetAllAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Loan>(@"
            SELECT l.*, b.Title AS BookTitle, 
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName
            FROM Loans l
            JOIN Books b ON l.BookId = b.BookId
            JOIN Members m ON l.MemberId = m.Id
            ORDER BY l.BorrowDate DESC");
    }

    public async Task<IEnumerable<Loan>> GetActiveLoansAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Loan>(@"
            SELECT l.*, b.Title AS BookTitle, 
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName
            FROM Loans l
            JOIN Books b ON l.BookId = b.BookId
            JOIN Members m ON l.MemberId = m.Id
            WHERE l.Status = 'Active'
            ORDER BY l.DueDate");
    }

    public async Task<IEnumerable<Loan>> GetOverdueLoansAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Loan>(@"
            SELECT l.*, b.Title AS BookTitle, 
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName
            FROM Loans l
            JOIN Books b ON l.BookId = b.BookId
            JOIN Members m ON l.MemberId = m.Id
            WHERE l.Status = 'Active' AND l.DueDate < GETDATE()
            ORDER BY l.DueDate");
    }

    public async Task<IEnumerable<LoanDetail>> GetLoanDetailsAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<LoanDetail>(@"
            SELECT l.Id AS LoanId, b.Title AS BookTitle, b.Author AS BookAuthor,
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName, m.Email AS MemberEmail,
                   l.BorrowDate, l.DueDate, l.ReturnDate, l.Status, l.FineAmount
            FROM Loans l
            JOIN Books b ON l.BookId = b.BookId
            JOIN Members m ON l.MemberId = m.Id
            ORDER BY l.BorrowDate DESC");
    }

    public async Task<Loan?> GetByIdAsync(int id)
    {
        using var conn = GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Loan>(@"
            SELECT l.*, b.Title AS BookTitle, 
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName
            FROM Loans l
            JOIN Books b ON l.BookId = b.BookId
            JOIN Members m ON l.MemberId = m.Id
            WHERE l.Id = @Id", new { Id = id });
    }

    public async Task<int> AddAsync(Loan loan)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            // Insert the loan
            var loanId = await conn.ExecuteScalarAsync<int>(@"
                INSERT INTO Loans (BookId, MemberId, BorrowDate, DueDate, Status)
                VALUES (@BookId, @MemberId, @BorrowDate, @DueDate, 'Active');
                SELECT SCOPE_IDENTITY();", loan, transaction);

            // Decrement available copies
            await conn.ExecuteAsync(@"
                UPDATE Books SET AvailableQuantity = AvailableQuantity - 1 
                WHERE BookId = @BookId AND AvailableQuantity > 0", 
                new { loan.BookId }, transaction);

            transaction.Commit();
            return loanId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> ReturnBookAsync(int loanId, decimal fineAmount = 0)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            // Get the loan to find the book
            var loan = await conn.QueryFirstOrDefaultAsync<Loan>(
                "SELECT * FROM Loans WHERE Id = @Id", new { Id = loanId }, transaction);

            if (loan == null) return false;

            // Update the loan
            await conn.ExecuteAsync(@"
                UPDATE Loans SET 
                    ReturnDate = GETDATE(), Status = 'Returned', 
                    FineAmount = @FineAmount, UpdatedAt = GETDATE()
                WHERE Id = @LoanId", 
                new { LoanId = loanId, FineAmount = fineAmount }, transaction);

            // Increment available copies
            await conn.ExecuteAsync(@"
                UPDATE Books SET AvailableQuantity = AvailableQuantity + 1 
                WHERE BookId = @BookId", 
                new { loan.BookId }, transaction);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<int> GetActiveCountAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Loans WHERE Status = 'Active'");
    }

    public async Task<int> GetOverdueCountAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Loans WHERE Status = 'Active' AND DueDate < GETDATE()");
    }

    public async Task<int> GetLoansThisMonthCountAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Loans 
            WHERE MONTH(BorrowDate) = MONTH(GETDATE()) AND YEAR(BorrowDate) = YEAR(GETDATE())");
    }

    public async Task<int> GetReturnsThisMonthCountAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Loans 
            WHERE Status = 'Returned' AND MONTH(ReturnDate) = MONTH(GETDATE()) AND YEAR(ReturnDate) = YEAR(GETDATE())");
    }

    public async Task<decimal> GetTotalFinesAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(FineAmount), 0) FROM Loans");
    }

    public async Task<IEnumerable<Loan>> GetMemberLoansAsync(int memberId)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Loan>(@"
            SELECT l.*, b.Title AS BookTitle
            FROM Loans l
            JOIN Books b ON l.BookId = b.BookId
            WHERE l.MemberId = @MemberId
            ORDER BY l.BorrowDate DESC", new { MemberId = memberId });
    }

    public async Task<IEnumerable<Loan>> SearchAsync(string term)
    {
        using var conn = GetConnection();
        var like = $"%{term}%";
        return await conn.QueryAsync<Loan>(@"
            SELECT l.*, b.Title AS BookTitle,
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName
            FROM Loans l
            JOIN Books b ON l.BookId = b.BookId
            JOIN Members m ON l.MemberId = m.Id
            WHERE b.Title LIKE @Like
               OR CONCAT(m.FirstName, ' ', m.LastName) LIKE @Like
               OR b.ISBN LIKE @Like
            ORDER BY l.BorrowDate DESC", new { Like = like });
    }

    public async Task<bool> CheckActiveLoanExistsAsync(int bookId, int memberId)
    {
        using var conn = GetConnection();
        var count = await conn.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Loans
            WHERE BookId = @BookId AND MemberId = @MemberId AND Status = 'Active'",
            new { BookId = bookId, MemberId = memberId });
        return count > 0;
    }

    public async Task<IEnumerable<Loan>> GetReturnHistoryAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<Loan>(@"
            SELECT l.*, b.Title AS BookTitle,
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName
            FROM Loans l
            JOIN Books b ON l.BookId = b.BookId
            JOIN Members m ON l.MemberId = m.Id
            WHERE l.Status = 'Returned'
            ORDER BY l.ReturnDate DESC");
    }

    public async Task<decimal> GetOutstandingFinesAsync()
    {
        using var conn = GetConnection();
        return await conn.ExecuteScalarAsync<decimal>(@"
            SELECT ISNULL(SUM(FineAmount), 0) FROM Loans
            WHERE Status = 'Active' AND DueDate < GETDATE()");
    }
}
