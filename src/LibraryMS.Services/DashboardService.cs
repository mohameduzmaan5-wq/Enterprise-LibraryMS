using LibraryMS.Core.DTOs;
using LibraryMS.Core.Interfaces;
using LibraryMS.Data.Repositories;

namespace LibraryMS.Services;

/// <summary>
/// Aggregation service for dashboard statistics.
/// </summary>
public class DashboardService
{
    private readonly IBookRepository _bookRepo;
    private readonly IMemberRepository _memberRepo;
    private readonly ILoanRepository _loanRepo;

    public DashboardService()
    {
        _bookRepo = new BookRepository();
        _memberRepo = new MemberRepository();
        _loanRepo = new LoanRepository();
    }

    /// <summary>
    /// Retrieves all dashboard statistics in a single call.
    /// </summary>
    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var stats = new DashboardStats();

        // Run all queries in parallel for performance
        var tasks = new List<Task>
        {
            Task.Run(async () => stats.TotalBooks = await _bookRepo.GetTotalCountAsync()),
            Task.Run(async () => stats.TotalMembers = await _memberRepo.GetTotalCountAsync()),
            Task.Run(async () => stats.ActiveLoans = await _loanRepo.GetActiveCountAsync()),
            Task.Run(async () => stats.OverdueLoans = await _loanRepo.GetOverdueCountAsync()),
            Task.Run(async () => stats.BooksAddedThisMonth = await _bookRepo.GetAddedThisMonthCountAsync()),
            Task.Run(async () => stats.NewMembersThisMonth = await _memberRepo.GetNewThisMonthCountAsync()),
            Task.Run(async () => stats.LoansThisMonth = await _loanRepo.GetLoansThisMonthCountAsync()),
            Task.Run(async () => stats.ReturnsThisMonth = await _loanRepo.GetReturnsThisMonthCountAsync()),
            Task.Run(async () => stats.TotalFinesCollected = await _loanRepo.GetTotalFinesAsync()),
        };

        await Task.WhenAll(tasks);
        return stats;
    }
}
