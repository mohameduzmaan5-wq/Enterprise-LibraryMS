using Dapper;
using Microsoft.Data.SqlClient;
using LibraryMS.Core.DTOs;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Database;

namespace LibraryMS.Data.Repositories;

/// <summary>
/// SQL Server implementation of all report queries using Dapper.
/// Each query is optimised with date-range parameters and JOINs.
/// </summary>
public class ReportRepository : IReportRepository
{
    private SqlConnection GetConnection() => new(ConnectionString.Value);

    // ── Borrow History ───────────────────────────────────────────
    public async Task<IEnumerable<BorrowHistoryRow>> GetBorrowHistoryAsync(DateTime from, DateTime to)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<BorrowHistoryRow>(@"
            SELECT l.Id AS LoanId,
                   b.Title AS BookTitle, b.Author,
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName,
                   l.BorrowDate, l.DueDate, l.ReturnDate,
                   l.Status, l.FineAmount
            FROM Loans l
            JOIN Books   b ON b.BookId = l.BookId
            JOIN Members m ON m.Id     = l.MemberId
            WHERE l.BorrowDate >= @From AND l.BorrowDate <= @To
            ORDER BY l.BorrowDate DESC",
            new { From = from, To = to.AddDays(1) });
    }

    // ── Overdue Report ───────────────────────────────────────────
    public async Task<IEnumerable<OverdueReportRow>> GetOverdueReportAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<OverdueReportRow>(@"
            SELECT l.Id AS LoanId,
                   b.Title AS BookTitle,
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName,
                   ISNULL(m.Email, '—') AS MemberEmail,
                   l.BorrowDate, l.DueDate,
                   DATEDIFF(DAY, l.DueDate, GETDATE()) AS DaysOverdue,
                   CASE WHEN DATEDIFF(DAY, l.DueDate, GETDATE()) > 7
                        THEN (DATEDIFF(DAY, l.DueDate, GETDATE()) - 7) * 1.00
                        ELSE 0 END AS AccruedFine
            FROM Loans l
            JOIN Books   b ON b.BookId = l.BookId
            JOIN Members m ON m.Id     = l.MemberId
            WHERE l.Status = 'Active' AND l.DueDate < GETDATE()
            ORDER BY l.DueDate ASC");
    }

    // ── Top Borrowed Books ───────────────────────────────────────
    public async Task<IEnumerable<TopBookRow>> GetTopBorrowedBooksAsync(int topN = 10)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<TopBookRow>(@"
            SELECT TOP (@TopN)
                   b.Title, b.Author, b.Category,
                   b.Quantity, b.AvailableQuantity AS Available,
                   COUNT(l.Id) AS BorrowCount
            FROM Books b
            LEFT JOIN Loans l ON l.BookId = b.BookId
            GROUP BY b.BookId, b.Title, b.Author, b.Category, b.Quantity, b.AvailableQuantity
            ORDER BY BorrowCount DESC",
            new { TopN = topN });
    }

    // ── Member Activity ──────────────────────────────────────────
    public async Task<IEnumerable<MemberActivityRow>> GetMemberActivityAsync(DateTime from, DateTime to)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<MemberActivityRow>(@"
            SELECT m.Id AS MemberId,
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName,
                   m.MembershipType,
                   COUNT(l.Id)                                                 AS TotalLoans,
                   SUM(CASE WHEN l.Status = 'Active'   THEN 1 ELSE 0 END)     AS ActiveLoans,
                   SUM(CASE WHEN l.Status = 'Returned' THEN 1 ELSE 0 END)     AS ReturnedLoans,
                   ISNULL(SUM(l.FineAmount), 0)                                AS TotalFines,
                   ISNULL(MAX(l.BorrowDate), m.JoinDate)                       AS LastActivity
            FROM Members m
            LEFT JOIN Loans l ON l.MemberId = m.Id
                              AND l.BorrowDate >= @From AND l.BorrowDate <= @To
            GROUP BY m.Id, m.FirstName, m.LastName, m.MembershipType, m.JoinDate
            ORDER BY TotalLoans DESC",
            new { From = from, To = to.AddDays(1) });
    }

    // ── Fine Report ──────────────────────────────────────────────
    public async Task<IEnumerable<FineReportRow>> GetFineReportAsync(DateTime from, DateTime to)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<FineReportRow>(@"
            SELECT l.Id AS LoanId,
                   b.Title AS BookTitle,
                   CONCAT(m.FirstName, ' ', m.LastName) AS MemberName,
                   l.DueDate,
                   ISNULL(l.ReturnDate, GETDATE()) AS ReturnDate,
                   DATEDIFF(DAY, l.DueDate, ISNULL(l.ReturnDate, GETDATE())) AS OverdueDays,
                   l.FineAmount,
                   l.Status
            FROM Loans l
            JOIN Books   b ON b.BookId = l.BookId
            JOIN Members m ON m.Id     = l.MemberId
            WHERE l.FineAmount > 0
              AND l.BorrowDate >= @From AND l.BorrowDate <= @To
            ORDER BY l.FineAmount DESC",
            new { From = from, To = to.AddDays(1) });
    }

    // ── Inventory Report ─────────────────────────────────────────
    public async Task<IEnumerable<InventoryRow>> GetInventoryReportAsync()
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<InventoryRow>(@"
            SELECT b.BookId,
                   b.Title, b.Author, b.Category,
                   ISNULL(b.ISBN, '—') AS ISBN,
                   b.Quantity,
                   b.AvailableQuantity AS Available,
                   b.Quantity - b.AvailableQuantity AS CheckedOut,
                   CASE WHEN b.AvailableQuantity > 0 THEN 'Available' ELSE 'Fully Checked Out' END AS Status
            FROM Books b
            ORDER BY b.Category, b.Title");
    }

    // ── Monthly Trends ───────────────────────────────────────────
    public async Task<IEnumerable<MonthlyTrendRow>> GetMonthlyTrendsAsync(int months = 12)
    {
        using var conn = GetConnection();
        return await conn.QueryAsync<MonthlyTrendRow>(@"
            SELECT YEAR(l.BorrowDate)  AS Year,
                   MONTH(l.BorrowDate) AS Month,
                   COUNT(l.Id)         AS Loans,
                   SUM(CASE WHEN l.Status = 'Returned' THEN 1 ELSE 0 END) AS Returns,
                   ISNULL(SUM(l.FineAmount), 0) AS Fines
            FROM Loans l
            WHERE l.BorrowDate >= DATEADD(MONTH, -@Months, GETDATE())
            GROUP BY YEAR(l.BorrowDate), MONTH(l.BorrowDate)
            ORDER BY Year, Month",
            new { Months = months });
    }
}
